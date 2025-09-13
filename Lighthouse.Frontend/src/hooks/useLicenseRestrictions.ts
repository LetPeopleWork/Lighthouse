import { useContext, useEffect, useState } from "react";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

interface LicenseRestrictions {
	canCreateTeam: boolean;
	canUpdateTeamData: boolean;
	canUpdateTeamSettings: boolean;
	canCreateProject: boolean;
	canUpdateProjectData: boolean;
	canUpdateProjectSettings: boolean;
	canUseNewItemForecaster: boolean;
	canUpdateAllTeamsAndProjects: boolean;
	teamCount: number;
	projectCount: number;
	licenseStatus: ILicenseStatus | null;
	isLoading: boolean;
	createTeamTooltip: string;
	updateTeamDataTooltip: string;
	updateTeamSettingsTooltip: string;
	createProjectTooltip: string;
	updateProjectDataTooltip: string;
	updateProjectSettingsTooltip: string;
	newItemForecasterTooltip: string;
	updateAllTeamsAndProjectsTooltip: string;
}

const MAX_TEAMS_WITHOUT_PREMIUM = 3;
const MAX_PROJECTS_WITHOUT_PREMIUM = 1;

export const useLicenseRestrictions = (): LicenseRestrictions => {
	const [licenseStatus, setLicenseStatus] = useState<ILicenseStatus | null>(
		null,
	);
	const [teamCount, setTeamCount] = useState<number>(0);
	const [projectCount, setProjectCount] = useState<number>(0);
	const [isLoading, setIsLoading] = useState<boolean>(true);

	const { licensingService, teamService, projectService } =
		useContext(ApiServiceContext);

	useEffect(() => {
		const fetchData = async () => {
			try {
				setIsLoading(true);
				const [license, teams, projects] = await Promise.all([
					licensingService.getLicenseStatus(),
					teamService.getTeams(),
					projectService.getProjects(),
				]);

				setLicenseStatus(license);
				setTeamCount(teams.length);
				setProjectCount(projects.length);
			} catch (error) {
				console.error("Failed to fetch license, team, or project data:", error);
				setLicenseStatus(null);
				setTeamCount(0);
				setProjectCount(0);
			} finally {
				setIsLoading(false);
			}
		};

		fetchData();
	}, [licensingService, teamService, projectService]);

	// Premium users have no restrictions
	if (licenseStatus?.canUsePremiumFeatures) {
		return {
			canCreateTeam: true,
			canUpdateTeamData: true,
			canUpdateTeamSettings: true,
			canCreateProject: true,
			canUpdateProjectData: true,
			canUpdateProjectSettings: true,
			canUseNewItemForecaster: true,
			canUpdateAllTeamsAndProjects: true,
			teamCount,
			projectCount,
			licenseStatus,
			isLoading,
			createTeamTooltip: "",
			updateTeamDataTooltip: "",
			updateTeamSettingsTooltip: "",
			createProjectTooltip: "",
			updateProjectDataTooltip: "",
			updateProjectSettingsTooltip: "",
			newItemForecasterTooltip: "",
			updateAllTeamsAndProjectsTooltip: "",
		};
	}

	// When license status is null (API error), be permissive and allow access
	if (licenseStatus === null) {
		return {
			canCreateTeam: true,
			canUpdateTeamData: true,
			canUpdateTeamSettings: true,
			canCreateProject: true,
			canUpdateProjectData: true,
			canUpdateProjectSettings: true,
			canUseNewItemForecaster: true,
			canUpdateAllTeamsAndProjects: true,
			teamCount,
			projectCount,
			licenseStatus,
			isLoading,
			createTeamTooltip: "",
			updateTeamDataTooltip: "",
			updateTeamSettingsTooltip: "",
			createProjectTooltip: "",
			updateProjectDataTooltip: "",
			updateProjectSettingsTooltip: "",
			newItemForecasterTooltip: "",
			updateAllTeamsAndProjectsTooltip: "",
		};
	}

	// Non-premium users have team and project count restrictions
	const canCreateTeam = teamCount < MAX_TEAMS_WITHOUT_PREMIUM;
	const canUpdateTeamData = teamCount <= MAX_TEAMS_WITHOUT_PREMIUM;
	const canUpdateTeamSettings = teamCount <= MAX_TEAMS_WITHOUT_PREMIUM;

	const canCreateProject = projectCount < MAX_PROJECTS_WITHOUT_PREMIUM;
	const canUpdateProjectData = projectCount <= MAX_PROJECTS_WITHOUT_PREMIUM;
	const canUpdateProjectSettings = projectCount <= MAX_PROJECTS_WITHOUT_PREMIUM;

	const createTeamTooltip = canCreateTeam
		? ""
		: `Free users can only create up to ${MAX_TEAMS_WITHOUT_PREMIUM} teams. You currently have ${teamCount} teams. Please obtain a premium license to create more teams.`;

	const updateTeamDataTooltip = canUpdateTeamData
		? ""
		: `Free users can only update team data for up to ${MAX_TEAMS_WITHOUT_PREMIUM} teams. You currently have ${teamCount} teams. Please delete some teams or obtain a premium license.`;

	const updateTeamSettingsTooltip = canUpdateTeamSettings
		? ""
		: `Free users can only update team settings for up to ${MAX_TEAMS_WITHOUT_PREMIUM} teams. You currently have ${teamCount} teams. Please delete some teams or obtain a premium license.`;

	const createProjectTooltip = canCreateProject
		? ""
		: `Free users can only create up to ${MAX_PROJECTS_WITHOUT_PREMIUM} project. You currently have ${projectCount} project${projectCount === 1 ? "" : "s"}. Please obtain a premium license to create more projects.`;

	const projectPlural = projectCount === 1 ? "" : "s";
	const updateProjectDataTooltip = canUpdateProjectData
		? ""
		: `Free users can only update project data for up to ${MAX_PROJECTS_WITHOUT_PREMIUM} project. You currently have ${projectCount} project${projectPlural}. Please delete some projects or obtain a premium license.`;

	const updateProjectSettingsTooltip = canUpdateProjectSettings
		? ""
		: `Free users can only update project settings for up to ${MAX_PROJECTS_WITHOUT_PREMIUM} project. You currently have ${projectCount} project${projectPlural}. Please delete some projects or obtain a premium license.`;

	const newItemForecasterTooltip =
		"This feature requires a premium license. Please obtain a premium license to use new item forecasting.";

	const updateAllTeamsAndProjectsTooltip =
		"This feature requires a premium license. Please obtain a premium license to update all teams and projects.";

	return {
		canCreateTeam,
		canUpdateTeamData,
		canUpdateTeamSettings,
		canCreateProject,
		canUpdateProjectData,
		canUpdateProjectSettings,
		canUseNewItemForecaster: false,
		canUpdateAllTeamsAndProjects: false,
		teamCount,
		projectCount,
		licenseStatus,
		isLoading,
		createTeamTooltip,
		updateTeamDataTooltip,
		updateTeamSettingsTooltip,
		createProjectTooltip,
		updateProjectDataTooltip,
		updateProjectSettingsTooltip,
		newItemForecasterTooltip,
		updateAllTeamsAndProjectsTooltip,
	};
};
