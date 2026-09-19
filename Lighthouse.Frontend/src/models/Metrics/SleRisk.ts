import { z } from "zod";

/**
 * One in-flight item's chance of missing its team's target. A null risk means the question has no
 * answer for that item, and the item is still listed anyway, because leaving it out would hide it
 * rather than say nothing about it.
 */
export const SleRiskSchema = z.object({
	referenceId: z.string(),
	risk: z.number().nullable(),
	/**
	 * How many finished items this one could be compared against. Zero means nothing the team ever
	 * finished ran this long; anything below the backend's minimum means too little did for a share
	 * of it to be worth showing. Those are different things to tell a reader.
	 */
	comparableItems: z.number(),
});

export type ISleRisk = z.infer<typeof SleRiskSchema>;
