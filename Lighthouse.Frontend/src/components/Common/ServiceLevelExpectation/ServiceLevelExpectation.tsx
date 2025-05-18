import { Card, CardContent, Typography } from "@mui/material";
import type React from "react";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";

interface ServiceLevelExpectationProps {
	featureOwner: IFeatureOwner;
}

const ServiceLevelExpectation: React.FC<ServiceLevelExpectationProps> = ({
	featureOwner,
}) => {
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
			elevation={1}
			sx={{
				backgroundColor: (theme) => theme.palette.primary.light,
				borderRadius: 1,
				maxHeight: 40,
				minWidth: 250,
				maxWidth: 300,
				border: (theme) => `1px solid ${theme.palette.primary.main}`,
			}}
		>
			<CardContent
				sx={{ padding: "4px 8px", "&:last-child": { paddingBottom: "4px" } }}
			>
				<Typography
					variant="caption"
					sx={{
						display: "block",
						fontWeight: 500,
						lineHeight: 1,
						color: "primary.contrastText",
					}}
				>
					Service Level Expectation
				</Typography>
				<Typography
					variant="body2"
					sx={{
						color: "primary.contrastText",
					}}
				>
					{Math.round(probability)}% of items within {range} days or less
				</Typography>
			</CardContent>
		</Card>
	);
};

export default ServiceLevelExpectation;
