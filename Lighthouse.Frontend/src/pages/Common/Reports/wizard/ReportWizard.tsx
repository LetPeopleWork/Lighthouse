import { Dialog, DialogTitle } from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import { useNavigate } from "react-router";
import WizardStepperFrame from "../../../../components/Common/CreateWizards/WizardStepperFrame";
import type {
	IReportOwner,
	IReportTemplates,
	ReportTemplateKey,
} from "../../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../services/TerminologyContext";
import {
	usageDataReportTemplateFor,
	useUsageDataReporter,
} from "../../../../services/UsageData/usageDataReporter";
import { refusalCopyFor } from "./reportRefusalCopy";
import { reportTypeSteps } from "./reportTypeSteps";
import TypeStep, { type ReportTypesStatus } from "./TypeStep";

const TITLE_ID = "report-wizard-title";
const TYPE_STEP = 0;

interface ReportWizardProps {
	owner: IReportOwner;
	onClose: () => void;
}

function statusOf(isFetching: boolean, isError: boolean): ReportTypesStatus {
	if (isFetching) return "loading";
	return isError ? "error" : "ready";
}

/** Creates a report: the Type first, then the steps that Type adds. */
const ReportWizard: React.FC<Readonly<ReportWizardProps>> = ({
	owner,
	onClose,
}) => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const navigate = useNavigate();
	const reportUsage = useUsageDataReporter();
	const { getTerm } = useTerminology();
	const [activeStep, setActiveStep] = useState(TYPE_STEP);
	const chosen: ReportTemplateKey = "then-and-now";
	const type = reportTypeSteps[chosen];
	const [draft, setDraft] = useState(type.newDraft);
	const [isCreating, setIsCreating] = useState(false);
	const [refusal, setRefusal] = useState<string | null>(null);

	// Today and the bounds are the server's; an answer kept from an earlier opening may name yesterday.
	const { data, isFetching, isError, refetch } = useQuery({
		queryKey: ["reportTemplates", owner.kind, owner.id],
		queryFn: () => reportService.getTemplates(owner),
		staleTime: 0,
		refetchOnMount: "always",
		refetchOnWindowFocus: false,
	});

	const status = statusOf(isFetching, isError);
	const templates = status === "ready" ? data : undefined;
	const typeStep = activeStep - 1;
	const current = typeStep >= 0 ? type.steps[typeStep] : undefined;
	const isLastStep = activeStep === type.steps.length;
	const isComplete =
		templates !== undefined &&
		(current === undefined || current.isComplete(draft, templates));

	const create = async (bounds: IReportTemplates) => {
		setIsCreating(true);
		setRefusal(null);
		let reportId: number;
		try {
			reportId = await reportService.createReport(
				owner,
				type.toNewReport(draft, bounds),
			);
		} catch (error) {
			setRefusal(
				refusalCopyFor(error, {
					minimumLengthDays: bounds.minimumLengthDays,
					earliestStart: bounds.earliestStart,
					ownerTerm: getTerm(
						owner.kind === "team"
							? TERMINOLOGY_KEYS.TEAM
							: TERMINOLOGY_KEYS.PORTFOLIO,
					),
					workItemsTerm: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
				}),
			);
			setIsCreating(false);
			return;
		}

		reportUsage({
			name: "ReportCreated",
			reportTemplate: usageDataReportTemplateFor(chosen),
		});
		await queryClient.invalidateQueries({
			queryKey: ["reports", owner.kind, owner.id],
		});
		onClose();
		navigate(`/${owner.kind}s/${owner.id}/reports/${reportId}`);
	};

	const StepContent = current?.component;

	return (
		<Dialog
			open
			onClose={onClose}
			aria-labelledby={TITLE_ID}
			maxWidth="md"
			fullWidth
		>
			<DialogTitle id={TITLE_ID}>Create Report</DialogTitle>
			{/* A fresh frame per step: its Next button stays disabled for a moment after each click. */}
			<WizardStepperFrame
				key={activeStep}
				steps={["Type", ...type.steps.map((step) => step.label)]}
				activeStep={activeStep}
				busy={false}
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
								label: "Create",
								onClick: () => create(templates),
								disabled: !isComplete || isCreating,
							}
						: undefined
				}
				error={refusal === null ? null : { message: refusal }}
			>
				{StepContent === undefined || templates === undefined ? (
					<TypeStep status={status} chosen={chosen} onRetry={() => refetch()} />
				) : (
					<StepContent
						owner={owner}
						templates={templates}
						draft={draft}
						onChange={setDraft}
						disabled={isCreating}
					/>
				)}
			</WizardStepperFrame>
		</Dialog>
	);
};

export default ReportWizard;
