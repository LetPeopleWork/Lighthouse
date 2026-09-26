import { describe, expect, it } from "vitest";
import {
	type ForecastGrade,
	type GradedCheck,
	levelCloseness,
	missOf,
	readCheck,
} from "./realityCheckGrading";

/**
 * How close one forecast landed, on Nick Brown's scale: held or not is the server's judgement and sets the
 * hue; the miss as a share of what the period delivered sets the shade - within 10%, 10 to 25%, more than
 * 25%, the same on both sides. The shown whole-number percentage never contradicts its band, and a period
 * in which nothing was delivered has no percentage at all.
 *
 * Pending until the grading exists (Story 6094, slice 05); the miss alone belongs to slice 04.
 */

const PENDING = "the grading is not built yet";

const aCheck = (
	actualCompleted: number,
	forecastValue: number,
	held = actualCompleted >= forecastValue,
): GradedCheck => ({ actualCompleted, forecastValue, held });

const checksOf = (
	count: number,
	actualCompleted: number,
	forecastValue: number,
): GradedCheck[] =>
	Array.from({ length: count }, () => aCheck(actualCompleted, forecastValue));

// One check per grade, all against an actual of 42.
const heldWithin10 = (count: number) => checksOf(count, 42, 40);
const held10To25 = (count: number) => checksOf(count, 42, 36);
const heldOver25 = (count: number) => checksOf(count, 42, 31);
const notHeldWithin10 = (count: number) => checksOf(count, 42, 45);
const notHeld10To25 = (count: number) => checksOf(count, 42, 48);

const bandOf = (grade: ForecastGrade): "within" | "between" | "over" => {
	if (grade.endsWith("Within10")) {
		return "within";
	}
	return grade.endsWith("10To25") ? "between" : "over";
};

// The band decided on whole numbers, so 3 of 30 is exactly 10% and no rounding can move a check across an edge.
const bandByTheRule = (
	off: number,
	actual: number,
): "within" | "between" | "over" => {
	if (10 * off <= actual) {
		return "within";
	}
	return 4 * off <= actual ? "between" : "over";
};

const SHOWN_RANGE_OF_BAND = {
	within: [0, 10],
	between: [11, 25],
	over: [26, Number.POSITIVE_INFINITY],
} as const;

const shownInsideItsBand = (
	band: keyof typeof SHOWN_RANGE_OF_BAND,
	percent: number,
): boolean =>
	percent >= SHOWN_RANGE_OF_BAND[band][0] &&
	percent <= SHOWN_RANGE_OF_BAND[band][1];

describe("@us-04 @slice-04 the miss in Work Items", () => {
	it.each([
		{ actual: 42, forecast: 36, miss: 6 },
		{ actual: 42, forecast: 48, miss: -6 },
		{ actual: 40, forecast: 40, miss: 0 },
		{ actual: 0, forecast: 2, miss: -2 },
	])(
		`is what the Team delivered minus what was forecast, so its sign always agrees with whether it held - $actual against $forecast`,
		({ actual, forecast, miss }) => {
			expect(missOf({ actualCompleted: actual, forecastValue: forecast })).toBe(
				miss,
			);
		},
	);
});

describe("@us-05 @slice-05 @kpi-OUT-6094-no-percentage-without-its-work-items how close one forecast landed", () => {
	it.skip.each<{
		story: string;
		actual: number;
		forecast: number;
		grade: ForecastGrade;
		miss: number;
		percent: number;
	}>([
		{
			story: "Ocean Explorer's 70th",
			actual: 42,
			forecast: 40,
			grade: "HeldWithin10",
			miss: 2,
			percent: 5,
		},
		{
			story: "Ocean Explorer's 85th",
			actual: 42,
			forecast: 36,
			grade: "Held10To25",
			miss: 6,
			percent: 14,
		},
		{
			story: "Ocean Explorer's 95th",
			actual: 42,
			forecast: 31,
			grade: "HeldOver25",
			miss: 11,
			percent: 26,
		},
		{
			story: "Ocean Explorer's 50th",
			actual: 42,
			forecast: 48,
			grade: "NotHeld10To25",
			miss: -6,
			percent: 14,
		},
		{
			story: "Coastal Survey's one-Work-Item miss",
			actual: 3,
			forecast: 4,
			grade: "NotHeldOver25",
			miss: -1,
			percent: 33,
		},
	])(
		`reads the stories' examples as the stories wrote them - $story (${PENDING})`,
		({ actual, forecast, grade, miss, percent }) => {
			expect(readCheck(aCheck(actual, forecast))).toEqual({
				grade,
				miss,
				percentOfActual: percent,
			});
		},
	);

	it.skip.each<{
		actual: number;
		forecast: number;
		grade: ForecastGrade;
		percent: number;
	}>([
		{ actual: 40, forecast: 36, grade: "HeldWithin10", percent: 10 },
		{ actual: 39, forecast: 35, grade: "Held10To25", percent: 11 },
		{ actual: 100, forecast: 75, grade: "Held10To25", percent: 25 },
		{ actual: 99, forecast: 74, grade: "HeldOver25", percent: 26 },
		{ actual: 300, forecast: 299, grade: "HeldWithin10", percent: 1 },
		{ actual: 40, forecast: 44, grade: "NotHeldWithin10", percent: 10 },
		{ actual: 39, forecast: 43, grade: "NotHeld10To25", percent: 11 },
	])(
		`@boundary shows a percentage that never contradicts its band at the edges - $forecast against $actual is $percent% (${PENDING})`,
		({ actual, forecast, grade, percent }) => {
			const reading = readCheck(aCheck(actual, forecast));

			expect(reading.grade).toBe(grade);
			expect(reading.percentOfActual).toBe(percent);
		},
	);

	it.skip(`@boundary a forecast the Team delivered exactly held within 10%, 0 off and 0% (${PENDING})`, () => {
		expect(readCheck(aCheck(40, 40))).toEqual({
			grade: "HeldWithin10",
			miss: 0,
			percentOfActual: 0,
		});
	});

	it.skip(`@error a forecast of nothing in a period that delivered nothing held exactly, with no percentage (${PENDING})`, () => {
		expect(readCheck(aCheck(0, 0))).toEqual({
			grade: "HeldWithin10",
			miss: 0,
			percentOfActual: null,
		});
	});

	it.skip(`@error a forecast above nothing in a period that delivered nothing did not hold, the largest miss, with no percentage (${PENDING})`, () => {
		expect(readCheck(aCheck(0, 2))).toEqual({
			grade: "NotHeldOver25",
			miss: -2,
			percentOfActual: null,
		});
	});

	it.skip(`takes whether a forecast held from the server and never recounts it (${PENDING})`, () => {
		expect(readCheck(aCheck(42, 40, false)).grade).toBe("NotHeldWithin10");
		expect(readCheck(aCheck(40, 42, true)).grade).toBe("HeldWithin10");
	});

	it.skip(`@property for every actual from 1 to 200 and every forecast from 0 to 400 the percentage sits in its band, stays within 1 of the true share of the actual, and is at least 1 whenever the forecast missed (${PENDING})`, () => {
		const contradictions: string[] = [];

		for (let actual = 1; actual <= 200; actual++) {
			for (let forecast = 0; forecast <= 400; forecast++) {
				const held = actual >= forecast;
				const off = Math.abs(actual - forecast);
				const reading = readCheck(aCheck(actual, forecast, held));
				const percent = reading.percentOfActual ?? -1;
				const band = bandOf(reading.grade);

				if (
					band !== bandByTheRule(off, actual) ||
					!shownInsideItsBand(band, percent) ||
					(off > 0 && percent < 1) ||
					Math.abs(percent - (100 * off) / actual) >= 1 ||
					reading.grade.startsWith("Held") !== held ||
					reading.miss !== actual - forecast
				) {
					contradictions.push(
						`${forecast} against ${actual}: ${reading.grade}, ${reading.miss}, ${percent}%`,
					);
				}
			}
		}

		expect(contradictions.slice(0, 10)).toEqual([]);
		expect(contradictions).toHaveLength(0);
	});
});

describe("@us-05 @slice-05 @kpi-OUT-6094-caution-is-visible how close one level usually landed", () => {
	it.skip(`counts the checks that landed within 10% on either side, held or not (${PENDING})`, () => {
		const closeness = levelCloseness([
			...heldWithin10(2),
			...notHeldWithin10(4),
			...held10To25(5),
			...heldOver25(5),
		]);

		expect(closeness.gradedChecks).toBe(16);
		expect(closeness.withinTenPercent).toBe(6);
	});

	it.skip(`names the grade more than half the checks share - Maria's 85th, low by more than a quarter in 9 of 16 (${PENDING})`, () => {
		const closeness = levelCloseness([
			...heldOver25(9),
			...heldWithin10(3),
			...held10To25(3),
			...notHeld10To25(1),
		]);

		expect(closeness).toEqual({
			gradedChecks: 16,
			withinTenPercent: 3,
			usualGrade: "HeldOver25",
		});
	});

	it.skip(`@boundary names no grade when one holds exactly half the checks (${PENDING})`, () => {
		expect(
			levelCloseness([...heldOver25(8), ...held10To25(8)]).usualGrade,
		).toBeNull();
	});

	it.skip(`@boundary names no grade when the checks within 10% are split between held and not held (${PENDING})`, () => {
		const closeness = levelCloseness([
			...heldWithin10(5),
			...notHeldWithin10(5),
			...held10To25(3),
			...notHeld10To25(3),
		]);

		expect(closeness.withinTenPercent).toBe(10);
		expect(closeness.usualGrade).toBeNull();
	});

	it.skip(`@error a level with no check that could run has nothing within 10% and no usual grade (${PENDING})`, () => {
		expect(levelCloseness([])).toEqual({
			gradedChecks: 0,
			withinTenPercent: 0,
			usualGrade: null,
		});
	});
});
