import { z } from "zod";

export const WhenForecastSchema = z.object({
	probability: z.number(),
	expectedDate: z.coerce.date(),
	filterApplied: z.boolean().optional(),
	excludedSummary: z.string().optional(),
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
	excludedSummary: z.string().optional(),
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
	excludedSummary: z.string().optional(),
});

export type BacktestResultResponse = z.infer<typeof BacktestResultSchema>;
