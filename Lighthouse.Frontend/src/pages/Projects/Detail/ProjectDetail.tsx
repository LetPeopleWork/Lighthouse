import { Button, Container, Grid, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { Project } from '../../../models/Project/Project';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IApiService } from '../../../services/Api/IApiService';
import LocalDateTimeDisplay from '../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay';
import ProjectFeatureList from './ProjectFeatureList';
import InvolvedTeamsList from './InvolvedTeamsList';
import ActionButton from '../../../components/Common/ActionButton/ActionButton';
import TutorialButton from '../../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import ProjectDetailTutorial from '../../../components/App/LetPeopleWork/Tutorial/Tutorials/ProjectDetailTutorial';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';
import { IMilestone } from '../../../models/Project/Milestone';
import MilestonesComponent from '../../../components/Common/Milestones/MilestonesComponent';
import { ITeamSettings } from '../../../models/Team/TeamSettings';

const ProjectDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const apiService: IApiService = ApiServiceProvider.getApiService();
    const projectId = Number(id);

    const [project, setProject] = useState<Project>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [projectSettings, setProjectSettings] = useState<IProjectSettings | null>(null);
    const [involvedTeams, setInvolvedTeams] = useState<ITeamSettings[]>([]);

    const navigate = useNavigate();

    const fetchProject = async () => {
        try {
            setIsLoading(true);
            const projectData = await apiService.getProject(projectId)
            const settings = await apiService.getProjectSettings(projectId);

            if (projectData && settings) {
                setProject(projectData);
                setProjectSettings(settings);
            }
            else {
                setHasError(true);
            }

            setIsLoading(false);
        } catch (error) {
            console.error('Error fetching project data:', error);
            setHasError(true);
        }
    }

    const onRefreshFeaturesClick = async () => {
        try {
            if (project == null) {
                return;
            }

            const projectData = await apiService.refreshFeaturesForProject(project.id);

            if (projectData) {
                setProject(projectData)
            }
        }
        catch (error) {
            console.error('Error Refreshing Features:', error);
        }
    }

    const onRefreshForecastsClick = async () => {
        try {
            if (project == null) {
                return;
            }

            const projectData = await apiService.refreshForecastsForProject(project.id);

            if (projectData) {
                setProject(projectData)
            }
        }
        catch (error) {
            console.error('Error Refreshing Features:', error);
        }
    }

    const handleAddMilestone = async (milestone: IMilestone) => {
        if (!projectSettings) {
            return;
        }

        const updatedProjectSettings: IProjectSettings = {
            ...projectSettings,
            milestones: [...(projectSettings.milestones || []), milestone]
        };

        await onMilestonesChanged(updatedProjectSettings);
    };

    const handleRemoveMilestone = async (name: string) => {
        if (!projectSettings) {
            return;
        }

        const updatedProjectSettings: IProjectSettings = {
            ...projectSettings,
            milestones: (projectSettings.milestones || []).filter(milestone => milestone.name !== name)
        };

        await onMilestonesChanged(updatedProjectSettings);
    };


    const handleUpdateMilestone = async (name: string, updatedMilestone: Partial<IMilestone>) => {
        if (!projectSettings) {
            return;
        }

        const updatedProjectSettings: IProjectSettings = {
            ...projectSettings,
            milestones: (projectSettings?.milestones || []).map(milestone =>
                milestone.name === name ? { ...milestone, ...updatedMilestone } : milestone
            )
        };

        await onMilestonesChanged(updatedProjectSettings);
    };

    const onMilestonesChanged = async (updatedProjectSettings: IProjectSettings) => {
        setProjectSettings(updatedProjectSettings);
        await apiService.updateProject(updatedProjectSettings);

        const projectData = await apiService.refreshForecastsForProject(projectId);
        if (projectData) {
            setProject(projectData);
        }
    }

    const onEditProject = () => {
        navigate(`/projects/edit/${id}`);
    }

    const onTeamSettingsChange = async (updatedTeamSettings : ITeamSettings) => {
        await apiService.updateTeam(updatedTeamSettings);
        
        const projectData = await apiService.refreshForecastsForProject(projectId);
        if (projectData) {
            setProject(projectData);
        }
    }

    useEffect(() => {
        const fetchInvolvedTeamSettings = async () => {
            if (project) {
                const teamSettings : ITeamSettings[] = [];

                for (const involvedTeam of project.involvedTeams) {
                    const involvedTeamSetting = await apiService.getTeamSettings(involvedTeam.id);
                    teamSettings.push(involvedTeamSetting);
                }

                setInvolvedTeams(teamSettings);
            }
        }

        fetchInvolvedTeamSettings();
    }, [apiService, project])

    useEffect(() => {
        fetchProject();        
    }, []);


    return (
        <LoadingAnimation hasError={hasError} isLoading={isLoading}>
            <Container>
                {project == null ? (<></>) : (
                    <Grid container spacing={3}>
                        <Grid item xs={6}>
                            <Typography variant='h3'>{project.name}</Typography>
                            <Typography variant='h6'>
                                Last Updated on <LocalDateTimeDisplay utcDate={project.lastUpdated} showTime={true} />
                            </Typography>
                        </Grid>
                        <Grid item xs={6} sx={{ display: 'flex', gap: 2 }}>
                            <ActionButton
                                buttonText='Refresh Features'
                                onClickHandler={onRefreshFeaturesClick}
                                maxHeight='40px'
                            />
                            <ActionButton
                                buttonText='Refresh Forecasts'
                                onClickHandler={onRefreshForecastsClick}
                                maxHeight='40px'
                            />
                            <Button
                                variant="contained"
                                onClick={onEditProject}
                                sx={{ maxHeight: '40px' }}
                            >
                                Edit Project
                            </Button>
                        </Grid>

                        <Grid item xs={12}>
                            <MilestonesComponent
                                milestones={projectSettings?.milestones || []}
                                initiallyExpanded={false}
                                onAddMilestone={handleAddMilestone}
                                onRemoveMilestone={handleRemoveMilestone}
                                onUpdateMilestone={handleUpdateMilestone} />
                        </Grid>
                        <Grid item xs={12}>
                            <InvolvedTeamsList teams={involvedTeams} onTeamUpdated={onTeamSettingsChange} />
                        </Grid>
                        <Grid item xs={12}>
                            <ProjectFeatureList project={project} />
                        </Grid>
                    </Grid>)}

            </Container>
            <TutorialButton
                tutorialComponent={<ProjectDetailTutorial />}
            />
        </LoadingAnimation>
    );
}

export default ProjectDetail;
