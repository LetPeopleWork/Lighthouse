import React from "react";
import { Grid, Typography } from "@mui/material";
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
        <Grid item xs={12}>
            <ForecastsHeader variant="body1">{title}</ForecastsHeader>
            {forecasts.slice().reverse().map((forecast, index) => (
                <ForecastInfo key={index} forecast={forecast} />
            ))}
        </Grid>
    );
};

export default ForecastInfoList;
