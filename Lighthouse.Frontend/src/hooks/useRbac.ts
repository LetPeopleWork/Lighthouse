import { useCallback, useContext, useEffect, useState } from "react";
import type { UserAuthorizationSummary } from "../models/Authorization/RbacModels";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

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

export function useRbac(): RbacState {
	const { rbacService } = useContext(ApiServiceContext);
	const [isLoading, setIsLoading] = useState(true);
	const [loadFailed, setLoadFailed] = useState(false);
	const [summary, setSummary] =
		useState<UserAuthorizationSummary>(PERMISSIVE_SUMMARY);

	const retry = useCallback(() => {
		setIsLoading(true);
		setLoadFailed(false);
	}, []);

	useEffect(() => {
		if (!isLoading) return;
		let cancelled = false;

		rbacService
			.getAuthorizationSummary()
			.then((data) => {
				if (!cancelled) {
					setSummary(data);
				}
			})
			.catch(() => {
				if (!cancelled) {
					setSummary(PERMISSIVE_SUMMARY);
					setLoadFailed(true);
				}
			})
			.finally(() => {
				if (!cancelled) {
					setIsLoading(false);
				}
			});

		return () => {
			cancelled = true;
		};
	}, [rbacService, isLoading]);

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
