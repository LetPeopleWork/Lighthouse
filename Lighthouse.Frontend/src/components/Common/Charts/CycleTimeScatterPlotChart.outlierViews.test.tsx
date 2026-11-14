import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell from "../../../pages/Common/MetricsView/WidgetShell";
import type { WidgetStatus } from "../../../pages/Common/MetricsView/widgetStatus";
import { testTheme } from "../../../tests/testTheme";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => vi.fn(),
}));

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return { ...actual, useTheme: () => testTheme };
});

vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: () => null,
}));

vi.mock("./TimeBlackoutOverlay", () => ({ default: () => null }));

// The chart container is replaced by a box that publishes what it was asked to draw.
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		ChartsContainer: ({
			series,
			yAxis,
			children,
		}: {
			series: unknown;
			yAxis: unknown;
			children: React.ReactNode;
		}) => (
			<div
				data-testid="drawn-chart"
				data-series={JSON.stringify(series)}
				data-yaxis={JSON.stringify(yAxis)}
			>
				{children}
			</div>
		),
		ScatterPlot: () => null,
		ChartsXAxis: () => null,
		ChartsYAxis: () => null,
		ChartsTooltip: () => null,
		useDrawingArea: () => ({ left: 0, top: 0, width: 400, height: 200 }),
		ChartsReferenceLine: ({ label, y }: { label: string; y: number }) => (
			<div data-testid={`reference-line-${label}`} data-y={y}>
				{label}
			</div>
		),
	};
});

const WIDGET_KEY = "cycleScatter";
const DEV_TIME = 1;
const REVIEW_TIME = 2;
const definitions: INamedCycleTimeDefinition[] = [
	{ id: DEV_TIME, name: "Development time" },
	{ id: REVIEW_TIME, name: "Review time" },
];

const closedItem = (
	referenceId: string,
	cycleTime: number,
	closedDate: Date,
	type = "Story",
): IWorkItem => ({
	id: Number(referenceId.replace(/\D/g, "")),
	referenceId,
	name: referenceId,
	url: null,
	cycleTime,
	startedDate: new Date(2026, 1, 1),
	closedDate,
	workItemAge: cycleTime,
	state: "Done",
	stateCategory: "Done",
	type,
	parentWorkItemReference: "",
	isBlocked: false,
});

// Five typical closed Work Items followed by one more, closed last.
const closedSet = (
	typical: number[],
	last: number,
	month: number,
	type = "Story",
): IWorkItem[] =>
	[...typical, last].map((days, index) =>
		closedItem(
			index === typical.length ? "TZ-301" : `TZ-${101 + index}`,
			days,
			new Date(2026, month, 2 + index),
			type,
		),
	);

// Every definition measures the same days, so only the percentiles tell the answers apart.
const measuredAs = (items: IWorkItem[], days: number[]): IWorkItem[] =>
	items.map((item, index) => ({
		...item,
		namedCycleTimes: [
			{ definitionId: DEV_TIME, days: days[index] },
			{ definitionId: REVIEW_TIME, days: days[index] },
		],
	}));

const answer = (p95: number): IPercentileValue[] => [
	{ percentile: 50, value: 4 },
	{ percentile: 85, value: 5 },
	{ percentile: 95, value: p95 },
];

const chartOf = (
	items: IWorkItem[],
	status: WidgetStatus = "ready",
	fetcher?: (definitionId: number) => Promise<IPercentileValue[]>,
) => (
	<WidgetShell
		widgetKey={WIDGET_KEY}
		title="Cycle Time scatterplot"
		status={status}
		hasContentToDim={true}
	>
		<CycleTimeScatterPlotChart
			ownerKind="team"
			percentileValues={[]}
			cycleTimeDataPoints={items}
			namedCycleTimeDefinitions={definitions}
			onFetchNamedCycleTimePercentiles={fetcher}
		/>
	</WidgetShell>
);

const header = () => screen.getByTestId(`widget-shell-header-${WIDGET_KEY}`);

const outlierChip = () =>
	within(header()).getByRole("button", { name: /outlier/i });

const noOutlierChip = () =>
	within(header()).queryByRole("button", { name: /outlier/i });

const drawnHeights = (): number[] =>
	(
		JSON.parse(screen.getByTestId("drawn-chart").dataset.series ?? "[]")[0]
			.data as { y: number }[]
	).map((dot) => dot.y);

const axisTop = (): number =>
	JSON.parse(screen.getByTestId("drawn-chart").dataset.yaxis ?? "[]")[0].max;

const lineValue = (label: string): number =>
	Number(screen.getByTestId(`reference-line-${label}`).dataset.y);

const choose = async (name: string) => {
	fireEvent.mouseDown(screen.getByRole("combobox", { name: "Cycle Time" }));
	fireEvent.click(await screen.findByRole("option", { name }));
};

const toggle = (type: string) =>
	fireEvent.click(
		screen.getByRole("button", { name: `${type} visibility toggle` }),
	);

describe("The outlier chip stays truthful while the reader changes the view", () => {
	it("finds the outliers on the selected named Cycle Time and follows its percentiles", async () => {
		const fetcher = vi.fn(async () => answer(7));
		const items = measuredAs(
			closedSet([2, 4, 6, 6, 8], 58, 2),
			[2, 3, 4, 4, 5, 26],
		);
		render(chartOf(items, "ready", fetcher));

		// Default Cycle Time: 58 days is far above 5 times the median of 6.
		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);
		expect(outlierChip()).toHaveTextContent("1 outlier");

		// Development time: the same Work Item took 26 days, the median is 4 and the cut is 20.
		await choose("Development time");
		await waitFor(() => expect(lineValue("95%")).toBe(7));

		expect(drawnHeights()).toEqual([2, 3, 4, 4, 5]);
		expect(axisTop()).toBeCloseTo(7.7, 5);
		expect(outlierChip()).toHaveTextContent("1 outlier");
		fireEvent.click(outlierChip());
		expect(
			screen.getByText(
				"Not drawn: longer than 20 days (5 times the median, 4 days).",
			),
		).toBeVisible();
		const row = screen.getByRole("row", { name: /TZ-301/ });
		expect(within(row).getByText("26")).toBeVisible();
		expect(within(row).getByText("6.5x")).toBeInTheDocument();
	});

	it("shows no chip when the selected named Cycle Time makes the long Work Item ordinary", async () => {
		const fetcher = vi.fn(async () => answer(40));
		const items = measuredAs(
			closedSet([2, 4, 6, 6, 8], 58, 2),
			[20, 22, 24, 24, 26, 58],
		);
		render(chartOf(items, "ready", fetcher));
		expect(outlierChip()).toHaveTextContent("1 outlier");

		await choose("Development time");
		await waitFor(() => expect(lineValue("95%")).toBe(40));

		expect(noOutlierChip()).toBeNull();
		expect(drawnHeights()).toEqual([20, 22, 24, 24, 26, 58]);
	});

	it("ignores a late answer for a named Cycle Time the reader has already left", async () => {
		const pending = new Map<number, (values: IPercentileValue[]) => void>();
		const fetcher = vi.fn(
			(definitionId: number) =>
				new Promise<IPercentileValue[]>((resolve) => {
					pending.set(definitionId, resolve);
				}),
		);
		const items = measuredAs(
			closedSet([2, 4, 6, 6, 8], 58, 2),
			[2, 3, 4, 4, 5, 26],
		);
		render(chartOf(items, "ready", fetcher));

		await choose("Development time");
		await choose("Review time");
		await act(async () => pending.get(REVIEW_TIME)?.(answer(7)));
		await waitFor(() => expect(lineValue("95%")).toBe(7));
		await act(async () => pending.get(DEV_TIME)?.(answer(90)));

		expect(lineValue("95%")).toBe(7);
		expect(axisTop()).toBeCloseTo(7.7, 5);
		expect(outlierChip()).toHaveTextContent("1 outlier");
	});

	it("recomputes the median, the cut, the chip and the axis when the date range changes", () => {
		const view = render(
			chartOf(
				measuredAs(closedSet([2, 4, 6, 6, 8], 58, 2), [1, 1, 1, 1, 1, 1]),
			),
		);
		expect(outlierChip()).toHaveTextContent("1 outlier");
		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);

		// A slower range: 58 days is ordinary next to a median of 24, so it is drawn and the chip goes.
		view.rerender(chartOf(closedSet([20, 22, 24, 24, 26], 58, 3)));

		expect(noOutlierChip()).toBeNull();
		expect(drawnHeights()).toEqual([20, 22, 24, 24, 26, 58]);
		expect(axisTop()).toBeCloseTo(63.8, 5);

		// A faster range: 20 days is now long next to a median of 2, and the cut is 10 days.
		view.rerender(chartOf(closedSet([1, 2, 2, 2, 3], 20, 4)));

		expect(outlierChip()).toHaveTextContent("1 outlier");
		expect(drawnHeights()).toEqual([1, 2, 2, 2, 3]);
		expect(axisTop()).toBeCloseTo(3.3, 5);
		fireEvent.click(outlierChip());
		expect(
			screen.getByText(
				"Not drawn: longer than 10 days (5 times the median, 2 days).",
			),
		).toBeVisible();
	});

	it("shows no count of the previous range while the next range loads", () => {
		const view = render(chartOf(closedSet([2, 4, 6, 6, 8], 58, 2)));
		expect(outlierChip()).toHaveTextContent("1 outlier");

		const nextRange = [
			...closedSet([2, 4, 6, 6, 8], 58, 3),
			closedItem("TZ-302", 70, new Date(2026, 3, 20)),
		];
		view.rerender(chartOf(nextRange, "loading"));

		expect(outlierChip().matches(":disabled, [aria-disabled='true']")).toBe(
			true,
		);
		expect(outlierChip()).not.toHaveTextContent(/\d/);
		expect(outlierChip()).not.toHaveAccessibleName(/\d/);

		view.rerender(chartOf(nextRange, "ready"));

		expect(outlierChip()).toHaveTextContent("2 outliers");
	});

	it("counts a Work Item without a type as visible whichever types are hidden", () => {
		const items = [
			...closedSet([2, 4, 6, 6, 8], 58, 2).slice(0, 5),
			closedItem("TZ-301", 58, new Date(2026, 2, 12), ""),
		];
		render(chartOf(items));
		expect(outlierChip()).toHaveTextContent("1 outlier");

		toggle("Story");

		expect(
			screen.getByText("All visible Work Items are outliers."),
		).toBeVisible();
		expect(screen.queryByText("All types are hidden.")).toBeNull();
		expect(outlierChip()).toHaveTextContent("1 outlier");
	});
});
