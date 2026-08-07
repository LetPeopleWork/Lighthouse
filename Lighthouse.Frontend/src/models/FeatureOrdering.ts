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

/** What the position column calls itself under each policy (AC-2.x, AC-5.4). */
export const SOURCE_ORDER_COLUMN_LABEL = "#";

export const MANUAL_ORDER_COLUMN_LABEL = "Manual";
