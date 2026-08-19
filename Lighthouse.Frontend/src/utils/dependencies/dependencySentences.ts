import type { NotHonouredReason } from "../../models/FeatureDependency";

/**
 * The words for what stands against a dependency, built here from a code and a name in the instance's
 * own vocabulary. The row and the dialog say the same thing about the same dependency because they ask
 * the same function - two copies would drift apart a phrase at a time and nobody would notice which
 * one they had read.
 *
 * None of these uses the word this product reserves for an item held up right now: that word is
 * renameable, and both meanings would follow one rename onto the same screen.
 */
export type DependencyTerms = {
	featureTerm: string;
	portfolioTerm: string;
};

const LEFT_OUT = "That dependency is not included in the forecast.";

export const withheldName = (terms: DependencyTerms): string =>
	`a ${terms.featureTerm} you do not have access to`;

export const reasonSentence = (
	reason: NotHonouredReason | null,
	waitedOn: string,
	terms: DependencyTerms,
): string => {
	if (reason === "OutsideThisPortfolio") {
		return `This ${terms.featureTerm} depends on ${waitedOn}, which is in no ${terms.portfolioTerm} they share. ${LEFT_OUT}`;
	}

	if (reason === "InALoop") {
		return `This ${terms.featureTerm} and ${waitedOn} are waiting on each other. ${LEFT_OUT}`;
	}

	if (reason === "BlockerCannotBeForecast") {
		return `${waitedOn} has no measured delivery to forecast from, so the wait cannot be given a date. ${LEFT_OUT}`;
	}

	return "";
};

/** The one thing worth saying that is no reason to leave a dependency out: the order is the reader's. */
export const positionedBelowSentence = (
	waitedOn: string,
	terms: DependencyTerms,
): string =>
	`This ${terms.featureTerm} depends on ${waitedOn}, which sits below it in the order. The order is yours, so nothing was moved.`;
