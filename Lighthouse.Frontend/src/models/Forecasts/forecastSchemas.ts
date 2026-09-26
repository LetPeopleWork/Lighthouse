import { z } from "zod";
import {
	CELL_OUTCOMES,
	DETERMINATIONS,
	LEVEL_READINGS,
	NOT_TESTED_REASONS,
	STANDINGS,
	SUFFICIENCY_REASONS,
} from "./RealityCheckResult";

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
	likelihood: z.number().nullable(),
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

const RealityCheckCellSchema = z.object({
	horizonDays: z.number(),
	samplingWindowDays: z.number(),
	scoredPeriodStart: z.string(),
	scoredPeriodEnd: z.string(),
	historyWindowStart: z.string(),
	historyWindowEnd: z.string(),
	sufficiency: z.object({
		isSufficient: z.boolean(),
		reason: z.enum(SUFFICIENCY_REASONS),
		daysWithCompletedWork: z.number(),
	}),
	forecast: z.array(HowManyForecastSchema).nullable(),
	actualCompleted: z.number().nullable(),
	outcome: z.enum(CELL_OUTCOMES).nullable(),
	levelOutcomes: z
		.array(
			z.object({
				confidenceLevel: z.number(),
				forecastValue: z.number(),
				held: z.boolean(),
			}),
		)
		.nullable(),
});

export const RealityCheckResultSchema = z.object({
	teamId: z.number(),
	teamName: z.string(),
	anchorDate: z.string(),
	standardWindowDays: z.array(z.number()),
	sampledWindowDays: z.array(z.number()),
	sampledHorizonDays: z.array(z.number()),
	confidenceLevels: z.array(z.number()),
	filterApplied: z.boolean(),
	excludedSummary: z.string().nullable(),
	minimumActiveDays: z.number(),
	denominator: z.object({
		runsAttempted: z.number(),
		runsEvaluated: z.number(),
		levelsPerRun: z.number(),
		scoresEvaluated: z.number(),
	}),
	soundWindow: z.object({
		soundWindowDays: z.array(z.number()),
		unevaluatedWindowDays: z.array(z.number()),
		determination: z.enum(DETERMINATIONS),
		currentSettingDays: z.number(),
		currentSettingWasTested: z.boolean(),
		currentSettingStanding: z.enum(STANDINGS),
		currentSettingNotTestedReason: z.enum(NOT_TESTED_REASONS).nullable(),
	}),
	levelCoverage: z.array(
		z.object({
			confidenceLevel: z.number(),
			heldCount: z.number(),
			expectedHeldCount: z.number(),
			reading: z.enum(LEVEL_READINGS),
		}),
	),
	cells: z.array(RealityCheckCellSchema),
});
