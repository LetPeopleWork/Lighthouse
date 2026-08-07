import { z } from "zod";

/**
 * Who decides the order this instance forecasts in. An enum rather than a boolean, because
 * "manual sorting on/off" names a switch in the UI, not the thing being decided (ADR-132).
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

// __SCAFFOLD__ (Epic 5375 slice 03) — types only. Nothing here is wired through the zod boundary yet;
// wiring it would make part of the slice green from DISTILL.

/**
 * Why a Feature's move actions are not available. Four of the five are instance- or grid-wide and the
 * client already knows them; `no-write` and `orphan` are the server's verdict and are never re-derived
 * on the client (ADR-136 SA-10 — the natural client-side expression fails open twice).
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
			/** Named only when the caller may read them (ADR-136 §3 / SA-9). */
			blockingPortfolios: string[];
	  };

/** The one gesture, in the one shape the endpoint takes (D18 / DDD-7). */
export type FeatureMoveTarget =
	| { beforeFeatureId: number | null }
	| { afterFeatureId: number };

/** What the position column calls itself under each policy (AC-2.x, AC-5.4). */
export const SOURCE_ORDER_COLUMN_LABEL = "#";

export const MANUAL_ORDER_COLUMN_LABEL = "Manual";
