import "@svar-ui/react-gantt/all.css";

import { alpha, Box, useTheme } from "@mui/material";
import { Gantt, Willow, WillowDark } from "@svar-ui/react-gantt";
import type React from "react";
import {
	type ComponentProps,
	useCallback,
	useEffect,
	useMemo,
	useRef,
	useState,
} from "react";
import { type TimelineBar, timelineWindow } from "./deliveryTimelineModel";
import {
	chartHeight,
	columnHighlight,
	ganttColorOverrides,
	ROW_HEIGHT,
	SCALE_HEIGHT,
	scalesForSpan,
	TIMELINE_SCALES,
	toGanttTasks,
} from "./ganttShapes";
import TimelineBarContent from "./TimelineBarContent";
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

/** How far apart two measurements have to be before the axis is re-ruled. */
const WIDTH_STEP = 24;

const asDay = (date?: Date) => date?.toLocaleDateString() ?? "";

/**
 * A hover label on a marked column, drawn as a pseudo-element on the column itself.
 *
 * The columns belong to the chart library, so there is no element of ours to hang a real tooltip
 * on — but the class that tints them is ours, and CSS can put a label on it. The text travels as a
 * custom property because `content` cannot be built from anything else.
 *
 * This replaces a legend beside the chart. The legend was there because the markers were invisible
 * and therefore unexplainable; now that they read clearly, naming them where the eye already is
 * beats a key the reader has to look away to consult.
 */
function markerLabel(text: string) {
	return {
		"--delivery-marker-label": `"${text}"`,
		position: "relative",
		"&:hover::after": {
			content: "var(--delivery-marker-label)",
			position: "absolute",
			left: "50%",
			top: "100%",
			transform: "translateX(-50%)",
			zIndex: 10,
			whiteSpace: "nowrap",
			pointerEvents: "none",
			px: 1,
			py: 0.25,
			borderRadius: 1,
			fontSize: "0.75rem",
			backgroundColor: "rgba(0, 0, 0, 0.87)",
			color: "#fff",
		},
	};
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

	// Measured rather than assumed, so the axis re-rules itself when the window changes. Only the
	// width is held, and only in steps: a value that moved by a pixel would re-render the chart,
	// whose own layout can move the panel by a pixel, which is how a resize loop starts.
	const panel = useRef<HTMLDivElement>(null);
	const [panelWidth, setPanelWidth] = useState(0);

	useEffect(() => {
		const element = panel.current;

		if (!element || typeof ResizeObserver === "undefined") {
			return;
		}

		const observer = new ResizeObserver(([entry]) => {
			const measured = Math.round(entry.contentRect.width);
			setPanelWidth((held) =>
				Math.abs(held - measured) < WIDTH_STEP ? held : measured,
			);
		});

		observer.observe(element);

		return () => observer.disconnect();
	}, []);

	const scales = useMemo(
		() =>
			axisRange
				? scalesForSpan(axisRange.start, axisRange.end, panelWidth)
				: TIMELINE_SCALES,
		[axisRange, panelWidth],
	);

	// The finest row of the axis, which is the one worth marking. It changes with the Delivery's
	// length, so it is read off the scales rather than assumed to be days.
	const finestUnit = scales[scales.length - 1].unit;

	// The library hands this callback the start of each column and expects a class name back. A
	// tinted column is the free edition's substitute for the vertical marker, which is a paid
	// feature; buying one later replaces this without changing anything above.
	const highlightTime = useCallback(
		(date: Date, unit: string) =>
			columnHighlight(date, unit, finestUnit, { targetDate, today }),
		[targetDate, today, finestUnit],
	);

	const barsById = useMemo(
		() => new Map(bars.map((bar) => [bar.featureId, bar])),
		[bars],
	);

	const BarContent = useCallback(
		({ data }: { data: { id?: string | number } }) => (
			<TimelineBarContent
				bar={barsById.get(Number(data.id))}
				onSelect={onBarSelected}
			/>
		),
		[barsById, onBarSelected],
	);

	const GanttTheme = isDark ? WillowDark : Willow;

	return (
		<Box
			ref={panel}
			data-testid="delivery-gantt"
			data-theme-mode={isDark ? "dark" : "light"}
			data-axis-unit={finestUnit}
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
					...markerLabel(`Target date · ${asDay(targetDate)}`),
				},
				[`& .${TODAY_CLASS}`]: {
					backgroundColor: alpha(marks.today, 0.12),
					boxShadow: `inset 3px 0 0 0 ${marks.today}`,
					...markerLabel(`Today · ${asDay(today)}`),
				},
			}}
		>
			<GanttTheme>
				<Gantt
					// Remounts when the axis changes resolution, and only then. The library reads
					// `scales` into its own store when it initialises and does not pick up a later
					// change, so on a resize the new scales were computed and never drawn — the old
					// axis stayed until something else forced a redraw, which made it look as though
					// the resolution only followed the percentile buttons. Crossing a threshold is
					// rare, so a remount is cheaper than it sounds and is the only lever we have from
					// outside their store.
					key={finestUnit}
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
