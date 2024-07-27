import React, {  } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';
import ModifyProjectSettings from '../../../components/Common/ProjectSettings/ModifyProjectSettings';

const EditProjectPage: React.FC = () => {
    const { id } = useParams<{ id?: string }>();
    const isNewProject = id === undefined;

    const navigate = useNavigate();
    const apiService = ApiServiceProvider.getApiService();

    const pageTitle = isNewProject ? "Create Project" : "Update Project";    

    const getProjectSettings = async () => {
        if (!isNewProject && id) {
            return await apiService.getProjectSettings(parseInt(id, 10));
        }
        else{
            return await apiService.getDefaultProjectSettings();
        }
    }

    const getWorkTrackingSystems = async () => {
        const systems = await apiService.getConfiguredWorkTrackingSystems();
        return systems;
    }


    const saveProjectSettings = async (updatedSettings : IProjectSettings) => {
        if (isNewProject) {
            updatedSettings = await apiService.createProject(updatedSettings);
        } else {
            updatedSettings = await apiService.updateProject(updatedSettings);
        }

        navigate(`/projects/${updatedSettings.id}`);
    };

    return (
        <ModifyProjectSettings
            title={pageTitle}
            getProjectSettings={getProjectSettings}
            getWorkTrackingSystems={getWorkTrackingSystems}
            saveProjectSettings={saveProjectSettings} />
    );
};

export default EditProjectPage;