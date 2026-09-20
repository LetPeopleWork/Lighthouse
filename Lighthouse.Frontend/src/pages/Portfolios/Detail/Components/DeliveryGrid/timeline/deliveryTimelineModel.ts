import type { IFeature } from "../../../../../../models/Feature";
import type { IWhenForecast } from "../../../../../../models/Forecasts/WhenForecast";
import { formatLocalDate } from "../../../../../../utils/date/localDate";
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

const NO_START = "No forecast for when work on this begins.";
const NO_END = "No forecast for when work on this finishes.";

const dateAt = (
	percentiles: IWhenForecast[],
	probability: TimelinePercentile,
): Date | undefined =>
	percentiles.find((forecast) => forecast.probability === probability)
		?.expectedDate;

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

		if (
			cannotBeForecast({ teamsWithoutForecast: feature.teamsWithoutForecast })
		) {
			cannotPlace(cannotForecastReason(feature.teamsWithoutForecast ?? []));
			continue;
		}

		const start = feature.startForecast;
		const observedStart =
			start?.source === "Observed" ? start.observedDate : undefined;
		const startsOn =
			observedStart ?? dateAt(start?.percentiles ?? [], percentile);

		if (!startsOn) {
			cannotPlace(NO_START);
			continue;
		}

		const endsOn = dateAt(feature.forecasts, percentile);

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
 * The calendar day of a Delivery's target, which is stored as an instant and is not the viewer's day.
 *
 * The Delivery heading prints this date with `timeZone: "UTC"`, so a target late in the UTC day shows
 * as that day even to a reader whose own clock has already rolled over. Reducing it locally instead
 * would tint one column on the timeline while the heading above it names another.
 */
const targetCalendarDay = (target: Date): string =>
	[
		target.getUTCFullYear(),
		String(target.getUTCMonth() + 1).padStart(2, "0"),
		String(target.getUTCDate()).padStart(2, "0"),
	].join("-");

/**
 * Whether an axis column is the Delivery's target day.
 *
 * The two sides are reduced differently on purpose, and that is the whole difficulty here. The chart
 * hands its scale callbacks a date at LOCAL midnight, so a column is a local day; the target is an
 * instant the product reads as a UTC day. Reducing both the same way agrees with itself and disagrees
 * with the heading.
 */
export function isTargetDay(day: Date, targetDate?: Date): boolean {
	if (!targetDate) {
		return false;
	}

	return formatLocalDate(day) === targetCalendarDay(targetDate);
}
