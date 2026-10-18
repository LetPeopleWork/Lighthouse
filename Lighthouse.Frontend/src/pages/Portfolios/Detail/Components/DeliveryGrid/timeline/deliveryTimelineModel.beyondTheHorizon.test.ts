import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../../../../models/Feature";
import { WhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import {
	buildDeliveryTimeline,
	TIMELINE_PERCENTILES,
	type TimelinePercentile,
} from "./deliveryTimelineModel";

const pendingEpic6311 =
	"Pending: the timeline leaves out a Feature that ends beyond the horizon (epic 6311, slice 03)";

const october = (day: number) => new Date(2026, 9, day);

const theStandInDate = new Date(2031, 9, 18);

const aPercentile = (probability: number, day: number) =>
	WhenForecast.new(probability, october(day));

const beyond = (probability: number) => {
	const percentile = WhenForecast.new(probability, theStandInDate);
	percentile.beyondHorizon = true;
	percentile.horizonYears = 5;
	return percentile;
};

/** The completion percentiles from the one asked about upwards are beyond the horizon. */
const endsBeyondFrom = (first: TimelinePercentile) =>
	[50, 70, 85, 95].map((probability) =>
		probability >= first ? beyond(probability) : aPercentile(probability, 20),
	);

const aFeature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Billing Migration",
		startForecast: {
			source: "Forecast",
			percentiles: [50, 70, 85, 95].map((p) => aPercentile(p, 10)),
		},
		forecasts: endsBeyondFrom(85),
		teamsWithoutForecast: [],
		...overrides,
	}) as IFeature;

describe("a Feature that ends beyond the horizon, on the Delivery timeline", () => {
	it.skip(`gets no bar and is listed with the reason (${pendingEpic6311})`, () => {
		const { bars, unplaceable } = buildDeliveryTimeline([aFeature()], 85);

		expect(bars).toEqual([]);
		expect(unplaceable).toEqual([
			{
				featureId: 1,
				name: "Billing Migration",
				reason: "Ends more than 5 years out at 85%",
			},
		]);
	});

	it.skip.each(TIMELINE_PERCENTILES)(
		`names the percentile being read, here %i%, in the reason (${pendingEpic6311})`,
		(percentile) => {
			const { bars, unplaceable } = buildDeliveryTimeline(
				[aFeature({ forecasts: endsBeyondFrom(percentile) })],
				percentile,
			);

			expect(bars).toEqual([]);
			expect(unplaceable[0]?.reason).toBe(
				`Ends more than 5 years out at ${percentile}%`,
			);
		},
	);

	/**
	 * Already true; the guard that a Feature beyond the horizon only at a higher percentile still gets
	 * its bar at the one being read.
	 */
	it("draws the bar at a percentile that is within the horizon", () => {
		const { bars } = buildDeliveryTimeline([aFeature()], 70);

		expect(bars).toHaveLength(1);
		expect(bars[0].end).toEqual(october(20));
	});

	/** Already true; a finished Feature ends where the work stopped, whatever its forecast says. */
	it("draws a finished Feature to the day it finished", () => {
		const { bars } = buildDeliveryTimeline(
			[aFeature({ closedDate: october(25) } as Partial<IFeature>)],
			85,
		);

		expect(bars[0]?.end).toEqual(october(25));
	});
});
