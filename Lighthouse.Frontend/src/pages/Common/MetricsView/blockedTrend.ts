import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import type { TrendPayload } from "./trendTypes";

export const __SCAFFOLD__ = true;

/**
 * B3 (slice-06): previous-period trend for the Blocked overview widget.
 *
 * Compares the current blocked count against the BlockedCountSnapshot on the LAST DAY of the previous
 * period — where "period" is the dashboard's selected [startDate, endDate] range — and returns a
 * TrendPayload for the EXISTING WidgetShell trend chrome (no new UI). Returns undefined (chrome hidden)
 * when no snapshot exists at or before the previous-period boundary, so the widget never shows a
 * fabricated zero-delta.
 *
 * RED scaffold — authored by DISTILL (ADR-025); implemented in DELIVER slice-06.
 */
export function computeBlockedTrend(
	_history: BlockedCountSnapshot[] | null,
	_startDate: Date,
	_endDate: Date,
): TrendPayload | undefined {
	throw new Error("Not yet implemented — RED scaffold (DISTILL slice-06)");
}
