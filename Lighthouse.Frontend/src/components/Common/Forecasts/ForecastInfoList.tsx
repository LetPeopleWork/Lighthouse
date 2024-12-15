import React from "react";
import { Typography } from "@mui/material";
import Grid from '@mui/material/Grid2'
import ForecastInfo from "./ForecastInfo";
import { styled } from '@mui/system';
import { IForecast } from "../../../models/Forecasts/IForecast";

const ForecastsHeader = styled(Typography)({
    marginBottom: 'inherit',
    fontWeight: 'bold',
});

interface ForecastInfoListProps {
    title: string;
    forecasts: IForecast[];
}

const ForecastInfoList: React.FC<ForecastInfoListProps> = ({ title, forecasts }) => {
    return (
        <Grid  size={{ xs: 12 }}>
            <ForecastsHeader variant="body1">{title}</ForecastsHeader>
            {forecasts.slice().reverse().map((forecast) => (
                <ForecastInfo key={forecast.probability} forecast={forecast} />
            ))}
        </Grid>
    );
};

export default ForecastInfoList;
