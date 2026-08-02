import { describe, expect, it } from "vitest";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "./DeliveryMetricsHistory";

const getValidPoint = (overrides?: Record<string, unknown>) => ({
	date: "2026-06-01T00:00:00Z",
	totalWork: 20,
	doneWork: 3,
	remainingWork: 17,
	estimatedItemCount: 25,
	forecastHowMany: 18,
	likelihoodPercentage: 84,
	whenDistribution: [{ probability: 70, expectedDate: "2026-06-08T00:00:00Z" }],
	...overrides,
});

const getValidResponse = (overrides?: Record<string, unknown>) => ({
	deliveryDate: "2026-06-10T00:00:00Z",
	firstSnapshotDate: "2026-06-01T00:00:00Z",
	points: [getValidPoint()],
	...overrides,
});

const responseWithPoint = (pointOverrides: Record<string, unknown>) =>
	getValidResponse({ points: [getValidPoint(pointOverrides)] });

const expectBoundaryError = (
	response: unknown,
	contextFragment: string,
): void => {
	expect(() => parseDeliveryMetricsHistory(response)).toThrow(contextFragment);
};

describe("parseDeliveryMetricsHistory", () => {
	it("maps every forward field of a point onto its own property", () => {
		const result = parseDeliveryMetricsHistory(getValidResponse());

		const point = result.points[0];
		expect(point.totalWork).toBe(20);
		expect(point.doneWork).toBe(3);
		expect(point.remainingWork).toBe(17);
		expect(point.estimatedItemCount).toBe(25);
		expect(point.forecastHowMany).toBe(18);
		expect(point.likelihoodPercentage).toBe(84);
		expect(point.date).toEqual(new Date("2026-06-01T00:00:00Z"));
	});

	it("maps the per-point target date when present and null when absent", () => {
		const withTarget = parseDeliveryMetricsHistory(
			responseWithPoint({ targetDateAtSnapshot: "2026-06-09T00:00:00Z" }),
		);
		expect(withTarget.points[0].targetDateAtSnapshot).toEqual(
			new Date("2026-06-09T00:00:00Z"),
		);

		const withoutTarget = parseDeliveryMetricsHistory(getValidResponse());
		expect(withoutTarget.points[0].targetDateAtSnapshot).toBeNull();
	});

	it("maps the top-level delivery and first-snapshot dates", () => {
		const result = parseDeliveryMetricsHistory(getValidResponse());

		expect(result.deliveryDate).toEqual(new Date("2026-06-10T00:00:00Z"));
		expect(result.firstSnapshotDate).toEqual(new Date("2026-06-01T00:00:00Z"));
	});

	it("maps each when-distribution entry's probability and expected date", () => {
		const result = parseDeliveryMetricsHistory(
			getValidResponse({
				points: [
					{
						date: "2026-06-01T00:00:00Z",
						totalWork: 1,
						doneWork: 0,
						remainingWork: 1,
						estimatedItemCount: null,
						forecastHowMany: null,
						likelihoodPercentage: null,
						whenDistribution: [
							{ probability: 50, expectedDate: "2026-06-05T00:00:00Z" },
							{ probability: 85, expectedDate: "2026-06-09T00:00:00Z" },
						],
					},
				],
			}),
		);

		const distribution = result.points[0].whenDistribution as NonNullable<
			DeliveryMetricsHistory["points"][number]["whenDistribution"]
		>;
		expect(distribution[0].probability).toBe(50);
		expect(distribution[0].expectedDate).toEqual(
			new Date("2026-06-05T00:00:00Z"),
		);
		expect(distribution[1].probability).toBe(85);
		expect(distribution[1].expectedDate).toEqual(
			new Date("2026-06-09T00:00:00Z"),
		);
	});

	it("keeps null for the nullable forward fields when the server omits them", () => {
		const result = parseDeliveryMetricsHistory(
			getValidResponse({
				firstSnapshotDate: null,
				points: [
					{
						date: "2026-06-01T00:00:00Z",
						totalWork: 0,
						doneWork: 0,
						remainingWork: 0,
						estimatedItemCount: null,
						forecastHowMany: null,
						likelihoodPercentage: null,
						whenDistribution: null,
					},
				],
			}),
		);

		expect(result.firstSnapshotDate).toBeNull();
		expect(result.points[0].estimatedItemCount).toBeNull();
		expect(result.points[0].forecastHowMany).toBeNull();
		expect(result.points[0].likelihoodPercentage).toBeNull();
		expect(result.points[0].whenDistribution).toBeNull();
	});

	it("treats an omitted nullable field (undefined) the same as an explicit null", () => {
		const result = parseDeliveryMetricsHistory(
			getValidResponse({
				firstSnapshotDate: undefined,
				points: [
					getValidPoint({
						estimatedItemCount: undefined,
						forecastHowMany: undefined,
						likelihoodPercentage: undefined,
						whenDistribution: undefined,
					}),
				],
			}),
		);

		expect(result.firstSnapshotDate).toBeNull();
		expect(result.points[0].estimatedItemCount).toBeNull();
		expect(result.points[0].forecastHowMany).toBeNull();
		expect(result.points[0].likelihoodPercentage).toBeNull();
		expect(result.points[0].whenDistribution).toBeNull();
	});

	it("rejects a response that is not an object", () => {
		expectBoundaryError(42, "metrics-history response");
		expectBoundaryError(null, "metrics-history response");
		expectBoundaryError([], "metrics-history response");
	});

	it("rejects a response whose points is not an array", () => {
		expectBoundaryError(getValidResponse({ points: "nope" }), "points");
	});

	it("rejects a point that is not an object", () => {
		expectBoundaryError(
			getValidResponse({ points: [5] }),
			"metrics-history point",
		);
	});

	it("rejects a when-distribution that is present but not an array", () => {
		expectBoundaryError(
			responseWithPoint({ whenDistribution: { probability: 50 } }),
			"Expected an array for whenDistribution",
		);
	});

	it("rejects a when-distribution entry that is not an object", () => {
		expectBoundaryError(
			responseWithPoint({ whenDistribution: [42] }),
			"whenDistribution entry",
		);
	});

	it.each([
		["point.date", responseWithPoint({ date: 20260601 }), "point.date"],
		[
			"point.totalWork",
			responseWithPoint({ totalWork: "lots" }),
			"point.totalWork",
		],
		[
			"point.doneWork",
			responseWithPoint({ doneWork: "lots" }),
			"point.doneWork",
		],
		[
			"point.remainingWork",
			responseWithPoint({ remainingWork: "lots" }),
			"point.remainingWork",
		],
		[
			"point.estimatedItemCount",
			responseWithPoint({ estimatedItemCount: "lots" }),
			"point.estimatedItemCount",
		],
		[
			"point.forecastHowMany",
			responseWithPoint({ forecastHowMany: "lots" }),
			"point.forecastHowMany",
		],
		[
			"point.likelihoodPercentage",
			responseWithPoint({ likelihoodPercentage: "lots" }),
			"point.likelihoodPercentage",
		],
		[
			"whenDistribution.probability",
			responseWithPoint({
				whenDistribution: [
					{ probability: "high", expectedDate: "2026-06-05T00:00:00Z" },
				],
			}),
			"whenDistribution.probability",
		],
		[
			"whenDistribution.expectedDate",
			responseWithPoint({
				whenDistribution: [{ probability: 50, expectedDate: "nope" }],
			}),
			"whenDistribution.expectedDate",
		],
		[
			"deliveryDate",
			getValidResponse({ deliveryDate: "not-a-date" }),
			"deliveryDate",
		],
		[
			"firstSnapshotDate",
			getValidResponse({ firstSnapshotDate: 12345 }),
			"firstSnapshotDate",
		],
	])(
		"names the offending field %s in the boundary error",
		(_field, response, contextFragment) => {
			expectBoundaryError(response, contextFragment);
		},
	);

	it("rejects a NaN number even though it is typeof number", () => {
		expectBoundaryError(
			responseWithPoint({ totalWork: Number.NaN }),
			"point.totalWork",
		);
	});
});

// Epic #5585 slice 02 (US-02, AC-2.4). The breakdown payload gains two optional fields. Every
// snapshot recorded before this slice keeps the original four, and must keep parsing — there is no
// backfill (D5), so both shapes arrive on the same wire for as long as the old days are retained.
describe("parseDeliveryMetricsHistory feature breakdown", () => {
	const legacyEntry = {
		referenceId: "EPIC-1",
		name: "Checkout",
		completion: 40,
		likelihood: 80,
	};

	const breakdownOf = (...entries: Record<string, unknown>[]) =>
		parseDeliveryMetricsHistory(
			responseWithPoint({ featureBreakdown: entries }),
		).points[0].featureBreakdown;

	it("reads the size and estimate flag an epic was recorded with", () => {
		const [entry] = breakdownOf({
			...legacyEntry,
			totalItems: 8,
			isUsingDefaultSize: true,
		});

		expect(entry.totalItems).toBe(8);
		expect(entry.isUsingDefaultSize).toBe(true);
	});

	it("still reads an epic recorded before sizes were ever written", () => {
		const [entry] = breakdownOf(legacyEntry);

		expect(entry.referenceId).toBe("EPIC-1");
		expect(entry.totalItems).toBeNull();
		expect(entry.isUsingDefaultSize).toBeNull();
	});

	it("still reads an epic that could not be forecast", () => {
		// ADR-120 / DDD-6, the frontend half: the backend records a null likelihood for a feature whose
		// contributing team has no throughput (ADR-112), and asNumber throws BoundaryError on it —
		// which takes down the whole tab, not just that epic.
		const [entry] = breakdownOf({ ...legacyEntry, likelihood: null });

		expect(entry.likelihood).toBeNull();
	});

	it("treats a missing estimate flag as unknown rather than as a guess", () => {
		// AC-3.5: absence must never be read as true, or every pre-slice epic renders hatched once
		// slice 03 lands.
		const [entry] = breakdownOf({ ...legacyEntry, totalItems: 8 });

		expect(entry.isUsingDefaultSize).toBeNull();
	});
});
