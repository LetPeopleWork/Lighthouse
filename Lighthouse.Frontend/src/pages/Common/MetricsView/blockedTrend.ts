import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import type { TrendDirection, TrendPayload } from "./trendTypes";

const METRIC_LABEL = "Blocked Items";
const ONE_DAY_MS = 24 * 60 * 60 * 1000;

const timeOf = (snapshot: BlockedCountSnapshot): number =>
	new Date(snapshot.recordedAt).getTime();

const latestAtOrBefore = (
	history: BlockedCountSnapshot[],
	cutoff: number,
): BlockedCountSnapshot | undefined =>
	history
		.filter((snapshot) => timeOf(snapshot) <= cutoff)
		.reduce<BlockedCountSnapshot | undefined>(
			(latest, snapshot) =>
				!latest || timeOf(snapshot) >= timeOf(latest) ? snapshot : latest,
			undefined,
		);

const directionOf = (current: number, previous: number): TrendDirection => {
	if (current > previous) {
		return "up";
	}
	if (current < previous) {
		return "down";
	}
	return "flat";
};

const signOf = (change: number): string => {
	if (change > 0) {
		return "+";
	}
	if (change < 0) {
		return "-";
	}
	return "";
};

const formatDelta = (current: number, previous: number): string | undefined => {
	if (previous === 0) {
		return undefined;
	}
	const change = ((current - previous) / previous) * 100;
	return `${signOf(change)}${Math.abs(change).toFixed(1)}%`;
};

/**
 * B3 (slice-06): previous-period trend for the Blocked overview widget.
 *
 * Compares the current blocked count against the BlockedCountSnapshot on the LAST DAY of the previous
 * period — where "period" is the dashboard's selected [startDate, endDate] range — and returns a
 * TrendPayload for the EXISTING WidgetShell trend chrome (no new UI). Returns undefined (chrome hidden)
 * when no snapshot exists at or before the previous-period boundary, so the widget never shows a
 * fabricated zero-delta.
 *
 * Pure selector: read-only over the already-loaded BlockedCountSnapshot history. No side effects.
 */
export function computeBlockedTrend(
	history: BlockedCountSnapshot[] | null,
	startDate: Date,
	endDate: Date,
): TrendPayload | undefined {
	if (!history || history.length === 0) {
		return undefined;
	}

	const boundary = startDate.getTime() - ONE_DAY_MS;
	const baseline = latestAtOrBefore(history, boundary);
	if (!baseline) {
		return undefined;
	}

	const current = latestAtOrBefore(history, endDate.getTime());
	if (!current) {
		return undefined;
	}

	const percentageDelta = formatDelta(
		current.blockedCount,
		baseline.blockedCount,
	);

	return {
		direction: directionOf(current.blockedCount, baseline.blockedCount),
		metricLabel: METRIC_LABEL,
		currentLabel: current.recordedAt,
		currentValue: String(current.blockedCount),
		previousLabel: baseline.recordedAt,
		previousValue: String(baseline.blockedCount),
		...(percentageDelta ? { percentageDelta } : {}),
	};
}
