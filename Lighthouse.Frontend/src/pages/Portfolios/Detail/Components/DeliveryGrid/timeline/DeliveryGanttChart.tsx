import "@svar-ui/react-gantt/all.css";

import { alpha, Box, Tooltip, useTheme } from "@mui/material";
import { Gantt, Willow, WillowDark } from "@svar-ui/react-gantt";
import type React from "react";
import { type ComponentProps, useCallback, useMemo } from "react";
import {
	isSameLocalDay,
	isTargetDay,
	type TimelineBar,
	timelineWindow,
} from "./deliveryTimelineModel";
import { markerColors, TARGET_DAY_CLASS, TODAY_CLASS } from "./timelineMarkers";

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
	/** Passed in rather than read here, so the chart and its legend mark the same day. */
	today: Date;
	/** Called with the Feature's id when its bar is chosen. Absent means the bars are inert. */
	onBarSelected?: (featureId: number) => void;
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
 * The assertion below pins the shape; the rendered result is the screenshot test's job.
 */
export function ganttColorOverrides(
	barColor: string,
	fontColor: string,
): {
	[selector: string]: Record<string, string>;
} {
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
const monthAndYear = (date: Date) =>
	date.toLocaleDateString(undefined, { month: "long", year: "numeric" });

const shortMonthAndYear = (date: Date) =>
	date.toLocaleDateString(undefined, { month: "short", year: "numeric" });

const dayAndMonth = (date: Date) =>
	date.toLocaleDateString(undefined, { day: "numeric", month: "numeric" });

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
 * gives each column a fixed width, so this is a count rather than a span — and it is what makes the
 * rule below hold at any window size worth supporting rather than only at the one it was written on.
 */
const COLUMNS_THAT_FIT = 16;

const DAYS_PER = { day: 1, week: 7, month: 30 } as const;

/**
 * How coarsely to rule the axis, so a Delivery of any length arrives fitting the screen.
 *
 * Choose the *finest* unit whose columns still fit, rather than judging by the number of days.
 * Judging by days was the first attempt and it was wrong by a wide margin: three weeks of work
 * already overflows, because each column is a fixed width and a fortnight of them is all a panel
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
