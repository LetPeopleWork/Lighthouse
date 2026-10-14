import { describe, expect, it } from "vitest";
import type { ReportAbsentReason } from "../../../../models/Reports/Report";
import {
	absentReasonText,
	changeText,
	compareThenNow,
	type DirectionOfGood,
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
