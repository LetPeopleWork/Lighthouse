import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "./DeliveryMetricsHistory";
import {
	AMBER_RED_INTERCEPT,
	BAND_SLOPE,
	deriveFeverTrail,
	type FeverZone,
	feverZonePolygons,
} from "./FeverTrail";

type RawPoint = {
	date: string;
	totalWork?: number;
	doneWork?: number;
	likelihoodPercentage?: number | null;
};

const getMockPoint = (overrides: RawPoint) => ({
	date: overrides.date,
	totalWork: overrides.totalWork ?? 20,
	doneWork: overrides.doneWork ?? 0,
	remainingWork: (overrides.totalWork ?? 20) - (overrides.doneWork ?? 0),
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage:
		overrides.likelihoodPercentage === undefined
			? 90
			: overrides.likelihoodPercentage,
	whenDistribution: null,
});

const getMockHistory = (overrides: {
	points: RawPoint[];
}): DeliveryMetricsHistory =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-21T00:00:00Z",
		firstSnapshotDate: "2026-06-01T00:00:00Z",
		points: overrides.points.map(getMockPoint),
	});

const healthyHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", doneWork: 10, likelihoodPercentage: 80 },
		{ date: "2026-06-08T00:00:00Z", doneWork: 16, likelihoodPercentage: 95 },
	],
});

const degradingHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", doneWork: 2, likelihoodPercentage: 99 },
		{ date: "2026-06-08T00:00:00Z", doneWork: 5, likelihoodPercentage: 65 },
		{ date: "2026-06-15T00:00:00Z", doneWork: 8, likelihoodPercentage: 35 },
	],
});

const zonesOf = (history: DeliveryMetricsHistory): FeverZone[] =>
	deriveFeverTrail(history).points.map((point) => point.zone);

describe("deriveFeverTrail", () => {
	it("keeps a healthy delivery clear of the red zone", () => {
		const trail = deriveFeverTrail(healthyHistory);

		expect(trail.empty).toBe(false);
		expect(zonesOf(healthyHistory)).not.toContain("red");
	});

	it("crosses green then amber then red as completion stalls and lateness rises", () => {
		const zones = zonesOf(degradingHistory);

		const firstAmber = zones.indexOf("amber");
		const firstRed = zones.indexOf("red");
		expect(zones.indexOf("green")).toBeLessThan(firstAmber);
		expect(firstAmber).toBeLessThan(firstRed);
		expect(zones).toEqual(["green", "amber", "red"]);
	});

	it("maps completion rate onto x and chance of being late onto y", () => {
		const trail = deriveFeverTrail(
			getMockHistory({
				points: [
					{
						date: "2026-06-08T00:00:00Z",
						doneWork: 10,
						likelihoodPercentage: 80,
					},
				],
			}),
		);
		const point = trail.points[0];

		expect(point.completion).toBe(50);
		expect(point.chanceOfLate).toBe(20);
		expect(point.zone).toBe("green");
	});

	it("treats a point on the green/amber boundary as amber", () => {
		const onBoundary = getMockHistory({
			points: [
				{
					date: "2026-06-08T00:00:00Z",
					doneWork: 0,
					likelihoodPercentage: 100,
				},
			],
		});

		expect(zonesOf(onBoundary)).toEqual(["amber"]);
	});

	it("plots a barely-started long-shot delivery in the red zone", () => {
		const atRisk = getMockHistory({
			points: [
				{ date: "2026-06-08T00:00:00Z", doneWork: 2, likelihoodPercentage: 10 },
			],
		});

		expect(zonesOf(atRisk)).toEqual(["red"]);
	});

	it("excludes snapshots recorded before any likelihood was captured", () => {
		const trail = deriveFeverTrail(
			getMockHistory({
				points: [
					{
						date: "2026-06-01T00:00:00Z",
						doneWork: 4,
						likelihoodPercentage: null,
					},
					{
						date: "2026-06-08T00:00:00Z",
						doneWork: 8,
						likelihoodPercentage: 70,
					},
				],
			}),
		);

		expect(trail.points).toHaveLength(1);
		expect(trail.points[0].completion).toBe(40);
	});

	it("excludes zero-scope points and reports empty when none are plottable", () => {
		const mixed = deriveFeverTrail(
			getMockHistory({
				points: [
					{ date: "2026-06-01T00:00:00Z", totalWork: 0, doneWork: 0 },
					{
						date: "2026-06-08T00:00:00Z",
						doneWork: 8,
						likelihoodPercentage: 70,
					},
				],
			}),
		);
		expect(mixed.points).toHaveLength(1);
		expect(mixed.empty).toBe(false);

		const none = deriveFeverTrail(
			getMockHistory({
				points: [
					{ date: "2026-06-01T00:00:00Z", totalWork: 0, doneWork: 0 },
					{
						date: "2026-06-08T00:00:00Z",
						doneWork: 8,
						likelihoodPercentage: null,
					},
				],
			}),
		);
		expect(none.points).toEqual([]);
		expect(none.empty).toBe(true);
	});

	it("reports an empty trail when there are no snapshot points", () => {
		const trail = deriveFeverTrail(getMockHistory({ points: [] }));

		expect(trail.empty).toBe(true);
		expect(trail.points).toEqual([]);
	});
});

describe("feverZonePolygons", () => {
	const byZone = (zone: FeverZone) =>
		feverZonePolygons().find((polygon) => polygon.zone === zone);

	it("anchors the amber/red band to the top-right corner and the green band to the bottom-right", () => {
		expect(byZone("green")?.points).toEqual([
			[0, 0],
			[100, 0],
			[100, BAND_SLOPE * 100],
		]);
		expect(byZone("red")?.points).toEqual([
			[0, AMBER_RED_INTERCEPT],
			[100, 100],
			[0, 100],
		]);
	});

	it("fills the plane with green, amber and red regions", () => {
		expect(feverZonePolygons().map((polygon) => polygon.zone)).toEqual([
			"green",
			"amber",
			"red",
		]);
	});
});
