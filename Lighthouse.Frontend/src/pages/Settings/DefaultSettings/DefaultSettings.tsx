import React from "react";
import { ApiServiceProvider } from "../../../services/Api/ApiServiceProvider";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import { ITeamSettings } from "../../../models/Team/TeamSettings";

const DefaultSettings: React.FC = () => {
    const apiService = ApiServiceProvider.getApiService();

    const getDefaultTeamSettings = async () => {
        return await apiService.getDefaultTeamSettings();
    }

    const saveDefaultTeamSettings = async (updatedDefaultSettings : ITeamSettings) => {
        await apiService.updateDefaultTeamSettings(updatedDefaultSettings);
    }

    const getWorkTrackingSystems = async () => {
        return Promise.resolve([]);
    }

    return (
        <InputGroup title={'Default Team Settings'}>
            <ModifyTeamSettings
                title=""
                getTeamSettings={getDefaultTeamSettings}
                saveTeamSettings={saveDefaultTeamSettings}
                getWorkTrackingSystems={getWorkTrackingSystems}
                />
        </InputGroup>
    )
}

export default DefaultSettings;