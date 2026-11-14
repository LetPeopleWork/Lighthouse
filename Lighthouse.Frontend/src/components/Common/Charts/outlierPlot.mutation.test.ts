import { describe, expect, it } from "vitest";
import {
	allLineValues,
	buildPlotLines,
	currentNamedPercentiles,
	reservedMarkerRows,
} from "./outlierPlot";

const p = (percentile: number, value: number) => ({ percentile, value });

describe("currentNamedPercentiles", () => {
	const fetcher = async () => [];
	const answer = { definitionId: 3, fetcher, percentiles: [p(50, 4)] };

	it("returns the answer only for the same definition and fetcher", () => {
		expect(currentNamedPercentiles(answer, 3, fetcher)).toEqual([p(50, 4)]);
		expect(currentNamedPercentiles(answer, 4, fetcher)).toEqual([]);
		expect(currentNamedPercentiles(answer, 3, async () => [])).toEqual([]);
		expect(currentNamedPercentiles(null, 3, fetcher)).toEqual([]);
	});
});

describe("buildPlotLines", () => {
	const input = {
		percentiles: [p(50, 4), p(85, 9)],
		visiblePercentiles: { 50: false, 85: true } as Record<number, boolean>,
		sle: p(90, 12),
		sleVisible: true,
		sleTerm: "Target",
		sleColor: "red",
	};

	it("keeps only the ticked percentiles and names each line", () => {
		const lines = buildPlotLines(input);
		expect(lines.map((l) => l.key)).toEqual([
			"percentile-85",
			"sle-reference-line",
		]);
		expect(lines[0].aboveAxis).toEqual({
			kind: "percentile",
			percentile: 85,
			value: 9,
		});
		expect(lines[0].markerText).toBe("85%: 9 days");
		expect(lines[1].aboveAxis).toEqual({ kind: "sle", value: 12 });
		expect(lines[1].markerText).toBe("Target: 90% @ 12 days or less");
	});

	it("leaves the SLE out when it is switched off", () => {
		expect(buildPlotLines({ ...input, sleVisible: false })).toHaveLength(1);
	});
});

describe("allLineValues", () => {
	it("lists the percentile values, and the SLE only when there is one", () => {
		expect(allLineValues([p(50, 4)], null)).toEqual([4]);
		expect(allLineValues([p(50, 4)], p(90, 12))).toEqual([4, 12]);
	});
});

describe("reservedMarkerRows", () => {
	const percentiles = [p(50, 4), p(85, 20)];

	it("counts the lines strictly above the axis, ticked or not", () => {
		expect(reservedMarkerRows(percentiles, p(90, 30), 10, true)).toBe(2);
		expect(reservedMarkerRows(percentiles, null, 20, true)).toBe(0);
		expect(reservedMarkerRows(percentiles, null, 19, true)).toBe(1);
		expect(reservedMarkerRows(percentiles, null, 4, true)).toBe(1);
	});

	it("reserves nothing while no outlier shapes the axis", () => {
		expect(reservedMarkerRows(percentiles, p(90, 30), 10, false)).toBe(0);
	});
});
