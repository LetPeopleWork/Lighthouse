/**
 * Which empty-state sentence is honest for an over-time chart (DISCUSS D10 / DESIGN DDD-13).
 *
 * An empty series has two different honest explanations and the widget must give the right one.
 * It decides from the range it asked for — no response envelope (ADR-108 rejects envelopes) and no
 * second unfiltered request.
 *
 * The discriminator is the range's END, not "narrowed vs default range": the dashboard has no
 * unfiltered state, its default IS a bounded window (30 days for teams, 90 for portfolios), so
 * every request it makes is a narrowed one. Because recording is forward-only and per-day, a window
 * that still includes today would contain a point if recording had run — so "builds forward from
 * today" is the honest reading there. A window that ended before today cannot say that: the owner
 * may well have history, just not inside the window.
 *
 * Accepted edge, documented rather than fixed: an owner whose snapshots all predate a window that
 * still ends today reads the forward-only copy. Reaching that needs recording to have stopped more
 * than the default window ago, which a refreshing instance cannot do.
 */

/** Unchanged D6 copy. Two shipped E2Es assert this string verbatim — do not reword it. */
export const OVER_TIME_FORWARD_ONLY_EMPTY_COPY =
	"builds forward from today — no snapshots recorded yet";

export const OVER_TIME_RANGE_EMPTY_COPY =
	"no data recorded in the selected range";

export function resolveOverTimeEmptyCopy(endDate: Date): string {
	return endsBeforeToday(endDate)
		? OVER_TIME_RANGE_EMPTY_COPY
		: OVER_TIME_FORWARD_ONLY_EMPTY_COPY;
}

/**
 * Compared by calendar day, never by instant: the dashboard's default endDate is seeded from
 * `new Date()` and therefore carries a time-of-day, so an instant comparison would call the default
 * range "in the past" a millisecond after it was created — flipping both shipped E2Es to the wrong
 * copy.
 */
function endsBeforeToday(endDate: Date): boolean {
	return startOfDay(endDate) < startOfDay(new Date());
}

function startOfDay(date: Date): number {
	return new Date(
		date.getFullYear(),
		date.getMonth(),
		date.getDate(),
	).getTime();
}
