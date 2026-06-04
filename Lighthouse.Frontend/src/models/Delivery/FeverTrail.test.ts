import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "./DeliveryMetricsHistory";
import { deriveFeverTrail, type FeverZone } from "./FeverTrail";

type RawPoint = {
	date: string;
	totalWork?: number;
	remainingWork?: number;
};

const getMockPoint = (overrides: RawPoint) => ({
	date: overrides.date,
	totalWork: overrides.totalWork ?? 20,
	doneWork: 0,
	remainingWork: overrides.remainingWork ?? 20,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: null,
	whenDistribution: null,
});

const getMockHistory = (overrides: {
	firstSnapshotDate?: string | null;
	deliveryDate?: string;
	points: RawPoint[];
}): DeliveryMetricsHistory =>
	parseDeliveryMetricsHistory({
		deliveryDate: overrides.deliveryDate ?? "2026-06-11T00:00:00Z",
		firstSnapshotDate:
			overrides.firstSnapshotDate === undefined
				? "2026-06-01T00:00:00Z"
				: overrides.firstSnapshotDate,
		points: overrides.points.map(getMockPoint),
	});

const onTrackHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", totalWork: 20, remainingWork: 20 },
		{ date: "2026-06-04T00:00:00Z", totalWork: 20, remainingWork: 13 },
		{ date: "2026-06-08T00:00:00Z", totalWork: 20, remainingWork: 6 },
		{ date: "2026-06-11T00:00:00Z", totalWork: 20, remainingWork: 0 },
	],
});

const degradingHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", totalWork: 20, remainingWork: 20 },
		{ date: "2026-06-04T00:00:00Z", totalWork: 20, remainingWork: 18 },
		{ date: "2026-06-08T00:00:00Z", totalWork: 20, remainingWork: 18 },
		{ date: "2026-06-11T00:00:00Z", totalWork: 20, remainingWork: 16 },
	],
});

const atRiskHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", totalWork: 20, remainingWork: 20 },
		{ date: "2026-06-04T00:00:00Z", totalWork: 20, remainingWork: 20 },
		{ date: "2026-06-08T00:00:00Z", totalWork: 20, remainingWork: 19 },
	],
});

const zonesOf = (history: DeliveryMetricsHistory): FeverZone[] =>
	deriveFeverTrail(history).points.map((point) => point.zone);

describe("deriveFeverTrail", () => {
	it("keeps an on-track delivery clear of the red zone", () => {
		const trail = deriveFeverTrail(onTrackHistory);

		expect(trail.empty).toBe(false);
		expect(zonesOf(onTrackHistory)).not.toContain("red");
	});

	it("crosses green then amber then red in order as the delivery degrades", () => {
		const zones = zonesOf(degradingHistory);

		expect(zones).toContain("green");
		expect(zones).toContain("amber");
		expect(zones).toContain("red");

		const firstAmber = zones.indexOf("amber");
		const firstRed = zones.indexOf("red");
		expect(zones.indexOf("green")).toBeLessThan(firstAmber);
		expect(firstAmber).toBeLessThan(firstRed);
	});

	it("enters the red zone early for an at-risk delivery", () => {
		const zones = zonesOf(atRiskHistory);

		expect(zones).toContain("red");
		expect(zones[zones.length - 1]).toBe("red");
	});

	it("treats a point exactly on the green deviation threshold as green", () => {
		const onBoundary = getMockHistory({
			firstSnapshotDate: "2026-06-01T00:00:00Z",
			deliveryDate: "2026-06-21T00:00:00Z",
			points: [
				{ date: "2026-06-06T00:00:00Z", totalWork: 20, remainingWork: 16 },
			],
		});

		expect(zonesOf(onBoundary)).toEqual(["green"]);
	});

	it("reports an empty trail when the delivery date equals the first snapshot", () => {
		const trail = deriveFeverTrail(
			getMockHistory({
				firstSnapshotDate: "2026-06-08T00:00:00Z",
				deliveryDate: "2026-06-08T00:00:00Z",
				points: [{ date: "2026-06-08T00:00:00Z" }],
			}),
		);

		expect(trail.empty).toBe(true);
		expect(trail.points).toEqual([]);
	});

	it("maps each snapshot onto schedule-consumed x and buffer-remaining y", () => {
		const trail = deriveFeverTrail(onTrackHistory);
		const first = trail.points[0];
		const last = trail.points[trail.points.length - 1];

		expect(first.x).toBe(0);
		expect(first.y).toBe(100);
		expect(last.x).toBe(100);
		expect(last.y).toBe(0);
		expect(trail.points.every((point) => point.x >= 0 && point.x <= 100)).toBe(
			true,
		);
	});

	it("reports an empty trail when no first snapshot date has been recorded", () => {
		const trail = deriveFeverTrail(
			getMockHistory({
				firstSnapshotDate: null,
				points: [{ date: "2026-06-04T00:00:00Z" }],
			}),
		);

		expect(trail.empty).toBe(true);
		expect(trail.points).toEqual([]);
	});

	it("reports an empty trail when there are no snapshot points", () => {
		const trail = deriveFeverTrail(getMockHistory({ points: [] }));

		expect(trail.empty).toBe(true);
		expect(trail.points).toEqual([]);
	});

	it("reports an empty trail when the delivery date is not after the first snapshot", () => {
		const trail = deriveFeverTrail(
			getMockHistory({
				firstSnapshotDate: "2026-06-08T00:00:00Z",
				deliveryDate: "2026-06-01T00:00:00Z",
				points: [{ date: "2026-06-08T00:00:00Z" }],
			}),
		);

		expect(trail.empty).toBe(true);
		expect(trail.points).toEqual([]);
	});

	it("excludes zero-scope points and reports empty when every point has no work", () => {
		const mixed = deriveFeverTrail(
			getMockHistory({
				points: [
					{ date: "2026-06-01T00:00:00Z", totalWork: 0, remainingWork: 0 },
					{ date: "2026-06-08T00:00:00Z", totalWork: 20, remainingWork: 6 },
				],
			}),
		);
		expect(mixed.points).toHaveLength(1);
		expect(mixed.empty).toBe(false);

		const allZero = deriveFeverTrail(
			getMockHistory({
				points: [
					{ date: "2026-06-01T00:00:00Z", totalWork: 0, remainingWork: 0 },
					{ date: "2026-06-08T00:00:00Z", totalWork: 0, remainingWork: 0 },
				],
			}),
		);
		expect(allZero.points).toEqual([]);
		expect(allZero.empty).toBe(true);
	});
});
