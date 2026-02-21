import type React from "react";
import { useContext } from "react";
import { useNavigate, useParams } from "react-router-dom";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
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
	const { teamService, workTrackingSystemService } =
		useContext(ApiServiceContext);

	const { canCreateTeam, canUpdateTeamData, maxTeamsWithoutPremium } =
		useLicenseRestrictions();

	const canSave = isNewTeam ? canCreateTeam : canUpdateTeamData;

	const premiumToolTip = isNewTeam
		? `Free users can only create up to ${maxTeamsWithoutPremium} teams.`
		: `Free users can only update team data for up to ${maxTeamsWithoutPremium} teams`;

	const saveTooltip = canSave ? "" : premiumToolTip;

	const validateTeamSettings = async (updatedTeamSettings: ITeamSettings) => {
		return teamService.validateTeamSettings(updatedTeamSettings);
	};

	const saveTeamSettings = async (updatedSettings: ITeamSettings) => {
		let newSettings: ITeamSettings;
		if (isNewTeam) {
			newSettings = await teamService.createTeam(updatedSettings);
			await teamService.updateTeamData(newSettings.id);
			navigate(`/teams/${newSettings.id}/settings`);
		} else {
			newSettings = await teamService.updateTeam(updatedSettings);
			navigate(`/teams/${newSettings.id}`);
		}
	};

	const getTeamSettings = async () => {
		const urlParams = new URLSearchParams(globalThis.location.search);
		const cloneFromId = urlParams.get("cloneFrom");

		if (isNewTeam && cloneFromId) {
			const cloneId = Number.parseInt(cloneFromId, 10);
			if (!Number.isNaN(cloneId)) {
				const sourceSettings = await teamService.getTeamSettings(cloneId);
				return {
					...sourceSettings,
					id: 0,
					name: `Copy of ${sourceSettings.name}`,
				};
			}
		}

		if (!isNewTeam && id) {
			return await teamService.getTeamSettings(Number.parseInt(id, 10));
		}

		const defaultTeamSettings: ITeamSettings = {
			id: 0,
			name: "New Team",
			throughputHistory: 90,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 1,
			automaticallyAdjustFeatureWIP: false,
			dataRetrievalValue: "",
			workItemTypes: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
			tags: [],
			workTrackingSystemConnectionId: 0,
			serviceLevelExpectationProbability: 80,
			serviceLevelExpectationRange: 10,
			systemWIPLimit: 5,
			parentOverrideAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: [],
			doneItemsCutoffDays: 180,
			processBehaviourChartBaselineStartDate: null,
			processBehaviourChartBaselineEndDate: null,
			estimationAdditionalFieldDefinitionId: null,
			estimationUnit: null,
		};

		return defaultTeamSettings;
	};

	const getWorkTrackingSystems = async () => {
		const systems =
			await workTrackingSystemService.getConfiguredWorkTrackingSystems();
		return systems;
	};

	return (
		<SnackbarErrorHandler>
			<ModifyTeamSettings
				title={pageTitle}
				getWorkTrackingSystems={getWorkTrackingSystems}
				getTeamSettings={getTeamSettings}
				validateTeamSettings={validateTeamSettings}
				saveTeamSettings={saveTeamSettings}
				disableSave={!canSave}
				saveTooltip={saveTooltip}
			/>
		</SnackbarErrorHandler>
	);
};

export default EditTeamPage;
