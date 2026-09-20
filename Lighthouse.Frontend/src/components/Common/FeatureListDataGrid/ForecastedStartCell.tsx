import { Tooltip, Typography } from "@mui/material";
import type React from "react";
import type { IFeature } from "../../../models/Feature";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
	cannotForecastReason,
} from "../../../utils/forecast/cannotForecast";
import ForecastInfoList from "../Forecasts/ForecastInfoList";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";

// Said in the cell rather than in a tooltip. A reader scanning the column has to be able to tell an
// observed date from a percentile without stopping to hover over each one - otherwise the day work
// actually began reads as the day it is predicted to, which is the more confident of the two claims
// and the wrong one.
export const OBSERVED_START_LABEL = "Started";

/**
 * When work on a Feature begins, as the Feature table shows it.
 *
 * Three answers, and which one is given is the server's decision rather than this component's: a date
 * that was observed, four percentiles that were forecast, or nothing. Reading "nothing" off the absence
 * of percentiles would be re-deciding that here, and would make "already started" and "cannot be
 * forecast" indistinguishable.
 */
const ForecastedStartCell: React.FC<{ feature: IFeature }> = ({ feature }) => {
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

	const start = feature.startForecast;

	if (start?.source === "Observed" && start.observedDate) {
		return (
			<Typography
				variant="body2"
				data-testid="observed-start"
				sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
			>
				{OBSERVED_START_LABEL}
				<LocalDateTimeDisplay utcDate={start.observedDate} />
			</Typography>
		);
	}

	// A start nobody can say anything about renders as the same nothing the completion column renders
	// for a Feature with no forecasts. No new empty state is invented for this.
	return <ForecastInfoList title={""} forecasts={start?.percentiles ?? []} />;
};

export default ForecastedStartCell;
