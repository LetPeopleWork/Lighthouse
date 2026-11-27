import { describe, expect, it } from "vitest";
import type {
	IWithinSleDay,
	WithinSleDayOrigin,
} from "../../../models/Metrics/WithinSleOverTime";
import { withinSleLinesOf } from "./withinSleLines";

const day = (
	recordedAt: string,
	origin: WithinSleDayOrigin,
	closedCount: number,
	withinCount: number,
	sharePercent: number | null,
): IWithinSleDay => ({
	recordedAt,
	horizon: 30,
	closedCount,
	withinCount,
	slePercentile: 70,
	sleDays: 10,
	origin,
	verdict: sharePercent === null ? "TooFew" : "Measured",
	sharePercent,
});

describe("which days of the Within SLE line are drawn as filled in", () => {
	it("marks only a filled-in day that draws a point; a recorded day, a filled-in day too few to judge and a day with nothing stored are not marked", () => {
		const lines = withinSleLinesOf([
			day("2026-11-03", "FilledIn", 5, 4, 80),
			day("2026-11-04", "Recorded", 5, 3, 60),
			day("2026-11-05", "FilledIn", 3, 3, null),
			day("2026-11-07", "FilledIn", 8, 6, 75),
		]);

		expect(
			lines.dates.map((date, at) => [date, lines.filledIn[at]]),
			"Each drawn day must say whether it was filled in from history, so the chart can draw it as an open marker: " +
				"true only for a filled-in day with a share, false for a recorded day, for a day too few to judge and for the gap on the 6th.",
		).toEqual([
			["2026-11-03", true],
			["2026-11-04", false],
			["2026-11-05", false],
			["2026-11-06", false],
			["2026-11-07", true],
		]);
	});
});
