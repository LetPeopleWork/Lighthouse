import { Alert, Container, Link, Typography } from "@mui/material";
import type React from "react";
import { useContext } from "react";
import { Link as RouterLink, useNavigate, useParams } from "react-router";
import CreateTeamWizard from "../../../components/Common/CreateWizards/CreateTeamWizard";
import SnackbarErrorHandler from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import ModifyTeamSettings from "../../../components/Common/Team/ModifyTeamSettings";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useRbacGate } from "../../../hooks/useRbacGate";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { reportFailedNavigation } from "../../../utils/navigation";

const EditTeamPage: React.FC = () => {
	const { id } = useParams<{ id?: string }>();
	const isNewTeam = id === undefined;
	const navigate = useNavigate();
	const gate = useRbacGate({ kind: "systemAdmin" });
	const reportUsage = useUsageDataReporter();

	const cloneFromId = Number.parseInt(
		new URLSearchParams(globalThis.location.search).get("cloneFrom") ?? "",
		10,
	);
	const hasCloneSource = !Number.isNaN(cloneFromId);
	const useWizard = isNewTeam && !hasCloneSource;

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
			reportUsage({ name: UsageDataEventName.TeamCreated });
			await teamService.updateTeamData(newSettings.id);
			reportFailedNavigation(navigate(`/teams/${newSettings.id}/settings`));
		} else {
			newSettings = await teamService.updateTeam(updatedSettings);
			reportFailedNavigation(navigate(`/teams/${newSettings.id}`));
		}
		return newSettings;
	};

	const getTeamSettings = async (): Promise<ITeamSettings> => {
		if (id !== undefined) {
			return await teamService.getTeamSettings(Number.parseInt(id, 10));
		}

		const sourceSettings = await teamService.getTeamSettings(cloneFromId);
		return {
			...sourceSettings,
			id: 0,
			name: `Copy of ${sourceSettings.name}`,
		};
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
		reportUsage({ name: UsageDataEventName.TeamCreated });
		await teamService.updateTeamData(newSettings.id);
		reportFailedNavigation(navigate(`/teams/${newSettings.id}/metrics`));
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
