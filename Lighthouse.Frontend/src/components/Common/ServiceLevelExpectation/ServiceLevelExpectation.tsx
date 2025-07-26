import SpeedIcon from "@mui/icons-material/Speed";
import { IconButton, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";

interface ServiceLevelExpectationProps {
	featureOwner: IFeatureOwner;
	hide?: boolean;
}

const ServiceLevelExpectation: React.FC<ServiceLevelExpectationProps> = ({
	featureOwner,
	hide = false,
}) => {
	const theme = useTheme();
	if (
		hide ||
		!featureOwner.serviceLevelExpectationProbability ||
		!featureOwner.serviceLevelExpectationRange ||
		featureOwner.serviceLevelExpectationProbability <= 0 ||
		featureOwner.serviceLevelExpectationRange <= 0
	) {
		return null;
	}

	const probability = featureOwner.serviceLevelExpectationProbability;
	const range = featureOwner.serviceLevelExpectationRange;

	const tooltipText = `Service Level Expectation: ${Math.round(probability)}% of items within ${range} days or less`;

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
