import type React from "react";
import { useContext } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

const EditTeamPage: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewTeam = id === undefined;
	const navigate = useNavigate();

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const pageTitle = isNewTeam ? `Create ${teamTerm}` : `Update ${teamTerm}`;
	const { settingsService, teamService, workTrackingSystemService } =
		useContext(ApiServiceContext);

	const validateTeamSettings = async (updatedTeamSettings: ITeamSettings) => {
		return teamService.validateTeamSettings(updatedTeamSettings);
	};

	const saveTeamSettings = async (updatedSettings: ITeamSettings) => {
		let newSettings: ITeamSettings;
		if (isNewTeam) {
			newSettings = await teamService.createTeam(updatedSettings);
			await teamService.updateTeamData(newSettings.id);
		} else {
			newSettings = await teamService.updateTeam(updatedSettings);
		}

		navigate(`/teams/${newSettings.id}`);
	};

	const getTeamSettings = async () => {
		if (!isNewTeam && id) {
			return await teamService.getTeamSettings(Number.parseInt(id, 10));
		}
		return await settingsService.getDefaultTeamSettings();
	};

	const getWorkTrackingSystems = async () => {
		const systems =
			await workTrackingSystemService.getConfiguredWorkTrackingSystems();
		return systems;
	};

	return (
		<ModifyTeamSettings
			title={pageTitle}
			getWorkTrackingSystems={getWorkTrackingSystems}
			getTeamSettings={getTeamSettings}
			validateTeamSettings={validateTeamSettings}
			saveTeamSettings={saveTeamSettings}
		/>
	);
};

export default EditTeamPage;
