import React, { useContext } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ITeamSettings } from '../../../models/Team/TeamSettings';
import ModifyTeamSettings from '../../../components/Common/Team/ModifyTeamSettings';
import { ApiServiceContext } from '../../../services/Api/ApiServiceContext';

const EditTeamPage: React.FC = () => {
    const { id } = useParams<{ id?: string }>();
    const isNewTeam = id === undefined;
    const navigate = useNavigate();
    
    const pageTitle = isNewTeam ? "Create Team" : "Update Team";
    const { settingsService, teamService, workTrackingSystemService } = useContext(ApiServiceContext);

    const validateTeamSettings = async (updatedTeamSettings: ITeamSettings) => {
        return teamService.validateTeamSettings(updatedTeamSettings);
    }

    const saveTeamSettings = async (updatedSettings: ITeamSettings) => {
        if (isNewTeam) {
            updatedSettings = await teamService.createTeam(updatedSettings);
        } else {
            updatedSettings = await teamService.updateTeam(updatedSettings);
        }

        await teamService.updateTeamData(updatedSettings.id);
        navigate(`/teams/${updatedSettings.id}`);
    };

    const getTeamSettings = async () => {
        if (!isNewTeam && id) {
            return await teamService.getTeamSettings(parseInt(id, 10));
        }
        else{
            return await settingsService.getDefaultTeamSettings();
        }
    }

    const getWorkTrackingSystems = async () => {
        const systems = await workTrackingSystemService.getConfiguredWorkTrackingSystems();
        return systems;
    }


    return (
        <ModifyTeamSettings
            title={pageTitle}
            getWorkTrackingSystems={getWorkTrackingSystems}
            getTeamSettings={getTeamSettings} 
            validateTeamSettings={validateTeamSettings}
            saveTeamSettings={saveTeamSettings}/>
    );
};

export default EditTeamPage;