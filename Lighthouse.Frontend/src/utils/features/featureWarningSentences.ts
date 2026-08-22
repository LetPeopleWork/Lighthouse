import {
	type IFeatureDependency,
	isWorthWarningAbout,
} from "../../models/FeatureDependency";
import {
	type DependencyTerms,
	positionedBelowSentence,
	reasonSentence,
	withheldName,
} from "../dependencies/dependencySentences";

export const DONE_WITH_REMAINING_WORK_WARNING =
	"This feature is marked as done but still has remaining work items. Please verify if all work has been completed.";

export type FeatureWarningInput = {
	isDoneWithRemainingWork: boolean;
	isUsingDefaultFeatureSize: boolean;
	dependencies?: IFeatureDependency[];
};

export type FeatureWarningTerms = DependencyTerms & {
	workItemsTerm: string;
};

/**
 * Everything there is to say about why a Feature needs attention. The row's tooltip reads the list
 * and an export asks only whether it is empty, so neither can decide a row is clean while the other
 * shows it a reason.
 */
export function featureWarningSentences(
	{
		isDoneWithRemainingWork,
		isUsingDefaultFeatureSize,
		dependencies = [],
	}: FeatureWarningInput,
	{ workItemsTerm, featureTerm, portfolioTerm }: FeatureWarningTerms,
): string[] {
	const defaultSizeWarning = `No child ${workItemsTerm} were found for this ${featureTerm}. The remaining ${workItemsTerm} displayed are based on the default ${featureTerm} size specified in the advanced project settings.`;

	return [
		...(isDoneWithRemainingWork ? [DONE_WITH_REMAINING_WORK_WARNING] : []),
		...(isUsingDefaultFeatureSize ? [defaultSizeWarning] : []),
		// Having a dependency is not a warning; only one with something wrong with it is.
		...dependencies
			.filter(isWorthWarningAbout)
			.map((dependency) =>
				sentenceFor(dependency, { featureTerm, portfolioTerm }),
			),
	];
}

// The words themselves live beside the dialog's, so a row and the list opened from it say the same
// thing about the same dependency.
const sentenceFor = (
	dependency: IFeatureDependency,
	terms: DependencyTerms,
): string => {
	const waitedOn = dependency.isWithheld
		? withheldName(terms)
		: dependency.name;

	if (dependency.notHonouredReason) {
		return reasonSentence(dependency.notHonouredReason, waitedOn, terms);
	}

	return positionedBelowSentence(waitedOn, terms);
};
