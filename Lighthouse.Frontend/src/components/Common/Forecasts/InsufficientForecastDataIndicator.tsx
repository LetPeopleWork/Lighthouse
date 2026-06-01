import { Typography } from "@mui/material";
import type React from "react";

export const INSUFFICIENT_FORECAST_DATA_MESSAGE =
	"Not enough data yet — need at least 5 days with completed items to forecast.";

export const INSUFFICIENT_FORECAST_DATA_SHORT = "Not enough data";

interface InsufficientForecastDataIndicatorProps {
	variant?: "full" | "compact";
}

const InsufficientForecastDataIndicator: React.FC<
	InsufficientForecastDataIndicatorProps
> = ({ variant = "full" }) => {
	return (
		<Typography
			variant={variant === "compact" ? "caption" : "body2"}
			sx={{ fontStyle: "italic", color: "text.secondary" }}
			data-testid="insufficient-forecast-data"
		>
			{variant === "compact"
				? INSUFFICIENT_FORECAST_DATA_SHORT
				: INSUFFICIENT_FORECAST_DATA_MESSAGE}
		</Typography>
	);
};

export default InsufficientForecastDataIndicator;
