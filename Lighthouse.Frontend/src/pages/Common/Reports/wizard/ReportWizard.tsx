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
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import {
	usageDataReportTemplateFor,
	useUsageDataReporter,
} from "../../../../services/UsageData/usageDataReporter";
import { queryStatusOf } from "../../MetricsView/widgetStatus";
import { reportPathOf } from "../reportOwner";
import { refusalCopyFor, useRefusalWords } from "./reportRefusalCopy";
import { reportTypes } from "./reportTypes";
import TypeStep from "./TypeStep";

const TITLE_ID = "report-wizard-title";
const TYPE_STEP = 0;

interface ReportWizardProps {
	owner: IReportOwner;
	onClose: () => void;
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
	const [activeStep, setActiveStep] = useState(TYPE_STEP);
	const chosen: ReportTemplateKey = "then-and-now";
	const type = reportTypes[chosen];
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

	const status = queryStatusOf(isFetching, isError);
	const templates = status === "ready" ? data : undefined;
	const typeStep = activeStep - 1;
	const current = typeStep >= 0 ? type.steps[typeStep] : undefined;
	const isLastStep = activeStep === type.steps.length;
	const isComplete =
		templates !== undefined &&
		(current === undefined || current.isComplete(draft, templates));

	const refusalWordsFor = useRefusalWords(owner);

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
			setRefusal(refusalCopyFor(error, refusalWordsFor(bounds)));
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
		navigate(reportPathOf(owner, reportId));
	};

	const StepContent = current?.component;

	return (
		<Dialog
			open
			onClose={isCreating ? undefined : onClose}
			aria-labelledby={TITLE_ID}
			maxWidth="md"
			fullWidth
		>
			<DialogTitle id={TITLE_ID}>Create Report</DialogTitle>
			<WizardStepperFrame
				steps={["Type", ...type.steps.map((step) => step.label)]}
				activeStep={activeStep}
				busy={false}
				navigationDisabled={isCreating}
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
