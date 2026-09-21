import { describe, expect, it } from "vitest";
import type { IFeature, IFeatureStart } from "../../../../../../models/Feature";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import {
	buildDeliveryTimeline,
	DEFAULT_TIMELINE_PERCENTILE,
	TIMELINE_PERCENTILES,
	targetCalendarDate,
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

const startedOn = (day: number): IFeatureStart => ({
	source: "Observed",
	observedDate: october(day),
	percentiles: [],
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
		expect(bar.endIsObserved).toBe(false);
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
		const bar = onlyBar([feature({ startForecast: startedOn(3) })]);

		expect(bar.start).toEqual(october(3));
		expect(bar.startIsObserved).toBe(true);
		expect(bar.end).toEqual(october(21));
	});

	it("ends a finished Feature on the day it closed, not on a forecast still running", () => {
		// A Feature can be closed while one of its children is still open, and the run that is still
		// forecasting that child goes on writing a completion date for the parent — a date in a
		// future the work is already past. A bar drawn out to it shows finished work as still
		// running and weeks late.
		const finished = feature({
			startForecast: startedOn(3),
			closedDate: october(9),
			forecasts: spreadFrom(20),
		});

		const bar = onlyBar([finished]);

		expect(bar.start).toEqual(october(3));
		expect(bar.end).toEqual(october(9));
		// Both halves. The date alone was already right before this flag existed, so on its own it
		// says nothing about whether anything downstream can tell a day work stopped from a day it
		// was predicted to.
		expect(bar.endIsObserved).toBe(true);
	});

	it("tells the two ends apart on a Feature that has started and not finished", () => {
		// The likeliest way to add the second flag is to set it from the first. This is the Feature
		// where that is wrong: work began on a day somebody can point at, and when it ends is still
		// a guess.
		const running = feature({
			startForecast: startedOn(3),
			closedDate: undefined,
		});

		const bar = onlyBar([running]);

		expect(bar.startIsObserved).toBe(true);
		expect(bar.endIsObserved).toBe(false);
	});

	it("places a finished Feature that has no completion forecast left to read", () => {
		// With nothing left to simulate the forecast can be gone entirely. The day the Feature
		// closed is an end, and one that plainly has an end does not belong in the list of what
		// could not be drawn.
		const finished = feature({
			startForecast: startedOn(3),
			closedDate: october(9),
			forecasts: [],
		});

		const bar = onlyBar([finished]);

		expect(bar.end).toEqual(october(9));
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

	it("places a Feature from a payload that predates the team field", () => {
		// `teamsWithoutForecast` is optional on the model for the reason every additive field is:
		// an older instance, or a fixture built before it existed, simply omits it. Reading its
		// length without a fallback throws and takes the whole timeline down.
		const bar = onlyBar([feature({ teamsWithoutForecast: undefined })]);

		expect(bar.start).toEqual(october(11));
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

	it("reaches today when the work is all still ahead of it", () => {
		// A Delivery nobody has started sits entirely in the future. Without today in the window
		// the reader gets bars and no sense of how far off they are.
		const window = timelineWindow([bar(20, 25)], undefined, october(1));

		expect(window?.start).toEqual(new Date(2026, 8, 28));
	});

	it("has no window at all when there is nothing to draw", () => {
		expect(timelineWindow([], october(15), october(1))).toBeUndefined();
	});

	it("widens for a lane reaching past every bar rather than clipping it off the axis", () => {
		// A Team's percentile is not bounded by its Feature's, so a lane can reach past both ends
		// of the last bar. Clipped, it is simply not on the axis — no error, no gap, nothing to
		// notice. Three halves: the far end, the near end, and a call with no lanes at all.
		const bars = [bar(10, 20)];
		const reachingLate = { start: october(12), end: october(30) };
		const reachingEarly = { start: october(4), end: october(18) };

		expect(timelineWindow([...bars, reachingLate])?.end).toEqual(
			new Date(2026, 10, 2),
		);
		expect(timelineWindow([...bars, reachingEarly])?.start).toEqual(october(1));
		// Pinned to the days themselves rather than to a second call on the same input, which is
		// satisfied by a function that ignores what it is handed.
		expect(timelineWindow(bars)).toEqual({
			start: october(7),
			end: october(23),
		});
	});
});

describe("targetCalendarDate", () => {
	const targetStoredAt = (iso: string) => new Date(iso);

	it("gives the calendar day the target names", () => {
		expect(targetCalendarDate(targetStoredAt("2026-10-15T00:00:00Z"))).toEqual(
			new Date(2026, 9, 15),
		);
	});

	it("gives the day the Delivery heading names, for a target late in the UTC day", () => {
		// The heading prints the target with `timeZone: "UTC"`, so this one reads as the 15th.
		// The suite is pinned to Europe/Zurich, where the same instant is already 01:30 on the
		// 16th locally — so reading it with the local getters yields the 16th and disagrees with
		// the heading directly above the chart. That is the failure this pins; a target at UTC
		// midnight cannot see it, because at this offset both readings agree.
		const target = targetStoredAt("2026-10-15T23:30:00Z");
		expect(target.getDate()).toBe(16);

		expect(targetCalendarDate(target)).toEqual(new Date(2026, 9, 15));
	});

	it("keeps a single-digit month and day on the right day", () => {
		expect(targetCalendarDate(targetStoredAt("2026-03-07T00:00:00Z"))).toEqual(
			new Date(2026, 2, 7),
		);
	});

	it("returns a date at local midnight, so it compares cleanly against an axis column", () => {
		const day = targetCalendarDate(targetStoredAt("2026-10-15T23:30:00Z"));

		expect([day.getHours(), day.getMinutes(), day.getSeconds()]).toEqual([
			0, 0, 0,
		]);
	});
});
