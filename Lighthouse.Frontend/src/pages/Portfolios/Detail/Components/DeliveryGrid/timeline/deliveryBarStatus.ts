import { type TimelineBar, targetCalendarDate } from "./deliveryTimelineModel";

/**
 * What a bar has to say about the Delivery's target date.
 *
 * `finished` is not a verdict against the target and never becomes one. A finished bar already ends
 * at the day the work stopped rather than at a forecast, so asking whether it is late would be
 * asking about a prediction nobody is making.
 */
export type BarStatusKind =
	| "finished"
	| "startsAfterTarget"
	| "endsAfterTarget";

/** The day a moment falls on, in the reader's own frame, with the time of day dropped. */
const dayOf = (moment: Date): number =>
	new Date(moment.getFullYear(), moment.getMonth(), moment.getDate()).getTime();

/**
 * What each bar has to say about the Delivery's target date.
 *
 * **One verdict per bar, because the bar wears it whole.** The reader is asked one question at a
 * time and the answer takes the whole bar, so the three cases are ranked rather than combined:
 *
 * - **Finished** outranks everything. Its ends are days work actually stopped, so the target is not
 *   a question anyone is asking about it - including when it finished after the date.
 * - **Not started in time** outranks merely finishing late. Every bar that starts after the date
 *   also ends after it, so without the ranking the sharper case would never be seen; and it is a
 *   different conversation, about what the Delivery contains rather than about how fast anyone is
 *   going.
 * - **Finishes late** is what is left.
 *
 * **The comparison is between days, and it is strict.** The target is a stored instant this product
 * reads as a UTC day, and a bar's ends come from forecast dates carrying a time, so both are reduced
 * before they are compared - otherwise a bar due at nine in the morning on the target day is late by
 * fifteen hours, silently, and only for readers in some time zones. A bar ending *on* the day is on
 * track.
 *
 * A bar with nothing to say is absent from the result rather than present with a value meaning
 * nothing. On track is the ordinary case, and a colour worn by nearly every bar tells the reader
 * nothing about any of them.
 */
export function buildDeliveryBarStatuses(
	bars: TimelineBar[],
	targetDate?: Date,
): ReadonlyMap<number, BarStatusKind> {
	const statuses = new Map<number, BarStatusKind>();

	// Undefined rather than falsy: a target at the epoch is a real day and zero is not.
	const target =
		targetDate === undefined
			? undefined
			: dayOf(targetCalendarDate(targetDate));

	for (const bar of bars) {
		if (bar.endIsObserved) {
			statuses.set(bar.featureId, "finished");
			continue;
		}

		if (target === undefined) {
			continue;
		}

		if (dayOf(bar.start) > target) {
			statuses.set(bar.featureId, "startsAfterTarget");
			continue;
		}

		if (dayOf(bar.end) > target) {
			statuses.set(bar.featureId, "endsAfterTarget");
		}
	}

	return statuses;
}
