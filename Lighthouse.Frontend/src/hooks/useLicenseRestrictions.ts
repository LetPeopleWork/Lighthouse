import { useContext, useEffect, useState } from "react";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

interface LicenseRestrictions {
	canCreateTeam: boolean;
	canUpdateTeamData: boolean;
	canUpdateTeamSettings: boolean;
	teamCount: number;
	licenseStatus: ILicenseStatus | null;
	isLoading: boolean;
	createTeamTooltip: string;
	updateTeamDataTooltip: string;
	updateTeamSettingsTooltip: string;
}

const MAX_TEAMS_WITHOUT_PREMIUM = 3;

export const useLicenseRestrictions = (): LicenseRestrictions => {
	const [licenseStatus, setLicenseStatus] = useState<ILicenseStatus | null>(
		null,
	);
	const [teamCount, setTeamCount] = useState<number>(0);
	const [isLoading, setIsLoading] = useState<boolean>(true);

	const { licensingService, teamService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchData = async () => {
			try {
				setIsLoading(true);
				const [license, teams] = await Promise.all([
					licensingService.getLicenseStatus(),
					teamService.getTeams(),
				]);

				setLicenseStatus(license);
				setTeamCount(teams.length);
			} catch (error) {
				console.error("Failed to fetch license or team data:", error);
				setLicenseStatus(null);
				setTeamCount(0);
			} finally {
				setIsLoading(false);
			}
		};

		fetchData();
	}, [licensingService, teamService]);

	// Premium users have no restrictions
	if (licenseStatus?.canUsePremiumFeatures) {
		return {
			canCreateTeam: true,
			canUpdateTeamData: true,
			canUpdateTeamSettings: true,
			teamCount,
			licenseStatus,
			isLoading,
			createTeamTooltip: "",
			updateTeamDataTooltip: "",
			updateTeamSettingsTooltip: "",
		};
	}

	// Non-premium users have team count restrictions
	const canCreateTeam = teamCount < MAX_TEAMS_WITHOUT_PREMIUM;
	const canUpdateTeamData = teamCount <= MAX_TEAMS_WITHOUT_PREMIUM;
	const canUpdateTeamSettings = teamCount <= MAX_TEAMS_WITHOUT_PREMIUM;

	const createTeamTooltip = canCreateTeam
		? ""
		: `Free users can only create up to ${MAX_TEAMS_WITHOUT_PREMIUM} teams. You currently have ${teamCount} teams. Please obtain a premium license to create more teams.`;

	const updateTeamDataTooltip = canUpdateTeamData
		? ""
		: `Free users can only update team data for up to ${MAX_TEAMS_WITHOUT_PREMIUM} teams. You currently have ${teamCount} teams. Please delete some teams or obtain a premium license.`;

	const updateTeamSettingsTooltip = canUpdateTeamSettings
		? ""
		: `Free users can only update team settings for up to ${MAX_TEAMS_WITHOUT_PREMIUM} teams. You currently have ${teamCount} teams. Please delete some teams or obtain a premium license.`;

	return {
		canCreateTeam,
		canUpdateTeamData,
		canUpdateTeamSettings,
		teamCount,
		licenseStatus,
		isLoading,
		createTeamTooltip,
		updateTeamDataTooltip,
		updateTeamSettingsTooltip,
	};
};
