import { z } from "zod";

// Backend serialises absent optional strings as JSON null (not omitted), so these
// must accept null; normalise to undefined to keep the `string | undefined` contract.
const optionalString = z
	.string()
	.nullish()
	.transform((value) => value ?? undefined);

export const WhenForecastSchema = z.object({
	probability: z.number(),
	expectedDate: z.coerce.date(),
	filterApplied: z.boolean().optional(),
	excludedSummary: optionalString,
});

export const HowManyForecastSchema = z.object({
	probability: z.number(),
	value: z.number(),
});

export const ManualForecastSchema = z.object({
	remainingItems: z.number(),
	targetDate: z.coerce.date(),
	whenForecasts: z.array(WhenForecastSchema),
	howManyForecasts: z.array(HowManyForecastSchema),
	likelihood: z.number(),
	filterApplied: z.boolean().optional().default(false),
	excludedSummary: optionalString,
	hasSufficientData: z.boolean().optional().default(true),
});

export type ManualForecastResponse = z.infer<typeof ManualForecastSchema>;

export const BacktestResultSchema = z.object({
	startDate: z.coerce.date(),
	endDate: z.coerce.date(),
	historicalStartDate: z.coerce.date(),
	historicalEndDate: z.coerce.date(),
	percentiles: z.array(HowManyForecastSchema),
	actualThroughput: z.number(),
	filterApplied: z.boolean().optional().default(false),
	excludedSummary: optionalString,
});

export type BacktestResultResponse = z.infer<typeof BacktestResultSchema>;
