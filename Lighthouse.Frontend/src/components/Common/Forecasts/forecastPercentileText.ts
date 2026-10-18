import type { IWhenForecast } from "../../../models/Forecasts/WhenForecast";

type Percentile = Pick<IWhenForecast, "beyondHorizon" | "horizonYears">;

/**
 * The years of the horizon a percentile lies beyond, or undefined when the percentile has a date to show.
 * An older backend sends no horizon, and its percentile keeps the date it carries.
 */
export const yearsBeyondHorizon = (
	percentile: Percentile,
): number | undefined =>
	percentile.beyondHorizon === true ? percentile.horizonYears : undefined;

/** The words shown in place of a date for a percentile too few simulated runs reached. */
export const moreThanYearsText = (horizonYears: number): string =>
	`More than ${horizonYears} years`;

/** A percentile as one line of text: its date in the caller's own format, or the words when it has none. */
export const percentileDateText = (
	percentile: Percentile & Pick<IWhenForecast, "expectedDate">,
	formatDate: (date: Date) => string,
): string => {
	const years = yearsBeyondHorizon(percentile);
	return years === undefined
		? formatDate(percentile.expectedDate)
		: moreThanYearsText(years);
};

/** Why a percentile has no date, for a tooltip or a reason line. */
export const beyondHorizonReason = (
	probability: number,
	horizonYears: number,
): string =>
	`Fewer than ${probability}% of simulated runs finished within ${horizonYears} years.`;
