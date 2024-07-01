import React, { useState, useEffect } from 'react';
import { ApiServiceProvider } from '../../services/Api/ApiServiceProvider';
import { IApiService } from '../../services/Api/IApiService';
import FilterBar from './FilterBar';
import ProjectOverviewTable from './ProjectOverviewTable';
import { Project } from '../../models/Project';
import LoadingAnimation from '../Common/LoadingAnimation/LoadingAnimation';

const OverviewDashboard: React.FC = () => {
    const [projects, setProjects] = useState<Project[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [hasError, setHasError] = useState(false);
    const [filterText, setFilterText] = useState('');

    useEffect(() => {
        const fetchData = async () => {
            try {
                const apiService: IApiService = ApiServiceProvider.getApiService();
                const projectData = await apiService.getProjectOverviewData();
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
                <ProjectOverviewTable projects={projects} filterText={filterText} />
            </div>
        </LoadingAnimation>
    )
};

export default OverviewDashboard;
