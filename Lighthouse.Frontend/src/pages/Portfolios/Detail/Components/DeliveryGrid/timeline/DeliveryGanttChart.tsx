import "@svar-ui/react-gantt/all.css";

import { Box, useTheme } from "@mui/material";
import { Gantt, Willow, WillowDark } from "@svar-ui/react-gantt";
import type React from "react";
import { type ComponentProps, useCallback, useMemo } from "react";
import {
	isSameLocalDay,
	isTargetDay,
	type TimelineBar,
	timelineWindow,
} from "./deliveryTimelineModel";

/**
 * The one place in this application that knows a third-party Gantt exists.
 *
 * Everything above it speaks in bars, days and a Delivery's target date; the translation into the
 * library's own vocabulary happens here and nowhere else, so swapping the library is a rewrite of
 * this file rather than a search across the app. Two consequences worth stating, because both are
 * easy to undo by accident:
 *
 * - Nothing here leaks outward. No type from the library appears in this module's exported surface.
 * - Nothing is handed a bar it cannot place. The free edition draws an undated task at a position
 *   the data does not support instead of leaving it out, so the caller filters first and this
 *   component receives only spans with both ends.
 */
export interface DeliveryGanttChartProps {
	bars: TimelineBar[];
	targetDate?: Date;
}

const TARGET_DAY_CLASS = "delivery-target-day";
const TODAY_CLASS = "delivery-today";

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
 * The assertion below pins the shape; the rendered result is the screenshot test's job.
 */
export function ganttColorOverrides(barColor: string, fontColor: string) {
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
 *
 * Exported and tested directly because the axis needs a measured width and so never renders outside
 * a browser — nothing invokes this callback in a test run otherwise.
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

const ROW_HEIGHT = 38;
const SCALE_HEIGHT = 30;
const CHART_PADDING = 20;

/**
 * The translation into the library's vocabulary, and the only place it happens.
 *
 * Exported so it can be tested as the pure function it is. Nothing asserts on what the library then
 * renders — that is markup we did not write — so if this mapping is not tested here it is not tested
 * anywhere, and it is the one piece of the adapter where a mistake is silent rather than loud.
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

/** A chart with nothing to draw still needs a row's height, or it collapses to its axis alone. */
export function chartHeight(barCount: number): number {
	return Math.max(barCount, 1) * ROW_HEIGHT + SCALE_HEIGHT + CHART_PADDING;
}

/**
 * The two rows of the date axis.
 *
 * `format` MUST be a function. The library calls it only when it is one and otherwise prints the
 * value as it stands, so a pattern like "MMMM yyyy" heads every column with those eight characters
 * instead of a date. Formatting by hand also keeps the axis in the reader's own regional format,
 * like every other date in the product.
 *
 * Exported because the axis needs a measured width to render and therefore draws nothing outside a
 * browser; asserting on these directly is the only way this stays pinned.
 */
export const TIMELINE_SCALES = [
	{
		unit: "month" as const,
		step: 1,
		format: (date: Date) =>
			date.toLocaleDateString(undefined, { month: "long", year: "numeric" }),
	},
	{
		unit: "day" as const,
		step: 1,
		format: (date: Date) => String(date.getDate()),
	},
];

const DeliveryGanttChart: React.FC<DeliveryGanttChartProps> = ({
	bars,
	targetDate,
}) => {
	const theme = useTheme();
	const isDark = theme.palette.mode === "dark";
	const barColor = theme.palette.primary.main;

	const tasks = useMemo(() => toGanttTasks(bars), [bars]);

	// Fixed for as long as the chart is mounted, rather than read afresh on every render. A new
	// Date each time is a new identity, which would invalidate everything memoised against it on
	// every render — and the cost of holding it is only that a tab left open across midnight keeps
	// yesterday's marker until something else redraws it.
	const today = useMemo(() => new Date(), []);

	const axisRange = useMemo(
		() => timelineWindow(bars, targetDate, today),
		[bars, targetDate, today],
	);

	// The library hands this callback a date at local midnight and expects a class name back. A
	// tinted column is the free edition's substitute for the vertical marker, which is a paid
	// feature; buying one later replaces this without changing anything above.
	const highlightTime = useCallback(
		(date: Date, unit: string) =>
			dayHighlight(date, unit, { targetDate, today }),
		[targetDate, today],
	);

	const GanttTheme = isDark ? WillowDark : Willow;

	return (
		<Box
			data-testid="delivery-gantt"
			data-theme-mode={isDark ? "dark" : "light"}
			sx={{
				height: chartHeight(bars.length),
				...ganttColorOverrides(
					barColor,
					theme.palette.getContrastText(barColor),
				),
				// The two are drawn differently on purpose, so a reader can tell at a glance which is
				// which without a legend: the target is a filled band (a date being aimed at), today
				// is a ruled edge (a position being stood on). A day that is both shows both.
				[`& .${TARGET_DAY_CLASS}`]: {
					backgroundColor: theme.palette.action.selected,
				},
				[`& .${TODAY_CLASS}`]: {
					boxShadow: `inset 2px 0 0 0 ${theme.palette.primary.main}`,
				},
			}}
		>
			<GanttTheme>
				<Gantt
					tasks={tasks}
					links={[]}
					scales={TIMELINE_SCALES}
					start={axisRange?.start}
					end={axisRange?.end}
					cellHeight={ROW_HEIGHT}
					scaleHeight={SCALE_HEIGHT}
					highlightTime={highlightTime}
					// Off, or the library recomputes the range from the tasks alone and discards the
					// start and end above — which silently drops a target date that falls beyond the
					// last bar, exactly the case a reader opens this chart to check.
					autoScale={false}
					// No task-name pane. Every name it would list is already on its own bar, and the
					// pane is a fixed width that pushes the chart off-screen on a narrow window.
					//
					// The cast is the library's bug, not ours: its component documents and accepts
					// `false` here, but the prop type is intersected with a config type declaring an
					// array, and `false` satisfies neither half of the result.
					columns={false as unknown as ComponentProps<typeof Gantt>["columns"]}
					readonly
				/>
			</GanttTheme>
		</Box>
	);
};

export default DeliveryGanttChart;
