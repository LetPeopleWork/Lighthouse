import { describe, expect, it } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { computeWorkItemAgePercentilesTrend } from "./workItemAgePercentilesTrend";

/**
 * US-05 AC4 / D5 — previous-period trend for the Work Item Age Percentiles widget.
 *
 * These tests exist because mutation testing (2026-07-19) found the whole selector reachable only
 * through BaseMetricsView, where the trend chrome is rendered by a mock WidgetShell: every branch
 * below — the highest-percentile pick, the direction, the zero substitution, the detail rows —
 * survived mutation. The logic was extracted to its own module for the same reason blockedTrend is
 * its own module, and is now pinned directly.
 */
describe("computeWorkItemAgePercentilesTrend", () => {
	const pct = (percentile: number, value: number): IPercentileValue =>
		({ percentile, value }) as IPercentileValue;

	it("headlines the HIGHEST configured percentile, not the first or the median", () => {
		const trend = computeWorkItemAgePercentilesTrend(
			[pct(50, 3), pct(95, 21), pct(85, 12)],
			[pct(50, 2), pct(95, 14), pct(85, 9)],
		);

		expect(trend.metricLabel).toBe("Highest Work Item Age Percentile");
		expect(trend.currentLabel).toBe("95th percentile, selected range");
		expect(trend.previousLabel).toBe("95th percentile, previous range");
		expect(trend.currentValue).toBe("21 days");
		expect(trend.previousValue).toBe("14 days");
	});

	it("points the arrow at the direction the headline percentile moved", () => {
		const worse = computeWorkItemAgePercentilesTrend(
			[pct(85, 20)],
			[pct(85, 10)],
		);
		const better = computeWorkItemAgePercentilesTrend(
			[pct(85, 10)],
			[pct(85, 20)],
		);
		const unchanged = computeWorkItemAgePercentilesTrend(
			[pct(85, 10)],
			[pct(85, 10)],
		);

		expect(worse.direction).toBe("up");
		expect(better.direction).toBe("down");
		expect(unchanged.direction).toBe("flat");
	});

	it("carries the lower percentiles as detail rows so the arrow is never the whole story", () => {
		const trend = computeWorkItemAgePercentilesTrend(
			[pct(50, 3), pct(85, 12), pct(95, 21)],
			[pct(50, 5), pct(85, 9)],
		);

		expect(trend.detailRows).toEqual([
			{
				label: "50th percentile",
				currentValue: "3 days",
				previousValue: "5 days",
			},
			{
				label: "85th percentile",
				currentValue: "12 days",
				previousValue: "9 days",
			},
		]);
	});

	it("omits detail rows entirely when the highest percentile is the only one configured", () => {
		const trend = computeWorkItemAgePercentilesTrend(
			[pct(85, 4)],
			[pct(85, 4)],
		);

		expect(trend.detailRows).toBeUndefined();
	});

	it("counts a percentile missing from the previous window as zero rather than dropping the row", () => {
		// An empty previous snapshot means nothing was ageing then — a real reading, not a gap.
		const trend = computeWorkItemAgePercentilesTrend(
			[pct(50, 3), pct(95, 21)],
			[],
		);

		expect(trend.direction).toBe("up");
		expect(trend.previousValue).toBe("0 days");
		expect(trend.detailRows).toEqual([
			{
				label: "50th percentile",
				currentValue: "3 days",
				previousValue: "0 days",
			},
		]);
	});

	it("still names the percentile when only the previous window has one", () => {
		const trend = computeWorkItemAgePercentilesTrend([], [pct(85, 6)]);

		expect(trend.currentLabel).toBe("85th percentile, selected range");
		expect(trend.currentValue).toBe("0 days");
		expect(trend.direction).toBe("down");
	});

	it("falls back to range-only labels when neither window has a percentile", () => {
		const trend = computeWorkItemAgePercentilesTrend([], []);

		expect(trend.currentLabel).toBe("Selected range");
		expect(trend.previousLabel).toBe("Previous range");
		expect(trend.direction).toBe("flat");
		expect(trend.currentValue).toBe("0 days");
	});

	it("says '1 day', not '1 days'", () => {
		const trend = computeWorkItemAgePercentilesTrend(
			[pct(50, 1), pct(85, 1)],
			[pct(85, 2)],
		);

		expect(trend.currentValue).toBe("1 day");
		expect(trend.previousValue).toBe("2 days");
		expect(trend.detailRows?.[0].currentValue).toBe("1 day");
	});
});
