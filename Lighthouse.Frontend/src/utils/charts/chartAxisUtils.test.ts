import { describe, expect, it } from "vitest";
import type { IPercentileValue } from "../../models/PercentileValue";
import {
	createLinearAxis,
	createTimeAxis,
	dateValueFormatter,
	getDateOnlyTimestamp,
	getMaxYAxisHeight,
	integerValueFormatter,
} from "./chartAxisUtils";

describe("chartAxisUtils", () => {
	describe("integerValueFormatter", () => {
		it("should return string for integer values", () => {
			expect(integerValueFormatter(5)).toBe("5");
			expect(integerValueFormatter(0)).toBe("0");
			expect(integerValueFormatter(-10)).toBe("-10");
		});

		it("should return empty string for non-integer values", () => {
			expect(integerValueFormatter(5.5)).toBe("");
			expect(integerValueFormatter(0.1)).toBe("");
			expect(integerValueFormatter(-3.7)).toBe("");
		});
	});

	describe("dateValueFormatter", () => {
		it("should format timestamp using toLocaleDateString", () => {
			const timestamp = new Date(2026, 0, 15).getTime(); // Jan 15, 2026
			const expected = new Date(timestamp).toLocaleDateString();
			expect(dateValueFormatter(timestamp)).toBe(expected);
		});

		it("should handle different dates consistently with toLocaleDateString", () => {
			const dates = [
				new Date(2025, 5, 20).getTime(),
				new Date(2024, 11, 31).getTime(),
				new Date(2026, 0, 1).getTime(),
			];

			for (const timestamp of dates) {
				expect(dateValueFormatter(timestamp)).toBe(
					new Date(timestamp).toLocaleDateString(),
				);
			}
		});
	});

	describe("getMaxYAxisHeight", () => {
		const createPercentile = (
			percentile: number,
			value: number,
		): IPercentileValue => ({
			percentile,
			value,
		});

		it("should return 1.1x the max from percentiles", () => {
			const percentiles = [
				createPercentile(50, 10),
				createPercentile(85, 20),
				createPercentile(95, 30),
			];

			const result = getMaxYAxisHeight({
				percentiles,
				dataPoints: [],
				getDataValue: () => 0,
			});

			expect(result).toBe(33); // 30 * 1.1 = 33
		});

		it("should consider serviceLevelExpectation in max calculation", () => {
			const percentiles = [createPercentile(85, 20)];
			const sle = createPercentile(90, 50);

			const result = getMaxYAxisHeight({
				percentiles,
				serviceLevelExpectation: sle,
				dataPoints: [],
				getDataValue: () => 0,
			});

			expect(result).toBeCloseTo(55); // 50 * 1.1 = 55
		});

		it("should consider data points in max calculation", () => {
			const dataPoints = [{ value: 100 }, { value: 50 }, { value: 75 }];

			const result = getMaxYAxisHeight({
				percentiles: [],
				dataPoints,
				getDataValue: (item) => item.value,
			});

			expect(result).toBeCloseTo(110); // 100 * 1.1 = 110
		});

		it("should respect minValue when it is the largest", () => {
			const result = getMaxYAxisHeight({
				percentiles: [],
				dataPoints: [],
				getDataValue: () => 0,
				minValue: 200,
			});

			expect(result).toBeCloseTo(220); // 200 * 1.1 = 220
		});

		it("should handle empty arrays with default minValue", () => {
			const result = getMaxYAxisHeight({
				percentiles: [],
				dataPoints: [],
				getDataValue: () => 0,
			});

			expect(result).toBe(0); // 0 * 1.1 = 0
		});

		it("should return max across all sources multiplied by 1.1", () => {
			const percentiles = [createPercentile(85, 40)];
			const sle = createPercentile(90, 30);
			const dataPoints = [{ value: 25 }];

			const result = getMaxYAxisHeight({
				percentiles,
				serviceLevelExpectation: sle,
				dataPoints,
				getDataValue: (item) => item.value,
				minValue: 10,
			});

			expect(result).toBe(44); // 40 * 1.1 = 44
		});

		it("should handle null serviceLevelExpectation", () => {
			const result = getMaxYAxisHeight({
				percentiles: [createPercentile(85, 25)],
				serviceLevelExpectation: null,
				dataPoints: [],
				getDataValue: () => 0,
			});

			expect(result).toBeCloseTo(27.5); // 25 * 1.1 = 27.5
		});
	});

	describe("createTimeAxis", () => {
		it("should create time axis with label and formatter", () => {
			const axis = createTimeAxis("Date");

			expect(axis.id).toBe("timeAxis");
			expect(axis.scaleType).toBe("time");
			expect(axis.label).toBe("Date");
			expect(axis.min).toBeUndefined();
			expect(axis.max).toBeUndefined();
			expect(axis.valueFormatter).toBe(dateValueFormatter);
		});

		it("should set domain min and max when provided", () => {
			const domain: [number, number] = [1000, 2000];
			const axis = createTimeAxis("Timeline", domain);

			expect(axis.min).toBe(1000);
			expect(axis.max).toBe(2000);
		});

		it("should handle null domain", () => {
			const axis = createTimeAxis("Date", null);

			expect(axis.min).toBeUndefined();
			expect(axis.max).toBeUndefined();
		});
	});

	describe("createLinearAxis", () => {
		it("should create linear axis with id and label", () => {
			const axis = createLinearAxis("yAxis", "Value");

			expect(axis.id).toBe("yAxis");
			expect(axis.scaleType).toBe("linear");
			expect(axis.label).toBe("Value");
		});

		it("should set min and max when provided", () => {
			const axis = createLinearAxis("yAxis", "Count", { min: 0, max: 100 });

			expect(axis.min).toBe(0);
			expect(axis.max).toBe(100);
		});

		it("should use integer formatter when requested", () => {
			const axis = createLinearAxis("yAxis", "Items", {
				useIntegerFormatter: true,
			});

			expect(axis.valueFormatter).toBe(integerValueFormatter);
		});

		it("should not set formatter when useIntegerFormatter is false", () => {
			const axis = createLinearAxis("yAxis", "Items", {
				useIntegerFormatter: false,
			});

			expect(axis.valueFormatter).toBeUndefined();
		});

		it("should set tick options when provided", () => {
			const tickLabelInterval = () => true;
			const axis = createLinearAxis("yAxis", "Items", {
				tickNumber: 5,
				tickLabelInterval,
				disableTicks: true,
			});

			expect(axis.tickNumber).toBe(5);
			expect(axis.tickLabelInterval).toBe(tickLabelInterval);
			expect(axis.disableTicks).toBe(true);
		});
	});

	describe("getDateOnlyTimestamp", () => {
		it("should return timestamp with time set to midnight", () => {
			const date = new Date(2026, 0, 27, 14, 30, 45, 123);
			const result = getDateOnlyTimestamp(date);

			const expectedDate = new Date(2026, 0, 27, 0, 0, 0, 0);
			expect(result).toBe(expectedDate.getTime());
		});

		it("should handle date already at midnight", () => {
			const date = new Date(2026, 5, 15, 0, 0, 0, 0);
			const result = getDateOnlyTimestamp(date);

			expect(result).toBe(date.getTime());
		});

		it("should not mutate the original date", () => {
			const original = new Date(2026, 0, 27, 14, 30, 45, 123);
			const originalTime = original.getTime();

			getDateOnlyTimestamp(original);

			expect(original.getTime()).toBe(originalTime);
		});

		it("should handle dates at end of day", () => {
			const date = new Date(2026, 11, 31, 23, 59, 59, 999);
			const result = getDateOnlyTimestamp(date);

			const expectedDate = new Date(2026, 11, 31, 0, 0, 0, 0);
			expect(result).toBe(expectedDate.getTime());
		});
	});
});
