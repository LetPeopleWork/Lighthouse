import { fireEvent, render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell from "../../../pages/Common/MetricsView/WidgetShell";
import type { WidgetStatus } from "../../../pages/Common/MetricsView/widgetStatus";
import { testTheme } from "../../../tests/testTheme";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

const { reportUsage } = vi.hoisted(() => ({ reportUsage: vi.fn() }));

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => reportUsage,
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
		ChartsReferenceLine: ({ label }: { label: string }) => (
			<div data-testid={`reference-line-${label}`}>{label}</div>
		),
	};
});

const WIDGET_KEY = "cycleScatter";

// Closed Work Items whose 50th percentile is 6 days, so the cut is 30 days.
const closedItem = (
	referenceId: string,
	name: string,
	cycleTime: number,
	closedDate: Date,
): IWorkItem => ({
	id: Number(referenceId.replace(/\D/g, "")),
	referenceId,
	name,
	url: null,
	cycleTime,
	startedDate: new Date(2026, 1, 1),
	closedDate,
	workItemAge: cycleTime,
	state: "Done",
	stateCategory: "Done",
	type: "Task",
	parentWorkItemReference: "",
	isBlocked: false,
});

const typicalItems = (): IWorkItem[] => [
	closedItem("TZ-101", "Login", 2, new Date(2026, 2, 2)),
	closedItem("TZ-102", "Search", 4, new Date(2026, 2, 3)),
	closedItem("TZ-103", "Export", 6, new Date(2026, 2, 4)),
	closedItem("TZ-104", "Import", 6, new Date(2026, 2, 5)),
	closedItem("TZ-105", "Filter", 8, new Date(2026, 2, 6)),
];

const withLongItem = (cycleTime: number): IWorkItem[] => [
	...typicalItems(),
	closedItem(
		"TZ-301",
		"Legacy data store migration",
		cycleTime,
		new Date(2026, 2, 12),
	),
];

const ofType = (items: IWorkItem[], type: string): IWorkItem[] =>
	items.map((item) => ({ ...item, type }));

// Typical work is Stories; the one 58-day Work Item is a Bug.
const storiesAndOneLongBug = (): IWorkItem[] => [
	...ofType(typicalItems(), "Story"),
	...ofType(withLongItem(58).slice(5), "Bug"),
];

const percentiles = (p95: number | null): IPercentileValue[] =>
	p95 === null
		? []
		: [
				{ percentile: 50, value: 6 },
				{ percentile: 85, value: 8 },
				{ percentile: 95, value: p95 },
			];

const sle = (value: number | null): IPercentileValue | null =>
	value === null ? null : { percentile: 85, value };

const frozen = <T extends object>(values: T[]): T[] =>
	Object.freeze(values.map((value) => Object.freeze(value))) as T[];

const chartIn = (
	items: IWorkItem[],
	p95: number | null,
	sleValue: number | null,
	status: WidgetStatus,
) => (
	<WidgetShell
		widgetKey={WIDGET_KEY}
		title="Cycle Time scatterplot"
		status={status}
		hasContentToDim
	>
		<CycleTimeScatterPlotChart
			ownerKind="team"
			percentileValues={frozen(percentiles(p95))}
			cycleTimeDataPoints={frozen(items)}
			serviceLevelExpectation={sle(sleValue)}
		/>
	</WidgetShell>
);

const showChart = (
	items: IWorkItem[],
	p95: number | null,
	sleValue: number | null,
	status: WidgetStatus = "ready",
) => render(chartIn(items, p95, sleValue, status));

const header = () => screen.getByTestId(`widget-shell-header-${WIDGET_KEY}`);

const outlierChip = () =>
	within(header()).getByRole("button", { name: /outlier/i });

const drawnHeights = (): number[] =>
	(
		JSON.parse(screen.getByTestId("drawn-chart").dataset.series ?? "[]")[0]
			.data as { y: number }[]
	).map((dot) => dot.y);

const axisTop = (): number =>
	JSON.parse(screen.getByTestId("drawn-chart").dataset.yaxis ?? "[]")[0].max;

const isDisabled = (element: HTMLElement): boolean =>
	element.matches(":disabled, [aria-disabled='true']");

describe("Cycle Time scatterplot hides exceptionally long closed Work Items", () => {
	beforeEach(() => {
		reportUsage.mockClear();
	});

	it("leaves the chart as it was when no closed Work Item is longer than 5 times the median", () => {
		// 30 days is exactly 5 times the median of 6: not long enough to be left out.
		showChart(withLongItem(30), 15, 7);

		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8, 30]);
		expect(axisTop()).toBeCloseTo(33, 5);
		expect(
			within(header()).queryByRole("button", { name: /outlier/i }),
		).toBeNull();
	});

	it("shows the one 58-day Work Item as an outlier in the header while the statistics stay the same", () => {
		showChart(withLongItem(58), 15, 7);

		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);
		expect(axisTop()).toBeCloseTo(16.5, 5);
		for (const label of ["50%", "85%", "95%"]) {
			expect(screen.getByTestId(`reference-line-${label}`)).toBeVisible();
		}

		expect(outlierChip()).toHaveTextContent("1 outlier");
		expect(isDisabled(outlierChip())).toBe(false);

		fireEvent.click(outlierChip());

		expect(
			screen.getByText(
				"Not drawn: longer than 30 days (5 times the median, 6 days).",
			),
		).toBeVisible();
		expect(
			screen.getByText("Still counted in the percentiles, SLE and View Data."),
		).toBeVisible();
		const row = screen.getByRole("row", { name: /TZ-301/ });
		expect(row).toHaveTextContent("12 Mar 2026");
		expect(within(row).getByText("58")).toBeVisible();
		expect(within(row).getByText("9.7x")).toBeInTheDocument();
		expect(within(row).getByText("9.7 times the median")).toBeInTheDocument();
		expect(screen.queryByText(/Above the axis/)).toBeNull();
	});

	it("flags a Work Item one day over 5 times the median", () => {
		showChart(withLongItem(31), 15, 7);

		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);
		expect(outlierChip()).toHaveTextContent("1 outlier");
	});

	it("tells the reader about a percentile and an SLE line that lie above the axis", () => {
		showChart(withLongItem(58), 33, 35);

		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);
		expect(axisTop()).toBeCloseTo(8.8, 5);
		// Lines that fit stay in place; the two that do not are named instead of lost.
		expect(screen.getByTestId("reference-line-85%")).toBeVisible();
		expect(screen.queryByTestId("reference-line-95%")).toBeNull();
		expect(
			screen.queryByTestId("reference-line-SLE: 85% @ 35 days or less"),
		).toBeNull();

		fireEvent.click(outlierChip());

		expect(
			screen.getByText("Above the axis: 95% at 33 days, SLE at 35 days."),
		).toBeVisible();
	});

	it("keeps a line that sits exactly at the top of the axis in place", () => {
		// The 95% line at 30 days is the cut itself, so the axis stops there and the line is not above it.
		showChart(withLongItem(58), 30, 7);

		expect(axisTop()).toBeCloseTo(30, 5);
		expect(screen.getByTestId("reference-line-95%")).toBeVisible();

		fireEvent.click(outlierChip());

		expect(screen.queryByText(/Above the axis/)).toBeNull();
	});

	it("scales the axis to the typical items when there are no percentile or SLE lines", () => {
		showChart(withLongItem(58), null, null);

		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);
		expect(axisTop()).toBeCloseTo(8.8, 5);

		fireEvent.click(outlierChip());

		expect(screen.queryByText(/Above the axis/)).toBeNull();
	});

	it.each([
		["a single closed Work Item", [10]],
		["closed Work Items that all took the same time", [7, 7, 7]],
	])("shows no outlier chip for %s", (_case, cycleTimes) => {
		showChart(
			cycleTimes.map((days, index) =>
				closedItem(`TZ-${index}`, "Same", days, new Date(2026, 2, 2 + index)),
			),
			null,
			null,
		);

		expect(drawnHeights()).toEqual(cycleTimes);
		expect(
			within(header()).queryByRole("button", { name: /outlier/i }),
		).toBeNull();
	});

	it("shows no outlier chip when there are no closed Work Items", () => {
		showChart([], 15, 7);

		expect(screen.getByText("No data available")).toBeVisible();
		expect(
			within(header()).queryByRole("button", { name: /outlier/i }),
		).toBeNull();
	});

	it("keeps the cut and the axis still while the reader hides and shows Work Item types", () => {
		showChart(storiesAndOneLongBug(), 15, 7);
		const toggle = (type: string) =>
			fireEvent.click(
				screen.getByRole("button", { name: `${type} visibility toggle` }),
			);
		expect(axisTop()).toBeCloseTo(16.5, 5);

		// Only the outlier is left visible: the plot says so and the cut still comes from every item.
		toggle("Story");

		expect(
			screen.getByText("All visible Work Items are outliers."),
		).toBeVisible();
		expect(outlierChip()).toHaveTextContent("1 outlier");

		// The outlier's type is hidden: no chip, same axis.
		toggle("Story");
		toggle("Bug");

		expect(
			within(header()).queryByRole("button", { name: /outlier/i }),
		).toBeNull();
		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);
		expect(axisTop()).toBeCloseTo(16.5, 5);

		// Every type hidden: the plot says so, without a chip, and a type can be shown again.
		toggle("Story");

		expect(screen.getByText("All types are hidden.")).toBeVisible();
		expect(
			within(header()).queryByRole("button", { name: /outlier/i }),
		).toBeNull();

		toggle("Story");

		expect(drawnHeights()).toEqual([2, 4, 6, 6, 8]);
	});

	it("reports one usage event per opening and shows no count while the widget reloads", () => {
		const view = showChart(withLongItem(58), 15, 7);

		fireEvent.click(outlierChip());

		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith({ name: "OutlierPopoverOpened" });
		expect(screen.getByText(/Not drawn: longer than/)).toBeVisible();

		view.rerender(chartIn(withLongItem(58), 15, 7, "loading"));

		expect(isDisabled(outlierChip())).toBe(true);
		expect(outlierChip()).not.toHaveTextContent(/\d/);
		expect(outlierChip()).not.toHaveAccessibleName(/\d/);
		expect(screen.queryByText(/Not drawn: longer than/)).toBeNull();
		expect(reportUsage).toHaveBeenCalledTimes(1);
	});
});
