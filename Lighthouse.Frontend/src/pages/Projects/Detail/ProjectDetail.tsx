import { Container, Grid, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { Project } from '../../../models/Project';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IApiService } from '../../../services/Api/IApiService';
import LocalDateTimeDisplay from '../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay';
import ProjectFeatureList from './ProjectFeatureList';
import InvolvedTeamsList from './InvolvedTeamsList';
import MilestoneList from './MilestoneList';
import ActionButton from '../../../components/Common/ActionButton/ActionButton';

const ProjectDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const apiService: IApiService = ApiServiceProvider.getApiService();
    const projectId = Number(id);

    const [project, setProject] = useState<Project>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);

    const [isRefreshingFeatures, setIsRefreshingFeatures] = useState<boolean>(false);

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
        try{
            if (project == null){
                return;
            }

            setIsRefreshingFeatures(true);
            const projectData = await apiService.refreshFeaturesForProject(project.id);

            if (projectData) {
                setProject(projectData)
            }
        }
        catch (error){
            console.error('Error Refreshing Features:', error);
        }
        finally{
            setIsRefreshingFeatures(false);
        }
    }

    useEffect(() => {
        fetchProject();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);


    return (
        <Container>
            <LoadingAnimation hasError={hasError} isLoading={isLoading}>
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
                        <Grid item xs={12}>
                            <ActionButton buttonText='Refresh Features' isWaiting={isRefreshingFeatures} onClickHandler={onRefreshFeaturesClick} />
                        </Grid>
                        <Grid item xs={12}>
                            <ProjectFeatureList project={project} />
                        </Grid>
                    </Grid>)}
            </LoadingAnimation>
        </Container>
    );
}

export default ProjectDetail;
