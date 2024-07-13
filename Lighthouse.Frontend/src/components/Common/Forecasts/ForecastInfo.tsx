import React from "react";
import { Tooltip, Typography } from "@mui/material";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";
import { IForecast } from "../../../models/Forecasts/IForecast";
import { IWhenForecast } from "../../../models/Forecasts/WhenForecast";
import { ForecastLevel } from "./ForecastLevel";
import { IHowManyForecast } from "../../../models/Forecasts/HowManyForecast";

interface ForecastInfoProps {
    forecast: IForecast;
}

const TooltipText: React.FC<{ level: string; percentage: number }> = ({ level, percentage }) => (
    <Typography variant="body1">
        {level} ({percentage}% Chance)
    </Typography>
);

const ForecastInfo: React.FC<ForecastInfoProps> = ({ forecast }) => {
    const forecastLevel = new ForecastLevel(forecast.probability);

    const isWhenForecast = (forecast: IForecast): forecast is IWhenForecast => {
        return (forecast as IWhenForecast).expectedDate !== undefined;
    };

    const isHowManyForecast = (forecast: IForecast): forecast is IHowManyForecast => {
        return (forecast as IHowManyForecast).expectedItems !== undefined;
    };

    return (
        <Tooltip title={<TooltipText level={forecastLevel.level} percentage={forecast.probability} />} arrow>
            <Typography variant="body1" sx={{ display: 'flex', alignItems: 'center' }}>
                <forecastLevel.IconComponent style={{ color: forecastLevel.color, marginRight: 8 }} />
                {isWhenForecast(forecast) ? (
                    <LocalDateTimeDisplay utcDate={forecast.expectedDate} />
                ) : isHowManyForecast(forecast) ? (
                    <Typography variant="body2" >
                        {forecast.expectedItems} Items
                    </Typography>
                ) : (
                    <div>Forecast Type not Supported supported</div>
                )}
            </Typography>
        </Tooltip>
    );
};

export default ForecastInfo;
