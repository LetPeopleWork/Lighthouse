import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { formatLikelihood } from "../../../utils/forecast/formatLikelihood";
import { isForecastDataInsufficient } from "../../../utils/forecast/isForecastDataInsufficient";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";
import { ForecastLevel } from "./ForecastLevel";
import InsufficientForecastDataIndicator from "./InsufficientForecastDataIndicator";

interface ForecastLikelihoodProps {
	remainingItems: number;
	targetDate: Date;
	likelihood: number;
	showText?: boolean;
	hasSufficientData?: boolean;
}

const ForecastLikelihood: React.FC<ForecastLikelihoodProps> = ({
	remainingItems,
	targetDate,
	likelihood,
	showText = true,
	hasSufficientData,
}) => {
	const forecastLevel = new ForecastLevel(likelihood);
	const formattedLikelihood = formatLikelihood(likelihood, {
		hasRemainingWork: remainingItems > 0,
		precision: "fixed2",
	});
	const dataInsufficient = isForecastDataInsufficient({
		hasRemainingWork: remainingItems > 0,
		hasSufficientData,
	});

	return (
		<Grid container sx={{ width: "100%", flexDirection: "column" }}>
			{showText && (
				<Typography
					variant="body1"
					sx={{
						marginBottom: "inherit",
						fontWeight: "bold",
						fontSize: "0.875rem",
					}}
				>
					Likelihood to close {remainingItems} Items by{" "}
					<LocalDateTimeDisplay utcDate={targetDate} />:
				</Typography>
			)}

			{dataInsufficient ? (
				<InsufficientForecastDataIndicator />
			) : (
				<Typography
					variant="h1"
					sx={{ color: forecastLevel.color, flex: 1, fontSize: "2rem" }}
				>
					<forecastLevel.IconComponent
						style={{ fontSize: 32, color: forecastLevel.color, marginRight: 8 }}
						data-testid="forecast-level-icon"
					/>
					{formattedLikelihood}
				</Typography>
			)}
		</Grid>
	);
};

export default ForecastLikelihood;
