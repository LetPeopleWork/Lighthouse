import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";

interface ServiceLevelExpectationProps {
	featureOwner: IFeatureOwner;
}

const ServiceLevelExpectation: React.FC<ServiceLevelExpectationProps> = ({
	featureOwner,
}) => {
	const theme = useTheme();
	if (
		!featureOwner.serviceLevelExpectationProbability ||
		!featureOwner.serviceLevelExpectationRange ||
		featureOwner.serviceLevelExpectationProbability <= 0 ||
		featureOwner.serviceLevelExpectationRange <= 0
	) {
		return null;
	}

	const probability = featureOwner.serviceLevelExpectationProbability;
	const range = featureOwner.serviceLevelExpectationRange;

	return (
		<Card
			elevation={0}
			sx={{
				backgroundColor: "transparent",
				borderRadius: 2,
				minWidth: 250,
				maxWidth: 300,
				border: `2px dashed ${theme.palette.primary.main}`,
				boxShadow: "none",
				p: 0,
			}}
		>
			<CardContent
				sx={{ padding: "8px 12px", "&:last-child": { paddingBottom: "8px" } }}
			>
				<Typography
					variant="caption"
					sx={{
						display: "block",
						fontWeight: theme.emphasis.high,
						lineHeight: 1,
						color: theme.palette.primary.main,
						mb: 0.5,
					}}
				>
					Service Level Expectation
				</Typography>
				<Typography
					variant="body2"
					sx={{
						color: theme.palette.text.primary,
						fontWeight: theme.emphasis.medium,
					}}
				>
					{Math.round(probability)}% of items within {range} days or less
				</Typography>
			</CardContent>
		</Card>
	);
};

export default ServiceLevelExpectation;
