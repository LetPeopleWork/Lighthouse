import {
	type ForecastGrade,
	type RealityCheckCell,
	type RealityCheckDenominator,
	type RealityCheckScoredPeriod,
	SUFFICIENCY_REASONS,
	type SufficiencyReason,
} from "../../../models/Forecasts/RealityCheckResult";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { parseLocalDate } from "../../../utils/date/localDate";
import type { WindowBadgeState } from "./realityCheckGrading";

type TermGetter = (key: string) => string;

export const listOf = (items: readonly (number | string)[]): string => {
	if (items.length < 2) {
		return items.join("");
	}
	return `${items.slice(0, -1).join(", ")} and ${items[items.length - 1]}`;
};

const samplingWindowsNamed = (windowDays: readonly number[]): string => {
	const windows = listOf(windowDays.map((days) => `${days}-day`));
	return windowDays.length === 1
		? `${windows} sampling window`
		: `${windows} sampling windows`;
};

export const unevaluatedSentence = (
	unevaluatedWindowDays: readonly number[],
): string | null => {
	if (unevaluatedWindowDays.length === 0) {
		return null;
	}
	const windows = samplingWindowsNamed(unevaluatedWindowDays);
	if (unevaluatedWindowDays.length === 1) {
		return `The ${windows} could not be checked, so it is not counted either way.`;
	}
	return `The ${windows} could not be checked, so they are not counted either way.`;
};

export interface UnrunChecks {
	checkCount: number;
	windowDays: number[];
	minimumActiveDays: number;
	getTerm: TermGetter;
}

const checks = (checkCount: number): string =>
	checkCount === 1 ? "1 check" : `${checkCount} checks`;

const checksOn = ({ checkCount, windowDays }: UnrunChecks): string =>
	`${checks(checkCount)} on the ${samplingWindowsNamed(windowDays)}`;

// Thin history and a forecast that could not be worked out are different troubles with different
// remedies, so each is told in its own words and neither borrows the other's.
export const sufficiencyReasonCopy: Record<
	SufficiencyReason,
	(unrun: UnrunChecks) => string | null
> = {
	Sufficient: () => null,
	TooFewActiveDays: (unrun) =>
		`${checksOn(unrun)} had fewer than ${unrun.minimumActiveDays} days with completed ${unrun.getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} to draw on.`,
	DegenerateForecast: (unrun) =>
		`For ${checksOn(unrun)}, no forecast could be worked out from the history.`,
};

/**
 * One sentence per reason a check could not run, naming the windows it happened on in the order they
 * were swept.
 */
export const whyChecksCouldNotRun = (
	cells: readonly RealityCheckCell[],
	sampledWindowDays: readonly number[],
	minimumActiveDays: number,
	getTerm: TermGetter,
): string[] =>
	SUFFICIENCY_REASONS.flatMap((reason) => {
		const unrun = cells.filter((cell) => cell.sufficiency.reason === reason);
		const sentence =
			unrun.length === 0
				? null
				: sufficiencyReasonCopy[reason]({
						checkCount: unrun.length,
						windowDays: sampledWindowDays.filter((days) =>
							unrun.some((cell) => cell.samplingWindowDays === days),
						),
						minimumActiveDays,
						getTerm,
					});
		return sentence === null ? [] : [sentence];
	});

export const LOADING = "Crunching the numbers…";

export const triggerExplanation = (getTerm: TermGetter): string =>
	`Replays this ${getTerm(TERMINOLOGY_KEYS.TEAM)}'s recent forecasts — each recent period, forecast from several sampling windows — and compares every one with the ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} actually completed.`;

export const horizonLabel = (horizonDays: number): string => {
	if (horizonDays % 7 !== 0) {
		return `${horizonDays} days`;
	}
	const weeks = horizonDays / 7;
	return weeks === 1 ? "1 week" : `${weeks} weeks`;
};

const workItemCount = (count: number, getTerm: TermGetter): string =>
	`${count} ${getTerm(count === 1 ? TERMINOLOGY_KEYS.WORK_ITEM : TERMINOLOGY_KEYS.WORK_ITEMS)}`;

export const periodActual = (
	actualCompleted: number,
	getTerm: TermGetter,
): string => `${workItemCount(actualCompleted, getTerm)} completed`;

// A period's day is a calendar day, not an instant: read through UTC it would show as the day before
// for anyone west of Greenwich. The digits are the reader's own, as the back-test date pickers show them.
export const dayInDigits = (isoDay: string, locale?: string): string =>
	parseLocalDate(isoDay)?.toLocaleDateString(locale, {
		day: "2-digit",
		month: "2-digit",
		year: "numeric",
	}) ?? isoDay;

const horizonHeading = (horizonDays: number): string =>
	`Forecast Horizon: ${horizonLabel(horizonDays)}`;

export const periodHeader = (
	horizonDays: number,
	period: RealityCheckScoredPeriod | undefined,
	getTerm: TermGetter,
	locale?: string,
): string => {
	if (period === undefined) {
		return horizonHeading(horizonDays);
	}
	const firstDay = dayInDigits(period.scoredPeriodStart, locale);
	const lastDay = dayInDigits(period.scoredPeriodEnd, locale);
	return `${horizonHeading(horizonDays)} (${firstDay} – ${lastDay}) – ${periodActual(period.actualCompleted, getTerm)}`;
};

export const heldUpMeans =
	"A sampling window held up when its 95th forecast held in more than half of the checks that could be run on it.";

const yourWindow = (days: number): string => `Your ${days}-day sampling window`;

export const badgeWords: Record<
	WindowBadgeState,
	(days: number, getTerm: TermGetter) => string
> = {
	Fine: (days) => `${yourWindow(days)}: fine`,
	DidNotHoldUp: (days) => `${yourWindow(days)}: did not hold up`,
	NoWindowHeldUp: (days) =>
		`${yourWindow(days)}: did not hold up (no window did)`,
	CouldNotBeChecked: (days) => `${yourWindow(days)}: could not be checked`,
	FixedDates: (_days, getTerm) =>
		`Your ${getTerm(TERMINOLOGY_KEYS.TEAM)} forecasts from fixed dates: sampling window not tested`,
	NotAPositiveLength: () =>
		"Your sampling window: not tested (not a positive number of days)",
};

export const windowRowLabel = (
	windowDays: number,
	isYourSetting: boolean,
): string =>
	isYourSetting ? `${windowDays} days, your setting` : `${windowDays} days`;

export const tableRegionName = (teamName: string): string =>
	`Forecasts checked for ${teamName}, by forecast horizon and sampling window`;

export interface UnevaluableRowFacts {
	daysWithCompletedWork: number;
	minimumActiveDays: number;
	getTerm: TermGetter;
}

const dayCount = (days: number): string =>
	days === 1 ? "1 day" : `${days} days`;

// Said where the band would be, so the row is never blank; each reason keeps its own words.
export const unevaluableRowCopy: Record<
	SufficiencyReason,
	(facts: UnevaluableRowFacts) => string
> = {
	Sufficient: () => "No forecast came back for this check.",
	TooFewActiveDays: ({ daysWithCompletedWork, minimumActiveDays, getTerm }) =>
		`Not enough history in this window to check: ${dayCount(daysWithCompletedWork)} with completed ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)}, ${minimumActiveDays} needed.`,
	DegenerateForecast: () =>
		"No forecast could be worked out from the history in this window.",
};

export interface GradedCellFacts {
	forecastValue: number;
	actualCompleted: number;
	/** What the Team completed minus what was forecast. */
	miss: number;
	/** None when the period completed nothing, since a share of nothing has no meaning. */
	percentOfActual: number | null;
}

// A typographic minus, not a hyphen, so a negative figure reads as a number and not as a dash.
const MINUS = "−";

export const notCheckedCell = "Not checked at this confidence level.";

export const heldGlyph = (held: boolean): string => (held ? "✓" : "✗");

export const percentShown = (percentOfActual: number): string =>
	`${percentOfActual}%`;

const signOf = (miss: number): string => (miss > 0 ? "+" : MINUS);

const howFarOff = (miss: number, percentOfActual: number | null): string =>
	percentOfActual === null
		? "No percentage — nothing was completed."
		: `Forecast off by ${signOf(miss)}${percentShown(percentOfActual)}`;

const moreOrFewer = (miss: number): string => (miss > 0 ? "more" : "fewer");

/** How a forecast compared with what was completed, in words: the cell's tooltip and its accessible name. */
export const cellComparison = (
	{ forecastValue, actualCompleted, miss, percentOfActual }: GradedCellFacts,
	getTerm: TermGetter,
): string => {
	if (miss === 0) {
		return forecastValue === 0
			? `Closed no ${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)}, exactly as forecasted.`
			: `Closed exactly the forecasted ${workItemCount(forecastValue, getTerm)}.`;
	}
	const difference = workItemCount(Math.abs(miss), getTerm);
	return `Closed ${difference} ${moreOrFewer(miss)} (${actualCompleted}) than forecasted (${forecastValue}). ${howFarOff(miss, percentOfActual)}`;
};

const scenarios = (count: number): string =>
	count === 1 ? "1 scenario" : `${count} scenarios`;

const forecasts = (count: number): string =>
	count === 1 ? "1 forecast" : `${count} forecasts`;

export const realityCheckHeadline = ({
	runsAttempted,
	runsEvaluated,
	scoresEvaluated,
}: RealityCheckDenominator): string => {
	if (runsEvaluated === 0) {
		return `None of the ${scenarios(runsAttempted)} could be backtested`;
	}
	const backtested =
		runsEvaluated === runsAttempted
			? scenarios(runsEvaluated)
			: `${runsEvaluated} of ${scenarios(runsAttempted)}`;
	return `Backtested ${backtested} · ${forecasts(scoresEvaluated)}`;
};

export const EXPLANATION_NAME = "About these numbers";

export const ACCURATE_MEANS =
	"Accurate means within 10% of what was completed, whether the forecast held or not.";

export const CREDIT_LEAD = "Inspired by Nick Brown's article";

export const FULL_MONTE = {
	title: "The Full Monte",
	url: "https://medium.com/asos-techblog/the-full-monte-901d721b8532",
} as const;

export interface ExplanationFacts {
	sampledHorizonDays: readonly number[];
	sampledWindowDays: readonly number[];
	minimumActiveDays: number;
	denominator: RealityCheckDenominator;
	cells: readonly RealityCheckCell[];
}

const confidenceLevels = (count: number): string =>
	count === 1 ? "1 confidence level" : `${count} confidence levels`;

const scenariosGive = (scenarioCount: number, forecastCount: number): string =>
	scenarioCount === 1
		? `1 scenario gives ${forecasts(forecastCount)}`
		: `${scenarios(scenarioCount)} give ${forecasts(forecastCount)}`;

// With nothing backtested, "0 scenarios give 0 forecasts" would only repeat the headline's "none".
const scenarioExplanation = (
	{ sampledHorizonDays, denominator }: ExplanationFacts,
	getTerm: TermGetter,
): string => {
	const { runsEvaluated, levelsPerRun, scoresEvaluated } = denominator;
	const periods = listOf(sampledHorizonDays.map(horizonLabel));
	const readAt = `Each is read at ${confidenceLevels(levelsPerRun)}`;
	const counted =
		runsEvaluated === 0
			? `${readAt}.`
			: `${readAt}, so ${scenariosGive(runsEvaluated, scoresEvaluated)}.`;
	return `Each scenario replays one forecast: a recent period (${periods}, each ending today), forecast from one sampling window of the history before it, then compared with what the ${getTerm(TERMINOLOGY_KEYS.TEAM)} actually completed. ${counted}`;
};

export const scenariosLeftOut = ({
	runsAttempted,
	runsEvaluated,
}: RealityCheckDenominator): string | null =>
	runsAttempted > runsEvaluated
		? `${runsAttempted - runsEvaluated} of the ${runsAttempted} scenarios could not run and are left out of every count.`
		: null;

const whyScenariosWereLeftOut = (
	facts: ExplanationFacts,
	getTerm: TermGetter,
): string[] => {
	const leftOut = scenariosLeftOut(facts.denominator);
	if (leftOut === null) {
		return [];
	}
	const why = whyChecksCouldNotRun(
		facts.cells,
		facts.sampledWindowDays,
		facts.minimumActiveDays,
		getTerm,
	);
	return [[leftOut, ...why].join(" ")];
};

/** The paragraphs the headline's explanation holds before its credit line. */
export const headlineExplanation = (
	facts: ExplanationFacts,
	getTerm: TermGetter,
): string[] => [
	scenarioExplanation(facts, getTerm),
	...whyScenariosWereLeftOut(facts, getTerm),
	ACCURATE_MEANS,
];

export interface LevelRowFacts {
	confidenceLevel: number;
	levelName: string;
	heldCount: number;
	runsEvaluated: number;
	/** The held share to show; none for a level no check could test. */
	share: number | null;
	withinTenPercent: number;
}

export const levelRowText = ({
	heldCount,
	runsEvaluated,
	share,
	withinTenPercent,
}: LevelRowFacts): string =>
	share === null
		? "Not tested — no check could run"
		: `${share}% (${heldCount} of ${runsEvaluated}) · ${withinTenPercent} accurate`;

/** What the bar and its tick show, for a reader who cannot see them. */
export const levelBarName = ({
	confidenceLevel,
	levelName,
	heldCount,
	runsEvaluated,
	share,
	withinTenPercent,
}: LevelRowFacts): string => {
	const level = `${confidenceLevel}th ${levelName}`;
	return share === null
		? `${level}: not tested, no check could run`
		: `${level}: held in ${share}% of checks (${heldCount} of ${runsEvaluated}), expected about ${confidenceLevel}%; ${withinTenPercent} accurate within 10%`;
};

// The legend's row title says held or missed, so a band label only says how far off.
export const gradeLegendCopy: Record<ForecastGrade, string> = {
	HeldWithin10: "within 10%",
	Held10To25: "10–25% off",
	HeldOver25: "more than 25% off",
	NotHeldWithin10: "within 10%",
	NotHeld10To25: "10–25% off",
	NotHeldOver25: "more than 25% off",
};

export const legendTitles = {
	held: "Forecast held",
	missed: "Forecast missed",
} as const;
