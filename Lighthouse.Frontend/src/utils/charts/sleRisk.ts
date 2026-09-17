import type { IWorkItem } from "../../models/WorkItem";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "./paceBands";

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
export const sleRiskColumnDescription = (workItemTerm: string): string =>
	`Of every ${workItemTerm} still open at this age, the share that went on to miss the target`;

/** The header follows whatever the team calls its target. */
export const sleRiskColumnHeaderName = (sleTerm: string): string =>
	`${sleTerm} Risk`;

/**
 * Where the odds turn against an item, in the palette the aging chart already paints its pace bands
 * with. Low to high is good to bad in both, so a reader who has learned one has learned the other.
 */
const SLE_RISK_THRESHOLDS_LOW_TO_HIGH = [25, 50, 75, 100] as const;

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
 * The number behind a rendered label. The column carries `86%` so that the export does too, which
 * leaves ordering with nothing but the text unless the number is read back out of it.
 */
export const sleRiskSortValue = (label: string): number | undefined => {
	// Naming the sentinel rather than leaving it to the parse below, which happens to reject it only
	// because the wording starts with a letter. A sentinel reworded to start with a digit would
	// otherwise be read as a risk, silently, on a column whose whole job is ordering.
	if (label === SLE_RISK_BEYOND_HISTORY_LABEL) {
		return undefined;
	}

	const risk = Number.parseInt(label, 10);
	return Number.isNaN(risk) ? undefined : risk;
};

const sleRiskColorFor = (risk: number | undefined): string | undefined => {
	if (risk === undefined) {
		return undefined;
	}

	const rank = SLE_RISK_THRESHOLDS_LOW_TO_HIGH.filter(
		(threshold) => risk >= threshold,
	).length;

	return PACE_BAND_COLORS_LOW_TO_HIGH[rank];
};

/**
 * Builds the descriptor, or returns nothing when the team published no target — in which case there
 * is no promise for anything to be at risk of breaking and the column would be a row of blanks. An
 * empty answer from the backend is exactly that case: it lists every in-flight item otherwise, even
 * the ones it cannot answer for.
 */
export const buildSleRiskColumnDescriptor = ({
	riskByReferenceId,
	headerName,
	description,
}: SleRiskColumnInputs): SleRiskColumnDescriptor | undefined => {
	if (riskByReferenceId.size === 0) {
		return undefined;
	}

	const riskFor = (workItem: IWorkItem): number | undefined =>
		riskByReferenceId.get(workItem.referenceId) ?? undefined;

	return {
		headerName,
		description,
		riskFor,
		labelFor: (workItem) => {
			const risk = riskFor(workItem);
			return risk === undefined ? SLE_RISK_BEYOND_HISTORY_LABEL : `${risk}%`;
		},
		colorForRisk: sleRiskColorFor,
	};
};
