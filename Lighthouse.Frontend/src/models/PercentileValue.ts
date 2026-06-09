import { z } from "zod";

export const PercentileValueSchema = z.object({
	percentile: z.number(),
	value: z.number(),
});

export type IPercentileValue = z.infer<typeof PercentileValueSchema>;
