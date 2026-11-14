import fc from "fast-check";
import { describe, expect, it } from "vitest";
import {
	axisTop,
	fiftiethPercentile,
	findOutliers,
	OUTLIER_MEDIAN_MULTIPLE,
} from "./outliers";

const runs = { numRuns: 100 };
const days = fc.integer({ min: 1, max: 400 });
const someDays = fc.array(days, { minLength: 1, maxLength: 60 });
const identity = (value: number) => value;

describe("fiftiethPercentile", () => {
	it.each([
		[[], null],
		[[7], 7],
		[[3, 9], 3],
		[[2, 4, 6, 6, 8], 4],
		[[2, 4, 6, 6, 8, 58], 6],
		[[9, 1, 5, 3], 3],
	])("reads %j as %s, as the backend percentile does", (values, expected) => {
		expect(fiftiethPercentile(values)).toBe(expected);
	});

	it("is always one of the values", () => {
		fc.assert(
			fc.property(someDays, (values) => {
				expect(values).toContain(fiftiethPercentile(values));
			}),
			runs,
		);
	});
});

describe("findOutliers", () => {
	it("partitions the input without changing it", () => {
		fc.assert(
			fc.property(someDays, (values) => {
				const input = [...values];

				const { outliers, typical } = findOutliers(input, identity);

				expect(input).toEqual(values);
				expect([...outliers, ...typical].sort((a, b) => a - b)).toEqual(
					[...values].sort((a, b) => a - b),
				);
			}),
			runs,
		);
	});

	it("flags exactly the values strictly above five times the median, longest first", () => {
		fc.assert(
			fc.property(someDays, (values) => {
				const { median, cut, outliers, typical } = findOutliers(
					values,
					identity,
				);

				expect(cut).toBe((median as number) * OUTLIER_MEDIAN_MULTIPLE);
				expect(outliers.every((value) => value > (cut as number))).toBe(true);
				expect(typical.every((value) => value <= (cut as number))).toBe(true);
				expect(outliers).toEqual([...outliers].sort((a, b) => b - a));
			}),
			runs,
		);
	});

	it("applies no rule to an empty list or a median of zero", () => {
		expect(findOutliers([], identity).cut).toBeNull();
		expect(findOutliers([0, 0, 40], identity)).toMatchObject({
			cut: null,
			outliers: [],
		});
	});
});

describe("axisTop", () => {
	const scenario = fc
		.record({ typical: someDays, lines: fc.array(days, { maxLength: 6 }) })
		.map(({ typical, lines }) => ({
			typical,
			lines,
			analysis: findOutliers(typical, identity),
		}));

	it("fits every typical value and every line within the cut, and never rises above the cut", () => {
		fc.assert(
			fc.property(scenario, ({ typical, lines, analysis }) => {
				const hasOutliers = analysis.outliers.length > 0;
				const top = axisTop({
					typicalValues: analysis.typical,
					lineValues: lines,
					cut: analysis.cut,
					hasOutliers,
				});

				if (!hasOutliers) {
					expect(top).toBeGreaterThanOrEqual(Math.max(...typical, ...lines));
					return;
				}
				expect(top).toBeLessThanOrEqual(analysis.cut as number);
				expect(top).toBeGreaterThanOrEqual(
					Math.min(Math.max(...analysis.typical), analysis.cut as number),
				);
				for (const outlier of analysis.outliers) {
					expect(outlier).toBeGreaterThan(top);
				}
			}),
			runs,
		);
	});
});
