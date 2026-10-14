import { Dialog, DialogTitle } from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import { useNavigate } from "react-router";
import WizardStepperFrame from "../../../../components/Common/CreateWizards/WizardStepperFrame";
import type {
	IReport,
	IReportOwner,
	IReportTemplates,
	ReportTemplateKey,
} from "../../../../models/Reports/Report";
import { ApiError } from "../../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import {
	usageDataReportTemplateFor,
	useUsageDataReporter,
} from "../../../../services/UsageData/usageDataReporter";
import { reportFailedNavigation } from "../../../../utils/navigation";
import { queryStatusOf } from "../../MetricsView/widgetStatus";
import {
	reportPathOf,
	reportQueryKeyOf,
	reportsQueryKeyOf,
} from "../reportOwner";
import {
	COULD_NOT_SAVE,
	NO_LONGER_EXISTS,
	type RefusalWords,
	refusalCopyFor,
	useRefusalWords,
} from "./reportRefusalCopy";
import { type ReportTypeStep, reportTypes } from "./reportTypes";
import TypeStep from "./TypeStep";

const TITLE_ID = "report-wizard-title";
const STALE_SAVE =
	"This report was changed by someone else. Reload to see their changes.";
const TYPE_STEP = 0;
/** Type and Then are locked on an edit, so it opens on Now, the first step it can change. */
const FIRST_EDITABLE_STEP = 2;

const firstStepOf = (isEditing: boolean): number =>
	isEditing ? FIRST_EDITABLE_STEP : TYPE_STEP;

const CREATING = { title: "Create Report", action: "Create" };
const EDITING = { title: "Edit report", action: "Save" };

/** Somebody else saved the report after this edit read it, so the edit would overwrite theirs. */
const isStaleSave = (error: unknown): boolean =>
	error instanceof ApiError && error.code === 409;

const isThenBeforeCutoff = (error: unknown): boolean =>
	error instanceof ApiError && error.problemCode === "then-before-cutoff";

/** Somebody else deleted the report after this edit read it. */
const isGone = (error: unknown): boolean =>
	error instanceof ApiError && error.code === 404;

const saveFailureCopyFor = (error: unknown, words: RefusalWords): string => {
	if (isStaleSave(error)) return STALE_SAVE;
	if (isGone(error)) return NO_LONGER_EXISTS;
	return refusalCopyFor(error, words, COULD_NOT_SAVE);
};

const canMoveOn = <TDraft,>(
	step: ReportTypeStep<TDraft> | undefined,
	isFrozen: boolean,
	draft: TDraft,
	templates: IReportTemplates | undefined,
): boolean =>
	templates !== undefined &&
	(step === undefined || isFrozen || step.isComplete(draft, templates));

interface ReportWizardProps {
	owner: IReportOwner;
	onClose: () => void;
	/** The report as it was read, when the wizard changes it instead of creating one. */
	editing?: IReport;
}

/** Creates a report, or edits one: the Type first, then the steps that Type adds. */
const ReportWizard: React.FC<Readonly<ReportWizardProps>> = ({
	owner,
	onClose,
	editing,
}) => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const navigate = useNavigate();
	const reportUsage = useUsageDataReporter();
	const chosen: ReportTemplateKey = "then-and-now";
	const type = reportTypes[chosen];
	const isEditing = editing !== undefined;
	const [activeStep, setActiveStep] = useState(firstStepOf(isEditing));
	const words = isEditing ? EDITING : CREATING;
	const [draft, setDraft] = useState(() =>
		editing === undefined ? type.newDraft() : type.draftOf(editing),
	);
	const [isSubmitting, setIsSubmitting] = useState(false);
	const [refusal, setRefusal] = useState<string | null>(null);

	// Today and the bounds are the server's; an answer kept from an earlier opening may name yesterday.
	const { data, isFetching, isError, refetch } = useQuery({
		queryKey: ["reportTemplates", owner.kind, owner.id],
		queryFn: () => reportService.getTemplates(owner),
		staleTime: 0,
		refetchOnMount: "always",
		refetchOnWindowFocus: false,
	});

	const status = queryStatusOf(isFetching, isError);
	const templates = status === "ready" ? data : undefined;
	const typeStep = activeStep - 1;
	const current = typeStep >= 0 ? type.steps[typeStep] : undefined;
	const isFrozen = isEditing && current?.frozenOnEdit === true;
	const isLastStep = activeStep === type.steps.length;
	const isComplete = canMoveOn(current, isFrozen, draft, templates);

	const refusalWordsFor = useRefusalWords(owner);

	const startSubmitting = () => {
		setIsSubmitting(true);
		setRefusal(null);
	};

	const refuse = (copy: string) => {
		setRefusal(copy);
		setIsSubmitting(false);
	};

	const create = async (bounds: IReportTemplates) => {
		startSubmitting();
		let reportId: number;
		try {
			reportId = await reportService.createReport(
				owner,
				type.toNewReport(draft, bounds),
			);
		} catch (error) {
			refuse(refusalCopyFor(error, refusalWordsFor(bounds)));
			// The oldest kept day has moved on since the wizard opened; ask again so Then's own check agrees.
			if (isThenBeforeCutoff(error)) {
				refetch().catch(() => undefined);
			}
			return;
		}

		reportUsage({
			name: "ReportCreated",
			reportTemplate: usageDataReportTemplateFor(chosen),
		});
		await queryClient.invalidateQueries({
			queryKey: reportsQueryKeyOf(owner),
		});
		onClose();
		reportFailedNavigation(navigate(reportPathOf(owner, reportId)));
	};

	const save = async (report: IReport, bounds: IReportTemplates) => {
		startSubmitting();
		let saved: IReport;
		try {
			saved = await reportService.editReport(owner, report.id, {
				...type.toChange(draft, bounds),
				concurrencyToken: report.concurrencyToken,
			});
		} catch (error) {
			refuse(saveFailureCopyFor(error, refusalWordsFor(bounds)));
			return;
		}

		// The saved report carries the new token, so the next edit is not refused as somebody else's change.
		queryClient.setQueryData(reportQueryKeyOf(owner, report.id), saved);
		// Not awaited: the page shows the saved report, dimmed, while it is read again.
		queryClient
			.invalidateQueries({ queryKey: reportQueryKeyOf(owner, report.id) })
			.catch(() => undefined);
		queryClient
			.invalidateQueries({ queryKey: reportsQueryKeyOf(owner) })
			.catch(() => undefined);
		onClose();
	};

	const submit = (bounds: IReportTemplates) =>
		editing === undefined ? create(bounds) : save(editing, bounds);

	const StepContent = current?.component;

	return (
		<Dialog
			open
			onClose={isSubmitting ? undefined : onClose}
			aria-labelledby={TITLE_ID}
			maxWidth="md"
			fullWidth
		>
			<DialogTitle id={TITLE_ID}>{words.title}</DialogTitle>
			<WizardStepperFrame
				steps={["Type", ...type.steps.map((step) => step.label)]}
				activeStep={activeStep}
				busy={false}
				navigationDisabled={isSubmitting}
				onCancel={onClose}
				onBack={
					activeStep > TYPE_STEP
						? () => setActiveStep(activeStep - 1)
						: undefined
				}
				next={
					isLastStep
						? undefined
						: {
								label: "Next",
								onClick: () => setActiveStep(activeStep + 1),
								disabled: !isComplete,
							}
				}
				primaryAction={
					isLastStep && templates !== undefined
						? {
								label: words.action,
								onClick: () => submit(templates),
								disabled: !isComplete || isSubmitting,
							}
						: undefined
				}
				error={refusal === null ? null : { message: refusal }}
			>
				{StepContent === undefined || templates === undefined ? (
					<TypeStep
						status={status}
						chosen={chosen}
						onRetry={() => refetch()}
						frozen={isEditing}
					/>
				) : (
					<StepContent
						owner={owner}
						templates={templates}
						draft={draft}
						onChange={setDraft}
						disabled={isSubmitting}
						frozen={isFrozen}
					/>
				)}
			</WizardStepperFrame>
		</Dialog>
	);
};

export default ReportWizard;
