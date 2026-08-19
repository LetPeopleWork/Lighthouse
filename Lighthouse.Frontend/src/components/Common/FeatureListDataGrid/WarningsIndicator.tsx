import CheckIcon from "@mui/icons-material/Check";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { IconButton, Tooltip } from "@mui/material";
import type React from "react";
import type {
	IFeatureDependencyWarning,
	NotHonouredReason,
} from "../../../models/FeatureDependency";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

type WarningsIndicatorProps = {
	isDoneWithRemainingWork: boolean;
	isUsingDefaultFeatureSize: boolean;
	dependencyWarnings?: IFeatureDependencyWarning[];
};

const DONE_WITH_REMAINING_WORK_TOOLTIP =
	"This feature is marked as done but still has remaining work items. Please verify if all work has been completed.";

const WarningsIndicator: React.FC<WarningsIndicatorProps> = ({
	isDoneWithRemainingWork,
	isUsingDefaultFeatureSize,
	dependencyWarnings = [],
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);

	if (
		!isDoneWithRemainingWork &&
		!isUsingDefaultFeatureSize &&
		dependencyWarnings.length === 0
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
			{dependencyWarnings.map((warning) => {
				const kind = kindOf(warning);

				return (
					<Tooltip
						key={`${kind}-${warning.blockerReferenceId}`}
						title={sentenceFor(warning, kind, {
							featureTerm,
							portfolioTerm,
						})}
					>
						<IconButton
							size="small"
							sx={{ ml: 1 }}
							aria-label={sentenceFor(warning, kind, {
								featureTerm,
								portfolioTerm,
							})}
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
	| "positioned-below";

const KIND_OF_REASON: Record<NotHonouredReason, DependencyWarningKind> = {
	OutsideThisPortfolio: "outside-portfolio",
	InALoop: "in-a-loop",
	BlockerCannotBeForecast: "cannot-be-forecast",
};

// A dependency Lighthouse cannot act on is reported as such; where it sits in the order is only worth
// mentioning about one it can act on, so the reason wins where a dependency has both.
const kindOf = (warning: IFeatureDependencyWarning): DependencyWarningKind => {
	if (warning.notHonouredReason) {
		return KIND_OF_REASON[warning.notHonouredReason];
	}

	return "positioned-below";
};

// Built here, from a code and a name, in this instance's own words. A sentence sent from the server
// would be one nobody could rename.
const sentenceFor = (
	warning: IFeatureDependencyWarning,
	kind: DependencyWarningKind,
	terms: { featureTerm: string; portfolioTerm: string },
): string => {
	const waitedOn = warning.isWithheld
		? `a ${terms.featureTerm} you do not have access to`
		: warning.blockerName;
	const leftOut = "That dependency is not included in the forecast.";

	if (kind === "outside-portfolio") {
		return `This ${terms.featureTerm} depends on ${waitedOn}, which is in no ${terms.portfolioTerm} they share. ${leftOut}`;
	}

	if (kind === "in-a-loop") {
		return `This ${terms.featureTerm} and ${waitedOn} are waiting on each other. ${leftOut}`;
	}

	if (kind === "cannot-be-forecast") {
		return `${waitedOn} has no measured delivery to forecast from, so the wait cannot be given a date. ${leftOut}`;
	}

	return `This ${terms.featureTerm} depends on ${waitedOn}, which sits below it in the order. The order is yours, so nothing was moved.`;
};

export default WarningsIndicator;
