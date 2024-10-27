import React, { useContext, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Team } from '../../../models/Team/Team';
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
import TutorialButton from '../../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import TeamDetailTutorial from '../../../components/App/LetPeopleWork/Tutorial/Tutorials/TeamDetailTutorial';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import LocalDateTimeDisplay from '../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay';

const TeamDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const teamId = Number(id);

    const [team, setTeam] = useState<Team>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);

    const [remainingItems, setRemainingItems] = useState<number>(10);
    const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(dayjs().add(2, 'week'));
    const [manualForecastResult, setManualForecastResult] = useState<ManualForecast | null>(null);

    const [throughput, setThroughput] = useState<Throughput>(new Throughput([]))

    const navigate = useNavigate();

    const { teamService, forecastService } = useContext(ApiServiceContext);

    const fetchTeam = async () => {
        try {
            setIsLoading(true);
            const teamData = await teamService.getTeam(teamId);

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
            const throughputData = await teamService.getThroughput(team.id);
            setThroughput(throughputData)
        } catch (error) {
            console.error('Error getting throughput:', error);
        }
    }

    const onUpdateThroughput = async () => {
        if (!team) {
            return;
        }

        try {
            await teamService.updateThroughput(team.id);
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

        try {
            const manualForecast = await forecastService.runManualForecast(team.id, remainingItems, targetDate?.toDate());
            setManualForecastResult(manualForecast);
        } catch (error) {
            console.error('Error getting throughput:', error);
        }
    };

    const onEditTeam = () => {
        navigate(`/teams/edit/${id}`);
    }

    useEffect(() => {
        fetchTeam();
    }, []);

    useEffect(() => {
        if (team) {
            fetchThroughput();
        }
    }, [team]);

    return (
        <LoadingAnimation hasError={hasError} isLoading={isLoading}>
            <Container>
                {team == null ? (<></>) : (
                    <Grid container spacing={3}>
                        <Grid item xs={6}>
                            <Typography variant='h3'>{team.name}</Typography>                            

                            <Typography variant='h6'>
                                Currently working on {team.actualFeatureWip} Features in parallel
                            </Typography>

                            <Typography variant='h6'>
                                Last Updated on <LocalDateTimeDisplay utcDate={team.lastUpdated} showTime={true} />
                            </Typography>
                        </Grid>
                        <Grid item xs={6} sx={{ display: 'flex', gap: 2 }}>
                            <ActionButton
                                onClickHandler={onUpdateThroughput}
                                buttonText="Update Team Data"
                                maxHeight='40px'
                            />
                            <Button variant="contained" onClick={onEditTeam} sx={{ maxHeight: '40px' }}>
                                Edit Team
                            </Button>
                        </Grid>
                        <InputGroup title='Features'>
                            <TeamFeatureList team={team} />
                        </InputGroup>
                        <InputGroup title='Team Forecast'>
                            <ManualForecaster
                                remainingItems={remainingItems}
                                targetDate={targetDate}
                                manualForecastResult={manualForecastResult}
                                onRemainingItemsChange={setRemainingItems}
                                onTargetDateChange={setTargetDate}
                                onRunManualForecast={onRunManualForecast}
                            />
                        </InputGroup>
                        <InputGroup title='Throughput' initiallyExpanded={false}>
                            <ThroughputBarChart throughputData={throughput} />
                        </InputGroup>
                    </Grid>)}
            </Container>
            <TutorialButton
                tutorialComponent={<TeamDetailTutorial />}
            />
        </LoadingAnimation>
    );
};

export default TeamDetail;
