import React, { useContext } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';
import ModifyProjectSettings from '../../../components/Common/ProjectSettings/ModifyProjectSettings';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';

const EditProject: React.FC = () => {
    const { id } = useParams<{ id?: string }>();
    const isNewProject = id === undefined;

    const navigate = useNavigate();
    const { settingsService, projectService, workTrackingSystemService, teamService } = useContext(ApiServiceContext);

    const pageTitle = isNewProject ? "Create Project" : "Update Project";    

    const getProjectSettings = async () => {
        if (!isNewProject && id) {
            return await projectService.getProjectSettings(parseInt(id, 10));
        } else {
            return await settingsService.getDefaultProjectSettings();
        }
    };

    const getWorkTrackingSystems = async () => {
        return await workTrackingSystemService.getConfiguredWorkTrackingSystems();
    };

    const getAllTeams = async () => {
        return await teamService.getTeams();
    };

    const validateProjectSettings = async (updatedProjectSettings: IProjectSettings) => {
        return await projectService.validateProjectSettings(updatedProjectSettings);
    }

    const saveProjectSettings = async (updatedSettings: IProjectSettings) => {
        if (isNewProject) {
            updatedSettings = await projectService.createProject(updatedSettings);
        } else {
            updatedSettings = await projectService.updateProject(updatedSettings);
        }

        navigate(`/projects/${updatedSettings.id}?triggerUpdate=true`);
    };

    return (
        <ModifyProjectSettings
            title={pageTitle}
            getProjectSettings={getProjectSettings}
            getWorkTrackingSystems={getWorkTrackingSystems}
            getAllTeams={getAllTeams}
            validateProjectSettings={validateProjectSettings}
            saveProjectSettings={saveProjectSettings} />
    );
};

export default EditProject;