import { describe, expect, it } from "vitest";
import type { IFeature, IFeatureStart } from "../../../../../../models/Feature";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
	isTargetDay,
	TIMELINE_PERCENTILES,
	timelineWindow,
} from "./deliveryTimelineModel";

const october = (day: number) => new Date(2026, 9, day);

const aPercentile = (probability: number, day: number) =>
	WhenForecast.new(probability, october(day));

/** The four every forecast in this product carries, spread so each one names a different day. */
const spreadFrom = (day: number) => [
	aPercentile(50, day),
	aPercentile(70, day + 1),
	aPercentile(85, day + 2),
	aPercentile(95, day + 4),
];

const startingAround = (day: number): IFeatureStart => ({
	source: "Forecast",
	percentiles: spreadFrom(day),
});

const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Deep Sea Mapping Initiative",
		startForecast: startingAround(10),
		forecasts: spreadFrom(20),
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

const onlyBar = (
	features: IFeature[],
	percentile = DEFAULT_TIMELINE_PERCENTILE,
) => {
	const { bars } = buildDeliveryTimeline(features, percentile);
	expect(bars).toHaveLength(1);
	return bars[0];
};

const onlyRefusal = (features: IFeature[]) => {
	const { bars, unplaceable } = buildDeliveryTimeline(
		features,
		DEFAULT_TIMELINE_PERCENTILE,
	);
	expect(bars).toHaveLength(0);
	expect(unplaceable).toHaveLength(1);
	return unplaceable[0];
};

describe("buildDeliveryTimeline", () => {
	it("spans one bar from the start percentile to the completion percentile", () => {
		const bar = onlyBar([feature()]);

		expect(bar.start).toEqual(october(11));
		expect(bar.end).toEqual(october(21));
		expect(bar.startIsObserved).toBe(false);
	});

	it("moves both ends of the bar when the percentile changes", () => {
		const atSeventy = onlyBar([feature()], 70);
		const atNinetyFive = onlyBar([feature()], 95);

		expect(atNinetyFive.start).toEqual(october(14));
		expect(atNinetyFive.end).toEqual(october(24));

		// Both ends moved, not one. A bar welding a P70 start to a P95 finish would read as one
		// scenario while describing two, and it is the failure this assertion exists to catch.
		expect(atNinetyFive.start.getTime()).toBeGreaterThan(
			atSeventy.start.getTime(),
		);
		expect(atNinetyFive.end.getTime()).toBeGreaterThan(atSeventy.end.getTime());
	});

	it("offers P70, P85 and P95, and starts at P70", () => {
		expect(TIMELINE_PERCENTILES).toEqual([70, 85, 95]);
		expect(DEFAULT_TIMELINE_PERCENTILE).toBe(70);
	});

	it("keeps the order the Features arrived in, rather than sorting by date", () => {
		const latest = feature({
			id: 1,
			name: "Latest",
			startForecast: startingAround(30),
		});
		const earliest = feature({
			id: 2,
			name: "Earliest",
			startForecast: startingAround(1),
		});

		const { bars } = buildDeliveryTimeline(
			[latest, earliest],
			DEFAULT_TIMELINE_PERCENTILE,
		);

		expect(bars.map((bar) => bar.name)).toEqual(["Latest", "Earliest"]);
	});

	it("begins a started bar on the day it actually started", () => {
		const bar = onlyBar([
			feature({
				startForecast: {
					source: "Observed",
					observedDate: october(3),
					percentiles: [],
				},
			}),
		]);

		expect(bar.start).toEqual(october(3));
		expect(bar.startIsObserved).toBe(true);
		expect(bar.end).toEqual(october(21));
	});

	it("refuses to place a Feature with no start, rather than inventing one", () => {
		const refusal = onlyRefusal([feature({ startForecast: undefined })]);

		expect(refusal.name).toBe("Deep Sea Mapping Initiative");
		expect(refusal.reason).toMatch(/begins/);
	});

	it("refuses to place a Feature whose start is known but whose end is not", () => {
		const refusal = onlyRefusal([feature({ forecasts: [] })]);

		expect(refusal.reason).toMatch(/finishes/);
	});

	it("refuses to place a Feature at a percentile its forecast does not carry", () => {
		// A distribution holding only P50 has nothing to say at P70. Falling back to the nearest
		// percentile would draw a bar the reader would read as the one they asked for.
		const refusal = onlyRefusal([
			feature({ forecasts: [aPercentile(50, 20)] }),
		]);

		expect(refusal.reason).toMatch(/finishes/);
	});

	it("names the teams when no contributing team can be forecast", () => {
		const refusal = onlyRefusal([
			feature({ teamsWithoutForecast: ["Deep Divers"] }),
		]);

		expect(refusal.reason).toContain("Deep Divers");
	});

	it("says nothing can be placed rather than returning an empty timeline", () => {
		const { bars, unplaceable } = buildDeliveryTimeline(
			[
				feature({ id: 1, name: "One", startForecast: undefined }),
				feature({ id: 2, name: "Two", forecasts: [] }),
			],
			DEFAULT_TIMELINE_PERCENTILE,
		);

		expect(bars).toHaveLength(0);
		expect(unplaceable.map((each) => each.name)).toEqual(["One", "Two"]);
	});

	it("places a Feature forecast to start and finish on the same day", () => {
		// Reachable at a Feature WIP of one, where the next Feature starts the day the last one is
		// delivered. A zero-length span is a real answer, and dropping it for having no duration
		// would vanish the Feature from a picture someone is planning against.
		const sameDay = feature({
			startForecast: { source: "Forecast", percentiles: [aPercentile(70, 15)] },
			forecasts: [aPercentile(70, 15)],
		});

		const bar = onlyBar([sameDay]);

		expect(bar.start).toEqual(october(15));
		expect(bar.end).toEqual(october(15));
	});
});

describe("timelineWindow", () => {
	const bar = (start: number, end: number) => ({
		featureId: 1,
		name: "One",
		start: october(start),
		end: october(end),
		startIsObserved: false,
	});

	it("covers every bar, with a few days of air either side", () => {
		const window = timelineWindow([bar(10, 20), bar(14, 25)]);

		expect(window?.start).toEqual(october(7));
		expect(window?.end).toEqual(october(28));
	});

	it("reaches a target date that falls beyond the last bar", () => {
		// The slack between the last forecast finish and the target is the thing a reader opens this
		// chart to see. A window that stopped at the last bar would crop out the answer.
		const window = timelineWindow([bar(10, 20)], october(31));

		expect(window?.end).toEqual(new Date(2026, 10, 3));
	});

	it("has no window at all when there is nothing to draw", () => {
		expect(timelineWindow([], october(15))).toBeUndefined();
	});
});

describe("isTargetDay", () => {
	// The axis hands its callbacks LOCAL midnight; the target arrives as the instant the backend
	// stored. The helper below builds each side the way its real producer does, because building
	// both the same way is what makes this whole question look settled when it is not.
	const axisColumnFor = (day: number) => new Date(2026, 9, day, 0, 0, 0);
	const targetStoredAt = (iso: string) => new Date(iso);

	it("marks the target day and no other", () => {
		const target = targetStoredAt("2026-10-15T00:00:00Z");

		expect(isTargetDay(axisColumnFor(15), target)).toBe(true);
		expect(isTargetDay(axisColumnFor(16), target)).toBe(false);
	});

	it("marks nothing when the Delivery has no target date", () => {
		expect(isTargetDay(axisColumnFor(15), undefined)).toBe(false);
	});

	it("tints the day the Delivery heading names, for a target late in the UTC day", () => {
		// The heading prints the target with `timeZone: "UTC"`, so this one reads as the 15th. The
		// suite is pinned to Europe/Zurich, where the same instant is already 01:30 on the 16th
		// locally — so reducing the target the way the axis column is reduced tints the 16th and
		// disagrees with the heading directly above it. That is the failure this pins; a target at
		// UTC midnight cannot see it, because at this offset both reductions agree.
		const target = targetStoredAt("2026-10-15T23:30:00Z");
		expect(target.getDate()).toBe(16);

		expect(isTargetDay(axisColumnFor(15), target)).toBe(true);
		expect(isTargetDay(axisColumnFor(16), target)).toBe(false);
	});
});
