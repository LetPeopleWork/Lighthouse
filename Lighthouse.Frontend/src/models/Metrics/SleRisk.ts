import { z } from "zod";

/**
 * One in-flight item's chance of missing its team's target. An item with no answer is absent from
 * the collection rather than present with a null - being listed at all is what says the item is in
 * flight today on a team that has published a target.
 */
export const SleRiskSchema = z.object({
	referenceId: z.string(),
	risk: z.number(),
	/**
	 * How much of the team's finished work was still open at this item's age. It qualifies the risk
	 * without explaining it: a share over two finished items and one over forty read identically
	 * otherwise, and the first moves by fifty points when one more item closes.
	 */
	finishedItemsStillOpenAtThisAge: z.number(),
});

export type ISleRisk = z.infer<typeof SleRiskSchema>;
