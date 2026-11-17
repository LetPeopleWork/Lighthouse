import { describe, expect, it } from "vitest";
import { axisTop } from "./outliers";

describe("the axis edge cases", () => {
	it("ignores the limit when no outlier is set apart", () => {
		expect(
			axisTop({
				typicalValues: [100],
				lineValues: [50],
				cut: 5,
				hasOutliers: false,
			}),
		).toBeCloseTo(110, 5);
	});

	it("fits every drawn item and line when outliers exist but no limit is known", () => {
		expect(
			axisTop({
				typicalValues: [10],
				lineValues: [20],
				cut: null,
				hasOutliers: true,
			}),
		).toBeCloseTo(22, 5);
	});
});
