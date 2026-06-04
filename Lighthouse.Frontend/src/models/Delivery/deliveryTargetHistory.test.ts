import { describe, expect, it } from "vitest";
import type { DeliveryMetricsHistoryPoint } from "./DeliveryMetricsHistory";
import { steppedTargetData, targetChanges } from "./deliveryTargetHistory";

const getMockPoint = (
	overrides?: Partial<DeliveryMetricsHistoryPoint>,
): DeliveryMetricsHistoryPoint => ({
	date: new Date("2026-06-01T00:00:00Z"),
	targetDateAtSnapshot: null,
	totalWork: 0,
	doneWork: 0,
	remainingWork: 0,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: null,
	whenDistribution: null,
	featureBreakdown: [],
	...overrides,
});

const earlier = new Date("2026-06-10T00:00:00Z");
const later = new Date("2026-06-24T00:00:00Z");

describe("steppedTargetData", () => {
	it("returns the per-point target timestamps, holding then stepping where the target moved", () => {
		const data = steppedTargetData([
			getMockPoint({ targetDateAtSnapshot: earlier }),
			getMockPoint({ targetDateAtSnapshot: earlier }),
			getMockPoint({ targetDateAtSnapshot: later }),
		]);

		expect(data).toEqual([
			earlier.getTime(),
			earlier.getTime(),
			later.getTime(),
		]);
	});

	it("returns null when no point recorded a target so the chart falls back to one flat line", () => {
		expect(steppedTargetData([getMockPoint(), getMockPoint()])).toBeNull();
	});

	it("gaps points without a recorded target while keeping the recorded ones", () => {
		const data = steppedTargetData([
			getMockPoint(),
			getMockPoint({ targetDateAtSnapshot: later }),
		]);

		expect(data).toEqual([null, later.getTime()]);
	});
});

describe("targetChanges", () => {
	it("reports one change per snapshot where the recorded target differs from the previous", () => {
		const changes = targetChanges([
			getMockPoint({ targetDateAtSnapshot: earlier }),
			getMockPoint({ targetDateAtSnapshot: earlier }),
			getMockPoint({ targetDateAtSnapshot: later }),
		]);

		expect(changes).toEqual([
			{ index: 2, previousTarget: earlier, newTarget: later },
		]);
	});

	it("reports no change when the target never moved", () => {
		expect(
			targetChanges([
				getMockPoint({ targetDateAtSnapshot: earlier }),
				getMockPoint({ targetDateAtSnapshot: earlier }),
			]),
		).toEqual([]);
	});

	it("reports no change when no target was recorded", () => {
		expect(targetChanges([getMockPoint(), getMockPoint()])).toEqual([]);
	});

	it("reports no change when a later snapshot has no recorded target", () => {
		expect(
			targetChanges([
				getMockPoint({ targetDateAtSnapshot: earlier }),
				getMockPoint(),
			]),
		).toEqual([]);
	});
});
