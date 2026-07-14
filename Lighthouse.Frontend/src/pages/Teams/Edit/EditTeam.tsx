import { Alert, Container, Link, Typography } from "@mui/material";
import type React from "react";
import { useContext } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import CreateTeamWizard from "../../../components/Common/CreateWizards/CreateTeamWizard";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useRbacGate } from "../../../hooks/useRbacGate";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

const EditTeamPage: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewTeam = id === undefined;
	const navigate = useNavigate();
	const gate = useRbacGate({ kind: "systemAdmin" });

	const urlParams = new URLSearchParams(globalThis.location.search);
	const hasCloneFrom = urlParams.get("cloneFrom") !== null;
	const useWizard = isNewTeam && !hasCloneFrom;

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const pageTitle = isNewTeam ? `Create ${teamTerm}` : `Update ${teamTerm}`;
	const { teamService, workTrackingSystemService } =
		useContext(ApiServiceContext);

	const { canCreateTeam, canUpdateTeamData } = useLicenseRestrictions();

	const canSave = isNewTeam ? canCreateTeam : canUpdateTeamData;

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
		return newSettings;
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
			featureWIP: 0,
			automaticallyAdjustFeatureWIP: false,
			dataRetrievalValue: "",
			workItemTypes: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
			workTrackingSystemConnectionId: 0,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			parentOverrideAdditionalFieldDefinitionId: null,
			stateMappings: [],
			doneItemsCutoffDays: 365,
			processBehaviourChartBaselineStartDate: null,
			processBehaviourChartBaselineEndDate: null,
			estimationAdditionalFieldDefinitionId: null,
			estimationUnit: null,
			useNonNumericEstimation: false,
			estimationCategoryValues: [],
			stalenessThresholdDays: 0,
			blockedStalenessThresholdDays: 0,
		};

		return defaultTeamSettings;
	};

	const getWorkTrackingSystems = async () => {
		const systems =
			await workTrackingSystemService.getConfiguredWorkTrackingSystems();
		return systems;
	};

	const getConnections = async () => {
		return await workTrackingSystemService.getConfiguredWorkTrackingSystems();
	};

	const wizardSaveTeamSettings = async (updatedSettings: ITeamSettings) => {
		const newSettings = await teamService.createTeam(updatedSettings);
		await teamService.updateTeamData(newSettings.id);
		navigate(`/teams/${newSettings.id}/metrics`);
	};

	if (gate.isLoading) {
		return null;
	}

	if (!gate.allowed) {
		return (
			<Container maxWidth={false}>
				<Alert
					severity="info"
					sx={{ mb: 2 }}
					data-testid="team-edit-no-access-alert"
				>
					You don't have permission to access this page.{" "}
					<Link component={RouterLink} to="/">
						Back to Overview
					</Link>
				</Alert>
			</Container>
		);
	}

	if (useWizard) {
		return (
			<SnackbarErrorHandler>
				<Container maxWidth={false}>
					<Typography variant="h4" sx={{ mb: 2 }}>
						{pageTitle}
					</Typography>
					<CreateTeamWizard
						getConnections={getConnections}
						validateTeamSettings={validateTeamSettings}
						saveTeamSettings={wizardSaveTeamSettings}
						onCancel={() => navigate("/")}
					/>
				</Container>
			</SnackbarErrorHandler>
		);
	}

	return (
		<SnackbarErrorHandler>
			<ModifyTeamSettings
				title={pageTitle}
				getWorkTrackingSystems={getWorkTrackingSystems}
				getTeamSettings={getTeamSettings}
				validateTeamSettings={validateTeamSettings}
				saveTeamSettings={saveTeamSettings}
				disableSave={!canSave}
			/>
		</SnackbarErrorHandler>
	);
};

export default EditTeamPage;
