import CheckIcon from "@mui/icons-material/Check";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { IconButton, Tooltip } from "@mui/material";
import type React from "react";
import {
	type IFeatureDependency,
	isWorthWarningAbout,
	type NotHonouredReason,
} from "../../../models/FeatureDependency";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	type DependencyTerms,
	positionedBelowSentence,
	reasonSentence,
	withheldName,
} from "../../../utils/dependencies/dependencySentences";

type WarningsIndicatorProps = {
	isDoneWithRemainingWork: boolean;
	isUsingDefaultFeatureSize: boolean;
	dependencies?: IFeatureDependency[];
};

const DONE_WITH_REMAINING_WORK_TOOLTIP =
	"This feature is marked as done but still has remaining work items. Please verify if all work has been completed.";

const WarningsIndicator: React.FC<WarningsIndicatorProps> = ({
	isDoneWithRemainingWork,
	isUsingDefaultFeatureSize,
	dependencies = [],
}) => {
	// Having a dependency is not a warning; only one with something wrong with it is.
	const worthReporting = dependencies.filter(isWorthWarningAbout);
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	if (
		!isDoneWithRemainingWork &&
		!isUsingDefaultFeatureSize &&
		worthReporting.length === 0
	) {
		return (
			<Tooltip title="No warnings">
				<IconButton
					size="small"
					sx={{ ml: 1 }}
					aria-label="No warnings"
					data-testid="no-warnings"
				>
					<CheckIcon sx={{ color: "success.main" }} />
				</IconButton>
			</Tooltip>
		);
	}

	const defaultSizeTooltip = `No child ${workItemsTerm} were found for this ${featureTerm}. The remaining ${workItemsTerm} displayed are based on the default ${featureTerm} size specified in the advanced project settings.`;

	return (
		<>
			{isDoneWithRemainingWork && (
				<Tooltip title={DONE_WITH_REMAINING_WORK_TOOLTIP}>
					<IconButton
						size="small"
						sx={{ ml: 1 }}
						aria-label={DONE_WITH_REMAINING_WORK_TOOLTIP}
						data-testid="warning-done-with-remaining-work"
					>
						<WarningAmberIcon sx={{ color: "warning.main" }} />
					</IconButton>
				</Tooltip>
			)}
			{isUsingDefaultFeatureSize && (
				<Tooltip title={defaultSizeTooltip}>
					<IconButton
						size="small"
						sx={{ ml: 1 }}
						aria-label={defaultSizeTooltip}
						data-testid="warning-default-feature-size"
					>
						<WarningAmberIcon sx={{ color: "warning.main" }} />
					</IconButton>
				</Tooltip>
			)}
			{worthReporting.map((dependency) => {
				const kind = kindOf(dependency);
				const sentence = sentenceFor(dependency, {
					featureTerm,
					portfolioTerm,
				});

				return (
					<Tooltip key={`${kind}-${dependency.referenceId}`} title={sentence}>
						<IconButton
							size="small"
							sx={{ ml: 1 }}
							aria-label={sentence}
							data-testid={`warning-dependency-${kind}`}
						>
							<WarningAmberIcon sx={{ color: "warning.main" }} />
						</IconButton>
					</Tooltip>
				);
			})}
		</>
	);
};

type DependencyWarningKind =
	| "outside-portfolio"
	| "in-a-loop"
	| "cannot-be-forecast"
	| "set-aside"
	| "positioned-below";

// "set-aside" is here for completeness of the mapping only: a dependency carrying that reason never
// reaches this component, because it is not worth warning anybody about.
const KIND_OF_REASON: Record<NotHonouredReason, DependencyWarningKind> = {
	OutsideThisPortfolio: "outside-portfolio",
	InALoop: "in-a-loop",
	BlockerCannotBeForecast: "cannot-be-forecast",
	IgnoredByPortfolio: "set-aside",
};

// A dependency Lighthouse cannot act on is reported as such; where it sits in the order is only worth
// mentioning about one it can act on, so the reason wins where a dependency has both.
const kindOf = (dependency: IFeatureDependency): DependencyWarningKind => {
	if (dependency.notHonouredReason) {
		return KIND_OF_REASON[dependency.notHonouredReason];
	}

	return "positioned-below";
};

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

export default WarningsIndicator;
