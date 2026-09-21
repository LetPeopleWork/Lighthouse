import type { IFeature } from "../../../../../../models/Feature";
import type { IWhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import {
	cannotBeForecast,
	cannotForecastReason,
} from "../../../../../../utils/forecast/cannotForecast";

export const TIMELINE_PERCENTILES = [70, 85, 95] as const;

export type TimelinePercentile = (typeof TIMELINE_PERCENTILES)[number];

export const DEFAULT_TIMELINE_PERCENTILE: TimelinePercentile = 70;

/** One Feature's span, in this product's words. Nothing here is the chart library's vocabulary. */
export interface TimelineBar {
	featureId: number;
	name: string;
	start: Date;
	end: Date;
	/** A start that already happened is a fact; a start at a percentile is a guess. Drawn differently. */
	startIsObserved: boolean;
}

export interface UnplaceableFeature {
	featureId: number;
	name: string;
	reason: string;
}

export interface DeliveryTimeline {
	bars: TimelineBar[];
	unplaceable: UnplaceableFeature[];
}

/** Days of air either side, so the first and last bars are not flush against the frame. */
const WINDOW_PADDING_DAYS = 3;

const shiftedByDays = (date: Date, days: number): Date => {
	const shifted = new Date(date);
	shifted.setDate(shifted.getDate() + days);
	return shifted;
};

/**
 * The span of time the chart should cover: everything drawn on it, the target date and today,
 * whichever of them reaches furthest in each direction, plus a little air.
 *
 * The two extra dates are the point. A Delivery whose last Feature finishes well before its target
 * has slack, and one whose work has not started yet sits entirely in the future — a window drawn
 * around the bars alone hides the first and gives the second no anchor to read against.
 *
 * It takes anything carrying a start and an end rather than bars specifically, because a Feature's
 * bar is not the only thing on the chart: a Team's own forecast is not bounded by its Feature's, so
 * a row drawn for one can reach past every bar. Handing this only the bars clips that row off the
 * axis with no error and no gap — it is simply not there.
 */
export function timelineWindow(
	spans: { start: Date; end: Date }[],
	targetDate?: Date,
	today?: Date,
): { start: Date; end: Date } | undefined {
	if (spans.length === 0) {
		return undefined;
	}

	const moments = spans.flatMap((span) => [span.start, span.end]);

	for (const marked of [targetDate, today]) {
		if (marked) {
			moments.push(marked);
		}
	}

	const times = moments.map((moment) => moment.getTime());

	return {
		start: shiftedByDays(new Date(Math.min(...times)), -WINDOW_PADDING_DAYS),
		end: shiftedByDays(new Date(Math.max(...times)), WINDOW_PADDING_DAYS),
	};
}

const NO_START = "No forecast for when work on this begins.";
const NO_END = "No forecast for when work on this finishes.";

/**
 * The date a list of percentiles gives for the probability being read, or nothing where it gives
 * none. Exported because a Feature's own ends and each contributing Team's are the same question
 * asked of two lists, and answering it twice is how the two come to disagree.
 */
export const forecastDateAt = (
	percentiles: IWhenForecast[],
	probability: TimelinePercentile,
): Date | undefined =>
	percentiles.find((forecast) => forecast.probability === probability)
		?.expectedDate;

/**
 * The day a Feature finished, or nothing for one still running. The field arrives as null until the
 * Feature closes, which its type does not admit, so the check has to be made here.
 */
const finishedOn = (feature: IFeature): Date | undefined =>
	feature.closedDate ?? undefined;

/**
 * Every Feature in a Delivery, sorted into the ones that can be drawn and the ones that cannot.
 *
 * The split is the point. Handed something it cannot place, the timeline component draws a bar at a
 * position the data does not support rather than leaving it out — so a span with a missing end is
 * worse than useless on the chart, and it still must not disappear from the page. It comes back here
 * instead, with the reason, for the caller to list beside the chart.
 */
export function buildDeliveryTimeline(
	features: IFeature[],
	percentile: TimelinePercentile,
): DeliveryTimeline {
	const bars: TimelineBar[] = [];
	const unplaceable: UnplaceableFeature[] = [];

	// Board order, exactly as handed over. The order they arrive in is the order the board carries and
	// the order the simulation itself works in, so re-sorting by date here would quietly re-rank the
	// board on the way to the screen.
	for (const feature of features) {
		const cannotPlace = (reason: string) => {
			unplaceable.push({ featureId: feature.id, name: feature.name, reason });
		};

		const teamsWithoutForecast = feature.teamsWithoutForecast ?? [];

		if (cannotBeForecast({ teamsWithoutForecast })) {
			cannotPlace(cannotForecastReason(teamsWithoutForecast));
			continue;
		}

		const start = feature.startForecast;
		const observedStart =
			start?.source === "Observed" ? start.observedDate : undefined;
		const startsOn =
			observedStart ?? forecastDateAt(start?.percentiles ?? [], percentile);

		if (!startsOn) {
			cannotPlace(NO_START);
			continue;
		}

		// Work that has finished ends where it stopped, not where it was predicted to. A closed
		// Feature is still handed a completion forecast: one running into the future while a child
		// of it stays open, or an empty one that resolves to today once nothing is left to simulate.
		// Either drawn as the end of the bar reads as work that is still going and badly overdue.
		const endsOn =
			finishedOn(feature) ?? forecastDateAt(feature.forecasts, percentile);

		if (!endsOn) {
			cannotPlace(NO_END);
			continue;
		}

		bars.push({
			featureId: feature.id,
			name: feature.name,
			start: startsOn,
			end: endsOn,
			startIsObserved: observedStart !== undefined,
		});
	}

	return { bars, unplaceable };
}

/**
 * A Delivery's target as a plain calendar date in the reader's own frame.
 *
 * The stored value is an instant, and the product reads it as a UTC day — the Delivery heading
 * prints it with `timeZone: "UTC"`, so a target late in the UTC day shows as that day even to a
 * reader whose own clock has already rolled over. Reducing it locally instead would mark one
 * column on the timeline while the heading above named another.
 *
 * Converting once, here, is what lets everything downstream compare it with ordinary local
 * arithmetic instead of each caller having to remember the asymmetry.
 */
export function targetCalendarDate(targetDate: Date): Date {
	return new Date(
		targetDate.getUTCFullYear(),
		targetDate.getUTCMonth(),
		targetDate.getUTCDate(),
	);
}
