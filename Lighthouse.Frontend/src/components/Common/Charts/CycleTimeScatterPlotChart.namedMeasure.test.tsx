import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, fireEvent, render, screen, within } from "@testing-library/react";
import type React from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IWorkItem } from "../../../models/WorkItem";
import WidgetShell from "../../../pages/Common/MetricsView/WidgetShell";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { testTheme } from "../../../tests/testTheme";
import { cellText, headerText } from "../../../tests/WorkItemsDialogTestKit";
import { formatLocalDate } from "../../../utils/date/localDate";
import type { WorkItemsDialogProps } from "../WorkItemsDialog/WorkItemsDialog";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return { ...actual, useTheme: () => testTheme };
});

vi.mock("../../../services/UsageData/usageDataReporter", async (original) => ({
	...(await original<
		typeof import("../../../services/UsageData/usageDataReporter")
	>()),
	useUsageDataReporter: () => vi.fn(),
}));

const theList = vi.hoisted(() => ({ isReal: false }));

// The list a dot opens shows its title and the Work Items it was handed, unless a test asks for the
// real list to read its columns.
vi.mock("../WorkItemsDialog/WorkItemsDialog", async (original) => {
	const RealList = (
		await original<typeof import("../WorkItemsDialog/WorkItemsDialog")>()
	).default;
	return {
		default: (props: WorkItemsDialogProps) => {
			if (theList.isReal) {
				return <RealList {...props} />;
			}
			return props.open ? (
				<section aria-label="Opened Work Items">
					<h2>{props.title}</h2>
					<ul>
						{props.items.map((item) => (
							<li key={item.id}>{item.name}</li>
						))}
					</ul>
				</section>
			) : null;
		},
	};
});

vi.mock("./TimeBlackoutOverlay", () => ({ default: () => null }));

// MUI X hides the chart's SVG from jsdom, so the plot is drawn as a list: one entry per dot, at the
// date and height the chart placed it, holding the chart's own clickable marker.
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	const { createContext, useContext } = await import("react");
	type Point = { x: number; y: number };
	const Plotted = createContext<Point[]>([]);
	return {
		...actual,
		ChartsContainer: ({
			xAxis,
			series,
			children,
		}: {
			xAxis?: Array<{ label?: string }>;
			series?: Array<{ data?: Point[] }>;
			children: React.ReactNode;
		}) => (
			<Plotted value={series?.[0]?.data ?? []}>
				<p data-testid="x-axis-label">{xAxis?.[0]?.label}</p>
				{children}
			</Plotted>
		),
		ScatterPlot: ({
			slots,
		}: {
			slots: {
				marker: React.ComponentType<{
					x: number;
					y: number;
					dataIndex: number;
				}>;
			};
		}) => {
			const points = useContext(Plotted);
			const Marker = slots.marker;
			return (
				<ul aria-label="Plotted dots">
					{points.map((point, index) => (
						<li key={`${point.x}-${point.y}`} data-x={point.x} data-y={point.y}>
							<svg>
								<title>Plotted dot</title>
								<Marker x={0} y={0} dataIndex={index} />
							</svg>
						</li>
					))}
				</ul>
			);
		},
		ChartsXAxis: () => null,
		ChartsYAxis: () => null,
		ChartsTooltip: () => null,
		ChartsReferenceLine: () => null,
		useDrawingArea: () => ({ left: 0, top: 0, width: 400, height: 200 }),
	};
});

const WIDGET_KEY = "cycleTimeScatter";
const COULD_NOT_LOAD =
	"This chart couldn't be loaded. Change the dates or reload to try again.";

const readyForRelease: INamedCycleTimeDefinition = {
	id: 7,
	name: "Analysis to Ready for Release",
	endState: "Ready for Release",
};
const inProduction: INamedCycleTimeDefinition = {
	id: 8,
	name: "Analysis to In Production",
	endState: "In Production",
};

const november = (day: number, hour = 14) => new Date(2026, 10, day, hour, 30);

interface ItemFacts {
	id: number;
	name: string;
	state: string;
	closedOn: Date | null;
	cycleTime: number;
	named: Array<{ definitionId: number; days: number; endDate: Date }>;
}

const workItem = (facts: ItemFacts): IWorkItem => ({
	id: facts.id,
	referenceId: facts.name,
	name: facts.name,
	url: null,
	state: facts.state,
	stateCategory: facts.closedOn ? "Done" : "Doing",
	type: "Story",
	startedDate: new Date(2026, 9, 20),
	// The server sends no closed date for an item that is not closed.
	closedDate: facts.closedOn as Date,
	cycleTime: facts.cycleTime,
	namedCycleTimes: facts.named,
	workItemAge: facts.cycleTime,
	parentWorkItemReference: "",
	isBlocked: false,
});

// Closed in the range: A reached Ready for Release on 5 Nov, C already on 28 Oct.
const closedItemA = workItem({
	id: 1,
	name: "A",
	state: "Done",
	closedOn: november(20),
	cycleTime: 15,
	named: [{ definitionId: 7, days: 6, endDate: november(5) }],
});
const closedItemC = workItem({
	id: 3,
	name: "C",
	state: "Done",
	closedOn: november(3),
	cycleTime: 9,
	named: [{ definitionId: 7, days: 8, endDate: new Date(2026, 9, 28, 14) }],
});
// Reached Ready for Release on 12 Nov and is still there.
const waitingItemB = workItem({
	id: 2,
	name: "B",
	state: "Ready for Release",
	closedOn: null,
	cycleTime: 0,
	named: [{ definitionId: 7, days: 4, endDate: november(12) }],
});
const releasedItemD = workItem({
	id: 4,
	name: "D",
	state: "In Production",
	closedOn: null,
	cycleTime: 0,
	named: [{ definitionId: 8, days: 12, endDate: november(18) }],
});

type NamedDataFetch = (definitionId: number) => Promise<IWorkItem[]>;

const theMeasuresOwnItems: NamedDataFetch = (definitionId) =>
	Promise.resolve(
		definitionId === readyForRelease.id
			? [closedItemA, waitingItemB]
			: [releasedItemD],
	);

const theChart = (fetchNamedData: NamedDataFetch) => {
	const client = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	return (
		<QueryClientProvider client={client}>
			<WidgetShell
				widgetKey={WIDGET_KEY}
				title="Cycle Time scatterplot"
				status="ready"
				hasContentToDim
			>
				<CycleTimeScatterPlotChart
					ownerKind="team"
					percentileValues={[]}
					cycleTimeDataPoints={[closedItemA, closedItemC]}
					serviceLevelExpectation={null}
					namedCycleTimeDefinitions={[readyForRelease, inProduction]}
					onFetchNamedCycleTimePercentiles={() => Promise.resolve([])}
					onFetchNamedCycleTimeData={fetchNamedData}
				/>
			</WidgetShell>
		</QueryClientProvider>
	);
};

const showTheChart = (fetchNamedData: NamedDataFetch) => {
	render(theChart(fetchNamedData));
};

const showTheChartWithItsRealList = (fetchNamedData: NamedDataFetch) => {
	theList.isReal = true;
	render(
		<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
			{theChart(fetchNamedData)}
		</ApiServiceContext.Provider>,
	);
};

const chooseMeasure = async (name: string) => {
	fireEvent.mouseDown(screen.getByRole("combobox", { name: "Cycle Time" }));
	fireEvent.click(await screen.findByRole("option", { name }));
};

const settle = () =>
	act(() => new Promise((resolve) => setTimeout(resolve, 0)));

const frame = () => screen.getByTestId(`widget-shell-${WIDGET_KEY}`);

// Hidden dots count too: the could-not-load look keeps the chart mounted out of sight.
const dots = () =>
	screen
		.queryAllByRole("listitem", { hidden: true })
		.filter((entry) => entry.dataset.x !== undefined);

/** Each dot as "<day it sits on>: <days high>", in date order. */
const plottedDots = () =>
	dots()
		.map((dot) => ({
			day: formatLocalDate(new Date(Number(dot.dataset.x))),
			days: Number(dot.dataset.y),
		}))
		.sort((a, b) => a.day.localeCompare(b.day))
		.map(({ day, days }) => `${day}: ${days}`);

const xAxisLabel = () => screen.getByTestId("x-axis-label").textContent;

const openTheDotOn = (day: string) => {
	const dot = dots().find(
		(entry) => formatLocalDate(new Date(Number(entry.dataset.x))) === day,
	);
	if (!dot) {
		throw new Error(
			`No dot sits on ${day}; the plotted dots are ${plottedDots().join(", ")}. ` +
				"A dot must sit on the day its Work Item reached the measure's end.",
		);
	}
	fireEvent.click(within(dot).getByRole("button"));
};

const openedListTitle = () =>
	within(screen.getByRole("region", { name: "Opened Work Items" })).getByRole(
		"heading",
	).textContent;

const expectDots = async (expected: string[], why: string) => {
	await vi.waitFor(() => {
		expect(plottedDots(), why).toEqual(expected);
	});
};

describe("Cycle Time scatterplot with a named Cycle Time", () => {
	it("keeps the default Cycle Time on the closed dates, without asking for a named measure's items", async () => {
		const fetchNamedData = vi.fn(theMeasuresOwnItems);
		showTheChart(fetchNamedData);
		await settle();

		expect(
			plottedDots(),
			"Default Cycle Time must still place each closed Work Item on its closed date at its Cycle Time; the fix may not change the default view.",
		).toEqual(["2026-11-03: 9", "2026-11-20: 15"]);
		expect(xAxisLabel()).toBe("Date");
		expect(
			fetchNamedData,
			"No named measure is selected, so nothing may be fetched for one; the default view needs no extra request.",
		).not.toHaveBeenCalled();

		openTheDotOn("2026-11-20");
		expect(openedListTitle()).toBe(
			`Work Items closed on ${november(20).toLocaleDateString()} with Cycle Time of 15 days`,
		);
	});

	it("shows the measure's own Work Items on the day each reached its end state", async () => {
		const fetchNamedData = vi.fn(theMeasuresOwnItems);
		showTheChart(fetchNamedData);

		await chooseMeasure("Analysis to Ready for Release");

		await expectDots(
			["2026-11-05: 6", "2026-11-12: 4"],
			"With Analysis to Ready for Release selected the chart must plot the Work Items fetched for that measure (A and B, not C, which reached it before the range) on the day each reached Ready for Release, at the named days. Plot the fetched population on its end dates, not the default list on its closed dates.",
		);
		expect(fetchNamedData).toHaveBeenCalledWith(readyForRelease.id);
		expect(
			xAxisLabel(),
			"The x-axis must say which date a dot sits on, naming the measure's end state.",
		).toBe("Date reached Ready for Release");

		openTheDotOn("2026-11-05");
		expect(openedListTitle()).toBe(
			`Work Items that reached Ready for Release on ${november(5).toLocaleDateString()} with Cycle Time of 6 days`,
		);
		expect(
			within(screen.getByRole("region", { name: "Opened Work Items" }))
				.getAllByRole("listitem")
				.map((row) => row.textContent),
		).toEqual(["A"]);
	});

	it("dims the previous dots under a spinner while the measure's Work Items are on their way", async () => {
		showTheChart(() => new Promise<IWorkItem[]>(() => undefined));

		await chooseMeasure("Analysis to Ready for Release");
		await settle();

		expect(
			frame(),
			"While the named measure's Work Items are loading, the frame must say it is loading.",
		).toHaveAttribute("data-widget-status", "loading");
		expect(
			screen.getByTestId(`widget-shell-body-${WIDGET_KEY}`),
			"The previous dots must stay on screen dimmed, so the reader sees they are not the current answer.",
		).toHaveStyle({ opacity: "0.4" });
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(plottedDots()).toEqual(["2026-11-03: 9", "2026-11-20: 15"]);
		for (const dot of dots()) {
			expect(dot).toBeVisible();
		}
	});

	it("says the chart could not be loaded, and shows no dots as current, when the measure's Work Items cannot be fetched", async () => {
		showTheChart(() => Promise.reject(new Error("server unavailable")));

		await chooseMeasure("Analysis to Ready for Release");
		await settle();

		expect(
			frame(),
			"A failed fetch of the named measure's Work Items must put the frame in its could-not-load look.",
		).toHaveAttribute("data-widget-status", "error");
		expect(screen.getByText(COULD_NOT_LOAD)).toBeVisible();
		for (const dot of dots()) {
			expect(
				dot,
				"After a failed fetch no dot may be shown as if it were the named measure's answer.",
			).not.toBeVisible();
		}
	});

	it("never lets a late answer for an earlier measure replace the selected one's", async () => {
		let answerForReadyForRelease: (items: IWorkItem[]) => void = () =>
			undefined;
		showTheChart((definitionId) =>
			definitionId === readyForRelease.id
				? new Promise<IWorkItem[]>((resolve) => {
						answerForReadyForRelease = resolve;
					})
				: theMeasuresOwnItems(definitionId),
		);

		await chooseMeasure("Analysis to Ready for Release");
		await chooseMeasure("Analysis to In Production");
		await expectDots(
			["2026-11-18: 12"],
			"The measure selected last must show its own Work Items.",
		);

		act(() => {
			answerForReadyForRelease([closedItemA, waitingItemB]);
		});
		await settle();

		expect(
			plottedDots(),
			"An answer for a measure the reader has left must never land on the chart.",
		).toEqual(["2026-11-18: 12"]);
		expect(xAxisLabel()).toBe("Date reached In Production");
	});
});

describe("the list a named Cycle Time's dot opens", () => {
	beforeEach(() => {
		theList.isReal = false;
	});
	afterEach(() => {
		theList.isReal = false;
	});

	it("shows each listed Work Item's Cycle Time for the selected measure, also for one that is not closed", async () => {
		showTheChartWithItsRealList(theMeasuresOwnItems);

		await chooseMeasure("Analysis to Ready for Release");
		await expectDots(
			["2026-11-05: 6", "2026-11-12: 4"],
			"The measure's own Work Items must be plotted before a dot can be opened.",
		);
		openTheDotOn("2026-11-12");

		await vi.waitFor(() => {
			expect(
				headerText("cycleTime"),
				"The list a named measure's dot opens must head its Cycle Time column with the measure, so the reader knows which Cycle Time the numbers are. Hand the list the selected measure as its Cycle Time scope.",
			).toBe("Analysis to Ready for Release");
		});
		expect(
			cellText("cycleTime", "B"),
			"B reached Ready for Release 4 days after entering Analysis and is not closed; the list under a title of 'Cycle Time of 4 days' must show those 4 days, not the empty default Cycle Time of an open Work Item.",
		).toBe("4");
	});
});
