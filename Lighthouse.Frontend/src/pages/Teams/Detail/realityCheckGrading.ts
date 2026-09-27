import type {
	ForecastGrade,
	RealityCheckCell,
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
	/** The grade more than half of the level's graded checks share, if one does. */
	usualGrade: ForecastGrade | null;
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

// 100 * off / actual rounded half up, kept in integers so the shown figure never picks up floating-point error.
const roundedPercentOf = (off: number, actual: number): number =>
	Math.floor((200 * off + actual) / (2 * actual));

// Rounding can pull a figure back onto the edge its band starts just past (10.3% to 10, a sliver of a miss to 0),
// which would read as the band next to it; the shown figure is raised to the least the band can mean.
const LEAST_SHOWN_PERCENT_OF_BAND: Record<Band, number> = {
	within10: 1,
	"10To25": 11,
	over25: 26,
};

const shownPercentOf = (off: number, actual: number, band: Band): number => {
	const leastShown = off === 0 ? 0 : LEAST_SHOWN_PERCENT_OF_BAND[band];
	return Math.max(roundedPercentOf(off, actual), leastShown);
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

// A usual grade needs a strict majority, so a level split evenly between two grades has none.
const isStrictMajority = (count: number, total: number): boolean =>
	2 * count > total;

export const levelCloseness = (
	checks: readonly GradedCheck[],
): LevelCloseness => {
	const countOfGrade = new Map<ForecastGrade, number>();
	for (const check of checks) {
		const { grade } = readCheck(check);
		countOfGrade.set(grade, (countOfGrade.get(grade) ?? 0) + 1);
	}

	let withinTenPercent = 0;
	let usualGrade: ForecastGrade | null = null;
	for (const [grade, count] of countOfGrade) {
		if (WITHIN_TEN_PERCENT.has(grade)) {
			withinTenPercent += count;
		}
		if (isStrictMajority(count, checks.length)) {
			usualGrade = grade;
		}
	}

	return { gradedChecks: checks.length, withinTenPercent, usualGrade };
};

export const __SCAFFOLD__ = true;

/**
 * The share of its checks a level held in, as the whole percentage its bar and row show: 100 only when
 * every check held and 0 only when none did, so a level that missed once never reads as always holding.
 * None for a level no check could test.
 */
export const heldShare = (
	heldCount: number,
	runsEvaluated: number,
): number | null => {
	throw new Error(
		`Not yet implemented -- RED scaffold: the share of ${runsEvaluated} checks a level held in, ${heldCount} of them held`,
	);
};
