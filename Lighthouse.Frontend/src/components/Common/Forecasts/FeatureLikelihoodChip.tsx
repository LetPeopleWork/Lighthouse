import { Chip, Tooltip } from "@mui/material";
import type React from "react";
import type { IFeatureLikelihood } from "../../../models/Delivery";
import {
	cannotBeForecast,
	cannotForecastReason,
} from "../../../utils/forecast/cannotForecast";
import { featureLikelihoodLabel } from "../../../utils/forecast/featureLikelihoodLabel";
import { ForecastLevel } from "./ForecastLevel";

export interface FeatureLikelihoodChipProps {
	featureLikelihood: IFeatureLikelihood;
	hasRemainingWork: boolean;
}

export const FeatureLikelihoodChip: React.FC<FeatureLikelihoodChipProps> = ({
	featureLikelihood,
	hasRemainingWork,
}) => {
	const teamsWithoutForecast = featureLikelihood.teamsWithoutForecast ?? [];
	const isUnforecastable =
		featureLikelihood.likelihoodPercentage === null ||
		cannotBeForecast({ teamsWithoutForecast });

	const chip = (
		<Chip
			label={featureLikelihoodLabel(featureLikelihood, hasRemainingWork)}
			size="small"
			sx={{
				bgcolor: new ForecastLevel(featureLikelihood.likelihoodPercentage)
					.color,
				color: "#fff",
				fontWeight: "bold",
			}}
		/>
	);

	if (!isUnforecastable) {
		return chip;
	}

	return (
		<Tooltip title={cannotForecastReason(teamsWithoutForecast)}>
			<span>{chip}</span>
		</Tooltip>
	);
};
