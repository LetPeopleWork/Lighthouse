import type { ISleRisk } from "../../models/Metrics/SleRisk";
import type { IWorkItem } from "../../models/WorkItem";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "./paceBands";

/**
 * The column's wording, written once because the count on the card stands for the same number and a
 * reader who meets it twice must not be told two different things about it.
 *
 * It names the evidence on purpose. The number is a share of the team's own finished work, over the
 * history the team configured - not over whatever range the reader happens to have on screen. Where
 * that history holds little at a given age the answer moves sharply, so the wording says what it
 * rests on rather than leaving a reader to assume a smooth curve.
 */
export const sleRiskColumnDescription = (workItemTerm: string): string =>
	`Of every ${workItemTerm} still open at this age across the team's configured history, the share that went on to miss the target`;

/** The header follows whatever the team calls its target. */
export const sleRiskColumnHeaderName = (sleTerm: string): string =>
	`${sleTerm} Risk`;

/**
 * What an item's number rests on, as a sentence about the team's history.
 *
 * It takes no risk, and that is the design rather than an omission. An item past its target reads
 * 100 because being past the target settles it — the history was never consulted — and that item's
 * count is often zero, because nothing the team finished ever ran that long. A sentence that could
 * see the risk could be written to explain it, and would then be telling a reader that the 100 came
 * from the emptiness. Having no risk to branch on makes that impossible rather than merely absent.
 *
 * So it states a fact that is true beside every answer: how much finished work was still open at
 * this age. It makes no claim about the derivation, so it cannot be wrong about one.
 */
export const sleRiskEvidenceDisclosure = (
	finishedItemsStillOpenAtThisAge: number,
	workItemTerm: string,
	workItemsTerm: string,
): string => {
	if (finishedItemsStillOpenAtThisAge === 0) {
		return `No ${workItemTerm} the team finished was ever still open this long.`;
	}

	// A team whose history is thin at a given age is the case this sentence exists for, so the
	// singular is not a rare path — it is the one a reader most needs to see.
	if (finishedItemsStillOpenAtThisAge === 1) {
		return `1 ${workItemTerm} the team finished was still open at this age.`;
	}

	return `${finishedItemsStillOpenAtThisAge} ${workItemsTerm} the team finished were still open at this age.`;
};

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
	/**
	 * What this row's number rests on, or nothing for a row the answers never mentioned — such a row
	 * makes no claim, so it has nothing to disclose.
	 */
	readonly disclosureFor: (workItem: IWorkItem) => string | undefined;
}

export interface SleRiskColumnInputs {
	readonly answers: readonly ISleRisk[];
	readonly headerName: string;
	readonly description: string;
	readonly workItemTerm: string;
	readonly workItemsTerm: string;
}

/**
 * The number behind a rendered label. The column carries `86%` so that the export does too, which
 * leaves ordering with nothing but the text unless the number is read back out of it.
 *
 * An empty label parses to nothing and sorts to the bottom in both directions, which is where a row
 * the answer set never mentioned belongs - it makes no claim, so it should not displace one.
 */
export const sleRiskSortValue = (label: string): number | undefined => {
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
	// Compared against undefined rather than tested for truthiness, for the same reason the column's
	// label is: a risk of zero is a real answer, and a truthiness check would leave it uncoloured
	// while type-checking. The rank below happens to handle zero correctly either way, which is what
	// makes the wrong version here survive review.
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
	workItemTerm,
	workItemsTerm,
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
			const risk = riskFor(workItem);

			// Compared against undefined rather than tested for truthiness. A risk of zero is a real
			// answer - the item is inside its target and the team's history holds nothing that ran this
			// long - and a truthiness check would blank exactly those cells while type-checking.
			return risk === undefined ? "" : `${risk}%`;
		},
		colorForRisk: sleRiskColorFor,
		disclosureFor: (workItem) => {
			const answer = byReferenceId.get(workItem.referenceId);

			return answer === undefined
				? undefined
				: sleRiskEvidenceDisclosure(
						answer.finishedItemsStillOpenAtThisAge,
						workItemTerm,
						workItemsTerm,
					);
		},
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
 * Where an item counts as at risk. One fixed, explainable line rather than a tunable nobody sets:
 * a coach can be told "seventy percent or worse" once and carry it between teams, which a
 * per-team setting would not allow.
 *
 * Seventy rather than fifty. Fifty reads as "more likely than not", which is a statement about a
 * coin rather than about work worth acting on — on a healthy board it counts items that are merely
 * unremarkable, and a count that is never small is one nobody reads. If it turns out to be the
 * wrong line, that is evidence for a follow-up and not a knob to ship now.
 */
const AT_RISK_FROM = 70;

/**
 * Counted off the answers the column already reads, never computed a second time — two readings of
 * one rule is how a count and the list behind it come to disagree.
 *
 * Every in-flight item now carries a number, so there is one rule and no arm for an absent answer.
 * An item the team's history cannot speak for is not missing from this count; it is in it, with the
 * number its history supports.
 */
export const sleRiskAtRiskSummary = (
	answers: readonly ISleRisk[],
): SleRiskAtRiskSummary => {
	const counted = answers.filter((answer) => answer.risk >= AT_RISK_FROM);

	if (counted.length === 0) {
		return { count: 0 };
	}

	const worst = Math.max(...counted.map((answer) => answer.risk));

	return { count: counted.length, color: sleRiskColorFor(worst) };
};
