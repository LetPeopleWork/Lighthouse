import React, { useState, useEffect } from 'react';
import { ApiServiceProvider } from '../../services/Api/ApiServiceProvider';
import { IApiService } from '../../services/Api/IApiService';
import FilterBar from '../../components/Common/FilterBar/FilterBar';
import ProjectOverview from './ProjectOverview';
import { Project } from '../../models/Project/Project';
import LoadingAnimation from '../../components/Common/LoadingAnimation/LoadingAnimation';
import LighthouseAppOverviewTutorial from '../../components/App/LetPeopleWork/Tutorial/Tutorials/LighthouseAppOverviewTutorial';
import TutorialButton from '../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import { Container, Box } from '@mui/material';

const OverviewDashboard: React.FC = () => {
    const [projects, setProjects] = useState<Project[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [hasError, setHasError] = useState(false);
    const [filterText, setFilterText] = useState('');

    useEffect(() => {
        const fetchData = async () => {
            try {
                const apiService: IApiService = ApiServiceProvider.getApiService();
                const projectData = await apiService.getProjects();
                setProjects(projectData);
                setIsLoading(false);
            } catch (error) {
                console.error('Error fetching project overview data:', error);
                setHasError(true);
            }
        };

        fetchData();
    }, []);

    return (
        <LoadingAnimation isLoading={isLoading} hasError={hasError}>
            <Container>
                <Box display="flex" alignItems="center" mb={2} sx={{ position: 'relative' }}>
                    <Box sx={{ flexGrow: 1 }}>
                        <FilterBar filterText={filterText} onFilterTextChange={setFilterText} />
                    </Box>
                    <Box ml={2}>
                        <TutorialButton
                            tutorialComponent={<LighthouseAppOverviewTutorial />}
                        />
                    </Box>
                </Box>

                {projects.length === 0 ? (
                    <LighthouseAppOverviewTutorial />
                ) : (
                    <ProjectOverview projects={projects} filterText={filterText} />
                )}
            </Container>
        </LoadingAnimation>
    );
};

export default OverviewDashboard;
