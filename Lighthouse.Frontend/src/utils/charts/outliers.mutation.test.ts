import { describe, expect, it } from "vitest";
import {
	axisTop,
	fiftiethPercentile,
	findOutliers,
	formatTimesTheMedian,
} from "./outliers";

describe("outliers edge cases", () => {
	it("sorts the values before taking the median", () => {
		expect(fiftiethPercentile([9, 1, 5, 3])).toBe(3);
	});

	it("leaves every item typical and names no outliers when the median is zero", () => {
		const result = findOutliers([0, 0, 0], (v) => v);
		expect(result.outliers).toEqual([]);
		expect(result.typical).toEqual([0, 0, 0]);
		expect(result.median).toBeNull();
	});

	it("shows a whole number of tenths with one decimal below 10 times", () => {
		expect(formatTimesTheMedian(30, 5).short).toBe("6.0x");
		expect(formatTimesTheMedian(30, 5).spoken).toBe("6.0 times the median");
		expect(formatTimesTheMedian(100, 5).short).toBe("20x");
	});

	it("ignores the cut when there are no outliers", () => {
		const open = { typicalValues: [100], lineValues: [50], hasOutliers: false };
		expect(axisTop({ ...open, cut: 5 })).toBe(axisTop({ ...open, cut: null }));
		expect(axisTop({ ...open, cut: 5 })).toBeGreaterThan(5);
	});

	it("fits all values when outliers exist but no cut is known", () => {
		const base = { typicalValues: [10], lineValues: [20] };
		expect(axisTop({ ...base, cut: null, hasOutliers: true })).toBe(
			axisTop({ ...base, cut: null, hasOutliers: false }),
		);
		expect(axisTop({ ...base, cut: null, hasOutliers: true })).toBeGreaterThan(
			0,
		);
	});
});
