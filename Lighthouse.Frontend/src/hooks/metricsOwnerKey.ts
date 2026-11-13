import type { IFeatureOwner } from "../models/IFeatureOwner";

export type OwnerType = "team" | "portfolio";

/** Whose metrics a request asks for, as every metrics request key names it. */
export type MetricsOwnerKey = {
	readonly ownerId: number;
	readonly ownerUpdatedAt: number;
};

// An update recomputes the owner's metrics, so its moment is in the key: an open dashboard asks
// again once the update is done rather than keeping the answer from before it.
export function metricsOwnerKeyOf(
	owner: Pick<IFeatureOwner, "id" | "lastUpdated">,
): MetricsOwnerKey {
	return {
		ownerId: owner.id,
		ownerUpdatedAt: new Date(owner.lastUpdated).getTime(),
	};
}

/** Whose metrics a request asks for, and whether that owner is a team or a portfolio. */
export type MetricsOwnerRequest = MetricsOwnerKey & {
	readonly ownerType: OwnerType;
};
export type AsOfRequest = MetricsOwnerRequest & { readonly asOf: string };
export type WindowRequest = MetricsOwnerRequest & {
	readonly from: string;
	readonly to: string;
};
