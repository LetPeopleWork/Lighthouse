export const DETERMINATIONS = [
	"AllWindowsAlike",
	"SomeWindowsSound",
	"NoWindowSound",
	"NotEnoughEvidence",
] as const;

export const STANDINGS = [
	"Inside",
	"Outside",
	"NotDetermined",
	"NotTested",
] as const;

export const NOT_TESTED_REASONS = [
	"UsesFixedDates",
	"NotAPositiveLength",
] as const;

export const SUFFICIENCY_REASONS = [
	"Sufficient",
	"TooFewActiveDays",
	"DegenerateForecast",
] as const;

export const CELL_OUTCOMES = [
	"OverForecast",
	"WithinBand",
	"UnderForecast",
] as const;

export const LEVEL_READINGS = [
	"SometimesHeld",
	"NeverHeld",
	"AlwaysHeld",
	"NotEvaluated",
] as const;

export type Determination = (typeof DETERMINATIONS)[number];

export type Standing = (typeof STANDINGS)[number];

export type NotTestedReason = (typeof NOT_TESTED_REASONS)[number];

export type SufficiencyReason = (typeof SUFFICIENCY_REASONS)[number];

export type CellOutcome = (typeof CELL_OUTCOMES)[number];

export type LevelReading = (typeof LEVEL_READINGS)[number];

export interface RealityCheckSufficiency {
	isSufficient: boolean;
	reason: SufficiencyReason;
	daysWithCompletedWork: number;
}

export interface RealityCheckForecastLevel {
	probability: number;
	value: number;
}

export interface RealityCheckLevelOutcome {
	confidenceLevel: number;
	forecastValue: number;
	held: boolean;
}

/**
 * One sampling window scored against one horizon. The dates stay the ISO day strings they travel as:
 * turning a day into a `Date` reads it as UTC midnight, which shows as the previous day west of UTC.
 */
export interface RealityCheckCell {
	horizonDays: number;
	samplingWindowDays: number;
	scoredPeriodStart: string;
	scoredPeriodEnd: string;
	historyWindowStart: string;
	historyWindowEnd: string;
	sufficiency: RealityCheckSufficiency;
	forecast: RealityCheckForecastLevel[] | null;
	actualCompleted: number | null;
	outcome: CellOutcome | null;
	levelOutcomes: RealityCheckLevelOutcome[] | null;
}

export interface RealityCheckDenominator {
	runsAttempted: number;
	runsEvaluated: number;
	levelsPerRun: number;
	scoresEvaluated: number;
}

export interface RealityCheckSoundWindow {
	soundWindowDays: number[];
	unevaluatedWindowDays: number[];
	determination: Determination;
	currentSettingDays: number;
	currentSettingWasTested: boolean;
	currentSettingStanding: Standing;
	currentSettingNotTestedReason: NotTestedReason | null;
}

export interface RealityCheckLevelCoverage {
	confidenceLevel: number;
	heldCount: number;
	expectedHeldCount: number;
	reading: LevelReading;
}

export interface RealityCheckResult {
	teamId: number;
	teamName: string;
	anchorDate: string;
	standardWindowDays: number[];
	sampledWindowDays: number[];
	sampledHorizonDays: number[];
	confidenceLevels: number[];
	filterApplied: boolean;
	excludedSummary: string | null;
	minimumActiveDays: number;
	denominator: RealityCheckDenominator;
	soundWindow: RealityCheckSoundWindow;
	levelCoverage: RealityCheckLevelCoverage[];
	cells: RealityCheckCell[];
}
