import {
	type Determination,
	type LevelReading,
	type NotTestedReason,
	type RealityCheckCell,
	type RealityCheckDenominator,
	type RealityCheckLevelCoverage,
	type RealityCheckSoundWindow,
	type Standing,
	SUFFICIENCY_REASONS,
	type SufficiencyReason,
} from "../../../models/Forecasts/RealityCheckResult";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { parseLocalDate } from "../../../utils/date/localDate";
import type { ForecastGrade, LevelCloseness } from "./realityCheckGrading";

type TermGetter = (key: string) => string;

export type Region =
	| { shape: "span"; firstDays: number; lastDays: number }
	| { shape: "list"; windowDays: number[] }
	| { shape: "none" };

export interface VerdictFacts {
	teamName: string;
	region: Region;
	soundWindow: RealityCheckSoundWindow;
}

/**
 * A span is named only when the windows that held up sit next to each other on the ladder the server
 * swept. Naming "between 14 and 90" over a ladder with 45 in it would claim 45 held up when it did not.
 */
export const regionOf = (
	soundWindowDays: readonly number[],
	sampledWindowDays: readonly number[],
): Region => {
	if (soundWindowDays.length === 0) {
		return { shape: "none" };
	}

	const start = sampledWindowDays.indexOf(soundWindowDays[0]);
	const run = sampledWindowDays.slice(start, start + soundWindowDays.length);
	const isContiguousRun =
		start >= 0 &&
		soundWindowDays.length > 1 &&
		run.length === soundWindowDays.length &&
		run.every((days, index) => days === soundWindowDays[index]);

	if (!isContiguousRun) {
		return { shape: "list", windowDays: [...soundWindowDays] };
	}

	return {
		shape: "span",
		firstDays: soundWindowDays[0],
		lastDays: soundWindowDays[soundWindowDays.length - 1],
	};
};

export const listOf = (items: readonly (number | string)[]): string => {
	if (items.length < 2) {
		return items.join("");
	}
	return `${items.slice(0, -1).join(", ")} and ${items[items.length - 1]}`;
};

const noWindowHeldUp = (teamName: string): string =>
	`None of the sampling windows checked held up for ${teamName}.`;

const regionSentence = ({ region, teamName }: VerdictFacts): string => {
	if (region.shape === "span") {
		return `Anything between ${region.firstDays} and ${region.lastDays} days would have behaved about the same for ${teamName}.`;
	}
	if (region.shape === "list") {
		return `Of the sampling windows checked, ${listOf(region.windowDays)} days held up for ${teamName}.`;
	}
	return noWindowHeldUp(teamName);
};

export const determinationCopy: Record<
	Determination,
	(facts: VerdictFacts) => string
> = {
	AllWindowsAlike: regionSentence,
	SomeWindowsSound: regionSentence,
	NoWindowSound: ({ teamName }) => noWindowHeldUp(teamName),
	NotEnoughEvidence: ({ teamName }) =>
		`No sampling window could be checked for ${teamName}, so nothing can be concluded about any of them.`,
};

const regionNoun: Record<Region["shape"], string> = {
	span: "that range",
	list: "that set",
	none: "any range that held up",
};

export const standingCopy: Record<
	Standing,
	(facts: VerdictFacts) => string | null
> = {
	Inside: ({ region, soundWindow }) =>
		soundWindow.determination === "AllWindowsAlike"
			? `Your current ${soundWindow.currentSettingDays} is inside ${regionNoun[region.shape]} — this setting is fine.`
			: `Your current ${soundWindow.currentSettingDays} is inside ${regionNoun[region.shape]}.`,
	Outside: ({ region, soundWindow }) =>
		`Your current ${soundWindow.currentSettingDays} is not inside ${regionNoun[region.shape]}.`,
	NotDetermined: ({ soundWindow }) =>
		`Your current ${soundWindow.currentSettingDays} could not be checked, so nothing can be said about where it stands.`,
	// The check never ran on this setting, so it makes no claim about it; the reason is told instead.
	NotTested: () => null,
};

export const notTestedReasonCopy: Record<
	NotTestedReason,
	(getTerm: TermGetter) => string
> = {
	UsesFixedDates: (getTerm) =>
		`This ${getTerm(TERMINOLOGY_KEYS.TEAM)} forecasts from fixed dates rather than a rolling sampling window, so its own setting was not tested.`,
	NotAPositiveLength: (getTerm) =>
		`This ${getTerm(TERMINOLOGY_KEYS.TEAM)}'s sampling window is not a positive number of days, so its own setting was not tested.`,
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

/**
 * The first finding, in reading order: where the windows that held up lie, where the setting stands (or
 * why it was not tested), and which windows could not be checked at all.
 */
export const windowVerdict = (
	teamName: string,
	sampledWindowDays: readonly number[],
	soundWindow: RealityCheckSoundWindow,
	getTerm: TermGetter,
): string => {
	const facts: VerdictFacts = {
		teamName,
		region: regionOf(soundWindow.soundWindowDays, sampledWindowDays),
		soundWindow,
	};
	const notTestedReason = soundWindow.currentSettingNotTestedReason;

	return [
		determinationCopy[soundWindow.determination](facts),
		standingCopy[soundWindow.currentSettingStanding](facts),
		notTestedReason === null
			? null
			: notTestedReasonCopy[notTestedReason](getTerm),
		unevaluatedSentence(soundWindow.unevaluatedWindowDays),
	]
		.filter((sentence): sentence is string => sentence !== null)
		.join(" ");
};

const runsChecked = (runsEvaluated: number): string =>
	runsEvaluated === 1
		? "1 forecast run was checked"
		: `${runsEvaluated} forecast runs were checked`;

export interface UnrunChecks {
	checkCount: number;
	windowDays: number[];
	minimumActiveDays: number;
	getTerm: TermGetter;
}

const checksOn = ({ checkCount, windowDays }: UnrunChecks): string =>
	`${checkCount === 1 ? "1 check" : `${checkCount} checks`} on the ${samplingWindowsNamed(windowDays)}`;

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

export const horizonLabel = (horizonDays: number): string => {
	if (horizonDays % 7 !== 0) {
		return `${horizonDays} days`;
	}
	const weeks = horizonDays / 7;
	return weeks === 1 ? "1 week" : `${weeks} weeks`;
};

export const periodActual = (
	actualCompleted: number,
	getTerm: TermGetter,
): string =>
	`${actualCompleted} ${getTerm(actualCompleted === 1 ? TERMINOLOGY_KEYS.WORK_ITEM : TERMINOLOGY_KEYS.WORK_ITEMS)} completed`;

// A period's day is a calendar day, not an instant: read through UTC it would show as the day before
// for anyone west of Greenwich.
export const dayInWords = (isoDay: string, locale?: string): string =>
	parseLocalDate(isoDay)?.toLocaleDateString(locale, {
		day: "numeric",
		month: "short",
		year: "numeric",
	}) ?? isoDay;

export const windowRowLabel = (
	windowDays: number,
	isYourSetting: boolean,
): string =>
	isYourSetting ? `${windowDays} days, your setting` : `${windowDays} days`;

export const tableCaption = (teamName: string, getTerm: TermGetter): string =>
	`Every forecast checked for ${teamName}, by period and sampling window, beside what the ${getTerm(TERMINOLOGY_KEYS.TEAM)} delivered.`;

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
	confidenceLevel: number;
	forecastValue: number;
	/** What the Team delivered minus what was forecast. */
	miss: number;
	held: boolean;
	/** None when the period delivered nothing, since a share of nothing has no meaning. */
	percentOfActual: number | null;
}

// A typographic minus, not a hyphen, so a negative miss reads as a number and not as a dash.
const MINUS = "−";

export const signedMiss = (miss: number): string => {
	if (miss > 0) {
		return `+${miss}`;
	}
	if (miss < 0) {
		return `${MINUS}${-miss}`;
	}
	return "0";
};

const missInWords = (miss: number): string => {
	if (miss > 0) {
		return `${miss} more delivered`;
	}
	if (miss < 0) {
		return `${-miss} fewer delivered`;
	}
	return "exactly as forecast";
};

export const heldWord = (held: boolean): string =>
	held ? "held" : "did not hold";

export const heldGlyph = (held: boolean): string => (held ? "✓" : "✗");

export const percentShown = (percentOfActual: number): string =>
	`${percentOfActual}%`;

const percentInWords = (percentOfActual: number | null): string =>
	percentOfActual === null
		? ""
		: `, ${percentShown(percentOfActual)} of the actual`;

export const gradedCellName = ({
	confidenceLevel,
	forecastValue,
	miss,
	held,
	percentOfActual,
}: GradedCellFacts): string =>
	`${confidenceLevel}th: ${forecastValue}, ${heldWord(held)}, ${missInWords(miss)}${percentInWords(percentOfActual)}`;

const runsLeftOut = ({
	runsAttempted,
	runsEvaluated,
}: RealityCheckDenominator): string | null =>
	runsAttempted > runsEvaluated
		? `${runsAttempted - runsEvaluated} of the ${runsAttempted} checks could not run, so they are left out of every count.`
		: null;

/**
 * What was counted, and why the counts must not be read as independent trials or ranked against each
 * other: the levels of one run share a simulation, and every run looks at a different stretch of time.
 */
export const denominatorStatement = (
	denominator: RealityCheckDenominator,
	whyLeftOut: readonly string[] = [],
): string => {
	const { runsEvaluated, levelsPerRun, scoresEvaluated } = denominator;
	return [
		`${runsChecked(runsEvaluated)}, each read at ${levelsPerRun} confidence levels — ${scoresEvaluated} scores in all.`,
		runsLeftOut(denominator),
		...whyLeftOut,
		`The ${levelsPerRun} levels of a single run come from the same simulation, so they are not independent of one another.`,
		"And each run covers a different stretch of real time — every one ends today and reaches back by its own length — so they are not repeated trials of one experiment and should not be ranked against each other.",
	]
		.filter((sentence): sentence is string => sentence !== null)
		.join(" ");
};

const heldAgainstExpected = (
	{ confidenceLevel, heldCount, expectedHeldCount }: RealityCheckLevelCoverage,
	runsEvaluated: number,
	{ withinTenPercent }: LevelCloseness,
): string =>
	`${confidenceLevel}th: held ${heldCount} of ${runsEvaluated} (should be about ${Math.round(expectedHeldCount)}), within 10% in ${withinTenPercent}`;

// A level between the two extremes gets its two counts and no adjective: checks that are not independent
// trials give no honest threshold for calling a level well calibrated.
export const levelReadingCopy: Record<
	LevelReading,
	(
		level: RealityCheckLevelCoverage,
		runsEvaluated: number,
		closeness: LevelCloseness,
	) => string
> = {
	SometimesHeld: (level, runsEvaluated, closeness) =>
		`${heldAgainstExpected(level, runsEvaluated, closeness)}.`,
	NeverHeld: (level, runsEvaluated, closeness) =>
		`${heldAgainstExpected(level, runsEvaluated, closeness)} — it never held, which is over-forecasting.`,
	AlwaysHeld: (level, runsEvaluated, closeness) =>
		`${heldAgainstExpected(level, runsEvaluated, closeness)} — it held every time, which is under-forecasting.`,
	NotEvaluated: ({ confidenceLevel }) =>
		`${confidenceLevel}th: no check could be run, so this level was not tested.`,
};

export const levelLine = (
	level: RealityCheckLevelCoverage,
	runsEvaluated: number,
	closeness: LevelCloseness,
): string => levelReadingCopy[level.reading](level, runsEvaluated, closeness);

/**
 * The check reports and the person acts. Only one of the two things it looked at is stored on the
 * team, so they are told apart rather than merged into one recommendation.
 */
export const findings = (getTerm: TermGetter, levelCount: number): string[] => [
	`The sampling window is a setting on this ${getTerm(TERMINOLOGY_KEYS.TEAM)}.`,
	`The confidence level is not a setting: it is which of the ${levelCount} numbers you choose to quote.`,
];

export const gradeLegendCopy: Record<ForecastGrade, string> = {
	HeldWithin10: "Held, within 10%",
	Held10To25: "Held, by 10-25%",
	HeldOver25: "Held, by more than 25%",
	NotHeldWithin10: "Did not hold, within 10%",
	NotHeld10To25: "Did not hold, by 10-25%",
	NotHeldOver25: "Did not hold, by more than 25%",
};

export const notCheckedLegend = "Not checked";

// The source method is credited in the same breath as what this product added, so the additions are
// never mistaken for the author's.
export const methodCredit =
	"Grading each forecast as held or not, and shading it by how close it landed, follows Nick Brown's method in The Full Monte. Reading a level that always held as under-forecasting, and the 95th level, are this product's additions.";
