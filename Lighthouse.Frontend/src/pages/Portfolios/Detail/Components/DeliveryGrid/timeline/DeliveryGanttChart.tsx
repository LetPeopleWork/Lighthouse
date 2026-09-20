import "@svar-ui/react-gantt/all.css";

import { Box, useTheme } from "@mui/material";
import { Gantt, Willow, WillowDark } from "@svar-ui/react-gantt";
import type React from "react";
import { type ComponentProps, useMemo } from "react";
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
	/** False below the narrow breakpoint, where a fixed name pane pushes the bars off-screen. */
	showTaskPane: boolean;
}

const TARGET_DAY_CLASS = "delivery-target-day";

const ROW_HEIGHT = 38;
const SCALE_HEIGHT = 30;

const SCALES = [
	{ unit: "month" as const, step: 1, format: "MMMM yyyy" },
	{ unit: "day" as const, step: 1, format: "d" },
];

const DeliveryGanttChart: React.FC<DeliveryGanttChartProps> = ({
	bars,
	targetDate,
	showTaskPane,
}) => {
	const theme = useTheme();
	const isDark = theme.palette.mode === "dark";

	const tasks = useMemo(
		() =>
			bars.map((bar) => ({
				id: bar.featureId,
				text: bar.name,
				start: bar.start,
				end: bar.end,
				type: "task",
			})),
		[bars],
	);

	const window = useMemo(
		() => timelineWindow(bars, targetDate),
		[bars, targetDate],
	);

	// The library hands this callback a date at local midnight and expects a class name back. A
	// tinted column is the free edition's substitute for the vertical marker, which is a paid
	// feature; buying one later replaces this without changing anything above.
	const highlightTime = (date: Date, unit: string) =>
		unit === "day" && isTargetDay(date, targetDate) ? TARGET_DAY_CLASS : "";

	const GanttTheme = isDark ? WillowDark : Willow;

	return (
		<Box
			data-testid="delivery-gantt"
			data-theme-mode={isDark ? "dark" : "light"}
			sx={{
				height: Math.max(bars.length, 1) * ROW_HEIGHT + SCALE_HEIGHT + 20,
				[`& .${TARGET_DAY_CLASS}`]: {
					backgroundColor: theme.palette.action.selected,
				},
			}}
		>
			<GanttTheme>
				<Gantt
					tasks={tasks}
					links={[]}
					scales={SCALES}
					start={window?.start}
					end={window?.end}
					cellHeight={ROW_HEIGHT}
					scaleHeight={SCALE_HEIGHT}
					highlightTime={highlightTime}
					// The cast is the library's bug, not ours. Its component documents and accepts
					// `false` here to drop the name pane, but the prop type is intersected with a
					// config type declaring an array, and `false` satisfies neither half of the
					// result. Dropping the pane is the only way the chart fits a phone.
					columns={
						showTaskPane
							? undefined
							: (false as unknown as ComponentProps<typeof Gantt>["columns"])
					}
					readonly
				/>
			</GanttTheme>
		</Box>
	);
};

export default DeliveryGanttChart;
