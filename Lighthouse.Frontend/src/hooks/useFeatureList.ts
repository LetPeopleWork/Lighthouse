import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import type { FeatureListOwner } from "../models/FeatureList";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

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

	return { ...query, look: lookOf(query) };
};

const lookOf = (query: {
	isError: boolean;
	isFetching: boolean;
}): FeatureListLook => {
	if (query.isError) return "error";
	return query.isFetching ? "loading" : "ready";
};
