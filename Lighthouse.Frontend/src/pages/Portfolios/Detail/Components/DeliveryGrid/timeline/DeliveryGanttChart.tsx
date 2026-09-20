import "@svar-ui/react-gantt/all.css";

import { Box, useTheme } from "@mui/material";
import { Gantt, Willow, WillowDark } from "@svar-ui/react-gantt";
import type React from "react";
import { type ComponentProps, useCallback, useMemo } from "react";
import {
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

/**
 * Which axis columns get the target tint.
 *
 * The `unit` guard is load-bearing: the scale calls this for the month row as well as the day row,
 * and without it the whole month containing the target would be shaded rather than the one day.
 *
 * Exported and tested directly because the axis needs a measured width and so never renders outside
 * a browser — nothing invokes this callback in a test run otherwise.
 */
export function targetDayHighlight(
	date: Date,
	unit: string,
	targetDate: Date | undefined,
): string {
	return unit === "day" && isTargetDay(date, targetDate)
		? TARGET_DAY_CLASS
		: "";
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

	const axisRange = useMemo(
		() => timelineWindow(bars, targetDate),
		[bars, targetDate],
	);

	// The library hands this callback a date at local midnight and expects a class name back. A
	// tinted column is the free edition's substitute for the vertical marker, which is a paid
	// feature; buying one later replaces this without changing anything above.
	const highlightTime = useCallback(
		(date: Date, unit: string) => targetDayHighlight(date, unit, targetDate),
		[targetDate],
	);

	const GanttTheme = isDark ? WillowDark : Willow;

	return (
		<Box
			data-testid="delivery-gantt"
			data-theme-mode={isDark ? "dark" : "light"}
			sx={{
				height: chartHeight(bars.length),
				// The library paints its bars from its own variables, not from the MUI theme, so the
				// brand colour is handed across here rather than left at its default blue. This is the
				// one place the two colour systems meet.
				"--wx-gantt-task-color": barColor,
				"--wx-gantt-task-fill-color": barColor,
				"--wx-gantt-task-border-color": barColor,
				"--wx-gantt-task-font-color": theme.palette.getContrastText(barColor),
				[`& .${TARGET_DAY_CLASS}`]: {
					backgroundColor: theme.palette.action.selected,
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
