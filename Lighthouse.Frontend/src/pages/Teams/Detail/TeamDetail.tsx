import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Team } from '../../../models/Team/Team';
import { IApiService } from '../../../services/Api/IApiService';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import dayjs from 'dayjs';
import { Typography, Grid, Container, Button } from '@mui/material';
import ThroughputBarChart from './ThroughputChart';
import { Throughput } from '../../../models/Forecasts/Throughput';
import { ManualForecast } from '../../../models/Forecasts/ManualForecast';
import TeamFeatureList from './TeamFeatureList';
import ManualForecaster from './ManualForecaster';
import ActionButton from '../../../components/Common/ActionButton/ActionButton';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';

const TeamDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const apiService: IApiService = ApiServiceProvider.getApiService();
    const teamId = Number(id);

    const [team, setTeam] = useState<Team>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [isUpdatingThroughput, setIsUpdatingThroughput] = useState<boolean>(false);

    const [remainingItems, setRemainingItems] = useState<number>(10);
    const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(dayjs().add(2, 'week'));
    const [manualForecastResult, setManualForecastResult] = useState<ManualForecast | null>(null);
    const [waitingForManualForecastResult, setWaitingForManualForecastResult] = useState<boolean>(false);

    const [throughput, setThroughput] = useState<Throughput>(new Throughput([]))

    const navigate = useNavigate();

    const fetchTeam = async () => {
        try {
            setIsLoading(true);
            const teamData = await apiService.getTeam(teamId);

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

    const onRunManualForecast = async () => {
        if (!team || !targetDate) {
            return;
        }

        setWaitingForManualForecastResult(true);

        try {
            const manualForecast = await apiService.runManualForecast(team.id, remainingItems, targetDate?.toDate());
            setManualForecastResult(manualForecast);
        } catch (error) {
            console.error('Error getting throughput:', error);
        }

        setWaitingForManualForecastResult(false);
    };

    const onEditTeam = () => {
        navigate(`/teams/edit/${id}`);
    }

    useEffect(() => {
        fetchTeam();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        if (team) {
            fetchThroughput();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [team]);

    return (
        <Container>
            <LoadingAnimation hasError={hasError} isLoading={isLoading}>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <Typography variant='h3'>{team?.name}</Typography>
                    </Grid>
                    <Grid container spacing={3}>
                        <Grid item xs={12} sx={{ display: 'flex', gap: 2 }}>
                            <ActionButton
                                onClickHandler={onUpdateThroughput}
                                buttonText="Update Throughput"
                                isWaiting={!team || isUpdatingThroughput}
                            />
                            <Button variant="contained" onClick={onEditTeam}>
                                Edit Team
                            </Button>
                        </Grid>
                    </Grid>
                    <InputGroup title='Features'>
                        {team != null ? (
                            <TeamFeatureList team={team} />) : (<></>)}
                    </InputGroup>
                    <InputGroup title='Team Forecast'>
                        <ManualForecaster
                            remainingItems={remainingItems}
                            targetDate={targetDate}
                            manualForecastResult={manualForecastResult}
                            waitingForResults={waitingForManualForecastResult}
                            onRemainingItemsChange={setRemainingItems}
                            onTargetDateChange={setTargetDate}
                            onRunManualForecast={onRunManualForecast}
                        />
                    </InputGroup>
                    <InputGroup title='Throughput' initiallyExpanded={false}>
                        <ThroughputBarChart throughputData={throughput} />
                    </InputGroup>
                </Grid>
            </LoadingAnimation>
        </Container>
    );
};

export default TeamDetail;
