import React, { useContext } from "react";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import { ITeamSettings } from "../../../models/Team/TeamSettings";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

const DefaultTeamSettings: React.FC = () => {
    const { settingsService } = useContext(ApiServiceContext);

    const getDefaultTeamSettings = async () => {
        return await settingsService.getDefaultTeamSettings();
    }

    const saveDefaultTeamSettings = async (updatedDefaultSettings: ITeamSettings) => {
        await settingsService.updateDefaultTeamSettings(updatedDefaultSettings);
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
            validateTeamSettings={() => Promise.resolve(true)}
            modifyDefaultSettings={true}
        />
    )
}

export default DefaultTeamSettings;