import { z } from "zod";

/**
 * One in-flight item's chance of missing its team's target. An item with no answer is absent from
 * the collection rather than present with a null - being listed at all is what says the item is in
 * flight today on a team that has published a target.
 */
export const SleRiskSchema = z.object({
	referenceId: z.string(),
	risk: z.number().nullable(),
});

export type ISleRisk = z.infer<typeof SleRiskSchema>;
