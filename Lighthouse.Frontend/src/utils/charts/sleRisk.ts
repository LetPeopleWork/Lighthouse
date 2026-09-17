import type { ISleRisk } from "../../models/Metrics/SleRisk";
import type { IWorkItem } from "../../models/WorkItem";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "./paceBands";

/**
 * What an item is given when nothing the team ever finished ran as long as it already has. There is
 * nothing to divide by, so there is no answer — and a number here, 100 most of all, would read as
 * certainty rather than as silence.
 */
export const SLE_RISK_BEYOND_HISTORY_LABEL = "Beyond history";

/**
 * What an item is given when work did run this long and too little of it did. A share of a handful
 * of items moves by ten points or more when one of them enters or leaves the window, so the number
 * would swing overnight on exactly the items a coach is being told to look at first. Deliberately
 * not the label above: saying nothing ran this long, when something did, is a different claim and a
 * false one.
 */
export const SLE_RISK_NOT_ENOUGH_HISTORY_LABEL = "Not enough history";

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
	/** `86%`, or whichever no-answer label applies. This is the column's value, so it is what exports. */
	readonly labelFor: (workItem: IWorkItem) => string;
	readonly colorForRisk: (risk: number | undefined) => string | undefined;
}

export interface SleRiskColumnInputs {
	readonly answers: readonly ISleRisk[];
	readonly headerName: string;
	readonly description: string;
}

/**
 * The number behind a rendered label. The column carries `86%` so that the export does too, which
 * leaves ordering with nothing but the text unless the number is read back out of it.
 */
export const sleRiskSortValue = (label: string): number | undefined => {
	// Naming the sentinels rather than leaving them to the parse below, which happens to reject them
	// only because the wording starts with a letter. One reworded to start with a digit would
	// otherwise be read as a risk, silently, on a column whose whole job is ordering.
	if (
		label === SLE_RISK_BEYOND_HISTORY_LABEL ||
		label === SLE_RISK_NOT_ENOUGH_HISTORY_LABEL
	) {
		return undefined;
	}

	const risk = Number.parseInt(label, 10);
	return Number.isNaN(risk) ? undefined : risk;
};

/**
 * The colour a risk reads as, shared by the dialog column, the card's count and the chart's
 * background bands - so a reader who has learned one has learned all three.
 */
export const sleRiskColorFor = (
	risk: number | undefined,
): string | undefined => {
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
	answers,
	headerName,
	description,
}: SleRiskColumnInputs): SleRiskColumnDescriptor | undefined => {
	if (answers.length === 0) {
		return undefined;
	}

	const byReferenceId = new Map(
		answers.map((answer) => [answer.referenceId, answer]),
	);
	const riskFor = (workItem: IWorkItem): number | undefined =>
		byReferenceId.get(workItem.referenceId)?.risk ?? undefined;

	return {
		headerName,
		description,
		riskFor,
		labelFor: (workItem) => {
			const answer = byReferenceId.get(workItem.referenceId);
			if (answer?.risk !== undefined && answer.risk !== null) {
				return `${answer.risk}%`;
			}

			// An item the answer never mentioned is treated as beyond history rather than as thinly
			// evidenced, because nothing was measured for it at all.
			return answer && answer.comparableItems > 0
				? SLE_RISK_NOT_ENOUGH_HISTORY_LABEL
				: SLE_RISK_BEYOND_HISTORY_LABEL;
		},
		colorForRisk: sleRiskColorFor,
	};
};

/**
 * What a coach needs to decide whether the dialog is worth opening: how many in-flight items are
 * more likely than not to miss the target, and how bad the worst of them is.
 */
export interface SleRiskAtRiskSummary {
	readonly count: number;
	readonly color?: string;
}

/**
 * More likely than not to breach. One explainable line rather than a tunable nobody sets; if it
 * turns out to be the wrong line, that is evidence for a follow-up and not a knob to ship now.
 */
const AT_RISK_FROM = 50;

/**
 * Counted off the answers the column already reads, never computed a second time — two readings of
 * one rule is how a chip and the list behind it come to disagree.
 *
 * An item beyond all history counts: it has outlasted everything the team ever finished, which is
 * what a coach means by trouble. An item too little history can speak for does not — the absence of
 * a signal is not a signal, and putting it here would fill the one number used to decide whether to
 * act with items nobody can act on.
 */
export const sleRiskAtRiskSummary = (
	answers: readonly ISleRisk[],
): SleRiskAtRiskSummary => {
	// One pass with two exclusive arms rather than two filters added together, so no item can be
	// counted twice however odd the answer it arrives in.
	const counted = answers.filter((answer) =>
		answer.risk === null
			? answer.comparableItems === 0
			: answer.risk >= AT_RISK_FROM,
	);

	if (counted.length === 0) {
		return { count: 0 };
	}

	// Nothing places an item beyond all history below the top band, so it reads as the worst there
	// is - which is the same reason it is counted at all.
	const worst = Math.max(...counted.map((answer) => answer.risk ?? 100));

	return { count: counted.length, color: sleRiskColorFor(worst) };
};
