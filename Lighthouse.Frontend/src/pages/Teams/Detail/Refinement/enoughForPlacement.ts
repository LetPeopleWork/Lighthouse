export type EnoughForSentence = "EnoughFor" | "AllNeeded";

export type LineSide = "above" | "below";

/** A row where the grid shows it: its place among the shown rows, and whether it is the last of them. */
export interface ShownRow {
	index: number;
	isLastShown: boolean;
	/** Kept rendered while scrolled away, because it holds the focus. */
	isOutOfSight?: boolean;
}

/** Which side of a row the line sits on, and what it says there. */
export interface LineBeside {
	side: LineSide;
	says: EnoughForSentence;
}

export const isNumbered = (high: number, index: number) =>
	index >= 0 && index < high;

/**
 * The line follows the row that makes up the number needed. When fewer rows are shown than that, it
 * follows the last one shown and says all of them are needed; when none are needed, it comes first.
 */
export const lineBeside = (high: number, row: ShownRow): LineBeside | null => {
	if (row.isOutOfSight) {
		return null;
	}
	if (high === 0) {
		return row.index === 0 ? { side: "above", says: "EnoughFor" } : null;
	}
	if (row.index === high - 1) {
		return { side: "below", says: "EnoughFor" };
	}
	if (row.isLastShown && row.index < high) {
		return { side: "below", says: "AllNeeded" };
	}
	return null;
};

/** The words a Team has renamed that the line uses. */
export interface EnoughForTerms {
	workItem: string;
	workItems: string;
	refinement: string;
}

export interface EnoughForFacts {
	shown: number;
	highPercentile: number;
	terms: EnoughForTerms;
}

const describeAllNeeded = ({ shown, terms }: EnoughForFacts): string => {
	const where = `in ${terms.refinement}`;
	const when = `before the next ${terms.refinement}.`;
	return shown === 1
		? `The only ${terms.workItem} ${where} is needed ${when}`
		: `All ${shown} ${terms.workItems} ${where} are needed ${when}`;
};

export const describeEnoughFor = (
	says: EnoughForSentence,
	facts: EnoughForFacts,
): string =>
	says === "AllNeeded"
		? describeAllNeeded(facts)
		: `enough for the next ${facts.terms.refinement} (${facts.highPercentile}%) · not needed before then`;

/** What a number in the "#" column counts, for a reader who cannot see the line. */
export const describeNeededNumber = (
	number: number,
	high: number,
	terms: EnoughForTerms,
): string => `${number} of ${high} needed before the next ${terms.refinement}`;
