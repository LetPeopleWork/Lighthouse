import type { Theme } from "@mui/material";

/**
 * The two dates the timeline marks, and how each is drawn.
 *
 * Shared between the chart, which paints them, and the legend, which names them — because a mark
 * nobody can name is decoration. Neither colour is the bar colour: a marker that shares the bars'
 * green reads as another bar rather than as an annotation on them.
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
