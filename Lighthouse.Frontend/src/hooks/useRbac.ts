import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useContext } from "react";
import type { UserAuthorizationSummary } from "../models/Authorization/RbacModels";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { pageFreshQueryKey, usePageFreshQuery } from "./usePageFreshQuery";

const PERMISSIVE_SUMMARY: UserAuthorizationSummary = {
	isRbacEnabled: false,
	isSystemAdmin: true,
	canCreateTeam: true,
	canCreatePortfolio: true,
	adminTeamIds: [],
	adminPortfolioIds: [],
};

export interface RbacState {
	isLoading: boolean;
	/** True when the permissions could not be loaded; every other answer here is then the permissive fallback. */
	loadFailed: boolean;
	/** Asks for the permissions again. */
	retry: () => void;
	isRbacEnabled: boolean;
	isSystemAdmin: boolean;
	/** Currently equivalent to: user is Team Admin on at least one team. */
	canCreateTeam: boolean;
	/** Currently equivalent to: user is Portfolio Admin on at least one portfolio. */
	canCreatePortfolio: boolean;
	/** Returns true if the current user is a Team Admin for the given team (by system-admin or explicit grant). */
	isTeamAdmin: (teamId: number) => boolean;
	/** Returns true if the current user is a Portfolio Admin for the given portfolio (by system-admin or explicit grant). */
	isPortfolioAdmin: (portfolioId: number) => boolean;
	summary: UserAuthorizationSummary;
}

const PERMISSIONS = ["authorization-summary"] as const;

/** Asks the shared permissions again at once, for every component; call it after the user's own change of rights. */
export function useRefreshPermissions(): () => Promise<void> {
	const queryClient = useQueryClient();
	return useCallback(
		() =>
			queryClient.invalidateQueries({
				queryKey: pageFreshQueryKey(PERMISSIONS),
				refetchType: "all",
			}),
		[queryClient],
	);
}

export function useRbac(): RbacState {
	const { rbacService } = useContext(ApiServiceContext);
	const permissions = usePageFreshQuery(PERMISSIONS, () =>
		rbacService.getAuthorizationSummary(),
	);
	const isLoading = permissions.isLoading;
	const loadFailed = permissions.isFailed;
	const retry = permissions.askAgain;
	const summary =
		loadFailed || permissions.answer === undefined
			? PERMISSIVE_SUMMARY
			: permissions.answer;

	const isTeamAdmin = useCallback(
		(teamId: number): boolean => {
			if (!summary.isRbacEnabled) return true;
			if (summary.isSystemAdmin) return true;
			return summary.adminTeamIds?.includes(teamId) ?? false;
		},
		[summary],
	);

	const isPortfolioAdmin = useCallback(
		(portfolioId: number): boolean => {
			if (!summary.isRbacEnabled) return true;
			if (summary.isSystemAdmin) return true;
			return summary.adminPortfolioIds?.includes(portfolioId) ?? false;
		},
		[summary],
	);

	return {
		isLoading,
		loadFailed,
		retry,
		isRbacEnabled: summary.isRbacEnabled,
		isSystemAdmin: summary.isSystemAdmin,
		canCreateTeam: summary.canCreateTeam,
		canCreatePortfolio: summary.canCreatePortfolio,
		isTeamAdmin,
		isPortfolioAdmin,
		summary,
	};
}
