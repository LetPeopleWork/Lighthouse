import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell, {
	COULD_NOT_LOAD_MESSAGE,
} from "../../../pages/Common/MetricsView/WidgetShell";
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
		ChartsReferenceLine: ({ label, y }: { label: string; y: number }) => (
			<div data-testid={`reference-line-${label}`} data-y={y}>
				{label}
			</div>
		),
	};
});

const WIDGET_KEY = "cycleScatter";

interface OutlierVerdict {
	readonly status: "Ready" | "NoLimits";
	readonly upperNaturalProcessLimit: number | null;
	readonly outlierWorkItemIds: number[];
}

type FetchOutliers = (definitionId: number | null) => Promise<OutlierVerdict>;

const closedItem = (id: number, cycleTime: number, day: number): IWorkItem => ({
	id,
	referenceId: `OE-${id}`,
	name: `Closed after ${cycleTime} days`,
	url: null,
	cycleTime,
	startedDate: new Date(2026, 1, 1),
	closedDate: new Date(2026, 2, day),
	workItemAge: cycleTime,
	state: "Done",
	stateCategory: "Done",
	type: "Story",
	parentWorkItemReference: "",
	isBlocked: false,
});

// Closed Work Items of 12, 30, 31 and 90 days; the backend judges a limit of 30 days.
const twelveThirtyThirtyOneNinety = (): IWorkItem[] => [
	closedItem(101, 12, 2),
	closedItem(102, 30, 3),
	closedItem(301, 31, 4),
	closedItem(302, 90, 5),
];

const aboveALimitOf30: OutlierVerdict = {
	status: "Ready",
	upperNaturalProcessLimit: 30,
	outlierWorkItemIds: [301, 302],
};

const answering = (verdict: OutlierVerdict) =>
	vi.fn<FetchOutliers>(() => Promise.resolve(verdict));

const neverAnswering = () =>
	vi.fn<FetchOutliers>(() => new Promise<OutlierVerdict>(() => {}));

const failing = () =>
	vi.fn<FetchOutliers>(() => Promise.reject(new Error("could not load")));

const percentiles = (p95: number | null): IPercentileValue[] =>
	p95 === null
		? []
		: [
				{ percentile: 50, value: 12 },
				{ percentile: 85, value: 28 },
				{ percentile: 95, value: p95 },
			];

const showChart = (
	items: IWorkItem[],
	fetchOutliers: FetchOutliers,
	p95: number | null = null,
	sleValue: number | null = null,
) => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	return render(
		<QueryClientProvider client={queryClient}>
			<WidgetShell
				widgetKey={WIDGET_KEY}
				title="Cycle Time scatterplot"
				status="ready"
			>
				<CycleTimeScatterPlotChart
					ownerKind="team"
					percentileValues={percentiles(p95)}
					cycleTimeDataPoints={items}
					serviceLevelExpectation={
						sleValue === null ? null : { percentile: 85, value: sleValue }
					}
					onFetchCycleTimeOutliers={fetchOutliers}
				/>
			</WidgetShell>
		</QueryClientProvider>,
	);
};

const frame = () => screen.getByTestId(`widget-shell-${WIDGET_KEY}`);

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

const theVerdictHasLanded = async () => {
	await screen.findByRole("button", { name: /^\d+ outliers? not drawn/ });
};

describe("Cycle Time scatterplot sets apart the Work Items above the upper natural process limit", () => {
	beforeEach(() => {
		reportUsage.mockClear();
	});

	it("sets apart the 31 and 90 day Work Items the backend judges above a limit of 30 days and stops the axis at the limit", async () => {
		const fetchOutliers = answering(aboveALimitOf30);
		showChart(twelveThirtyThirtyOneNinety(), fetchOutliers);

		await theVerdictHasLanded();

		expect(
			drawnHeights(),
			"WHAT: the drawn Work Items are not the ones the verdict leaves. WHY: the outliers must agree with the process behaviour chart. HOW: set apart the plotted items whose ids the verdict names.",
		).toEqual([12, 30]);
		expect(axisTop()).toBeCloseTo(30, 5);
		expect(outlierChip()).toHaveTextContent("2 outliers");
		expect(isDisabled(outlierChip())).toBe(false);
		expect(fetchOutliers).toHaveBeenCalledWith(null);

		fireEvent.click(outlierChip());

		expect(reportUsage).toHaveBeenCalledWith({ name: "OutlierListOpened" });
	});

	it("names the percentile and SLE lines that lie above a limit of 30 days", async () => {
		showChart(
			[closedItem(101, 12, 2), closedItem(102, 30, 3), closedItem(302, 90, 5)],
			answering({ ...aboveALimitOf30, outlierWorkItemIds: [302] }),
			33,
			35,
		);

		await theVerdictHasLanded();

		expect(screen.getByText("95%: 33 days")).toBeInTheDocument();
		expect(outlierChip()).toHaveAccessibleName(
			"1 outlier not drawn on the chart. Above the axis: 95% at 33 days, SLE at 35 days. Show the list.",
		);
	});

	it("sets nothing apart and shows no count while the verdict is on its way", async () => {
		showChart(twelveThirtyThirtyOneNinety(), neverAnswering());

		const chip = await within(header()).findByRole("button", {
			name: "Outliers, loading",
		});

		expect(isDisabled(chip)).toBe(true);
		expect(header()).not.toHaveTextContent(/\d/);
		expect(drawnHeights()).toEqual([12, 30, 31, 90]);
		expect(
			frame(),
			"WHAT: the frame does not say it is loading. WHY: a reader must not take a partial chart for the answer. HOW: report loading to the widget frame while the verdict is pending.",
		).toHaveAttribute("data-widget-status", "loading");
	});

	it("shows the could-not-load message when the verdict cannot be loaded", async () => {
		showChart(twelveThirtyThirtyOneNinety(), failing());

		expect(
			await screen.findByText(COULD_NOT_LOAD_MESSAGE),
			"WHAT: a failed verdict leaves the chart looking current. WHY: a chart without its outliers looks like every Work Item is typical. HOW: report failure to the widget frame when the verdict fails.",
		).toBeVisible();
		expect(frame()).toHaveAttribute("data-widget-status", "error");
		expect(header()).not.toHaveTextContent(/\d/);
	});
});
