import type {
	ForecastGrade,
	NotTestedReason,
	RealityCheckCell,
	RealityCheckSoundWindow,
} from "../../../models/Forecasts/RealityCheckResult";

/** One level of one check as the server sends it; whether it held is the server's judgement, never recounted. */
export interface GradedCheck {
	forecastValue: number;
	actualCompleted: number;
	held: boolean;
}

export interface CheckReading {
	grade: ForecastGrade;
	/** What the Team delivered minus what was forecast: positive when it delivered more. */
	miss: number;
	/** The miss as a whole-number share of the actual, never contradicting the grade; none when nothing was delivered. */
	percentOfActual: number | null;
}

export interface LevelCloseness {
	gradedChecks: number;
	withinTenPercent: number;
}

// A check lists its levels in its own order, so a level is found by its percentile, never by position.
export const gradedCheckAt = (
	{
		levelOutcomes,
		actualCompleted,
	}: Pick<RealityCheckCell, "levelOutcomes" | "actualCompleted">,
	confidenceLevel: number,
): GradedCheck | null => {
	const outcome = levelOutcomes?.find(
		(candidate) => candidate.confidenceLevel === confidenceLevel,
	);
	if (outcome === undefined || actualCompleted === null) {
		return null;
	}
	return {
		forecastValue: outcome.forecastValue,
		actualCompleted,
		held: outcome.held,
	};
};

export const gradedChecksAt = (
	confidenceLevel: number,
	cells: readonly RealityCheckCell[],
): GradedCheck[] =>
	cells.flatMap((cell) => {
		const check = gradedCheckAt(cell, confidenceLevel);
		return check === null ? [] : [check];
	});

export const missOf = ({
	actualCompleted,
	forecastValue,
}: Pick<GradedCheck, "forecastValue" | "actualCompleted">): number =>
	actualCompleted - forecastValue;

/** Which side of the hue a grade sits on: the forecast held, or it was missed. */
export const GRADE_HELD: Record<ForecastGrade, boolean> = {
	HeldWithin10: true,
	Held10To25: true,
	HeldOver25: true,
	NotHeldWithin10: false,
	NotHeld10To25: false,
	NotHeldOver25: false,
};

type Band = "within10" | "10To25" | "over25";

const HELD_GRADE_OF_BAND: Record<Band, ForecastGrade> = {
	within10: "HeldWithin10",
	"10To25": "Held10To25",
	over25: "HeldOver25",
};

const NOT_HELD_GRADE_OF_BAND: Record<Band, ForecastGrade> = {
	within10: "NotHeldWithin10",
	"10To25": "NotHeld10To25",
	over25: "NotHeldOver25",
};

// Decided on whole numbers, so 3 off an actual of 30 is exactly 10% and no rounding moves a check across an edge.
const bandOf = (off: number, actual: number): Band => {
	if (10 * off <= actual) {
		return "within10";
	}
	return 4 * off <= actual ? "10To25" : "over25";
};

// 100 * part / whole rounded half up, kept in integers so the shown figure never picks up floating-point error.
const wholePercentOf = (part: number, whole: number): number =>
	Math.floor((200 * part + whole) / (2 * whole));

// Rounding can pull a figure back onto the edge its band starts just past (10.3% to 10, a sliver of a miss to 0),
// which would read as the band next to it; the shown figure is raised to the least the band can mean.
const LEAST_SHOWN_PERCENT_OF_BAND: Record<Band, number> = {
	within10: 1,
	"10To25": 11,
	over25: 26,
};

const shownPercentOf = (off: number, actual: number, band: Band): number => {
	const leastShown = off === 0 ? 0 : LEAST_SHOWN_PERCENT_OF_BAND[band];
	return Math.max(wholePercentOf(off, actual), leastShown);
};

export const readCheck = (check: GradedCheck): CheckReading => {
	const miss = missOf(check);
	const off = Math.abs(miss);
	const actual = check.actualCompleted;
	const band = bandOf(off, actual);
	const gradeOfBand = check.held ? HELD_GRADE_OF_BAND : NOT_HELD_GRADE_OF_BAND;

	return {
		grade: gradeOfBand[band],
		miss,
		percentOfActual: actual === 0 ? null : shownPercentOf(off, actual, band),
	};
};

// Held and not held both count: the question is how close the forecast came, not which side it missed on.
const WITHIN_TEN_PERCENT: ReadonlySet<ForecastGrade> = new Set<ForecastGrade>([
	"HeldWithin10",
	"NotHeldWithin10",
]);

export const levelCloseness = (
	checks: readonly GradedCheck[],
): LevelCloseness => ({
	gradedChecks: checks.length,
	withinTenPercent: checks.filter((check) =>
		WITHIN_TEN_PERCENT.has(readCheck(check).grade),
	).length,
});

/**
 * The share of its checks a level held in, as the whole percentage its bar and row show: 100 only when
 * every check held and 0 only when none did, so a level that missed once never reads as always holding.
 * None for a level no check could test.
 */
export const heldShare = (
	heldCount: number,
	runsEvaluated: number,
): number | null => {
	if (runsEvaluated === 0) {
		return null;
	}
	if (heldCount === runsEvaluated) {
		return 100;
	}
	if (heldCount === 0) {
		return 0;
	}
	return Math.min(99, Math.max(1, wholePercentOf(heldCount, runsEvaluated)));
};

/** How the Team's own sampling window stood, as one badge reads it. */
export type WindowBadgeState =
	| "FixedDates"
	| "NotAPositiveLength"
	| "CouldNotBeChecked"
	| "NoWindowHeldUp"
	| "Fine"
	| "DidNotHoldUp";

const NOT_TESTED_BADGE: Record<NotTestedReason, WindowBadgeState> = {
	UsesFixedDates: "FixedDates",
	NotAPositiveLength: "NotAPositiveLength",
};

// A setting that was not tested stands as NotTested, which says nothing on its own, so its reason is read first.
export const windowBadgeOf = ({
	currentSettingNotTestedReason,
	currentSettingStanding,
	determination,
}: Pick<
	RealityCheckSoundWindow,
	"currentSettingNotTestedReason" | "currentSettingStanding" | "determination"
>): WindowBadgeState => {
	if (currentSettingNotTestedReason !== null) {
		return NOT_TESTED_BADGE[currentSettingNotTestedReason];
	}
	if (
		currentSettingStanding === "NotDetermined" ||
		determination === "NotEnoughEvidence"
	) {
		return "CouldNotBeChecked";
	}
	if (determination === "NoWindowSound") {
		return "NoWindowHeldUp";
	}
	return currentSettingStanding === "Inside" ? "Fine" : "DidNotHoldUp";
};
