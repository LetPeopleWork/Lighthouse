import type { IPercentileValue } from "../../../models/PercentileValue";
import type { TrendDirection, TrendPayload } from "./trendTypes";

// Deliberately NOT "Work Item Age Percentiles": that is the chart card's own visible title, and a
// trend label repeating it verbatim puts the same string on screen twice. This names what is
// actually being trended — the highest configured percentile — rather than the widget.
const METRIC_LABEL = "Highest Work Item Age Percentile";

const formatAgeInDays = (days: number): string =>
	days === 1 ? "1 day" : `${days} days`;

const highestPercentileOf = (
	values: readonly IPercentileValue[],
): IPercentileValue | undefined =>
	values.reduce<IPercentileValue | undefined>(
		(highest, candidate) =>
			!highest || candidate.percentile > highest.percentile
				? candidate
				: highest,
		undefined,
	);

const trendDirectionOf = (
	current: number,
	previous: number,
): TrendDirection => {
	if (current > previous) {
		return "up";
	}
	if (current < previous) {
		return "down";
	}
	return "flat";
};

/**
 * Previous-period trend for the Work Item Age Percentiles widget (US-05 AC4, D5).
 *
 * Both sides are the same backend snapshot read over two windows — the selected range, and a window
 * of equal length ending the day before it starts. The headline number is the HIGHEST configured
 * percentile, because that is the "almost nothing is older than this" figure a coach reads first;
 * the lower percentiles ride along as detail rows so the arrow is never the whole story.
 *
 * A missing percentile on either side counts as zero rather than suppressing the trend: an empty
 * snapshot means nothing was ageing, which is a real reading and not a missing one. This is the same
 * choice computeBlockedTrend makes for an absent baseline, and it is only honest because slice 03
 * made each window age to its own end date — before that both sides aged to today, so every
 * comparison read flat regardless of what actually happened.
 *
 * Lives beside blockedTrend.ts rather than inside BaseMetricsView for the same reason that one does:
 * a pure selector over loaded data is worth testing directly, and reaching it only through the view
 * left every branch below unguarded (found by mutation testing, 2026-07-19).
 *
 * Pure selector over already-loaded data. No side effects, no fetching.
 */
export function computeWorkItemAgePercentilesTrend(
	current: readonly IPercentileValue[],
	previous: readonly IPercentileValue[],
): TrendPayload {
	const currentHighest = highestPercentileOf(current);
	const previousHighest = highestPercentileOf(previous);
	const currentValue = currentHighest?.value ?? 0;
	const previousValue = previousHighest?.value ?? 0;
	const percentile = currentHighest?.percentile ?? previousHighest?.percentile;

	const detailRows = current
		.filter((entry) => entry.percentile !== percentile)
		.map((entry) => ({
			label: `${entry.percentile}th percentile`,
			currentValue: formatAgeInDays(entry.value),
			previousValue: formatAgeInDays(
				previous.find((other) => other.percentile === entry.percentile)
					?.value ?? 0,
			),
		}));

	return {
		direction: trendDirectionOf(currentValue, previousValue),
		metricLabel: METRIC_LABEL,
		currentLabel:
			percentile === undefined
				? "Selected range"
				: `${percentile}th percentile, selected range`,
		currentValue: formatAgeInDays(currentValue),
		previousLabel:
			percentile === undefined
				? "Previous range"
				: `${percentile}th percentile, previous range`,
		previousValue: formatAgeInDays(previousValue),
		...(detailRows.length > 0 ? { detailRows } : {}),
	};
}
