import { fireEvent, render, screen } from "@testing-library/react";
import type { ReactElement } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { parseDeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../tests/testTheme";
import { appColors } from "../../../utils/theme/colors";

const chartsContainerMock = vi.hoisted(() =>
	vi.fn(({ children }) => (
		<svg data-testid="mock-charts-container">
			<title>Test</title>
			{children}
		</svg>
	)),
);

// BarPlot, not ChartsContainer, is what routes slots down to BarElement — the container's `slots` is
// material-only and MUI ignores a `bar` key on it. Asserting the hatch on the container would pass
// against a chart that never wires the renderer at all.
const barPlotMock = vi.hoisted(() =>
	vi.fn((_props: { slots?: { bar?: unknown } }) => (
		<g data-testid="mock-bar-plot" />
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
	BarPlot: barPlotMock,
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
	valueFormatter?: (
		value: number | null,
		context: { dataIndex: number },
	) => string | null;
}

interface AxisEntry {
	id?: string;
	dataKey?: string;
	scaleType?: string;
	position?: string;
	label?: string;
	min?: number;
}

type DatasetRow = Record<string, string | number>;

interface BarOwnerState {
	seriesId: string;
	dataIndex: number;
	color?: string;
}

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

/** Same, plus the estimate flag slice 03 draws. */
const flaggedEpic = (
	referenceId: string,
	totalItems: number,
	isUsingDefaultSize: boolean,
) => ({
	...sizedEpic(referenceId, totalItems),
	isUsingDefaultSize,
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

	it("picks the count line out against the bars behind it", () => {
		// Review 2026-08-02: the line was theme.palette.primary.main and vanished into the stack, whose
		// segments come from the same green-teal ramp. Orange is the one hue the epic palette never uses.
		render(<DeliveryEpicSizeChart history={getMockHistory()} />);

		expect(getCountSeries()?.color).toBe(appColors.status.warning);
	});

	it("scales the count from zero so a flat line is not read as a cliff", () => {
		// Review 2026-08-02: the axis started at the lowest count in the window (3), so a delivery that
		// went 3 -> 6 looked like it had started from nothing.
		render(<DeliveryEpicSizeChart history={getMockHistory()} />);

		expect(
			getLatestChartProps()?.yAxis?.find((axis) => axis.id === COUNT_AXIS_ID)
				?.min,
		).toBe(0);
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

	it("declares no items scale when there is nothing to size against it", () => {
		// DISTILL's resolved open question from slice 01: an axis with no series is a rendering risk for
		// no gain, and a history recorded entirely before slice 02 has no bars at all.
		render(<DeliveryEpicSizeChart history={getMockHistory([7])} />);

		expect(
			getLatestChartProps()?.yAxis?.some((axis) => axis.id === ITEMS_AXIS_ID),
		).toBe(false);
		expect(screen.queryByTestId(`mock-y-axis-${ITEMS_AXIS_ID}`)).toBeNull();
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

	it("leaves an epic out of the day's tooltip when it was not in the delivery yet", () => {
		// Review 2026-08-02: every epic in the window got a tooltip row on every day, most of them blank.
		// MUI's ChartsAxisTooltipContent drops a row whose formattedValue is null, so the formatter has to
		// return null rather than the default empty string.
		render(
			<DeliveryEpicSizeChart
				history={historyOf([
					[sizedEpic("EPIC-A", 8)],
					[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)],
				])}
			/>,
		);

		const absentEpic = barSeries().find((entry) => entry.id === "EPIC-B");
		expect(absentEpic?.valueFormatter?.(null, { dataIndex: 0 })).toBeNull();
		expect(absentEpic?.valueFormatter?.(3, { dataIndex: 1 })).toContain("3");
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

// Epic #5585 slice 03 (US-03). A segment sized by the portfolio default renders hatched, so the day an
// epic stopped being a guess is visible without hovering anything.
//
// These assert the SLOT CONTRACT — the renderer the chart hands to MUI-X, exercised directly with an
// ownerState — rather than the series topology, so they hold under ADR-119 as originally written and
// under its 2026-08-02 revision (one series per epic, renderer keyed on seriesId + dataIndex).
describe("DeliveryEpicSizeChart estimate hatching", () => {
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

	const getBarSlot = () => {
		const calls = barPlotMock.mock.calls;
		const props = calls[calls.length - 1]?.[0];
		return props?.slots?.bar as
			| ((ownerState: BarOwnerState) => ReactElement | null)
			| undefined;
	};

	const renderBar = (seriesId: string, dataIndex: number) => {
		const slot = getBarSlot();
		expect(slot, "BarPlot was given no bar slot renderer").toBeTypeOf(
			"function",
		);
		const { container } = render(
			<svg aria-hidden="true">{slot?.({ seriesId, dataIndex })}</svg>,
		);
		return container.querySelector("rect, path");
	};

	const fillOf = (seriesId: string, dataIndex: number) =>
		renderBar(seriesId, dataIndex)?.getAttribute("fill") ?? "";

	it("hands BarPlot its own bar renderer so a segment can carry a pattern (ADR-119)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([[flaggedEpic("EPIC-A", 8, true)]])}
			/>,
		);

		expect(getBarSlot()).toBeTypeOf("function");
	});

	it("hatches a segment whose size is the portfolio default (AC-3.2)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([[flaggedEpic("EPIC-A", 8, true)]])}
			/>,
		);

		expect(fillOf("EPIC-A", 0)).toMatch(/^url\(#/);
	});

	it("leaves a broken-down epic solid, in its own colour (AC-3.2)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([[flaggedEpic("EPIC-A", 8, false)]])}
			/>,
		);

		// Asserting the colour, not merely "not a pattern": a renderer that dropped the fill entirely
		// would satisfy the weaker check while drawing an invisible bar.
		const slot = getBarSlot();
		const { container } = render(
			<svg aria-hidden="true">
				{slot?.({ seriesId: "EPIC-A", dataIndex: 0, color: "#123456" })}
			</svg>,
		);

		expect(container.querySelector("rect, path")?.getAttribute("fill")).toBe(
			"#123456",
		);
	});

	it("treats an epic with no flag as broken down, never as a guess (AC-3.5)", () => {
		// Absence is not truth. Every snapshot recorded before slice 02 has no flag at all, and if
		// absence read as "estimated" the whole of that history would render hatched.
		render(
			<DeliveryEpicSizeChart history={historyOf([[sizedEpic("EPIC-A", 8)]])} />,
		);

		expect(fillOf("EPIC-A", 0)).not.toMatch(/^url\(#/);
	});

	it("shows the day an epic stopped being a guess (AC-3.4)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([
					[flaggedEpic("EPIC-A", 8, true)],
					[flaggedEpic("EPIC-A", 8, true)],
					[flaggedEpic("EPIC-A", 6, false)],
				])}
			/>,
		);

		expect(fillOf("EPIC-A", 0)).toMatch(/^url\(#/);
		expect(fillOf("EPIC-A", 1)).toMatch(/^url\(#/);
		expect(fillOf("EPIC-A", 2)).not.toMatch(/^url\(#/);
	});

	it("says in the tooltip that a hatched size is a default (AC-3.3)", () => {
		render(
			<DeliveryEpicSizeChart
				history={historyOf([
					[flaggedEpic("EPIC-A", 8, true), flaggedEpic("EPIC-B", 3, false)],
				])}
			/>,
		);

		const seriesFor = (id: string) =>
			getLatestChartProps()?.series?.find((entry) => entry.id === id);

		// Wording shortened on review 2026-08-02 — AC-3.3's suggested sentence overran the tooltip row.
		expect(seriesFor("EPIC-A")?.valueFormatter?.(8, { dataIndex: 0 })).toMatch(
			/^8 \(estimated\)$/,
		);
		expect(seriesFor("EPIC-B")?.valueFormatter?.(3, { dataIndex: 0 })).toBe(
			"3",
		);
	});

	it("gives each chart on the page its own pattern so two deliveries do not collide (AC-3.6)", () => {
		const history = historyOf([[flaggedEpic("EPIC-A", 8, true)]]);

		const { container } = render(
			<>
				<DeliveryEpicSizeChart history={history} />
				<DeliveryEpicSizeChart history={history} />
			</>,
		);

		const ids = [...container.querySelectorAll("pattern")].map((node) =>
			node.getAttribute("id"),
		);
		expect(ids).toHaveLength(2);
		expect(new Set(ids).size).toBe(2);
	});
});

// Epic #5585 slice 04 (US-04). A fifteen-epic delivery renders a fifteen-segment stack per day —
// legible as a total, useless for following one epic. The legend filters the bars, and it is collapsed
// by default because filtering is a special-case action and the card already runs tall.
describe("DeliveryEpicSizeChart legend filtering", () => {
	beforeEach(() => {
		chartsContainerMock.mockClear();
		barPlotMock.mockClear();
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

	const twoEpics = () =>
		historyOf([[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)]]);

	const barSeriesIds = () =>
		(getLatestChartProps()?.series ?? [])
			.filter((entry) => entry.type === "bar")
			.map((entry) => entry.id);

	const openLegend = () =>
		fireEvent.click(screen.getByRole("button", { name: /legend/i }));

	const clickEntry = (label: string) =>
		fireEvent.click(screen.getByRole("button", { name: label }));

	it("keeps the legend out of the way until the forecaster wants it", () => {
		render(<DeliveryEpicSizeChart history={twoEpics()} />);

		expect(screen.queryByRole("button", { name: "Epic EPIC-A" })).toBeNull();
	});

	it("lists every epic in the window once, including one that has left (AC-4.1)", () => {
		// D7: an epic that left the delivery still had days in the window, so it stays selectable.
		render(
			<DeliveryEpicSizeChart
				history={historyOf([
					[sizedEpic("EPIC-A", 8), sizedEpic("EPIC-B", 3)],
					[sizedEpic("EPIC-A", 9)],
				])}
			/>,
		);

		openLegend();

		expect(screen.getByRole("button", { name: "Epic EPIC-A" })).toBeVisible();
		expect(screen.getByRole("button", { name: "Epic EPIC-B" })).toBeVisible();
	});

	it("leaves only the chosen epic's bars when one is picked (AC-4.2)", () => {
		render(<DeliveryEpicSizeChart history={twoEpics()} />);

		openLegend();
		clickEntry("Epic EPIC-B");

		expect(barSeriesIds()).toEqual(["EPIC-A"]);
	});

	it("brings a deselected epic back on a second click (AC-4.3)", () => {
		render(<DeliveryEpicSizeChart history={twoEpics()} />);

		openLegend();
		clickEntry("Epic EPIC-B");
		clickEntry("Epic EPIC-B");

		expect(barSeriesIds()).toEqual(["EPIC-A", "EPIC-B"]);
	});

	it("clears the whole filter in one action (AC-4.4)", () => {
		render(<DeliveryEpicSizeChart history={twoEpics()} />);

		openLegend();
		clickEntry("Epic EPIC-A");
		clickEntry("Epic EPIC-B");
		fireEvent.click(screen.getByRole("button", { name: /show all/i }));

		expect(barSeriesIds()).toEqual(["EPIC-A", "EPIC-B"]);
	});

	it("never filters the count line, which is a delivery-level fact (AC-4.5)", () => {
		// D8: the count answers "how many epics were in the delivery that day". Filtering it to a
		// subset would make it read as a different number for the same day.
		render(<DeliveryEpicSizeChart history={twoEpics()} />);

		const before = getCountSeries()?.dataKey;
		openLegend();
		clickEntry("Epic EPIC-B");

		expect(getCountSeries()?.dataKey).toBe(before);
		expect(getCountSeries()).toBeDefined();
	});

	it("filters one delivery's chart without touching another's (AC-4.6)", () => {
		const history = twoEpics();
		render(
			<>
				<DeliveryEpicSizeChart history={history} featuresTerm="Alpha" />
				<DeliveryEpicSizeChart history={history} featuresTerm="Beta" />
			</>,
		);

		const [firstLegend] = screen.getAllByRole("button", { name: /legend/i });
		fireEvent.click(firstLegend);
		fireEvent.click(screen.getByRole("button", { name: "Epic EPIC-B" }));

		// The second chart re-rendered from its own untouched state, so it still carries both bars.
		expect(barSeriesIds()).toEqual(["EPIC-A", "EPIC-B"]);
	});
});
