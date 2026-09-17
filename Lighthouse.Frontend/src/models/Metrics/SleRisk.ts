import { z } from "zod";

/**
 * One in-flight item's chance of missing its team's target. A null risk means the question has no
 * answer for that item — nothing the team finished ever ran as long as it already has — and the item
 * is still listed, because leaving it out would hide it rather than say nothing about it.
 */
export const SleRiskSchema = z.object({
	referenceId: z.string(),
	risk: z.number().nullable(),
});

export type ISleRisk = z.infer<typeof SleRiskSchema>;
