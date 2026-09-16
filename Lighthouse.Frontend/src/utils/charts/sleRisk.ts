import type { IWorkItem } from "../../models/WorkItem";

export const __SCAFFOLD__ = true;

/**
 * What an item is given when nothing the team ever finished ran as long as it already has. There is
 * nothing to divide by, so there is no answer — and a number here, 100 most of all, would read as
 * certainty rather than as silence.
 */
export const SLE_RISK_BEYOND_HISTORY_LABEL = "Beyond history";

/**
 * The column's wording, written once because the chip and the chart zones stand for the same number
 * and a reader who meets it twice must not be told two different things about it.
 */
export const sleRiskColumnDescription = (workItemsTerm: string): string =>
	`Of every ${workItemsTerm} still open at this age, the share that went on to miss the target`;

/** The header follows whatever the team calls its target. */
export const sleRiskColumnHeaderName = (sleTerm: string): string =>
	`${sleTerm} Risk`;

/**
 * Everything the work item dialog needs to draw the risk column, and nothing about where the risk
 * came from. The dialog is shared by many callers and stays ignorant of cycle times.
 */
export interface SleRiskColumnDescriptor {
	readonly headerName: string;
	readonly description: string;
	/** The percentage for an item, or undefined when its history cannot answer. */
	readonly riskFor: (workItem: IWorkItem) => number | undefined;
	/** `86%`, or the beyond-history label. This is the value the column carries, so it is what exports. */
	readonly labelFor: (workItem: IWorkItem) => string;
	readonly colorForRisk: (risk: number | undefined) => string | undefined;
}

export interface SleRiskColumnInputs {
	readonly riskByReferenceId: ReadonlyMap<string, number | null>;
	readonly headerName: string;
	readonly description: string;
}

/**
 * Builds the descriptor, or returns nothing when the team published no target — in which case there
 * is no promise for anything to be at risk of breaking and the column would be a row of blanks.
 */
export const buildSleRiskColumnDescriptor = (
	_inputs: SleRiskColumnInputs,
): SleRiskColumnDescriptor | undefined => {
	throw new Error("Not yet implemented -- RED scaffold");
};
