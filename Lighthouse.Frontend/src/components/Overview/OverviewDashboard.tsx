import React, { useEffect, useState } from 'react';
import { ApiServiceProvider } from '../../services/Api/ApiServiceProvider';
import { IApiService } from '../../services/Api/IApiService';
import FilterBar from './FilterBar';
import ProjectOverviewTable from './ProjectOverviewTable';
import { Project } from '../../models/Project';

const OverviewDashboard: React.FC = () => {
    const [projects, setProjects] = useState<Project[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const apiService: IApiService = ApiServiceProvider.getApiService();
                const projectData = await apiService.getProjectOverviewData();
                setProjects(projectData);
                setIsLoading(false);
            } catch (error) {
                console.error('Error fetching project overview data:', error);
                // Handle error state if needed
            }
        };

        fetchData();
    }, []);

    if (isLoading) {
        return <div>Loading...</div>;
    }

    return (
        <div>
            <FilterBar />
            <ProjectOverviewTable projects={projects} />
        </div>
    );
};

export default OverviewDashboard;
