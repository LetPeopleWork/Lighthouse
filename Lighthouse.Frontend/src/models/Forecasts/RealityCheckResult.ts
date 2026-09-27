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

/**
 * How one forecast landed against what its period delivered, on Nick Brown's scale: the hue is whether it
 * held, the shade how close it landed as a share of the actual.
 */
export const FORECAST_GRADES = [
	"HeldWithin10",
	"Held10To25",
	"HeldOver25",
	"NotHeldWithin10",
	"NotHeld10To25",
	"NotHeldOver25",
] as const;

export type Determination = (typeof DETERMINATIONS)[number];

export type Standing = (typeof STANDINGS)[number];

export type NotTestedReason = (typeof NOT_TESTED_REASONS)[number];

export type SufficiencyReason = (typeof SUFFICIENCY_REASONS)[number];

export type CellOutcome = (typeof CELL_OUTCOMES)[number];

export type LevelReading = (typeof LEVEL_READINGS)[number];

export type ForecastGrade = (typeof FORECAST_GRADES)[number];

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

/** One period the checks were scored on, and what the Team finished in it, whether or not any check of it could run. */
export interface RealityCheckScoredPeriod {
	horizonDays: number;
	scoredPeriodStart: string;
	scoredPeriodEnd: string;
	actualCompleted: number;
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
	scoredPeriods: RealityCheckScoredPeriod[];
	cells: RealityCheckCell[];
}
