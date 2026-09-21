import type {
	IFeatureDependency,
	NotHonouredReason,
} from "../../models/FeatureDependency";

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

/**
 * What to call the thing a Feature waits on, which is the one decision every sentence below shares.
 *
 * A withheld entry is never named. The point of withholding it is that this reader may not learn what
 * it is, and a sentence leaks it as readily as a link would - so the rule is asked here once rather
 * than remembered at each screen that writes one of these.
 *
 * `knownAs` is for a caller holding a better source for the name than the dependency entry's own copy,
 * which was written elsewhere and drifts away from what the board now says.
 */
export const waitedOnName = (
	dependency: IFeatureDependency,
	terms: DependencyTerms,
	knownAs?: string,
): string =>
	dependency.isWithheld ? withheldName(terms) : (knownAs ?? dependency.name);

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

	// Given only where nothing else stands against the dependency, so it is the one sentence here that
	// promises a date will move. Said about a wait a licence would not have accounted for either, it
	// would be selling something that changes nothing.
	if (reason === "NotLicensed") {
		return `This ${terms.featureTerm} depends on ${waitedOn}, and that wait is not accounted for in the dates. A premium licence accounts for it.`;
	}

	return `${waitedOn} has no measured delivery to forecast from, so the wait cannot be given a date. ${LEFT_OUT}`;
};

/**
 * What a bar waits on, said plainly. Names no obstacle, because there is none - this is the sentence
 * for a wait the forecast honoured whose line the chart simply did not draw.
 */
export const waitingSentence = (waitedOn: string): string =>
	`Waiting on ${waitedOn}.`;

/**
 * Why a dependency has no line on a chart drawn from a selection of Features, in words the reader can
 * act on. Three situations, three sentences: there is nothing to go and do about a Feature the reader
 * may not see, and what to do about one nobody has measured yet is not what to do about one this
 * Delivery simply did not select.
 *
 * Nothing here reads a withheld entry's own name. The point of withholding it is that this reader may
 * not learn what it is, and a sentence leaks it as readily as a link would.
 */
export const withheldSentence = (terms: DependencyTerms): string =>
	`Waiting on ${withheldName(terms)}.`;

export const notOnThisTimelineSentence = (waitedOn: string): string =>
	`Waiting on ${waitedOn}, which is not on this timeline.`;

export const noForecastToPlaceSentence = (waitedOn: string): string =>
	`Waiting on ${waitedOn}, which has no forecast to place on this timeline.`;

/** The one thing worth saying about a dependency that is no reason to leave it out of the forecast. */
export const positionedBelowSentence = (
	waitedOn: string,
	terms: DependencyTerms,
): string =>
	`This ${terms.featureTerm} depends on ${waitedOn}, which sits below it in the order.`;
