import { Button, Container, Typography } from '@mui/material';
import Grid from '@mui/material/Grid2'
import React, { useContext, useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { Project } from '../../../models/Project/Project';
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

const ProjectDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const projectId = Number(id);
    const [searchParams] = useSearchParams();

    const [project, setProject] = useState<Project>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);
    const [triggerUpdate, setTriggerUpdate] = useState<boolean>(false);

    const [projectSettings, setProjectSettings] = useState<IProjectSettings | null>(null);
    const [involvedTeams, setInvolvedTeams] = useState<ITeamSettings[]>([]);
    const [showLighthouseChart, setShowLighthouseChart] = useState<boolean>(false);

    const navigate = useNavigate();

    const { projectService, teamService, previewFeatureService } = useContext(ApiServiceContext);

    const fetchProject = async () => {
        try {
            setIsLoading(true);
            const projectData = await projectService.getProject(projectId)
            const settings = await projectService.getProjectSettings(projectId);
            const isLighthouseChartFeatureEnabled = await previewFeatureService.getFeatureByKey("LighthouseChart");

            if (projectData && settings) {
                setProject(projectData);
                setProjectSettings(settings);
                setShowLighthouseChart(isLighthouseChartFeatureEnabled?.enabled ?? false);
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

            setTriggerUpdate(false);

            const projectData = await projectService.refreshFeaturesForProject(project.id);

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
        await projectService.updateProject(updatedProjectSettings);

        const projectData = await projectService.refreshForecastsForProject(projectId);
        if (projectData) {
            setProject(projectData);
        }
    }

    const onEditProject = () => {
        navigate(`/projects/edit/${id}`);
    }

    const onTeamSettingsChange = async (updatedTeamSettings: ITeamSettings) => {
        await teamService.updateTeam(updatedTeamSettings);

        const projectData = await projectService.refreshForecastsForProject(projectId);
        if (projectData) {
            setProject(projectData);
        }
    }

    useEffect(() => {
        const fetchInvolvedTeamSettings = async () => {
            if (project) {
                const teamSettings: ITeamSettings[] = [];

                for (const involvedTeam of project.involvedTeams) {
                    const involvedTeamSetting = await teamService.getTeamSettings(involvedTeam.id);
                    teamSettings.push(involvedTeamSetting);
                }

                setInvolvedTeams(teamSettings);
            }
        }

        fetchInvolvedTeamSettings();
    }, [projectService, teamService, project])

    useEffect(() => {
        if (!project) {
            fetchProject();
        }

        if (searchParams.get('triggerUpdate') === 'true') {
            setTriggerUpdate(true);
        }        
    }, []);


    return (
        <LoadingAnimation hasError={hasError} isLoading={isLoading}>
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
                                onClickHandler={onRefreshFeaturesClick}
                                maxHeight='40px'
                                triggerUpdate={triggerUpdate}
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
