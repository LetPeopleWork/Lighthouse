import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";
import { ForecastLevel } from "./ForecastLevel";

interface ForecastLikelihoodProps {
	remainingItems: number;
	targetDate: Date;
	likelihood: number;
	showText?: boolean;
}

const ForecastLikelihood: React.FC<ForecastLikelihoodProps> = ({
	remainingItems,
	targetDate,
	likelihood,
	showText = true,
}) => {
	const forecastLevel = new ForecastLevel(likelihood);
	const formattedLikelihood = likelihood.toFixed(2);

	return (
		<Grid container direction="column" sx={{ width: "100%" }}>
			{showText && (
				<Typography
					variant="body1"
					sx={{
						marginBottom: "inherit",
						fontWeight: "bold",
						fontSize: "0.875rem",
					}}
				>
					Likelihood to close {remainingItems} Items until{" "}
					<LocalDateTimeDisplay utcDate={targetDate} />:
				</Typography>
			)}

			<Typography
				variant="h1"
				sx={{ color: forecastLevel.color, flex: 1, fontSize: "2rem" }}
			>
				<forecastLevel.IconComponent
					style={{ fontSize: 32, color: forecastLevel.color, marginRight: 8 }}
					data-testid="forecast-level-icon"
				/>
				{formattedLikelihood}%
			</Typography>
		</Grid>
	);
};

export default ForecastLikelihood;
