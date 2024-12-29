import React, { useContext, useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Team } from '../../../models/Team/Team';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import dayjs from 'dayjs';
import { Typography, Container, Button } from '@mui/material';
import Grid from '@mui/material/Grid2'
import ThroughputBarChart from './ThroughputChart';
import { ManualForecast } from '../../../models/Forecasts/ManualForecast';
import TeamFeatureList from './TeamFeatureList';
import ManualForecaster from './ManualForecaster';
import ActionButton from '../../../components/Common/ActionButton/ActionButton';
import InputGroup from '../../../components/Common/InputGroup/InputGroup';
import TutorialButton from '../../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import TeamDetailTutorial from '../../../components/App/LetPeopleWork/Tutorial/Tutorials/TeamDetailTutorial';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import LocalDateTimeDisplay from '../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay';
import SignalRService from '../../../services/SignalRService';

const TeamDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const [searchParams] = useSearchParams();
    const teamId = Number(id);

    const [team, setTeam] = useState<Team>();

    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [triggerUpdate, setTriggerUpdate] = useState<boolean>(false);

    const [remainingItems, setRemainingItems] = useState<number>(10);
    const [targetDate, setTargetDate] = useState<dayjs.Dayjs | null>(dayjs().add(2, 'week'));
    const [manualForecastResult, setManualForecastResult] = useState<ManualForecast | null>(null);

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

    const onUpdateTeamData = async () => {
        if (!team) {
            return;
        }

        setTriggerUpdate(false);

        try {
            await teamService.updateTeamData(team.id);
        } catch (error) {
            console.error('Error updating throughput:', error);
            setHasError(true);
        }
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
        if (team) {
            if (searchParams.get('triggerUpdate') === 'true') {
                setTriggerUpdate(true);
            }

            const handleTeamUpdate = (status: string) => {
                console.log('Team update received:', status);
                fetchTeam();
            };

            SignalRService.subscribeToTeamUpdates(teamId, handleTeamUpdate);

            return () => {
                SignalRService.unsubscribeFromTeamUpdates(teamId);
            };
        }
        else{
            fetchTeam();
        }
    }, [team]);

    return (
        <LoadingAnimation hasError={hasError} isLoading={isLoading}>
            <Container maxWidth={false}>
                {team == null ? (<></>) : (
                    <Grid container spacing={3}>
                        <Grid size={{ xs: 6 }}>
                            <Typography variant='h3'>{team.name}</Typography>

                            <Typography variant='h6'>
                                Currently working on {team.featuresInProgress.length} Features in parallel
                            </Typography>

                            <Typography variant='h6'>
                                Last Updated on <LocalDateTimeDisplay utcDate={team.lastUpdated} showTime={true} />
                            </Typography>
                        </Grid>
                        <Grid size={{ xs: 6 }} sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
                            <ActionButton
                                onClickHandler={onUpdateTeamData}
                                buttonText="Update Team Data"
                                maxHeight='40px'
                                triggerUpdate={triggerUpdate}
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
                            <ThroughputBarChart throughputData={team.throughput} />
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
