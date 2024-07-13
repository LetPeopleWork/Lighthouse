import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Team } from '../../../models/Team';
import { IApiService } from '../../../services/Api/IApiService';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import dayjs from 'dayjs';
import {
    Button,
    Typography,
    Grid,
    CircularProgress,
    Container
} from '@mui/material';
import ThroughputBarChart from './ThroughputChart';
import { Throughput } from '../../../models/Forecasts/Throughput';
import { ManualForecast } from '../../../models/Forecasts/ManualForecast';
import FeatureList from './FeatureList';
import ManualForecaster from './ManualForecaster';

const TeamDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const apiService: IApiService = ApiServiceProvider.getApiService();
    const numericId = Number(id);

    const [team, setTeam] = useState<Team>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [isUpdatingThroughput, setIsUpdatingThroughput] = useState<boolean>(false);
    const [isUpdatingForecast, setIsUpdatingForecast] = useState<boolean>(false);

    const [remainingItems, setRemainingItems] = useState<number>(10);
    const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(dayjs().add(2, 'week'));
    const [manualForecastResult, setManualForecastResult] = useState<ManualForecast | null>(null);

    const [throughput, setThroughput] = useState<Throughput>(new Throughput([]))

    const fetchTeam = async () => {
        try {
            setIsLoading(true);
            const teamData = await apiService.getTeam(numericId);

            if (teamData) {
                setTeam(teamData);
            } else {
                setHasError(true);
            }

            setIsLoading(false);
        } catch (error) {
            console.error('Error fetching team data:', error);
            setHasError(true);
        }
    };

    const fetchThroughput = async () => {
        if (!team) {
            return;
        }

        try {
            const throughputData = await apiService.getThroughput(team.id);
            setThroughput(throughputData)
        } catch (error) {
            console.error('Error getting throughput:', error);
        }
    }

    const onUpdateThroughput = async () => {
        if (!team) {
            return;
        }

        setIsUpdatingThroughput(true);

        try {
            await apiService.updateThroughput(team.id);
            setIsUpdatingThroughput(false);
        } catch (error) {
            console.error('Error updating throughput:', error);
            setHasError(true);
        }

        fetchThroughput();
    };

    const onUpdateForecast = async () => {
        if (!team) {
            return;
        }

        setIsUpdatingForecast(true);

        try {
            await apiService.updateForecast(team.id);
            setIsUpdatingForecast(false);
        } catch (error) {
            console.error('Error updating Forecasts:', error);
            setHasError(true);
        }

        fetchTeam();
    };

    const onRunManualForecast = async () => {
        if (!team || !targetDate) {
            return;
        }

        try {
            const manualForecast = await apiService.runManualForecast(team.id, remainingItems, targetDate?.toDate());
            setManualForecastResult(manualForecast);
        } catch (error) {
            console.error('Error getting throughput:', error);
        }
    };

    useEffect(() => {
        fetchTeam();
    }, []);

    useEffect(() => {
        if (team) {
            fetchThroughput();
        }
    }, [team]);

    return (
        <Container>
            <LoadingAnimation hasError={hasError} isLoading={isLoading}>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <Typography variant='h3'>{team?.name}</Typography>
                    </Grid>
                    <Grid item xs={12}>
                        <Button variant="contained" onClick={onUpdateThroughput} disabled={!team || isUpdatingThroughput}>
                            {isUpdatingThroughput ? <CircularProgress /> : 'Update Throughput'}
                        </Button>
                        <Button variant="contained" onClick={onUpdateForecast} disabled={!team || isUpdatingForecast}>
                            {isUpdatingForecast ? <CircularProgress /> : 'Update Forecast'}
                        </Button>
                    </Grid>
                    <Grid item xs={12}>
                        {team != null ? (
                            <FeatureList team={team} />) : (<></>)}
                    </Grid>
                    <Grid item xs={12}>
                        <ManualForecaster
                            remainingItems={remainingItems}
                            targetDate={targetDate}
                            manualForecastResult={manualForecastResult}
                            onRemainingItemsChange={setRemainingItems}
                            onTargetDateChange={setTargetDate}
                            onRunManualForecast={onRunManualForecast}
                        />
                    </Grid>
                    <Grid item xs={12}>
                        <ThroughputBarChart throughputData={throughput} />
                    </Grid>
                </Grid>
            </LoadingAnimation>
        </Container>
    );
};

export default TeamDetail;
