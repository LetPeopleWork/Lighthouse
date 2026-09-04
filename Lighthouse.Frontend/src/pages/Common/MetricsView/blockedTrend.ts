import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import { formatLocalDate } from "../../../utils/date/localDate";
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

/**
 * Previous-period trend for the Blocked overview widget: the current blocked count against the
 * count recorded on the last day of the previous period, where the period is the dashboard's
 * selected range. Feeds the existing widget trend chrome.
 *
 * An absent baseline counts as a blocked count of zero rather than a neutral placeholder, so a
 * day-one instance reads "+N since we started recording" instead of a dash that looks like
 * breakage. That only holds because the fetch window reaches the boundary day; while it did not,
 * the baseline sat outside the fetched history on every instance and this path fired everywhere,
 * permanently hiding the true comparison.
 *
 * Because that zero is assumed rather than measured, `previousLabel` is the only thing separating
 * the two cases — it states the boundary day, never a recording date that never existed.
 *
 * Pure selector: read-only over the already-loaded snapshot history. No side effects.
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
		// Records exist but every one of them postdates the selected range, so recording
		// demonstrably began after it ended. Nothing was measured at either end here, and
		// assuming a zero would invent a comparison rather than stand in for a day-one one.
		return noBaselineTrend();
	}

	const baseline = latestAtOrBefore(snapshots, boundary);

	const currentCount = current?.blockedCount ?? 0;
	const baselineCount = baseline?.blockedCount ?? 0;
	const percentageDelta = formatDelta(currentCount, baselineCount);

	return {
		direction: directionOf(currentCount, baselineCount),
		metricLabel: METRIC_LABEL,
		currentLabel: current?.recordedAt ?? formatLocalDate(endDate),
		currentValue: String(currentCount),
		// Never a fabricated recordedAt: state the boundary day the zero stands for.
		previousLabel: baseline?.recordedAt ?? formatLocalDate(new Date(boundary)),
		previousValue: String(baselineCount),
		...(percentageDelta ? { percentageDelta } : {}),
	};
}
