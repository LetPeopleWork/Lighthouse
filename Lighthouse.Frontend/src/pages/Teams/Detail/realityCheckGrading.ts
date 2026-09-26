// RED scaffold written by DISTILL for Story 6094; DELIVER replaces every body and removes the marker.
export const __SCAFFOLD__ = true;

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

export type ForecastGrade = (typeof FORECAST_GRADES)[number];

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

const notYetImplemented = (question: string, facts: unknown): never => {
	throw new Error(
		`Not yet implemented -- RED scaffold: ${question} for ${JSON.stringify(facts)}`,
	);
};

export const missOf = ({
	actualCompleted,
	forecastValue,
}: Pick<GradedCheck, "forecastValue" | "actualCompleted">): number =>
	actualCompleted - forecastValue;

export const readCheck = (check: GradedCheck): CheckReading =>
	notYetImplemented("the grade of one check", check);

export const levelCloseness = (
	checks: readonly GradedCheck[],
): LevelCloseness => notYetImplemented("how close one level landed", checks);
