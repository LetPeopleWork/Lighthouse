import { z } from "zod";

/**
 * Who decides the order this instance forecasts in. An enum rather than a boolean, because
 * "manual sorting on/off" names a switch in the UI, not the thing being decided.
 */
export const FeatureOrderingPolicySchema = z.enum([
	"SourceOrder",
	"ManualOrder",
]);

export type FeatureOrderingPolicy = z.infer<typeof FeatureOrderingPolicySchema>;

export const FeatureOrderingSchema = z.object({
	policy: FeatureOrderingPolicySchema,
});

export interface IFeatureOrdering {
	policy: FeatureOrderingPolicy;
}

/**
 * Why a Feature's move actions are not available. Four of the five are instance- or grid-wide and the
 * client already knows them; `no-write` and `orphan` are the server's verdict and are never re-derived
 * on the client, because the natural client-side expression fails open twice.
 */
export type FeatureMoveBlockReason =
	| "not-premium"
	| "policy-off"
	| "sorted"
	| "no-write"
	| "orphan";

export type FeatureMoveGate =
	| { enabled: true }
	| {
			enabled: false;
			reason: FeatureMoveBlockReason;
			/** Named only when the caller may read them. */
			blockingPortfolios: string[];
	  };

/** Every move gesture, in the one shape the endpoint takes. */
export type FeatureMoveTarget =
	| { beforeFeatureId: number | null }
	| { afterFeatureId: number };

/** What the position column calls itself under each policy. */
export const SOURCE_ORDER_COLUMN_LABEL = "#";

export const MANUAL_ORDER_COLUMN_LABEL = "Manual";
