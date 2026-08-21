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

// Asked only about a dependency that has a reason against it, so there is no "nothing to say" case
// here to fall through to - a caller with no reason is asking the wrong question.
export const reasonSentence = (
	reason: NotHonouredReason,
	waitedOn: string,
	terms: DependencyTerms,
): string => {
	if (reason === "OutsideThisPortfolio") {
		return `This ${terms.featureTerm} depends on ${waitedOn}, which is in no ${terms.portfolioTerm} they share. ${LEFT_OUT}`;
	}

	if (reason === "InALoop") {
		return `This ${terms.featureTerm} and ${waitedOn} are waiting on each other. ${LEFT_OUT}`;
	}

	if (reason === "IgnoredByPortfolio") {
		return `${terms.portfolioTerm} is set to ignore dependencies.`;
	}

	return `${waitedOn} has no measured delivery to forecast from, so the wait cannot be given a date. ${LEFT_OUT}`;
};

/** The one thing worth saying about a dependency that is no reason to leave it out of the forecast. */
export const positionedBelowSentence = (
	waitedOn: string,
	terms: DependencyTerms,
): string =>
	`This ${terms.featureTerm} depends on ${waitedOn}, which sits below it in the order.`;
