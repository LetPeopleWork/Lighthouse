import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { parseDeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../tests/testTheme";

const chartsContainerMock = vi.hoisted(() =>
	vi.fn(({ children }) => (
		<svg data-testid="mock-charts-container">
			<title>Test</title>
			{children}
		</svg>
	)),
);

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("@mui/x-charts", () => ({
	ChartsContainer: chartsContainerMock,
	BarPlot: () => <g data-testid="mock-bar-plot" />,
	LinePlot: () => <g data-testid="mock-line-plot" />,
	MarkPlot: () => <g data-testid="mock-mark-plot" />,
	ChartsXAxis: () => <g data-testid="mock-x-axis" />,
	ChartsYAxis: ({ axisId }: { axisId?: string }) => (
		<g data-testid={`mock-y-axis-${axisId ?? "default"}`} />
	),
	ChartsTooltip: () => <g data-testid="mock-tooltip" />,
	ChartsLegend: () => <g data-testid="mock-legend" />,
}));

import DeliveryEpicSizeChart from "./DeliveryEpicSizeChart";

const EPIC_COUNT_SERIES_ID = "epic-count";
const EPIC_COUNT_DATA_KEY = "epicCount";
const COUNT_AXIS_ID = "count";
const ITEMS_AXIS_ID = "items";

interface SeriesEntry {
	id?: string;
	type?: string;
	dataKey?: string;
	label?: string;
	yAxisId?: string;
	color?: string;
	stack?: string;
}

interface AxisEntry {
	id?: string;
	dataKey?: string;
	scaleType?: string;
	position?: string;
	label?: string;
}

type DatasetRow = Record<string, string | number>;

const getLatestChartProps = () => {
	const lastCall =
		chartsContainerMock.mock.calls[chartsContainerMock.mock.calls.length - 1];
	return lastCall?.[0] as
		| {
				dataset?: DatasetRow[];
				series?: SeriesEntry[];
				xAxis?: AxisEntry[];
				yAxis?: AxisEntry[];
		  }
		| undefined;
};

const getCountSeries = (): SeriesEntry | undefined =>
	getLatestChartProps()?.series?.find(
		(entry) => entry.id === EPIC_COUNT_SERIES_ID,
	);

const getCountValues = (): Array<string | number | undefined> =>
	(getLatestChartProps()?.dataset ?? []).map((row) => row[EPIC_COUNT_DATA_KEY]);

/** Breakdown entries in the shape recorded BEFORE this feature — the four original fields only. */
const legacyBreakdown = (count: number) =>
	Array.from({ length: count }, (_, index) => ({
		referenceId: `EPIC-${index + 1}`,
		name: `Epic ${index + 1}`,
		completion: 0,
		likelihood: 50,
	}));

/** One breakdown entry carrying the size Epic #5585 slice 02 records. */
const sizedEpic = (referenceId: string, totalItems: number) => ({
	referenceId,
	name: `Epic ${referenceId}`,
	completion: 0,
	likelihood: 50,
	totalItems,
});

const point = (date: string, epicCount: number) => ({
	date,
	totalWork: 20,
	doneWork: 4,
	remainingWork: 16,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: 70,
	whenDistribution: null,
	featureBreakdown: legacyBreakdown(epicCount),
});

const DATES = [
	"2026-06-01T00:00:00Z",
	"2026-06-02T00:00:00Z",
	"2026-06-03T00:00:00Z",
];

const getMockHistory = (counts: number[] = [7, 7, 9]) =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-10T00:00:00Z",
		firstSnapshotDate: DATES[0],
		points: counts.map((count, index) => point(DATES[index], count)),
	});

const getEmptyHistory = () =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-10T00:00:00Z",
		firstSnapshotDate: null,
		points: [],
	});

describe("DeliveryEpicSizeChart count line", () => {
	beforeEach(() => {
		chartsContainerMock.mockClear();
	});

	it("plots one point per recorded day whose value is that day's epic count (AC-1.1)", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory([7, 7, 9])} />);

		expect(getCountValues()).toEqual([7, 7, 9]);
	});

	it("labels each plotted point with the day it was recorded (AC-1.1)", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory([7, 7, 9])} />);

		const props = getLatestChartProps();
		const labelKey = props?.xAxis?.[0]?.dataKey;
		expect(labelKey).toBeTruthy();
		expect(
			(props?.dataset ?? []).map((row) => row[labelKey as string]),
		).toEqual(DATES.map((date) => new Date(date).toLocaleDateString()));
	});

	it("spaces the days evenly rather than by calendar distance (ADR-122)", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory()} />);

		expect(getLatestChartProps()?.xAxis?.[0]?.scaleType).toBe("band");
	});

	it("counts a day's epics from the breakdown recorded on that day (AC-1.6)", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory([4])} />);

		expect(getCountValues()).toEqual([4]);
		expect(chartsContainerMock).toHaveBeenCalled();
	});

	it("draws the count against its own right-hand scale so sizes can share the chart (ADR-122)", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory()} />);

		const countAxis = getLatestChartProps()?.yAxis?.find(
			(axis) => axis.id === COUNT_AXIS_ID,
		);
		expect(countAxis?.position).toBe("right");
		expect(getCountSeries()?.yAxisId).toBe(COUNT_AXIS_ID);
	});

	it("draws the count as a line, not as another bar (ADR-122)", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory()} />);

		expect(getCountSeries()?.type).toBe("line");
		expect(getCountSeries()?.label).toBeTruthy();
	});

	it("tells the forecaster the chart builds forward when nothing is recorded yet (AC-1.2)", () => {
		render(<DeliveryEpicSizeChart history={getEmptyHistory()} />);

		expect(
			screen.getByText(
				/builds forward from today — no snapshots recorded yet/i,
			),
		).toBeInTheDocument();
		expect(chartsContainerMock).not.toHaveBeenCalled();
	});

	it("names the chart after whatever this instance calls its epics (AC-1.5)", () => {
		render(
			<DeliveryEpicSizeChart
				history={getMockHistory()}
				featuresTerm="Initiatives"
			/>,
		);

		expect(screen.getByRole("heading")).toHaveTextContent(/Initiative/);
		expect(screen.getByRole("heading")).not.toHaveTextContent(/Epic/);
	});

	it("still names itself when the caller supplies no term", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory()} />);

		expect(screen.getByRole("heading")).toHaveTextContent(/Epics over Time/);
	});

	it("offers the forecaster the detail behind a day on hover", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory()} />);

		expect(screen.getByTestId("mock-tooltip")).toBeInTheDocument();
	});
});

// Epic #5585 slice 02 (US-02). Each day's bar carries one segment per epic, sized by that epic's
// total child items, so a backlog jump is attributable to a named epic instead of to "scope".
describe("DeliveryEpicSizeChart size bars", () => {
	beforeEach(() => {
		chartsContainerMock.mockClear();
	});

	const historyOf = (days: Record<string, unknown>[][]) =>
		parseDeliveryMetricsHistory({
			deliveryDate: "2026-06-10T00:00:00Z",
			firstSnapshotDate: DATES[0],
			points: days.map((entries, index) => ({
				...point(DATES[index], 0),
				featureBreakdown: entries,
			})),
		});

	const barSeries = () =>
		(getLatestChartProps()?.series ?? []).filter(
			(entry) => entry.type === "bar",
		);

	const valuesFor = (referenceId: string) => {
		const series = barSeries().find((entry) => entry.id === referenceId);
		const key = series?.dataKey;
		return (getLatestChartProps()?.dataset ?? []).map((row) =>
			key === undefined ? undefined : row[key],
		);
	};

	it("gives every epic on a day its own segment, sized by its items (AC-2.5)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)]])}
			/>,
		);

		expect(barSeries()).toHaveLength(2);
		expect(valuesFor("EPIC-A")).toEqual([8]);
		expect(valuesFor("EPIC-B")).toEqual([3]);
	});

	it("stacks the day's epics into one bar rather than standing them side by side (AC-2.5)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)]])}
			/>,
		);

		const stacks = new Set(barSeries().map((entry) => entry.stack));
		expect(stacks.size).toBe(1);
		expect([...stacks][0]).toBeTruthy();
	});

	it("draws no bar for a day recorded before sizes were written (AC-2.5)", () => {
		render(<DeliveryEpicSizeChart history={getMockHistory([7])} />);

		expect(barSeries()).toHaveLength(0);
		expect(getCountSeries()).toBeDefined();
	});

	it("keeps an epic that left the delivery on the days it was there (AC-2.6)", () => {
		// D7: the segments stop, they do not disappear retroactively — that is how a scope cut stays
		// visible. The epic keeps its series, so it keeps its legend entry.
		render(
			<DeliveryEpicSizeChart
				history={historyOf([
					[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)],
					[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)],
					[sizedEpic("EPIC-A", 9)],
				])}
			/>,
		);

		expect(valuesFor("EPIC-A")).toEqual([8, 8, 9]);
		expect(valuesFor("EPIC-B")).toEqual([3, 3, null]);
	});

	it("sizes the bars on their own left-hand scale, apart from the count (ADR-122)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)]])}
			/>,
		);

		const itemsAxis = getLatestChartProps()?.yAxis?.find(
			(axis) => axis.id === ITEMS_AXIS_ID,
		);
		expect(itemsAxis?.position).toBe("left");
		expect(barSeries().every((entry) => entry.yAxisId === ITEMS_AXIS_ID)).toBe(
			true,
		);
	});

	it("orders the stack by epic so the bars do not reshuffle between days", () => {
		// Membership changes daily; without a pinned order the same epic lands in a different band on
		// consecutive days and the chart reads as noise.
		render(
			<DeliveryEpicSizeChart
				history={historyOf([
					[sizedEpic("EPIC-B", 3), sizedEpic("EPIC-A", 8)],
					[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)],
				])}
			/>,
		);

		expect(barSeries().map((entry) => entry.id)).toEqual(["EPIC-A", "EPIC-B"]);
	});
});
