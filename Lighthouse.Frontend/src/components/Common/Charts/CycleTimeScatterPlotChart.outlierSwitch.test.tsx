import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell from "../../../pages/Common/MetricsView/WidgetShell";
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
		useDrawingArea: () => ({ left: 0, top: 0, width: 400, height: 200 }),
		ChartsReferenceLine: () => null,
	};
});

const WIDGET_KEY = "cycleScatter";

interface OutlierVerdict {
	readonly status: "Ready" | "NoLimits";
	readonly upperNaturalProcessLimit: number | null;
	readonly outlierWorkItemIds: number[];
}

// Stands in for the backend: it answers the ids of the given Work Items whose Cycle Time is above the limit it was given.
const backendJudgingAbove =
	(limit: number, items: readonly IWorkItem[]) =>
	(definitionId: number | null): Promise<OutlierVerdict> =>
		Promise.resolve({
			status: "Ready",
			upperNaturalProcessLimit: limit,
			outlierWorkItemIds: items
				.filter(
					(item) =>
						(definitionId === null
							? item.cycleTime
							: (item.namedCycleTimes?.find(
									(named) => named.definitionId === definitionId,
								)?.days ?? 0)) > limit,
				)
				.map((item) => item.id),
		});

let queryClient = new QueryClient();

const aFreshQueryClient = () => {
	queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
};

const theVerdictHasLanded = async () => {
	await within(
		screen.getByTestId(`widget-shell-header-${WIDGET_KEY}`),
	).findByRole("button", { name: /^\d+ outliers? not drawn/ });
};

const closedItem = (
	referenceId: string,
	cycleTime: number,
	day: number,
): IWorkItem => ({
	id: Number(referenceId.replace(/\D/g, "")),
	referenceId,
	name: referenceId,
	url: null,
	cycleTime,
	startedDate: new Date(2026, 1, 1),
	closedDate: new Date(2026, 2, day),
	workItemAge: cycleTime,
	state: "Done",
	stateCategory: "Done",
	type: "Task",
	parentWorkItemReference: "",
	isBlocked: false,
});

// Above a limit of 30 days, the 60-day item is the one outlier.
const closedItems = (): IWorkItem[] =>
	[
		closedItem("TZ-101", 2, 2),
		closedItem("TZ-102", 3, 3),
		closedItem("TZ-103", 3, 4),
		closedItem("TZ-104", 4, 5),
		closedItem("TZ-301", 60, 12),
	].map((item) => Object.freeze(item));

const percentiles: IPercentileValue[] = [
	{ percentile: 50, value: 3 },
	{ percentile: 85, value: 4 },
	{ percentile: 95, value: 5 },
];

const showChart = () =>
	render(
		<QueryClientProvider client={queryClient}>
			<WidgetShell
				widgetKey={WIDGET_KEY}
				title="Cycle Time scatterplot"
				status="ready"
				hasContentToDim
			>
				<CycleTimeScatterPlotChart
					ownerKind="team"
					percentileValues={percentiles}
					cycleTimeDataPoints={closedItems()}
					serviceLevelExpectation={{ percentile: 85, value: 4 }}
					onFetchCycleTimeOutliers={backendJudgingAbove(30, closedItems())}
				/>
			</WidgetShell>
		</QueryClientProvider>,
	);

const header = () => screen.getByTestId(`widget-shell-header-${WIDGET_KEY}`);

const outlierChips = () =>
	within(header()).queryAllByRole("button", { name: /outlier/i });

const hideOutliersSwitch = () =>
	screen.getByRole("switch", { name: "Hide outliers" });

const drawnHeights = (): number[] =>
	(
		JSON.parse(screen.getByTestId("drawn-chart").dataset.series ?? "[]")[0]
			.data as { y: number }[]
	).map((dot) => dot.y);

const axisTop = (): number =>
	JSON.parse(screen.getByTestId("drawn-chart").dataset.yaxis ?? "[]")[0].max;

describe("The reader can switch outlier handling off to see every closed Work Item", () => {
	beforeEach(() => {
		reportUsage.mockClear();
		aFreshQueryClient();
	});

	it("starts on, draws every item and drops the chip when switched off, and restores both when switched on again", async () => {
		showChart();
		await theVerdictHasLanded();

		expect(
			hideOutliersSwitch(),
			"Hide outliers must be on when the chart opens; make it a labelled switch whose state starts true.",
		).toBeChecked();
		expect(drawnHeights()).toEqual([2, 3, 3, 4]);
		expect(outlierChips()).toHaveLength(1);
		expect(outlierChips()[0]).toHaveTextContent("1 outlier");
		const shorterAxis = axisTop();
		expect(shorterAxis).toBeLessThan(60);

		fireEvent.click(hideOutliersSwitch());

		expect(
			drawnHeights(),
			"With Hide outliers off every closed Work Item must be drawn, so the reader can see them all.",
		).toEqual([2, 3, 3, 4, 60]);
		expect(axisTop()).toBeGreaterThanOrEqual(60);
		expect(hideOutliersSwitch()).not.toBeChecked();
		expect(outlierChips()).toHaveLength(0);
		expect(reportUsage).toHaveBeenCalledTimes(1);
		expect(reportUsage).toHaveBeenCalledWith({
			name: "OutlierHandlingSwitchedOff",
		});

		fireEvent.click(hideOutliersSwitch());

		expect(hideOutliersSwitch()).toBeChecked();
		expect(drawnHeights()).toEqual([2, 3, 3, 4]);
		expect(axisTop()).toBe(shorterAxis);
		expect(outlierChips()).toHaveLength(1);
		expect(outlierChips()[0]).toHaveTextContent("1 outlier");
		expect(reportUsage).toHaveBeenCalledTimes(1);
	});

	it("starts on again when the chart is opened anew after switching it off", async () => {
		const first = showChart();
		await theVerdictHasLanded();
		fireEvent.click(hideOutliersSwitch());
		expect(hideOutliersSwitch()).not.toBeChecked();
		first.unmount();

		showChart();
		await theVerdictHasLanded();

		expect(
			hideOutliersSwitch(),
			"The choice must not be remembered: every mount starts with Hide outliers on, held in local state only.",
		).toBeChecked();
		expect(drawnHeights()).toEqual([2, 3, 3, 4]);
		expect(outlierChips()).toHaveLength(1);
	});
});
