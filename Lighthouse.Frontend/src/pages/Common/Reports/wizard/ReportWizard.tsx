import { Dialog, DialogTitle } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import WizardStepperFrame from "../../../../components/Common/CreateWizards/WizardStepperFrame";
import type {
	IReportOwner,
	ReportTemplateKey,
} from "../../../../models/Reports/Report";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
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
	const [activeStep, setActiveStep] = useState(TYPE_STEP);
	const chosen: ReportTemplateKey = "then-and-now";
	const type = reportTypeSteps[chosen];
	const [draft, setDraft] = useState(type.newDraft);

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
					isLastStep
						? { label: "Create", onClick: () => {}, disabled: !isComplete }
						: undefined
				}
			>
				{StepContent === undefined || templates === undefined ? (
					<TypeStep status={status} chosen={chosen} onRetry={() => refetch()} />
				) : (
					<StepContent
						owner={owner}
						templates={templates}
						draft={draft}
						onChange={setDraft}
						disabled={false}
					/>
				)}
			</WizardStepperFrame>
		</Dialog>
	);
};

export default ReportWizard;
