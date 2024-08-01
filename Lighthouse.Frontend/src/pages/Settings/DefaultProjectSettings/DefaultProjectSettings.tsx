import React from "react";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import ModifyProjectSettings from "../../../components/Common/ProjectSettings/ModifyProjectSettings";
import { IProjectSettings } from "../../../models/Project/ProjectSettings";

const DefaultProjectSettings: React.FC = () => {
    const apiService = ApiServiceProvider.getApiService();

    const getDefaultProjectSettings = async () => {
        return await apiService.getDefaultProjectSettings();
    }

    const saveDefaultProjectSettings = async (updatedDefaultSettings: IProjectSettings) => {
        await apiService.updateDefaultProjectSettings(updatedDefaultSettings);
    }

    const getWorkTrackingSystems = async () => {
        return Promise.resolve([]);
    }

    return (
        <ModifyProjectSettings
            title=""
            getProjectSettings={getDefaultProjectSettings}
            saveProjectSettings={saveDefaultProjectSettings}
            getWorkTrackingSystems={getWorkTrackingSystems}
        />
    )
}

export default DefaultProjectSettings;