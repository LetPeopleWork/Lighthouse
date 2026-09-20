import {
	isSameLocalDay,
	isTargetDay,
	type TimelineBar,
} from "./deliveryTimelineModel";
import { TARGET_DAY_CLASS, TODAY_CLASS } from "./timelineMarkers";

/**
 * Everything the chart adapter has to decide, as plain functions.
 *
 * This is still adapter-layer — it knows the shape of a task, a scale and the library's CSS
 * variable names — but it imports nothing from the library, so it can be tested without loading
 * it. That matters more here than it usually would: the library paints to a canvas this test
 * environment does not have, and its axis needs a measured width it never gets, so a test that
 * pulls it in has to stub a drawing surface before it can assert on arithmetic.
 *
 * The component next door holds the only `@svar-ui` import, and an enforcement test keeps it that
 * way.
 */

export const ROW_HEIGHT = 38;
export const SCALE_HEIGHT = 30;
const CHART_PADDING = 20;

/** A chart with nothing to draw still needs a row's height, or it collapses to its axis alone. */
export function chartHeight(barCount: number): number {
	return Math.max(barCount, 1) * ROW_HEIGHT + SCALE_HEIGHT + CHART_PADDING;
}

/**
 * The translation into the library's vocabulary, and the only place it happens.
 *
 * Nothing asserts on what the library then renders — that is markup we did not write — so if this
 * mapping is not tested here it is not tested anywhere, and it is the one piece of the adapter
 * where a mistake is silent rather than loud.
 */
export function toGanttTasks(bars: TimelineBar[]) {
	return bars.map((bar) => ({
		id: bar.featureId,
		text: bar.name,
		start: bar.start,
		end: bar.end,
		type: "task",
	}));
}

/**
 * What a bar says when you hover it.
 *
 * Only the span, because the name is already written along the bar and repeating it in the tooltip
 * spends the reader's attention on something they can see.
 */
export function barTooltip(bar: TimelineBar, isClickable: boolean): string {
	const asDay = (date: Date) => date.toLocaleDateString();
	const span = `${asDay(bar.start)} – ${asDay(bar.end)}`;

	return isClickable ? `${span} (click for more details)` : span;
}

// The classes the library's own theme wrappers carry. Ours is the only code that needs to know
// them, and it needs to because they are where the library declares the variables we override.
const LIGHT_THEME_CLASS = "wx-willow-theme";
const DARK_THEME_CLASS = "wx-willow-dark-theme";

export const THEMED_ELEMENT_SELECTOR = `& .${LIGHT_THEME_CLASS}, & .${DARK_THEME_CLASS}`;

/**
 * The product's colour, handed to the library's own variables — the one place the two colour
 * systems meet.
 *
 * **The selector is the whole point.** The library declares these same variables on its theme
 * element, and a declaration on the element beats anything inherited from an ancestor, so setting
 * them on our wrapper reads correctly and draws the library's default blue. That is what shipped
 * first, and no unit test can catch it: this environment mocks the library's stylesheet away, so
 * the declaration that ought to win is not there to win and either arrangement looks identical.
 * The test pins the shape; the rendered result is the screenshot test's job.
 */
export function ganttColorOverrides(
	barColor: string,
	fontColor: string,
): { [selector: string]: Record<string, string> } {
	return {
		[THEMED_ELEMENT_SELECTOR]: {
			"--wx-gantt-task-color": barColor,
			"--wx-gantt-task-fill-color": barColor,
			"--wx-gantt-task-border-color": barColor,
			"--wx-gantt-task-font-color": fontColor,
		},
	};
}

/**
 * Which of the two dates worth finding at a glance an axis column is: the day the Delivery is due,
 * and the day the reader is standing on.
 *
 * The `unit` guard is load-bearing: the scale calls this for the month row as well as the day row,
 * and without it the whole month containing a marked day would be shaded rather than the one day.
 *
 * A day can be both, and then it says both — which is the most informative thing a Delivery due
 * today could render.
 */
export function dayHighlight(
	date: Date,
	unit: string,
	marks: { targetDate?: Date; today?: Date },
): string {
	if (unit !== "day") {
		return "";
	}

	const classes: string[] = [];

	if (isTargetDay(date, marks.targetDate)) {
		classes.push(TARGET_DAY_CLASS);
	}

	// Not `isTargetDay`: today is the reader's own clock, not a stored instant, so it is compared
	// as a local day. Reusing the target's comparison marks the wrong column for most of the day.
	if (isSameLocalDay(date, marks.today)) {
		classes.push(TODAY_CLASS);
	}

	return classes.join(" ");
}

const monthAndYear = (date: Date) =>
	date.toLocaleDateString(undefined, { month: "long", year: "numeric" });

const shortMonthAndYear = (date: Date) =>
	date.toLocaleDateString(undefined, { month: "short", year: "numeric" });

const dayAndMonth = (date: Date) =>
	date.toLocaleDateString(undefined, { day: "numeric", month: "numeric" });

/**
 * The two rows of the date axis.
 *
 * `format` MUST be a function. The library calls it only when it is one and otherwise prints the
 * value as it stands, so a pattern like "MMMM yyyy" heads every column with those eight characters
 * instead of a date. Formatting by hand also keeps the axis in the reader's own regional format,
 * like every other date in the product.
 */
export const TIMELINE_SCALES = [
	{ unit: "month" as const, step: 1, format: monthAndYear },
	{
		unit: "day" as const,
		step: 1,
		format: (date: Date) => String(date.getDate()),
	},
];

const WEEKLY_SCALES = [
	{ unit: "month" as const, step: 1, format: monthAndYear },
	{ unit: "week" as const, step: 1, format: dayAndMonth },
];

const MONTHLY_SCALES = [
	{
		unit: "year" as const,
		step: 1,
		format: (date: Date) => String(date.getFullYear()),
	},
	{ unit: "month" as const, step: 1, format: shortMonthAndYear },
];

/**
 * Roughly how many columns fit across the Delivery panel before it starts scrolling. The library
 * gives each column a fixed width, so this is a count rather than a span — and it is what makes
 * the rule below hold at any window size worth supporting, not only the one it was written on.
 */
const COLUMNS_THAT_FIT = 16;

const DAYS_PER = { day: 1, week: 7, month: 30 } as const;

/**
 * How coarsely to rule the axis, so a Delivery of any length arrives fitting the screen.
 *
 * Choose the *finest* unit whose columns still fit, rather than judging by the number of days.
 * Judging by days was the first attempt and it was wrong by a wide margin: three weeks of work
 * already overflowed, because each column is a fixed width and a fortnight of them is all a panel
 * holds. So days are for a short Delivery only, weeks carry the ordinary case, and months take
 * over past a season.
 *
 * A timeline the reader has to drag along is a plan they have to discover instead of see, which
 * loses the one thing this view is for. Coarsening trades exact days for a shape that arrives
 * whole — and the exact days are still a hover away on the bar.
 */
export function scalesForSpan(start: Date, end: Date) {
	const days = Math.round(
		(end.getTime() - start.getTime()) / (24 * 60 * 60 * 1000),
	);

	const fits = (unit: keyof typeof DAYS_PER) =>
		days / DAYS_PER[unit] <= COLUMNS_THAT_FIT;

	if (fits("day")) {
		return TIMELINE_SCALES;
	}

	return fits("week") ? WEEKLY_SCALES : MONTHLY_SCALES;
}
