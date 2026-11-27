import { describe, expect, it } from "vitest";
import type { IWithinSleDay } from "../../../models/Metrics/WithinSleOverTime";
import { withinSleLinesOf } from "./withinSleLines";

const day = (recordedAt: string, sharePercent: number): IWithinSleDay => ({
	recordedAt,
	horizon: 30,
	closedCount: 10,
	withinCount: 7,
	slePercentile: 70,
	sleDays: 10,
	sharePercent,
	verdict: "Measured",
	origin: "Recorded",
});

describe("withinSleLinesOf", () => {
	it("draws the days in calendar order whatever order the server sends them in", () => {
		const lines = withinSleLinesOf([
			day("2026-03-03T00:00:00Z", 80),
			day("2026-03-01T00:00:00Z", 60),
			day("2026-03-02T00:00:00Z", 70),
		]);

		expect(lines.dates).toEqual(["2026-03-01", "2026-03-02", "2026-03-03"]);
		expect(lines.share).toEqual([60, 70, 80]);
	});

	it("draws nothing when no day is stored", () => {
		expect(withinSleLinesOf([])).toEqual({
			dates: [],
			share: [],
			target: [],
			filledIn: [],
		});
	});
});
