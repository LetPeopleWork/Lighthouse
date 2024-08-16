import React from "react";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import { ITeamSettings } from "../../../models/Team/TeamSettings";

const DefaultTeamSettings: React.FC = () => {
    const apiService = ApiServiceProvider.getApiService();

    const getDefaultTeamSettings = async () => {
        return await apiService.getDefaultTeamSettings();
    }

    const saveDefaultTeamSettings = async (updatedDefaultSettings: ITeamSettings) => {
        await apiService.updateDefaultTeamSettings(updatedDefaultSettings);
    }

    const getWorkTrackingSystems = async () => {
        return Promise.resolve([]);
    }

    return (
        <ModifyTeamSettings
            title=""
            getTeamSettings={getDefaultTeamSettings}
            saveTeamSettings={saveDefaultTeamSettings}
            getWorkTrackingSystems={getWorkTrackingSystems}
            modifyDefaultSettings={true}
        />
    )
}

export default DefaultTeamSettings;