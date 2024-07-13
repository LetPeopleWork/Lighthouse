import React from "react";
import { Grid, Typography } from "@mui/material";
import { ForecastLevel } from "./ForecastLevel";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";

interface ForecastLikelihoodProps {
    howMany: number;
    when: Date;
    likelihood: number;
}

const ForecastLikelihood: React.FC<ForecastLikelihoodProps> = ({ howMany, when, likelihood }) => {
    const forecastLevel = new ForecastLevel(likelihood);

    return (
        <Grid item xs={12}>
            <Typography variant="body1" sx={{ marginBottom: 'inherit', fontWeight: 'bold' }}>
                Likelihood to close {howMany} Items until <LocalDateTimeDisplay utcDate={when} />:
            </Typography>

            <Typography variant="h1" sx={{ color: forecastLevel.color, flex: 1 }}>
                <forecastLevel.IconComponent style={{ fontSize: 64, color: forecastLevel.color, marginRight: 8 }} data-testid="forecast-level-icon"/>
                {likelihood}%
            </Typography>
        </Grid>
    );
};

export default ForecastLikelihood;
