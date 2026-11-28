import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useContext } from "react";
import type { Portfolio } from "../models/Portfolio/Portfolio";
import type { Team } from "../models/Team/Team";
import { ApiError } from "../services/Api/ApiError";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { PageFreshRead } from "./PageFreshRead";
import { pageFreshQueryKey } from "./pageFreshQueryKey";
import { usePageFreshLoader, usePageFreshQuery } from "./usePageFreshQuery";

const TEAMS = ["teams"] as const;
const PORTFOLIOS = ["portfolios"] as const;

interface UnitListOptions {
	/** False keeps the list from being asked for while the component does not need it. */
	enabled?: boolean;
}

const useAskTeams = (): (() => Promise<Team[]>) => {
	const { teamService } = useContext(ApiServiceContext);
	return useCallback(() => teamService.getTeams(), [teamService]);
};

/** The Teams every component on the page shares, asked once per page and once per own change. */
export function useTeamsQuery(
	options?: UnitListOptions,
): PageFreshRead<Team[]> {
	return usePageFreshQuery(TEAMS, useAskTeams(), options);
}

/** The Portfolios every component on the page shares, asked once per page and once per own change. */
export function usePortfoliosQuery(
	options?: UnitListOptions,
): PageFreshRead<Portfolio[]> {
	const { portfolioService } = useContext(ApiServiceContext);
	const ask = useCallback(
		() => portfolioService.getPortfolios(),
		[portfolioService],
	);
	return usePageFreshQuery(PORTFOLIOS, ask, options);
}

/** The shared Teams for a caller that needs them as a promise, such as a settings editor's callback. */
export function useTeamsLoader(): () => Promise<Team[]> {
	return usePageFreshLoader(TEAMS, useAskTeams());
}

/**
 * Asks the Teams and the Portfolios again at once, for every component; call it after the user's own create,
 * rename or delete of a Team or a Portfolio, so the lists show it without a page move.
 */
export function useUnitListsChanged(): () => Promise<void> {
	const queryClient = useQueryClient();
	return useCallback(async () => {
		await Promise.all(
			[TEAMS, PORTFOLIOS].map((list) =>
				queryClient.invalidateQueries({
					queryKey: pageFreshQueryKey(list),
					refetchType: "all",
				}),
			),
		);
	}, [queryClient]);
}

const isRefusal = (failure: unknown): boolean =>
	failure instanceof ApiError && failure.code === 403;

/**
 * The list a reader may count or show: the shared answer, or an empty list when the user may not read the list
 * at all. Undefined means there is nothing to go on yet, or the list could not be loaded.
 */
export const listOf = <T>(read: PageFreshRead<T[]>): T[] | undefined =>
	read.answer ?? (read.isFailed && isRefusal(read.failure) ? [] : undefined);

/** True when the list could not be loaded for a reason other than the user not being allowed to read it. */
export const listCouldNotBeLoaded = <T>(read: PageFreshRead<T[]>): boolean =>
	read.isFailed && !isRefusal(read.failure);
