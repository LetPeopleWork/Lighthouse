import { Tooltip, Typography } from "@mui/material";
import type React from "react";
import type { IHowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import type { IWhenForecast } from "../../../models/Forecasts/WhenForecast";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";
import { ForecastLevel } from "./ForecastLevel";

interface ForecastInfoProps {
	forecast: IForecast;
}

const TooltipText: React.FC<{ level: string; percentage: number }> = ({
	level,
	percentage,
}) => (
	<Typography variant="body1">
		{level} ({percentage}% Chance)
	</Typography>
);

const ForecastInfo: React.FC<ForecastInfoProps> = ({ forecast }) => {
	const forecastLevel = new ForecastLevel(forecast.probability);

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const isWhenForecast = (forecast: IForecast): forecast is IWhenForecast => {
		return (forecast as IWhenForecast).expectedDate !== undefined;
	};

	const isHowManyForecast = (
		forecast: IForecast,
	): forecast is IHowManyForecast => {
		return (forecast as IHowManyForecast).value !== undefined;
	};

	const renderForecast = (): React.ReactNode => {
		if (isWhenForecast(forecast)) {
			return <LocalDateTimeDisplay utcDate={forecast.expectedDate} />;
		}

		if (isHowManyForecast(forecast)) {
			return (
				<Typography variant="body2">
					{forecast.value} {workItemsTerm}
				</Typography>
			);
		}

		return <div>Forecast Type not Supported</div>;
	};

	return (
		<Tooltip
			title={
				<TooltipText
					level={forecastLevel.level}
					percentage={forecast.probability}
				/>
			}
			arrow
		>
			<Typography
				variant="body1"
				sx={{ display: "flex", alignItems: "center" }}
			>
				<forecastLevel.IconComponent
					style={{ color: forecastLevel.color, marginRight: 8 }}
				/>
				{renderForecast()}
			</Typography>
		</Tooltip>
	);
};

export default ForecastInfo;
