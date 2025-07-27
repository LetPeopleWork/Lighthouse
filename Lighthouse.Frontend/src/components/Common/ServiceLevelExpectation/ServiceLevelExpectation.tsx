import SpeedIcon from "@mui/icons-material/Speed";
import { IconButton, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface ServiceLevelExpectationProps {
	featureOwner: IFeatureOwner;
	hide?: boolean;
	itemTypeKey?: string;
}

const ServiceLevelExpectation: React.FC<ServiceLevelExpectationProps> = ({
	featureOwner,
	hide = false,
	itemTypeKey = TERMINOLOGY_KEYS.WORK_ITEMS,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();

	if (
		hide ||
		!featureOwner.serviceLevelExpectationProbability ||
		!featureOwner.serviceLevelExpectationRange ||
		featureOwner.serviceLevelExpectationProbability <= 0 ||
		featureOwner.serviceLevelExpectationRange <= 0
	) {
		return null;
	}

	const workItemsTerm = getTerm(itemTypeKey);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);

	const probability = featureOwner.serviceLevelExpectationProbability;
	const range = featureOwner.serviceLevelExpectationRange;

	const tooltipText = `${serviceLevelExpectationTerm}: ${Math.round(probability)}% of ${workItemsTerm} within ${range} days or less`;

	return (
		<Tooltip title={tooltipText} arrow>
			<IconButton
				size="small"
				sx={{
					color: theme.palette.primary.main,
					"&:hover": {
						backgroundColor: "action.hover",
					},
				}}
			>
				<SpeedIcon />
			</IconButton>
		</Tooltip>
	);
};

export default ServiceLevelExpectation;
