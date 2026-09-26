import { describe, expect, it } from "vitest";
import { positionIn, rowExtent } from "./RealityCheckBandRow";

describe("rowExtent", () => {
	it.each([
		{ where: "below the band", actual: 20, expected: { min: 17, max: 53 } },
		{
			where: "on the band's low edge",
			actual: 30,
			expected: { min: 28, max: 52 },
		},
		{ where: "inside the band", actual: 42, expected: { min: 28, max: 52 } },
		{
			where: "on the band's high edge",
			actual: 50,
			expected: { min: 28, max: 52 },
		},
		{ where: "above the band", actual: 60, expected: { min: 27, max: 63 } },
	])(
		"spans the band and a mark $where, padded by a tenth either side",
		({ actual, expected }) => {
			expect(rowExtent(30, 50, actual)).toEqual(expected);
		},
	);

	it.each([
		{ where: "below the band", actual: 20 },
		{ where: "above the band", actual: 60 },
	])(
		"keeps a mark $where inside the row rather than on its edge",
		({ actual }) => {
			const position = positionIn(rowExtent(30, 50, actual), actual);

			expect(position).toBeGreaterThan(0);
			expect(position).toBeLessThan(100);
		},
	);

	it("pads by at least one item when the band and the mark all sit on one count", () => {
		expect(rowExtent(5, 5, 5)).toEqual({ min: 4, max: 6 });
	});

	it("pads by at least one item when a tenth of the span would be less", () => {
		expect(rowExtent(4, 5, 5)).toEqual({ min: 3, max: 6 });
	});
});

describe("positionIn", () => {
	it.each([
		{ value: 28, expected: 0 },
		{ value: 40, expected: 50 },
		{ value: 52, expected: 100 },
	])("places $value at $expected% of the row", ({ value, expected }) => {
		expect(positionIn({ min: 28, max: 52 }, value)).toBe(expected);
	});
});
