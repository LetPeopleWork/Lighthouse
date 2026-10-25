import { QueryClientContext, useQuery } from "@tanstack/react-query";
import { useCallback, useContext, useMemo } from "react";
import type { FeatureListOwner } from "../models/FeatureList";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { ParentWorkItem } from "./useParentWorkItems";

export type FeatureListLook = "loading" | "ready" | "error";

/** Keyed by owner, so an answer for an owner the reader has left lands under its own key and is never read. */
export const featureListKey = (owner: FeatureListOwner) =>
	["feature-list", owner.kind, owner.id] as const;

/** Everything one Features tab shows, read once. */
export const useFeatureList = (owner: FeatureListOwner) => {
	const { featureService } = useContext(ApiServiceContext);

	const query = useQuery({
		queryKey: featureListKey(owner),
		queryFn: () => featureService.getFeatureList(owner),
	});

	const parentsByReferenceId = useMemo(
		() =>
			new Map<string, ParentWorkItem>(
				(query.data?.parents ?? []).map((parent) => [
					parent.referenceId,
					parent,
				]),
			),
		[query.data],
	);

	return {
		...query,
		look: lookOf(query.isError, query.isPending || query.isFetching),
		parentsByReferenceId,
	};
};

const lookOf = (isError: boolean, isLoading: boolean): FeatureListLook => {
	if (isError) return "error";
	return isLoading ? "loading" : "ready";
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
