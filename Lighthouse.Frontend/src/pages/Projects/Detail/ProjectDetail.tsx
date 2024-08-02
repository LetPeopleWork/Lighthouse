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
import MilestoneList from './MilestoneList';
import ActionButton from '../../../components/Common/ActionButton/ActionButton';
import TutorialButton from '../../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import ProjectDetailTutorial from '../../../components/App/LetPeopleWork/Tutorial/Tutorials/ProjectDetailTutorial';

const ProjectDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const apiService: IApiService = ApiServiceProvider.getApiService();
    const projectId = Number(id);

    const [project, setProject] = useState<Project>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);

    const navigate = useNavigate();

    const fetchProject = async () => {
        try {
            setIsLoading(true);
            const projectData = await apiService.getProject(projectId)

            if (projectData) {
                setProject(projectData)
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

    const onEditProject = () => {
        navigate(`/projects/edit/${id}`);
    }

    useEffect(() => {
        fetchProject();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);


    return (
        <LoadingAnimation hasError={hasError} isLoading={isLoading}>
            <Container>
                {project == null ? (<></>) : (
                    <Grid container spacing={3}>
                        <Grid item xs={12}>
                            <Typography variant='h3'>{project.name}</Typography><Typography variant='h6'>
                                Last Updated on <LocalDateTimeDisplay utcDate={project.lastUpdated} showTime={true} />
                            </Typography>
                        </Grid>
                        <Grid item xs={6}>
                            <MilestoneList milestones={project.milestones} />
                        </Grid>
                        <Grid item xs={6}>
                            <InvolvedTeamsList teams={project.involvedTeams} />
                        </Grid>

                        <Grid item xs={12} sx={{ display: 'flex', gap: 2 }}>
                            <ActionButton buttonText='Refresh Features' onClickHandler={onRefreshFeaturesClick} />
                            <ActionButton buttonText='Refresh Forecasts' onClickHandler={onRefreshForecastsClick} />
                            <Button variant="contained" onClick={onEditProject}>Edit Project</Button>
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
