export type EnoughForSentence = "EnoughFor" | "AllNeeded";

/** How many of the shown rows are numbered, after which row the line goes (-1: before the first), and what it says. */
export interface EnoughForPlacement {
	numbered: number;
	lineAfterRow: number;
	says: EnoughForSentence;
}

export const placeEnoughForLine = (
	high: number,
	listed: number,
): EnoughForPlacement => {
	const numbered = Math.min(high, listed);
	return {
		numbered,
		lineAfterRow: numbered - 1,
		says: high > listed ? "AllNeeded" : "EnoughFor",
	};
};

/** The words a Team has renamed that the line uses. */
export interface EnoughForTerms {
	workItem: string;
	workItems: string;
	refinement: string;
}

export interface EnoughForFacts {
	listed: number;
	highPercentile: number;
	terms: EnoughForTerms;
}

const describeAllNeeded = ({ listed, terms }: EnoughForFacts): string => {
	const where = `in ${terms.refinement}`;
	const when = `before the next ${terms.refinement}.`;
	return listed === 1
		? `The only ${terms.workItem} ${where} is needed ${when}`
		: `All ${listed} ${terms.workItems} ${where} are needed ${when}`;
};

export const describeEnoughFor = (
	says: EnoughForSentence,
	facts: EnoughForFacts,
): string =>
	says === "AllNeeded"
		? describeAllNeeded(facts)
		: `enough for the next ${facts.terms.refinement} (${facts.highPercentile}%) · not needed before then`;
