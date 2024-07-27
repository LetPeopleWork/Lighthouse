import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { ITeamSettings } from '../../../models/Team/TeamSettings';
import ModifyTeamSettings from '../../../components/Common/Team/ModifyTeamSettings';

const EditTeamPage: React.FC = () => {
    const { id } = useParams<{ id?: string }>();
    const isNewTeam = id === undefined;

    const pageTitle = isNewTeam ? "Create Team" : "Update Team";

    const navigate = useNavigate();
    const apiService = ApiServiceProvider.getApiService();

    const saveTeamSettings = async (updatedSettings: ITeamSettings) => {
        if (isNewTeam) {
            updatedSettings = await apiService.createTeam(updatedSettings);
        } else {
            updatedSettings = await apiService.updateTeam(updatedSettings);
        }

        navigate(`/teams/${updatedSettings.id}`);
    };

    const getTeamSettings = async () => {
        if (!isNewTeam && id) {
            return await apiService.getTeamSettings(parseInt(id, 10));
        }
        else{
            return await apiService.getDefaultTeamSettings();
        }
    }

    const getWorkTrackingSystems = async () => {
        const systems = await apiService.getConfiguredWorkTrackingSystems();
        return systems;
    }


    return (
        <ModifyTeamSettings
            title={pageTitle}
            getWorkTrackingSystems={getWorkTrackingSystems}
            getTeamSettings={getTeamSettings} 
            saveTeamSettings={saveTeamSettings}/>
    );
};

export default EditTeamPage;