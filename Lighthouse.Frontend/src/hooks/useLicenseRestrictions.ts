import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

/** Whether the license and the counts the create gates read have arrived. */
export type LicenseCheckStatus = "loading" | "ready" | "failed";

interface LicenseRestrictions {
	status: LicenseCheckStatus;
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

/** The one place the license is read for gating: every gate and the header icon share this query. */
export const useLicenseStatusQuery = () => {
	const { licensingService } = useContext(ApiServiceContext);

	return useQuery({
		queryKey: ["licenseStatus"],
		queryFn: () => licensingService.getLicenseStatus(),
		staleTime: 1000 * 60 * 5,
		refetchInterval: 1000 * 60 * 60,
	});
};

export const useLicenseRestrictions = (): LicenseRestrictions => {
	const { teamService, portfolioService } = useContext(ApiServiceContext);

	const license = useLicenseStatusQuery();
	const licenseStatus = license.data ?? null;
	const needsCounts = licenseStatus?.canUsePremiumFeatures === false;

	const teams = useQuery({
		queryKey: ["teams"],
		queryFn: () => teamService.getTeams(),
		enabled: needsCounts,
		staleTime: 0,
	});
	const portfolios = useQuery({
		queryKey: ["portfolios"],
		queryFn: () => portfolioService.getPortfolios(),
		enabled: needsCounts,
		staleTime: 0,
	});

	const limits = {
		licenseStatus,
		maxTeamsWithoutPremium: MAX_TEAMS_WITHOUT_PREMIUM,
		maxPortfoliosWithoutPremium: MAX_PORTFOLIOS_WITHOUT_PREMIUM,
	};

	if (licenseStatus?.canUsePremiumFeatures) {
		return {
			...limits,
			status: "ready",
			canCreateTeam: true,
			canUpdateTeamData: true,
			canCreatePortfolio: true,
			canUpdatePortfolioData: true,
		};
	}

	const unsettled = (status: LicenseCheckStatus): LicenseRestrictions => ({
		...limits,
		status,
		canCreateTeam: false,
		canUpdateTeamData: true,
		canCreatePortfolio: false,
		canUpdatePortfolioData: true,
	});

	if (license.isError || teams.isError || portfolios.isError) {
		return unsettled("failed");
	}

	if (!teams.data || !portfolios.data) {
		return unsettled("loading");
	}

	const teamCount = teams.data.length;
	const portfolioCount = portfolios.data.length;

	return {
		...limits,
		status: "ready",
		canCreateTeam: teamCount < MAX_TEAMS_WITHOUT_PREMIUM,
		canUpdateTeamData: teamCount <= MAX_TEAMS_WITHOUT_PREMIUM,
		canCreatePortfolio: portfolioCount < MAX_PORTFOLIOS_WITHOUT_PREMIUM,
		canUpdatePortfolioData: portfolioCount <= MAX_PORTFOLIOS_WITHOUT_PREMIUM,
	};
};
