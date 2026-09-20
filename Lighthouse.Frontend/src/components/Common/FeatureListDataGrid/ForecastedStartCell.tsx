import { Box, Tooltip, Typography } from "@mui/material";
import type React from "react";
import type { IFeature } from "../../../models/Feature";
import {
	type IWhenForecast,
	WhenForecast,
} from "../../../models/Forecasts/WhenForecast";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
	cannotForecastReason,
} from "../../../utils/forecast/cannotForecast";
import ForecastInfoList from "../Forecasts/ForecastInfoList";

// The four the forecast columns have always shown.
const THE_USUAL_PERCENTILES = [50, 70, 85, 95];

/**
 * A date that has already happened, rendered the way the completion column renders a Feature that is
 * already done: the same four confidence levels, all carrying the same day.
 *
 * It reads as settled precisely because the four agree - there is no spread left to show. Inventing a
 * second way to draw one date would make the column two visual languages, and a reader would have to
 * learn which one they were looking at.
 */
const asSettledOn = (day: Date): IWhenForecast[] =>
	THE_USUAL_PERCENTILES.map((probability) =>
		WhenForecast.new(probability, day),
	);

/**
 * When work on a Feature begins, as the Feature table shows it.
 *
 * Three answers, and which one is given is the server's decision rather than this component's: a date
 * that was observed, four percentiles that were forecast, or nothing. Reading "nothing" off the absence
 * of percentiles would be re-deciding that here, and would make "already started" and "cannot be
 * forecast" indistinguishable.
 */
const ForecastedStartCell: React.FC<{ feature: IFeature }> = ({ feature }) => {
	const start = feature.startForecast;

	// A day somebody already started on is a fact, and a fact outranks whether anything can be
	// forecast. A team with no history stops the simulation from saying when work will begin; it says
	// nothing about when work did begin. Asking "can this be forecast?" first would answer "cannot
	// forecast" for a date the server has already reported, under a tooltip offering to wait for
	// throughput that would not change it. The server settles this in the same order.
	if (start?.source === "Observed" && start.observedDate) {
		return (
			<Box data-testid="observed-start">
				<ForecastInfoList
					title={""}
					forecasts={asSettledOn(start.observedDate)}
				/>
			</Box>
		);
	}

	if (
		cannotBeForecast({ teamsWithoutForecast: feature.teamsWithoutForecast })
	) {
		return (
			<Tooltip title={cannotForecastReason(feature.teamsWithoutForecast ?? [])}>
				<Typography variant="body2" color="text.secondary">
					{CANNOT_FORECAST_SHORT}
				</Typography>
			</Tooltip>
		);
	}

	// A start nobody can say anything about renders as the same nothing the completion column renders
	// for a Feature with no forecasts. No new empty state is invented for this.
	return <ForecastInfoList title={""} forecasts={start?.percentiles ?? []} />;
};

export default ForecastedStartCell;
