import "@svar-ui/react-gantt/all.css";

import { alpha, Box, Tooltip, useTheme } from "@mui/material";
import { Gantt, Willow, WillowDark } from "@svar-ui/react-gantt";
import type React from "react";
import { type ComponentProps, useCallback, useMemo } from "react";
import { type TimelineBar, timelineWindow } from "./deliveryTimelineModel";
import {
	barTooltip,
	chartHeight,
	dayHighlight,
	ganttColorOverrides,
	ROW_HEIGHT,
	SCALE_HEIGHT,
	scalesForSpan,
	TIMELINE_SCALES,
	toGanttTasks,
} from "./ganttShapes";
import { markerColors, TARGET_DAY_CLASS, TODAY_CLASS } from "./timelineMarkers";

/**
 * The one place in this application that knows a third-party Gantt exists.
 *
 * Everything above it speaks in bars, days and a Delivery's target date; the translation into the
 * library's own vocabulary lives next door in `ganttShapes`, which this component arranges. So
 * swapping the library is a rewrite of these two files rather than a search across the app, and
 * an enforcement test keeps the import from spreading. Two consequences worth stating, because
 * both are easy to undo by accident:
 *
 * - Nothing here leaks outward. No type from the library appears in this module's exported surface.
 * - Nothing is handed a bar it cannot place. The free edition draws an undated task at a position
 *   the data does not support instead of leaving it out, so the caller filters first and this
 *   component receives only spans with both ends.
 */
export interface DeliveryGanttChartProps {
	bars: TimelineBar[];
	targetDate?: Date;
	/** Passed in rather than read here, so the chart and its legend mark the same day. */
	today: Date;
	/** Called with the Feature's id when its bar is chosen. Absent means the bars are inert. */
	onBarSelected?: (featureId: number) => void;
}

const DeliveryGanttChart: React.FC<DeliveryGanttChartProps> = ({
	bars,
	targetDate,
	today,
	onBarSelected,
}) => {
	const theme = useTheme();
	const isDark = theme.palette.mode === "dark";
	const barColor = theme.palette.primary.main;
	const marks = markerColors(theme);

	const tasks = useMemo(() => toGanttTasks(bars), [bars]);

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

	const scales = useMemo(
		() =>
			axisRange
				? scalesForSpan(axisRange.start, axisRange.end)
				: TIMELINE_SCALES,
		[axisRange],
	);

	const barsById = useMemo(
		() => new Map(bars.map((bar) => [bar.featureId, bar])),
		[bars],
	);

	/**
	 * The inside of each bar, rendered by us rather than by the library.
	 *
	 * This is the documented way to own a bar's content, and owning it is what keeps the hover text
	 * and the click in ordinary React. The alternative — subscribing to the library's own task
	 * events — types as valid whatever name is passed, because its props carry an `on${string}`
	 * index signature, so a wrong guess compiles and silently does nothing.
	 */
	const BarContent = useCallback(
		({ data }: { data: { id?: string | number } }) => {
			const bar = barsById.get(Number(data.id));

			if (!bar) {
				return null;
			}

			const select = onBarSelected;

			return (
				<Tooltip title={barTooltip(bar, select !== undefined)} followCursor>
					<Box
						component={select ? "button" : "div"}
						type={select ? "button" : undefined}
						onClick={select ? () => select(bar.featureId) : undefined}
						sx={{
							width: "100%",
							height: "100%",
							display: "flex",
							alignItems: "center",
							justifyContent: "center",
							overflow: "hidden",
							whiteSpace: "nowrap",
							textOverflow: "ellipsis",
							px: 1,
							background: "none",
							border: "none",
							font: "inherit",
							color: "inherit",
							cursor: select ? "pointer" : "default",
						}}
					>
						{bar.name}
					</Box>
				</Tooltip>
			);
		},
		[barsById, onBarSelected],
	);

	const GanttTheme = isDark ? WillowDark : Willow;

	return (
		<Box
			data-testid="delivery-gantt"
			data-theme-mode={isDark ? "dark" : "light"}
			sx={{
				height: chartHeight(bars.length),
				// White on the bar in both modes, rather than whatever contrasts best with the fill.
				// The two modes use different greens, so computing it per mode made the label flip
				// from white to near-black between them — the same bar reading as two components.
				...ganttColorOverrides(barColor, theme.palette.primary.contrastText),
				// Drawn differently from each other on purpose — the target is a filled band, a date
				// being aimed at; today is a ruled line, a position being stood on — and both in a
				// colour the bars never use, so neither reads as another bar. The legend beside the
				// chart names them, because a mark nobody can name is decoration.
				[`& .${TARGET_DAY_CLASS}`]: {
					backgroundColor: alpha(marks.target, 0.22),
					boxShadow: `inset 0 3px 0 0 ${marks.target}`,
				},
				[`& .${TODAY_CLASS}`]: {
					backgroundColor: alpha(marks.today, 0.12),
					boxShadow: `inset 3px 0 0 0 ${marks.today}`,
				},
			}}
		>
			<GanttTheme>
				<Gantt
					tasks={tasks}
					links={[]}
					scales={scales}
					start={axisRange?.start}
					end={axisRange?.end}
					cellHeight={ROW_HEIGHT}
					scaleHeight={SCALE_HEIGHT}
					highlightTime={highlightTime}
					taskTemplate={BarContent}
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
