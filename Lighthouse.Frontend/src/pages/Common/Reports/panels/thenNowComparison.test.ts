import { describe, expect, it } from "vitest";
import type {
	IReportMetric,
	IReportValue,
	ReportAbsentReason,
	ReportMetricKey,
} from "../../../../models/Reports/Report";
import { reportPanelRegistry } from "./reportPanelRegistry";
import {
	absentReasonText,
	changeText,
	compareThenNow,
	type DirectionOfGood,
	lineComparisonsOf,
	roundForDisplay,
	signed,
} from "./thenNowComparison";

/**
 * The change a panel shows is worked out from the two values exactly as they are displayed, so Then plus the
 * change reads as Now on screen. Whether a change is good depends on the metric, and it is told by sign and mark
 * as well as by the verdict the colour follows.
 */

describe("compareThenNow", () => {
	it.each<
		[
			string,
			number,
			number,
			DirectionOfGood,
			number,
			number,
			number | null,
			string,
			string,
		]
	>([
		["a shorter Cycle Time", 21, 12, "lower", 0, -9, -43, "▼", "better"],
		["a longer Cycle Time", 12, 21, "lower", 0, 9, 75, "▲", "worse"],
		["a higher Throughput", 0.58, 0.72, "higher", 2, 0.14, 24, "▲", "better"],
		["a lower Throughput", 0.72, 0.58, "higher", 2, -0.14, -19, "▼", "worse"],
		["a lower WIP", 9, 7, "lower", 0, -2, -22, "▼", "better"],
		[
			"an average WIP of 8.62 shown as 9",
			8.62,
			7.38,
			"lower",
			0,
			-2,
			-22,
			"▼",
			"better",
		],
		["a growing Work Item Age", 610, 880, "lower", 0, 270, 44, "▲", "worse"],
		["no change", 12, 12, "lower", 0, 0, 0, "–", "neutral"],
		["no change where higher is good", 5, 5, "higher", 0, 0, 0, "–", "neutral"],
		["a rise from a Then of 0", 0, 24, "higher", 0, 24, null, "▲", "better"],
		["a Then of 0 that stays 0", 0, 0, "higher", 0, 0, null, "–", "neutral"],
		[
			"values compared as displayed, not as sent",
			20.6,
			12.4,
			"lower",
			0,
			-9,
			-43,
			"▼",
			"better",
		],
		[
			"a half percent rounded away from zero when falling",
			8,
			7.96,
			"lower",
			2,
			-0.04,
			-1,
			"▼",
			"better",
		],
		[
			"a half percent rounded away from zero when rising",
			8,
			8.04,
			"lower",
			2,
			0.04,
			1,
			"▲",
			"worse",
		],
	])(
		"%s: Then %d, Now %d",
		(_case, then, now, good, decimals, delta, percent, mark, verdict) => {
			expect(compareThenNow(then, now, good, decimals)).toEqual({
				delta,
				percent,
				mark,
				verdict,
			});
		},
	);
});

describe("roundForDisplay", () => {
	it.each([
		[21.4, 0, 21],
		[21.5, 0, 22],
		[-21.5, 0, -22],
		[0.575, 1, 0.6],
		[0.13999999999999999, 2, 0.14],
		[7, 2, 7],
		// The binary double is rounded, not the decimal text: 1.005 × 100 is just below 100.5, so it rounds down. Accepted.
		[1.005, 2, 1],
	])("rounds %d to %d decimals as %d", (value, decimals, expected) => {
		expect(roundForDisplay(value, decimals)).toBe(expected);
	});
});

describe("signed", () => {
	it.each([
		[9, "+9"],
		[-9, "−9"],
		[0.14, "+0.14"],
		[-0.14, "−0.14"],
		[0, "0"],
	])("writes %d as %s", (value, expected) => {
		expect(signed(value)).toBe(expected);
	});
});

describe("changeText", () => {
	it.each<[number, number, DirectionOfGood, string, string]>([
		[21, 12, "lower", "9 days", "9 days (−43 %)"],
		[5, 6, "lower", "1 day", "1 day (+20 %)"],
		[9, 9, "lower", "0 days", "No change"],
		[0, 24, "higher", "24 per day", "24 per day (Then was 0)"],
	])(
		"tells Then %d against Now %d as written",
		(then, now, good, magnitude, expected) => {
			expect(changeText(compareThenNow(then, now, good, 0), magnitude)).toBe(
				expected,
			);
		},
	);
});

describe("absentReasonText", () => {
	it.each<[ReportAbsentReason, "Then" | "Now", string]>([
		["no-finished-items", "Then", "No finished Tickets in Then"],
		["no-finished-items", "Now", "No finished Tickets in Now"],
	])("says why %s on %s reads as a dash", (reason, side, expected) => {
		expect(absentReasonText(reason, side, "Tickets")).toBe(expected);
	});
});

const aValue = (value: number): IReportValue => ({
	value,
	sampleSize: 90,
	absentReason: null,
});

const nothingFinished: IReportValue = {
	value: null,
	sampleSize: 0,
	absentReason: "no-finished-items",
};

/** Every member any panel draws, on both sides, so each panel finds all of its lines. */
const EVERY_LINE: IReportMetric = {
	atThen: {
		p95: aValue(34),
		p85: aValue(21),
		p70: aValue(10),
		p50: aValue(5),
		total: aValue(52),
		perDay: aValue(0.58),
		average: aValue(8.62),
		median: aValue(9),
		min: aValue(6),
		max: aValue(12),
	},
	atNow: {
		p95: aValue(19),
		p85: aValue(12),
		p70: aValue(7),
		p50: aValue(4),
		total: aValue(65),
		perDay: aValue(0.72),
		average: aValue(7.38),
		median: aValue(7),
		min: aValue(5),
		max: aValue(9),
	},
};

const withThen = (atThen: IReportMetric["atThen"]): IReportMetric => ({
	...EVERY_LINE,
	atThen: { ...EVERY_LINE.atThen, ...atThen },
});

const withSides = (
	atThen: IReportMetric["atThen"],
	atNow: IReportMetric["atNow"],
): IReportMetric => ({
	atThen: { ...EVERY_LINE.atThen, ...atThen },
	atNow: { ...EVERY_LINE.atNow, ...atNow },
});

const comparisonsOf = (metric: IReportMetric, key: ReportMetricKey) =>
	lineComparisonsOf(metric, reportPanelRegistry[key]);

const changeOn = (metric: IReportMetric, key: ReportMetricKey, line: string) =>
	comparisonsOf(metric, key).find((comparison) => comparison.key === line);

/**
 * The summary header tallies the very judgement each panel draws, so a line is compared exactly as its panel shows
 * it: the panel's own change lines, at their decimals, by the panel's direction of good, and no change at all where
 * either side reads as a dash.
 */
describe("lineComparisonsOf", () => {
	// @us-05b @slice-05b @contract-shape:pure-function
	// Throughput's total carries no change mark, so it is no change line; the 95th sits above the 85th.
	it.each<[ReportMetricKey, string[]]>([
		["cycle-time", ["p95", "p85", "p70", "p50"]],
		["throughput", ["perDay"]],
		["wip", ["average", "median", "min", "max"]],
		["work-item-age", ["average", "median", "min", "max"]],
	])(
		"compares the change lines of %s, in the panel's row order",
		(key, lines) => {
			expect(comparisonsOf(EVERY_LINE, key).map((line) => line.key)).toEqual(
				lines,
			);
		},
	);

	// @us-05b @slice-05b @error @contract-shape:pure-function
	it.each<[string, IReportMetric]>([
		[
			"a report frozen before it was captured has no Then for it",
			{
				atThen: { p70: aValue(10), p50: aValue(5) },
				atNow: EVERY_LINE.atNow,
			},
		],
		["Then holds it as never captured", withThen({ p85: null })],
		["Then finished nothing", withThen({ p85: nothingFinished })],
		[
			"Then gives a reason this version does not know",
			withThen({
				p85: { value: null, sampleSize: null, absentReason: "unknown" },
			}),
		],
		["Now finished nothing", withSides({}, { p85: nothingFinished })],
	])(
		"compares nothing on a line where %s, and still compares the next",
		(_, metric) => {
			expect(changeOn(metric, "cycle-time", "p85")).toEqual({
				key: "p85",
				change: null,
			});
			expect(changeOn(metric, "cycle-time", "p70")?.change?.verdict).toBe(
				"better",
			);
		},
	);

	// @us-05b @slice-05b @boundary @contract-shape:pure-function
	// A move too small to show on the panel is no move; one the panel shows counts, however small.
	it.each<[string, ReportMetricKey, string, IReportMetric, number, string]>([
		[
			"an average WIP of 8.62 against 8.71, both shown as 9",
			"wip",
			"average",
			withSides({ average: aValue(8.62) }, { average: aValue(8.71) }),
			0,
			"neutral",
		],
		[
			"a Throughput of 0.584 against 0.576 per day, both shown as 0.58",
			"throughput",
			"perDay",
			withSides({ perDay: aValue(0.584) }, { perDay: aValue(0.576) }),
			0,
			"neutral",
		],
		[
			"a Throughput of 0.58 against 0.59 per day",
			"throughput",
			"perDay",
			withSides({ perDay: aValue(0.58) }, { perDay: aValue(0.59) }),
			0.01,
			"better",
		],
		[
			"a Cycle Time 70th of 7 against 8 days",
			"cycle-time",
			"p70",
			withSides({ p70: aValue(7) }, { p70: aValue(8) }),
			1,
			"worse",
		],
		[
			"a falling Throughput",
			"throughput",
			"perDay",
			withSides({ perDay: aValue(0.72) }, { perDay: aValue(0.58) }),
			-0.14,
			"worse",
		],
		["a falling WIP", "wip", "median", EVERY_LINE, -2, "better"],
	])(
		"compares %s as its panel shows it",
		(_, key, line, metric, delta, verdict) => {
			const change = changeOn(metric, key, line)?.change;

			expect(change?.delta).toBe(delta);
			expect(change?.verdict).toBe(verdict);
		},
	);

	// @us-05b @slice-05b @boundary @contract-shape:pure-function
	// A rise from nothing has no percentage, but it is still a rise, and the summary counts it.
	it("compares a line whose Then was 0, with no percentage", () => {
		const metric = withSides({ perDay: aValue(0) }, { perDay: aValue(0.27) });

		expect(changeOn(metric, "throughput", "perDay")).toEqual({
			key: "perDay",
			change: { delta: 0.27, percent: null, mark: "▲", verdict: "better" },
		});
	});
});
