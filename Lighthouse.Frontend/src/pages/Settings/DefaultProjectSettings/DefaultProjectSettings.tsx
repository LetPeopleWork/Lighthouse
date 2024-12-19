import React, { useContext } from "react";
import ModifyProjectSettings from "../../../components/Common/ProjectSettings/ModifyProjectSettings";
import { IProjectSettings } from "../../../models/Project/ProjectSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const DefaultProjectSettings: React.FC = () => {
    const { settingsService } = useContext(ApiServiceContext);

    const getDefaultProjectSettings = async () => {
        return await settingsService.getDefaultProjectSettings();
    }

    const saveDefaultProjectSettings = async (updatedDefaultSettings: IProjectSettings) => {
        await settingsService.updateDefaultProjectSettings(updatedDefaultSettings);
    }

    const getWorkTrackingSystems = async () => {
        return Promise.resolve([]);
    }

    const getAllTeams = () => {
        return Promise.resolve([]);
    };

    return (
        <ModifyProjectSettings
            title=""
            getProjectSettings={getDefaultProjectSettings}
            saveProjectSettings={saveDefaultProjectSettings}
            getWorkTrackingSystems={getWorkTrackingSystems}
            getAllTeams={getAllTeams}
            validateProjectSettings={() => Promise.resolve(true)}
            modifyDefaultSettings={true}
        />
    )
}

export default DefaultProjectSettings;