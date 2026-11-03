import type { WizardFrameAction } from "../../components/Common/CreateWizards/WizardStepperFrame";
import type { IReportTemplates } from "../../models/Reports/Report";
import {
	type ReportTypeStep,
	reportTypes,
} from "../Common/Reports/wizard/reportTypes";
import { type BulkRun, reportsWord } from "./BulkResult";
import {
	isOverTheCeiling,
	OVER_THE_CEILING_ID,
	type OwnersStatus,
} from "./OwnerStep";
import type { useBulkReportCreation } from "./useBulkReportCreation";

export const TYPE_STEP = 0;
export const chosen = "then-and-now";
export const type = reportTypes[chosen];
export type Draft = ReturnType<typeof type.forOwners.newDraft>;

/** Type, then the type's own steps, then who gets the report. */
export const STEP_LABELS = [
	"Type",
	...type.steps.map((step) => step.label),
	"Owners",
];
export const OWNER_STEP = STEP_LABELS.length - 1;

export const createLabelOf = (count: number): string =>
	`Create ${count} ${reportsWord(count)}`;

export const typeStepAt = (
	activeStep: number,
): ReportTypeStep<Draft> | undefined =>
	activeStep > TYPE_STEP && activeStep < OWNER_STEP
		? type.steps[activeStep - 1]
		: undefined;

export const nextActionOf = (
	activeStep: number,
	draft: Draft,
	templates: IReportTemplates,
	onNext: () => void,
): WizardFrameAction | undefined => {
	if (activeStep === OWNER_STEP) return undefined;
	const step = typeStepAt(activeStep);
	return {
		label: "Next",
		onClick: onNext,
		disabled: step !== undefined && !step.isComplete(draft, templates),
	};
};

export const createActionOf = (
	activeStep: number,
	pickedCount: number,
	ownersStatus: OwnersStatus,
	onCreate: () => void,
): WizardFrameAction | undefined =>
	activeStep === OWNER_STEP
		? {
				label: createLabelOf(pickedCount),
				onClick: onCreate,
				disabled:
					ownersStatus !== "ready" ||
					pickedCount === 0 ||
					isOverTheCeiling(pickedCount),
				describedBy: isOverTheCeiling(pickedCount)
					? OVER_THE_CEILING_ID
					: undefined,
			}
		: undefined;

export const runOf = (
	creation: ReturnType<typeof useBulkReportCreation>,
): BulkRun | null => {
	if (creation.isPending) return { status: "pending" };
	if (creation.isError) return { status: "error" };
	if (creation.isSuccess) return { status: "success", outcomes: creation.data };
	return null;
};
