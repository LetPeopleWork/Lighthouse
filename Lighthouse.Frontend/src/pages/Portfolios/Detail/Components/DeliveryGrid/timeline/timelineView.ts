import { type PageChoice, pageChoice, usePageChoice } from "./pageChoice";

/**
 * What the Delivery timeline is showing about its bars, beyond their dates.
 *
 * **One at a time, deliberately.** Each of these wants the bar's own colour, and a chart that
 * granted two of them at once would have to give one of them something else - a cap, an outline, a
 * pattern - which is a second visual language for the reader to learn and a smaller mark for them
 * to read. Asking one question at a time keeps every answer full width and full colour.
 *
 * The cost is real and is accepted: a reader asking "which Team is making this late" answers it in
 * two looks rather than one.
 */
export const TIMELINE_VIEWS = ["none", "teams", "status", "warnings"] as const;

export type TimelineView = (typeof TIMELINE_VIEWS)[number];

/**
 * Status, for a reader who has never chosen.
 *
 * It is the one of the four that answers the question the chart exists for - whether this Delivery
 * holds together - and the only one whose answer a reader cannot work out for themselves from the
 * bars. Teams and warnings are both available on the Feature table; whether a bar crosses the
 * target date is not available anywhere else.
 */
export const DEFAULT_TIMELINE_VIEW: TimelineView = "status";

export const timelineViewStore: PageChoice<TimelineView> = pageChoice(
	"lighthouse:deliveryTimeline:view",
	TIMELINE_VIEWS,
	DEFAULT_TIMELINE_VIEW,
);

export function useTimelineView(): {
	view: TimelineView;
	chooseView: (next: TimelineView) => void;
} {
	const { chosen, choose } = usePageChoice(timelineViewStore);

	return { view: chosen, chooseView: choose };
}
