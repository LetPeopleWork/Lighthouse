import { Chip, Tooltip } from "@mui/material";
import type React from "react";
import type { IFeatureLikelihood } from "../../../models/Delivery";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
	cannotForecastReason,
} from "../../../utils/forecast/cannotForecast";
import { formatLikelihood } from "../../../utils/forecast/formatLikelihood";
import { isForecastDataInsufficient } from "../../../utils/forecast/isForecastDataInsufficient";
import { ForecastLevel } from "./ForecastLevel";
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "./InsufficientForecastDataIndicator";

export interface FeatureLikelihoodChipProps {
	featureLikelihood: IFeatureLikelihood;
	hasRemainingWork: boolean;
}

export const FeatureLikelihoodChip: React.FC<FeatureLikelihoodChipProps> = ({
	featureLikelihood,
	hasRemainingWork,
}) => {
	const teamsWithoutForecast = featureLikelihood.teamsWithoutForecast ?? [];
	const likelihood = featureLikelihood.likelihoodPercentage;
	const isUnforecastable =
		likelihood === null || cannotBeForecast({ teamsWithoutForecast });

	let label: string;
	if (isUnforecastable) {
		label = CANNOT_FORECAST_SHORT;
	} else if (
		isForecastDataInsufficient({
			hasRemainingWork,
			hasSufficientData: featureLikelihood.hasSufficientData,
		})
	) {
		label = INSUFFICIENT_FORECAST_DATA_SHORT;
	} else {
		label = formatLikelihood(likelihood, {
			hasRemainingWork,
			precision: "round",
		});
	}

	const chip = (
		<Chip
			label={label}
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
