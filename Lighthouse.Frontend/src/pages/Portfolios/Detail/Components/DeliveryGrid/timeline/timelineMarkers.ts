import type { Theme } from "@mui/material";
import {
	certainColor,
	realisticColor,
	riskyColor,
} from "../../../../../../utils/theme/colors";
import type { BarStatusKind } from "./deliveryBarStatus";
import { targetCalendarDate } from "./deliveryTimelineModel";

/**
 * Every colour this chart uses to mean something, in one place.
 *
 * One place because each of them is painted somewhere and named somewhere else, and two answers to
 * "what colour is late" is how a chart comes to disagree with the key beside it.
 */

/**
 * The two dates the timeline marks, and how each is drawn.
 *
 * Neither colour is the bar colour: a marker that shares the bars' green reads as another bar
 * rather than as an annotation on them. They are not named beside the chart — hovering the marked
 * column says which date it is and what day that is, which beats a key the reader has to look away
 * to consult for a mark the eye is already on.
 */
export const TODAY_CLASS = "delivery-today";
export const TARGET_DAY_CLASS = "delivery-target-day";

export interface MarkerColors {
	today: string;
	target: string;
}

export function markerColors(theme: Theme): MarkerColors {
	return {
		today: theme.palette.info.main,
		target: theme.palette.warning.main,
	};
}

/**
 * What hovering a marked column says it is, and which day it is.
 *
 * **The target is reduced to its calendar day first, and that is the whole reason this is a
 * function rather than two lines at the call site.** The stored value is an instant the product
 * reads as a UTC day - the Delivery heading prints it with `timeZone: "UTC"`, and the column that
 * gets tinted is chosen by the same reduction - so a target stored late in the UTC day, read raw in
 * a zone ahead of it, names tomorrow. The label then sat on a column it disagreed with, and above a
 * heading it disagreed with too.
 *
 * Today needs no such care: it is the reader's own clock and is already their own day.
 */
export function markerLabels(
	targetDate: Date | undefined,
	today: Date,
): { target: string; today: string } {
	const asDay = (date?: Date) => date?.toLocaleDateString() ?? "";

	return {
		target: `Target date · ${asDay(targetDate && targetCalendarDate(targetDate))}`,
		today: `Today · ${asDay(today)}`,
	};
}

/**
 * The colour each end of a bar is capped in.
 *
 * These are the colours this product already uses for how a forecast is going, borrowed rather than
 * invented so that the chart does not start a fourth colour vocabulary beside the likelihood chip's,
 * the Teams' and the marked columns'.
 *
 * **"Ends after the target" is deliberately the same colour as the target column itself.** It is the
 * only one of the three that was already spoken for, and it was taken anyway rather than recolouring
 * a column that has shipped. The two are told apart by shape and position — a tinted column standing
 * up through the whole chart against a few pixels at the end of one bar — and by the key, which names
 * the cap. If they turn out not to be, the column moves to a colour nothing else uses and these stay
 * as they are.
 */
export const STATUS_CAP_COLORS: Record<BarStatusKind, string> = {
	finished: certainColor,
	startsAfterTarget: riskyColor,
	endsAfterTarget: realisticColor,
};
