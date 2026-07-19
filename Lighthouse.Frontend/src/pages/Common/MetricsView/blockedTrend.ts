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

const NO_BASELINE_HINT =
	"No previous-period baseline yet — the trend appears once a blocked-count snapshot exists on or before the day before the selected range.";

/**
 * Marker payload rendered as a neutral "—" placeholder (with an explanatory tooltip) when a real
 * directional comparison cannot be computed yet. BlockedCountSnapshot is forward-only, so a freshly
 * recording instance legitimately has no snapshot before the previous-period boundary; surfacing the
 * hint keeps the widget from reading as inert without fabricating a zero-delta.
 */
const noBaselineTrend = (): TrendPayload => ({
	direction: "none",
	metricLabel: METRIC_LABEL,
	noBaseline: true,
	hintText: NO_BASELINE_HINT,
});

const dayLabelOf = (time: number): string =>
	new Date(time).toISOString().slice(0, 10);

/**
 * B3 (slice-06): previous-period trend for the Blocked overview widget.
 *
 * Compares the current blocked count against the BlockedCountSnapshot on the LAST DAY of the previous
 * period — where "period" is the dashboard's selected [startDate, endDate] range — and returns a
 * TrendPayload for the EXISTING WidgetShell trend chrome (no new UI).
 *
 * Story 5508 slice 02 (D2, re-decided after UPSTREAM-4): an ABSENT baseline now counts as a blocked
 * count of ZERO rather than returning the neutral no-baseline marker, so a day-one instance reads
 * "+N since we started recording" instead of a dash that looks like breakage. That substitution is
 * only defensible because the fetch window was widened first (US-03 AC0, `useMetricsData`) — before
 * that fix the baseline day sat one day outside the fetched history on every instance, so this path
 * would have fired everywhere and permanently hidden the true comparison.
 *
 * Because the zero is ASSUMED rather than measured, that case originally carried an explanatory
 * `hintText` the measured case did not (AC5b). The tooltip was removed on 2026-07-19 by user
 * decision after manual verification — bare numbers only. `previousLabel` now carries the
 * assumed/measured distinction alone: it states the boundary DAY, never a fabricated `recordedAt`.
 *
 * The one remaining no-baseline case is a history that holds nothing at or before `endDate` (AC2b):
 * records exist but the selected range predates all of them, so there is no measurement at EITHER
 * end. Assuming zero there would be fabrication rather than a day-one assumption.
 *
 * Pure selector: read-only over the already-loaded BlockedCountSnapshot history. No side effects.
 */
export function computeBlockedTrend(
	history: BlockedCountSnapshot[] | null,
	startDate: Date,
	endDate: Date,
): TrendPayload | undefined {
	const snapshots = history ?? [];
	const boundary = startDate.getTime() - ONE_DAY_MS;

	const current = latestAtOrBefore(snapshots, endDate.getTime());
	if (!current && snapshots.length > 0) {
		// AC2b: recording demonstrably began after the selected range ended.
		return noBaselineTrend();
	}

	const baseline = latestAtOrBefore(snapshots, boundary);

	const currentCount = current?.blockedCount ?? 0;
	const baselineCount = baseline?.blockedCount ?? 0;
	const percentageDelta = formatDelta(currentCount, baselineCount);

	return {
		direction: directionOf(currentCount, baselineCount),
		metricLabel: METRIC_LABEL,
		currentLabel: current?.recordedAt ?? dayLabelOf(endDate.getTime()),
		currentValue: String(currentCount),
		// Never a fabricated recordedAt: state the boundary DAY the zero stands for (AC5).
		previousLabel: baseline?.recordedAt ?? dayLabelOf(boundary),
		previousValue: String(baselineCount),
		...(percentageDelta ? { percentageDelta } : {}),
	};
}
