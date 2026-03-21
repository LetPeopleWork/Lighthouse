import CheckIcon from "@mui/icons-material/Check";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { IconButton, Tooltip } from "@mui/material";
import type React from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

type WarningsIndicatorProps = {
	isDoneWithRemainingWork: boolean;
	isUsingDefaultFeatureSize: boolean;
};

const DONE_WITH_REMAINING_WORK_TOOLTIP =
	"This feature is marked as done but still has remaining work items. Please verify if all work has been completed.";

const WarningsIndicator: React.FC<WarningsIndicatorProps> = ({
	isDoneWithRemainingWork,
	isUsingDefaultFeatureSize,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);

	if (!isDoneWithRemainingWork && !isUsingDefaultFeatureSize) {
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
		</>
	);
};

export default WarningsIndicator;
