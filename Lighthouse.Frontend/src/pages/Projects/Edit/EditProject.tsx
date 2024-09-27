import React, { useContext } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';
import ModifyProjectSettings from '../../../components/Common/ProjectSettings/ModifyProjectSettings';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';

const EditProject: React.FC = () => {
    const { id } = useParams<{ id?: string }>();
    const isNewProject = id === undefined;

    const navigate = useNavigate();
    const { settingsService, projectService, workTrackingSystemService } = useContext(ApiServiceContext);

    const pageTitle = isNewProject ? "Create Project" : "Update Project";    

    const getProjectSettings = async () => {
        if (!isNewProject && id) {
            return await projectService.getProjectSettings(parseInt(id, 10));
        }
        else{
            return await settingsService.getDefaultProjectSettings();
        }
    }

    const getWorkTrackingSystems = async () => {
        const systems = await workTrackingSystemService.getConfiguredWorkTrackingSystems();
        return systems;
    }


    const saveProjectSettings = async (updatedSettings : IProjectSettings) => {
        if (isNewProject) {
            updatedSettings = await projectService.createProject(updatedSettings);
        } else {
            updatedSettings = await projectService.updateProject(updatedSettings);
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

export default EditProject;