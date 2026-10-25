import {
	QueryClientContext,
	useQuery,
	useQueryClient,
} from "@tanstack/react-query";
import { useCallback, useContext, useSyncExternalStore } from "react";
import type { FeatureListOwner } from "../models/FeatureList";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

export type FeatureListLook = "loading" | "ready" | "error";

/** Keyed by owner, so an answer for an owner the reader has left lands under its own key and is never read. */
export const featureListKey = (owner: FeatureListOwner) =>
	["feature-list", owner.kind, owner.id] as const;

/** Everything one Features tab shows, read once. */
export const useFeatureList = (owner: FeatureListOwner) => {
	const { featureService } = useContext(ApiServiceContext);

	const queryClient = useQueryClient();
	const queryKey = featureListKey(owner);

	const query = useQuery({
		queryKey,
		queryFn: () => featureService.getFeatureList(owner),
	});

	// Read from the cache itself, which tells its listeners at once, rather than through the query's
	// observer, which tells them a tick later: a refresh must dim the rows the moment it starts.
	const isFetching = useSyncExternalStore(
		(onChange) => queryClient.getQueryCache().subscribe(onChange),
		() => queryClient.isFetching({ queryKey }) > 0,
	);

	return { ...query, look: lookOf(query.isError, isFetching) };
};

const lookOf = (isError: boolean, isFetching: boolean): FeatureListLook => {
	if (isError) return "error";
	return isFetching ? "loading" : "ready";
};

/**
 * Makes an owner's Features list ask again. A page shown without a query client has no list to refresh,
 * so there it does nothing.
 */
export const useRefreshFeatureList = (owner: FeatureListOwner) => {
	const queryClient = useContext(QueryClientContext);
	const { kind, id } = owner;

	return useCallback(() => {
		void queryClient?.invalidateQueries({
			queryKey: featureListKey({ kind, id }),
		});
	}, [queryClient, kind, id]);
};
