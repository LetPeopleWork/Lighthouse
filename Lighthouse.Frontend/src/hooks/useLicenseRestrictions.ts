import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useContext } from "react";
import type { ILicenseStatus } from "../models/ILicenseStatus";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { PageMovesContext } from "./PageMovesContext";
import { recordAskOnPageFor } from "./pageOfTheLatestAsk";
import { listOf, usePortfoliosQuery, useTeamsQuery } from "./useUnitLists";

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

export const LICENSE_STATUS_KEY = ["licenseStatus"] as const;

/**
 * The one place the license is read for gating: every gate, the header icon and the survey nudge share this
 * query. The page-move refresh asks it again like the other shared reads, which is why each ask records its page.
 */
export const useLicenseStatusQuery = () => {
	const { licensingService } = useContext(ApiServiceContext);
	const page = useContext(PageMovesContext);

	return useQuery({
		queryKey: LICENSE_STATUS_KEY,
		queryFn: (context) => {
			recordAskOnPageFor(context, page);
			return licensingService.getLicenseStatus();
		},
		staleTime: 1000 * 60 * 5,
		refetchInterval: 1000 * 60 * 60,
	});
};

/** Why the license cannot be relied on yet: while it is unread, Premium is neither granted nor refused. */
export type UnreadLicense = "loading" | "failed";

/** Undefined once the license has been read. */
export const unreadLicenseOf = (
	license: ReturnType<typeof useLicenseStatusQuery>,
): UnreadLicense | undefined => {
	if (license.isError || license.data === null) return "failed";
	if (license.data === undefined) return "loading";
	return undefined;
};

export const LICENSE_UNREADABLE_MESSAGE = "Could not load the license.";

/** Shows an imported license at once wherever the license is read, then asks the server for the status it keeps. */
export const useStoreImportedLicense = (): ((
	imported: ILicenseStatus,
) => void) => {
	const queryClient = useQueryClient();
	return useCallback(
		(imported: ILicenseStatus) => {
			queryClient.setQueryData(LICENSE_STATUS_KEY, imported);
			queryClient
				.invalidateQueries({ queryKey: LICENSE_STATUS_KEY })
				.catch((error) => {
					console.error("Error refreshing the license status:", error);
				});
		},
		[queryClient],
	);
};

export const useLicenseRestrictions = (): LicenseRestrictions => {
	const license = useLicenseStatusQuery();
	const licenseStatus = license.data ?? null;
	const needsCounts = licenseStatus?.canUsePremiumFeatures === false;

	const teams = useTeamsQuery({ enabled: needsCounts });
	const portfolios = usePortfoliosQuery({ enabled: needsCounts });

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

	if (license.isError && !licenseStatus) {
		return unsettled("failed");
	}

	const teamList = listOf(teams);
	const portfolioList = listOf(portfolios);

	if (!teamList || !portfolioList) {
		return unsettled(
			teams.isFailed || portfolios.isFailed ? "failed" : "loading",
		);
	}

	const teamCount = teamList.length;
	const portfolioCount = portfolioList.length;

	return {
		...limits,
		status: "ready",
		canCreateTeam: teamCount < MAX_TEAMS_WITHOUT_PREMIUM,
		canUpdateTeamData: teamCount <= MAX_TEAMS_WITHOUT_PREMIUM,
		canCreatePortfolio: portfolioCount < MAX_PORTFOLIOS_WITHOUT_PREMIUM,
		canUpdatePortfolioData: portfolioCount <= MAX_PORTFOLIOS_WITHOUT_PREMIUM,
	};
};
