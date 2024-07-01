import React, { useState } from 'react';
import { ApiServiceProvider } from '../../services/Api/ApiServiceProvider';
import { IApiService } from '../../services/Api/IApiService';
import FilterBar from './FilterBar';
import ProjectOverviewTable from './ProjectOverviewTable';
import { Project } from '../../models/Project';
import LoadingAnimation from '../Common/LoadingAnimation/LoadingAnimation';

const OverviewDashboard: React.FC = () => {
    const [projects, setProjects] = useState<Project[]>([]);
    const [filterText, setFilterText] = useState('');    

    const fetchProjectData = async () => {
        const apiService: IApiService = ApiServiceProvider.getApiService();
        const projectData = await apiService.getProjectOverviewData();
        setProjects(projectData);
    };

    return (
        <LoadingAnimation asyncFunction={fetchProjectData}>
            <div>
                <FilterBar filterText={filterText} onFilterTextChange={setFilterText} />
                <ProjectOverviewTable projects={projects} filterText={filterText} />
            </div>
        </LoadingAnimation>
    );
};

export default OverviewDashboard;
