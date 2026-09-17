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

/**
 * One band of the aging chart's risk background: the ages over which an item's chance of missing the
 * target is at least `risk`. A band the history could not place is absent rather than guessed.
 */
export const SleRiskZoneSchema = z.object({
	risk: z.number(),
	fromAge: z.number(),
	/**
	 * Where the band stops. `null` means it never does, which is only ever true of certainty: past
	 * the target every item that ran that long had already missed, whatever the history says. Any
	 * other band ends where the evidence does, and nothing is painted above it.
	 */
	toAge: z.number().nullable(),
});

export type ISleRiskZone = z.infer<typeof SleRiskZoneSchema>;
