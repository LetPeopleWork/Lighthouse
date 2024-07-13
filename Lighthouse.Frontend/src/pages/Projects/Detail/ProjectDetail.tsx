import { Container, Grid, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import { Project } from '../../../models/Project';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IApiService } from '../../../services/Api/IApiService';
import LocalDateTimeDisplay from '../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay';
import { IMilestone } from '../../../models/Milestone';

const ProjectDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const apiService: IApiService = ApiServiceProvider.getApiService();
    const projectId = Number(id);

    const [project, setProject] = useState<Project>();
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasError, setHasError] = useState<boolean>(false);

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

    useEffect(() => {
        fetchProject();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);


    return (
        <Container>
            <LoadingAnimation hasError={hasError} isLoading={isLoading}>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        {project != null ? (
                            <><Typography variant='h3'>{project.name}</Typography><Typography variant='h6'>
                                Last Updated on <LocalDateTimeDisplay utcDate={project.lastUpdated} showTime={true} />
                            </Typography></>)
                            : (<></>)}
                    </Grid>
                    <Grid item xs={6}>
                        {project?.milestones.length ?? 0 > 0 ? (
                            <>
                                <Typography variant='h4'>Milestones</Typography>
                                {project?.milestones.map((milestone) => (
                                    <React.Fragment key={milestone.name}>
                                        <Typography variant='h6'>{milestone.name}</Typography>
                                        <LocalDateTimeDisplay utcDate={milestone.date} />
                                    </React.Fragment>
                                ))}
                            </>
                        ) : (
                            <></>
                        )}
                    </Grid>

                    <Grid item xs={6}>
                        {project?.involvedTeams.length ?? 0 > 0 ? (
                            <>
                                <Typography variant='h4'>Involved Teams</Typography>
                                {project?.involvedTeams.map((team) => (
                                    <React.Fragment key={team.id}>
                                        <Typography variant='h6'>
                                            <Link to={`/teams/${team.id}`}>{team.name}</Link>
                                        </Typography>
                                        <Typography variant='h6'>{team.featureWip}</Typography>
                                    </React.Fragment>
                                ))}
                            </>
                        ) : (
                            <></>
                        )}
                    </Grid>
                    <Grid item xs={12}>
                        <Typography variant='h4'>Features</Typography>
                    </Grid>
                    <Grid item xs={12}>
                        <Typography variant='h4'>Feature Timeline</Typography>
                    </Grid>
                </Grid>
            </LoadingAnimation>
        </Container>
    );
}

export default ProjectDetail;
