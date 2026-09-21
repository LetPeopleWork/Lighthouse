import { type TimelineBar, targetCalendarDate } from "./deliveryTimelineModel";

/**
 * What one end of a bar has to say about the Delivery's target date.
 *
 * `finished` is not a verdict against the target and never becomes one. A finished bar already ends
 * at the day the work stopped rather than at a forecast, so asking whether it is late would be
 * asking about a prediction nobody is making.
 */
export type BarStatusKind =
	| "finished"
	| "startsAfterTarget"
	| "endsAfterTarget";

/** What each end of one bar has to say. Either may be absent, and usually both are. */
export interface BarEndCaps {
	start?: BarStatusKind;
	end?: BarStatusKind;
}

/** The day a moment falls on, in the reader's own frame, with the time of day dropped. */
const dayOf = (moment: Date): number =>
	new Date(moment.getFullYear(), moment.getMonth(), moment.getDate()).getTime();

/**
 * Which end of which bar falls past the Delivery's target date.
 *
 * **Each end answers for itself.** A bar that begins after the date says so at its start and says
 * it does not finish in time at its end, and both are true — the second is the consequence of the
 * first rather than a competing verdict. There is deliberately no precedence here: a rule that
 * ranked the two would exist only to suppress one of them, and the ranking is what a reader would
 * then have to know in order to read the chart.
 *
 * **The comparison is between days, and it is strict.** The target is a stored instant this product
 * reads as a UTC day, and a bar's ends come from forecast dates carrying a time, so both are reduced
 * before they are compared — otherwise a bar due at nine in the morning on the target day is late by
 * fifteen hours, silently, and only for readers in some time zones. A bar ending *on* the day is on
 * track.
 *
 * A bar with nothing to say is absent from the result rather than present with an empty entry,
 * because an empty entry is drawn as a mark with nothing behind it.
 */
export function buildDeliveryBarCaps(
	bars: TimelineBar[],
	targetDate?: Date,
): ReadonlyMap<number, BarEndCaps> {
	const caps = new Map<number, BarEndCaps>();

	// Undefined rather than falsy: a target at the epoch is a real day and zero is not.
	const target =
		targetDate === undefined
			? undefined
			: dayOf(targetCalendarDate(targetDate));

	for (const bar of bars) {
		if (bar.endIsObserved) {
			caps.set(bar.featureId, { start: "finished", end: "finished" });
			continue;
		}

		if (target === undefined) {
			continue;
		}

		const marked: BarEndCaps = {};

		if (dayOf(bar.start) > target) {
			marked.start = "startsAfterTarget";
		}

		if (dayOf(bar.end) > target) {
			marked.end = "endsAfterTarget";
		}

		if (marked.start !== undefined || marked.end !== undefined) {
			caps.set(bar.featureId, marked);
		}
	}

	return caps;
}
