import React, { useState, useEffect, useContext } from 'react';
import FilterBar from '../../components/Common/FilterBar/FilterBar';
import ProjectOverview from './ProjectOverview';
import { Project } from '../../models/Project/Project';
import LoadingAnimation from '../../components/Common/LoadingAnimation/LoadingAnimation';
import LighthouseAppOverviewTutorial from '../../components/App/LetPeopleWork/Tutorial/Tutorials/LighthouseAppOverviewTutorial';
import TutorialButton from '../../components/App/LetPeopleWork/Tutorial/TutorialButton';
import { Container } from '@mui/material';
import { ApiServiceContext } from '../../services/Api/ApiServiceContext';

const OverviewDashboard: React.FC = () => {
    const [projects, setProjects] = useState<Project[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [hasError, setHasError] = useState(false);
    const [filterText, setFilterText] = useState('');

    const { projectService } = useContext(ApiServiceContext);

    useEffect(() => {
        const fetchData = async () => {
            try {                
                const projectData = await projectService.getProjects();
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
            <Container maxWidth={false}>
                <FilterBar filterText={filterText} onFilterTextChange={setFilterText} />

                {projects.length === 0 ? (
                    <LighthouseAppOverviewTutorial />
                ) : (
                    <ProjectOverview projects={projects} filterText={filterText} />
                )}
            </Container>


            <TutorialButton
                tutorialComponent={<LighthouseAppOverviewTutorial />}
            />
        </LoadingAnimation>
    );
};

export default OverviewDashboard;
