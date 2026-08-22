import { Typography } from "@mui/material";
import type React from "react";
import { INSUFFICIENT_FORECAST_DATA_MESSAGE } from "../../../utils/forecast/insufficientForecastData";

const InsufficientForecastDataIndicator: React.FC = () => {
	return (
		<Typography
			variant="body2"
			sx={{ fontStyle: "italic", color: "text.secondary" }}
			data-testid="insufficient-forecast-data"
		>
			{INSUFFICIENT_FORECAST_DATA_MESSAGE}
		</Typography>
	);
};

export default InsufficientForecastDataIndicator;
