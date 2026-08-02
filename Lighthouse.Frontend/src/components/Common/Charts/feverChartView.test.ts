import { describe, expect, it } from "vitest";
import type {
	FeatureFeverSeries,
	FeverPoint,
} from "../../../models/Delivery/FeverTrail";
import { testTheme } from "../../../tests/testTheme";
import {
	currentPoint,
	likelihoodTooltip,
	runButtonLabel,
	visiblePoints,
	zoneBandPath,
	zoneColors,
} from "./feverChartView";

const point = (completion: number, chanceOfLate: number): FeverPoint => ({
	date: new Date("2026-06-01T00:00:00Z"),
	completion,
	chanceOfLate,
});

const series = (points: FeverPoint[]): FeatureFeverSeries => ({
	referenceId: "F-1",
	name: "Checkout",
	points,
	latest: points[points.length - 1],
});

describe("likelihoodTooltip", () => {
	it("renders an empty string when there is no value", () => {
		expect(likelihoodTooltip(null)).toBe("");
	});

	it.each([
		[5, "95% Likelihood"],
		[90, "10% Likelihood"],
	])("renders %i%% chance-of-late as the likelihood", (chanceOfLate, label) => {
		expect(likelihoodTooltip({ x: 50, y: chanceOfLate })).toBe(label);
	});
});

describe("currentPoint", () => {
	const feature = series([point(20, 10), point(60, 5)]);

	it("shows the latest point when no frame is selected", () => {
		expect(currentPoint(feature, null)).toBe(feature.latest);
	});

	it("shows the first point at frame zero", () => {
		expect(currentPoint(feature, 0)).toBe(feature.points[0]);
	});

	it("holds at the last point when the frame runs past the feature length", () => {
		expect(currentPoint(feature, 9)).toBe(feature.points[1]);
	});
});

describe("visiblePoints", () => {
	const feature = series([point(20, 10), point(60, 5)]);

	it("projects the current point to a single scatter datum", () => {
		expect(visiblePoints(feature, null)).toEqual([{ x: 60, y: 5, id: 0 }]);
		expect(visiblePoints(feature, 0)).toEqual([{ x: 20, y: 10, id: 0 }]);
	});
});

describe("runButtonLabel", () => {
	it.each([
		[true, null, "Running…"],
		[false, null, "Run"],
		[false, 2, "Rerun"],
	])(
		"labels the run control for running=%s frame=%s",
		(isRunning, frame, label) => {
			expect(runButtonLabel(isRunning, frame)).toBe(label);
		},
	);
});

describe("zoneColors", () => {
	it("maps each fever zone to its semantic theme colour", () => {
		const colors = zoneColors(testTheme);

		expect(colors.green).toBe(testTheme.palette.success.main);
		expect(colors.amber).toBe(testTheme.palette.warning.main);
		expect(colors.red).toBe(testTheme.palette.error.main);
	});
});

describe("zoneBandPath", () => {
	it("builds a closed SVG path from the polygon points and scales", () => {
		const identity = (value: number) => value;
		const path = zoneBandPath(
			[
				[0, 0],
				[100, 0],
				[100, 50],
			],
			identity,
			identity,
		);

		expect(path).toBe("M 0 0 L 100 0 L 100 50 Z");
	});
});
