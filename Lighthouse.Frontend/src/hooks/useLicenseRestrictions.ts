import { useContext, useEffect, useState } from "react";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

interface LicenseRestrictions {
	canCreateTeam: boolean;
	canUpdateTeamData: boolean;
	canCreatePortfolio: boolean;
	canUpdatePortfolioData: boolean;
	licenseStatus: ILicenseStatus | null;
	maxTeamsWithoutPremium: number;
	maxPortfoliosWithoutPremium: number;
}

const MAX_TEAMS_WITHOUT_PREMIUM = 3;
const MAX_PORTFOLIOS_WITHOUT_PREMIUM = 1;

export const useLicenseRestrictions = (): LicenseRestrictions => {
	const [licenseStatus, setLicenseStatus] = useState<ILicenseStatus | null>(
		null,
	);
	const [teamCount, setTeamCount] = useState<number>(0);
	const [portfolioCount, setPortfolioCount] = useState<number>(0);

	const { licensingService, teamService, portfolioService } =
		useContext(ApiServiceContext);

	useEffect(() => {
		const fetchData = async () => {
			try {
				const [license, teams, portfolios] = await Promise.all([
					licensingService.getLicenseStatus(),
					teamService.getTeams(),
					portfolioService.getPortfolios(),
				]);

				setLicenseStatus(license);
				setTeamCount(teams.length);
				setPortfolioCount(portfolios.length);
			} catch (error) {
				console.error(
					"Failed to fetch license, team, or portfolio data:",
					error,
				);
				setLicenseStatus(null);
				setTeamCount(0);
				setPortfolioCount(0);
			}
		};

		fetchData();
	}, [licensingService, teamService, portfolioService]);

	// Premium users have no restrictions
	if (licenseStatus?.canUsePremiumFeatures) {
		return {
			canCreateTeam: true,
			canUpdateTeamData: true,
			canCreatePortfolio: true,
			canUpdatePortfolioData: true,
			licenseStatus,
			maxTeamsWithoutPremium: MAX_TEAMS_WITHOUT_PREMIUM,
			maxPortfoliosWithoutPremium: MAX_PORTFOLIOS_WITHOUT_PREMIUM,
		};
	}

	// When license status is null (API error), be permissive and allow access
	if (licenseStatus === null) {
		return {
			canCreateTeam: true,
			canUpdateTeamData: true,
			canCreatePortfolio: true,
			canUpdatePortfolioData: true,
			licenseStatus,
			maxTeamsWithoutPremium: MAX_TEAMS_WITHOUT_PREMIUM,
			maxPortfoliosWithoutPremium: MAX_PORTFOLIOS_WITHOUT_PREMIUM,
		};
	}

	// Non-premium users have team and portfolio count restrictions
	const canCreateTeam = teamCount < MAX_TEAMS_WITHOUT_PREMIUM;
	const canUpdateTeamData = teamCount <= MAX_TEAMS_WITHOUT_PREMIUM;

	const canCreatePortfolio = portfolioCount < MAX_PORTFOLIOS_WITHOUT_PREMIUM;
	const canUpdatePortfolioData =
		portfolioCount <= MAX_PORTFOLIOS_WITHOUT_PREMIUM;

	return {
		canCreateTeam,
		canUpdateTeamData,
		canCreatePortfolio: canCreatePortfolio,
		canUpdatePortfolioData: canUpdatePortfolioData,
		licenseStatus,
		maxTeamsWithoutPremium: MAX_TEAMS_WITHOUT_PREMIUM,
		maxPortfoliosWithoutPremium: MAX_PORTFOLIOS_WITHOUT_PREMIUM,
	};
};
