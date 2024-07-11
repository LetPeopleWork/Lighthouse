import React, { useState, useEffect } from 'react';
import { ApiServiceProvider } from '../../services/Api/ApiServiceProvider';
import { IApiService } from '../../services/Api/IApiService';
import FilterBar from '../../components/Common/FilterBar/FilterBar';
import ProjectOverview from './ProjectOverview';
import { Project } from '../../models/Project';
import LoadingAnimation from '../../components/Common/LoadingAnimation/LoadingAnimation';

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
            <div>
                <FilterBar filterText={filterText} onFilterTextChange={setFilterText} />
                <ProjectOverview projects={projects} filterText={filterText} />
            </div>
        </LoadingAnimation>
    )
};

export default OverviewDashboard;
