import { Button, Container, Typography } from '@mui/material';
import Grid from '@mui/material/Grid2'
import React, { useContext, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { IProject, Project } from '../../../models/Project/Project';
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
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';
import LighthouseChartComponent from './Charts/LighthouseChart/LighthouseChartComponent';
import { IUpdateStatus } from '../../../services/UpdateSubscriptionService';

const ProjectDetail: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const projectId = Number(id);

    let subscribedToUpdates = false;

    const [project, setProject] = useState<Project>();

    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [isProjectUpdating, setIsProjectUpdating] = useState<boolean>(false);

    const [projectSettings, setProjectSettings] = useState<IProjectSettings | null>(null);
    const [involvedTeams, setInvolvedTeams] = useState<ITeamSettings[]>([]);
    const [showLighthouseChart, setShowLighthouseChart] = useState<boolean>(false);

    const { projectService, teamService, previewFeatureService, updateSubscriptionService } = useContext(ApiServiceContext);


    const fetchInvolvedTeams = async (projectData: IProject | null) => {
        const teamSettings: ITeamSettings[] = [];

        for (const involvedTeam of projectData?.involvedTeams ?? []) {
            const involvedTeamSetting = await teamService.getTeamSettings(involvedTeam.id);
            teamSettings.push(involvedTeamSetting);
        }

        return teamSettings;
    }

    const fetchProject = async () => {
        const projectData = await projectService.getProject(projectId)
        const settings = await projectService.getProjectSettings(projectId);
        const involvedTeamData = await fetchInvolvedTeams(projectData);
        const isLighthouseChartFeatureEnabled = await previewFeatureService.getFeatureByKey("LighthouseChart");

        if (projectData && settings) {
            setProject(projectData);
            setProjectSettings(settings);
            setInvolvedTeams(involvedTeamData);
            setShowLighthouseChart(isLighthouseChartFeatureEnabled?.enabled ?? false);
        }

        setIsLoading(false);
    }

    const onRefreshFeatures = async () => {
        if (project == null) {
            return;
        }

        setIsProjectUpdating(true);
        await projectService.refreshFeaturesForProject(project.id);
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
        await projectService.updateProject(updatedProjectSettings);

        await projectService.refreshForecastsForProject(projectId);
    }

    const onEditProject = () => {
        navigate(`/projects/edit/${id}`);
    }

    const onTeamSettingsChange = async (updatedTeamSettings: ITeamSettings) => {
        await teamService.updateTeam(updatedTeamSettings);

        await projectService.refreshForecastsForProject(projectId);
    }

    const setUpProjectUpdateSubscription = async () => {
        const handleProjectUpdate = async (update: IUpdateStatus) => {
            if (update.status == 'Completed') {
                // Project was updated - reload data!
                await fetchProject();
            }
            
            updateProjectRefreshButton(update);
        };

        const updateProjectRefreshButton = (update: IUpdateStatus | null) => {
            const isFeatureUpdate = update?.updateType == 'Features';
            
            if (isFeatureUpdate) {
                const isUpdating = update?.status == 'Queued' || update?.status == 'InProgress';
                setIsProjectUpdating(isUpdating);
            }
        };

        await updateSubscriptionService.subscribeToFeatureUpdates(projectId, handleProjectUpdate);
        await updateSubscriptionService.subscribeToForecastUpdates(projectId, handleProjectUpdate);

        const updateStatus = await updateSubscriptionService.getUpdateStatus('Features', projectId);
        updateProjectRefreshButton(updateStatus);
    }

    useEffect(() => {
        if (project && !subscribedToUpdates) {
            subscribedToUpdates = true;
            setUpProjectUpdateSubscription();
        }
        else {
            fetchProject();
        }

        return () => {
            updateSubscriptionService.unsubscribeFromFeatureUpdates(projectId);
            updateSubscriptionService.unsubscribeFromForecastUpdates(projectId);
        }
    }, [teamService, project]);


    return (
        <LoadingAnimation hasError={false} isLoading={isLoading}>
            <Container maxWidth={false}>
                {project == null ? (<></>) : (
                    <Grid container spacing={3}>
                        <Grid size={{ xs: 6 }}>
                            <Typography variant='h3'>{project.name}</Typography>
                            <Typography variant='h6'>
                                Last Updated on <LocalDateTimeDisplay utcDate={project.lastUpdated} showTime={true} />
                            </Typography>
                        </Grid>

                        <Grid size={{ xs: 6 }} sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
                            <ActionButton
                                buttonText='Refresh Features'
                                onClickHandler={onRefreshFeatures}
                                maxHeight='40px'
                                externalIsWaiting={isProjectUpdating}
                            />
                            <Button
                                variant="contained"
                                onClick={onEditProject}
                                sx={{ maxHeight: '40px' }}
                            >
                                Edit Project
                            </Button>
                        </Grid>

                        <Grid size={{ xs: 12 }}>
                            <MilestonesComponent
                                milestones={projectSettings?.milestones || []}
                                initiallyExpanded={false}
                                onAddMilestone={handleAddMilestone}
                                onRemoveMilestone={handleRemoveMilestone}
                                onUpdateMilestone={handleUpdateMilestone} />
                        </Grid>
                        <Grid size={{ xs: 12 }}>
                            <InvolvedTeamsList teams={involvedTeams} onTeamUpdated={onTeamSettingsChange} />
                        </Grid>
                        <Grid size={{ xs: 12 }}>
                            <ProjectFeatureList project={project} />
                        </Grid>

                        {showLighthouseChart ? (
                            <Grid size={{ xs: 12 }}>
                                <LighthouseChartComponent projectId={projectId} />
                            </Grid>)
                            : (<></>)}
                    </Grid>)}

            </Container>
            <TutorialButton
                tutorialComponent={<ProjectDetailTutorial />}
            />
        </LoadingAnimation>
    );
}

export default ProjectDetail;
