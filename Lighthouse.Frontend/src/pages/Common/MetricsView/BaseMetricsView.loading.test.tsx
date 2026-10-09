import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	createHeldMetricsService,
	type HeldMetricsService,
	usualAnswer,
	windowOf,
} from "../../../tests/HeldMetricsService";
import {
	createMockApiServiceContext,
	createMockBlackoutPeriodService,
} from "../../../tests/MockApiServiceProvider";
import { BaseMetricsView } from "./BaseMetricsView";
import { type CategoryKey, getWidgetsForCategory } from "./categoryMetadata";
import type { DateWindowPreset } from "./dateWindow";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: true },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock("./DashboardHeader", () => ({
	default: ({
		startDate,
		onStartDateChange,
		presets,
		onSelectPreset,
		isCommitPending,
		stepDays,
		onStepWindow,
		onSelectCategory,
	}: {
		startDate: Date;
		onStartDateChange: (date: Date | null) => void;
		presets?: readonly DateWindowPreset[];
		onSelectPreset?: (days: number) => void;
		isCommitPending?: boolean;
		stepDays?: number;
		onStepWindow?: (direction: -1 | 1) => void;
		onSelectCategory: (key: CategoryKey) => void;
	}) => (
		<div>
			{presets?.map((preset) => (
				<button
					type="button"
					key={preset.days}
					onClick={() => onSelectPreset?.(preset.days)}
				>
					{preset.label}
				</button>
			))}
			<button type="button" onClick={() => onStepWindow?.(-1)}>
				{`Previous ${stepDays} days`}
			</button>
			<button
				type="button"
				onClick={() => {
					const earlier = new Date(startDate);
					earlier.setDate(earlier.getDate() - 30);
					onStartDateChange(earlier);
				}}
			>
				Pick a start date a month earlier
			</button>
			{(
				[
					"flow-overview",
					"flow-metrics",
					"predictability",
					"portfolio",
				] as const
			).map((category) => (
				<button
					type="button"
					key={category}
					onClick={() => onSelectCategory(category)}
				>
					{`Open ${category}`}
				</button>
			))}
			<span data-testid="step-waiting">{String(!!isCommitPending)}</span>
		</div>
	),
}));

vi.mock("./Dashboard", () => ({
	default: ({
		items,
	}: {
		items?: Array<{ id: string | number; node: ReactNode }>;
	}) => (
		<div>
			{items?.map((item) => (
				<div
					key={String(item.id)}
					data-testid={`dashboard-item-${String(item.id)}`}
				>
					{item.node}
				</div>
			))}
		</div>
	),
}));

vi.mock("../../../components/Common/WorkItemsDialog/WorkItemsDialog", () => ({
	default: () => null,
}));

vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: ({
		title,
		chartData,
		filterToggle,
	}: {
		title: string;
		chartData: RunChartData;
		filterToggle?: ReactNode;
	}) => (
		<div data-testid="run-chart">
			<span data-testid="run-chart-reads">{`${title}: ${chartData.total}`}</span>
			{filterToggle}
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/LineRunChart", () => ({
	default: () => <div data-testid="line-run-chart" />,
}));
vi.mock("../../../components/Common/Charts/BlockedItemsOverTimeChart", () => ({
	default: () => <div data-testid="blocked-over-time-chart" />,
}));
vi.mock("../../../components/Common/Charts/CycleTimeScatterPlotChart", () => ({
	default: () => <div data-testid="cycle-time-scatter-chart" />,
}));
vi.mock("../../../components/Common/Charts/CycleTimePercentiles", () => ({
	default: () => <div data-testid="cycle-time-percentiles" />,
}));
vi.mock("../../../components/Common/Charts/EstimationVsCycleTimeChart", () => ({
	default: () => <div data-testid="estimation-vs-cycle-time-chart" />,
}));
vi.mock(
	"../../../components/Common/Charts/FeatureSizeScatterPlotChart",
	() => ({
		default: ({ sizeDataPoints }: { sizeDataPoints: unknown[] }) => (
			<div data-testid="feature-size-chart">
				{`${sizeDataPoints.length} Features`}
			</div>
		),
	}),
);
vi.mock("../../../components/Common/Charts/LoadBalanceMatrixChart", () => ({
	default: () => <div data-testid="load-balance-chart" />,
}));
vi.mock("../../../components/Common/Charts/StackedAreaChart", () => ({
	default: () => <div data-testid="stacked-area-chart" />,
}));
vi.mock("../../../components/Common/Charts/TotalWorkItemAgeRunChart", () => ({
	default: () => <div data-testid="total-age-run-chart" />,
}));
vi.mock("../../../components/Common/Charts/WorkDistributionChart", () => ({
	default: () => <div data-testid="work-distribution-chart" />,
}));
vi.mock("../../../components/Common/Charts/WorkItemAgePercentiles", () => ({
	default: () => <div data-testid="work-item-age-percentiles" />,
}));
vi.mock("../../../components/Common/Charts/WorkItemAgingChart", () => ({
	default: () => <div data-testid="aging-chart" />,
}));
vi.mock("../../../components/Common/Charts/ProcessBehaviourChart", () => ({
	default: ({
		title,
		filterToggle,
	}: {
		title: string;
		filterToggle?: ReactNode;
	}) => (
		<div data-testid="process-behaviour-chart">
			{title}
			{filterToggle}
		</div>
	),
	ProcessBehaviourChartType: {
		Throughput: "Throughput",
		WorkInProgress: "WorkInProgress",
		TotalWorkItemAge: "TotalWorkItemAge",
		CycleTime: "CycleTime",
		Arrivals: "Arrivals",
		FeatureSize: "FeatureSize",
	},
}));
vi.mock("../../../components/Common/Charts/CumulativeStateTimeChart", () => ({
	default: ({
		data,
		scopeSlot,
		pickerSlot,
	}: {
		data: { states: { state: string }[] };
		scopeSlot?: ReactNode;
		pickerSlot?: ReactNode;
	}) => (
		<div data-testid="time-per-state-chart">
			<span data-testid="time-per-state-counts">
				{data.states[0]?.state ?? "nothing"}
			</span>
			{scopeSlot}
			{pickerSlot}
		</div>
	),
}));
vi.mock(
	"../../../components/Common/Charts/CumulativeStateTimeScopeControl",
	() => ({
		default: ({
			onScopeChange,
		}: {
			onScopeChange: (definitionId: number | null) => void;
		}) => (
			<button type="button" onClick={() => onScopeChange(7)}>
				Count only the In Review stretch
			</button>
		),
	}),
);
vi.mock(
	"../../../components/Common/Charts/CumulativeStateTimeItemPicker",
	() => ({
		default: ({
			candidates,
			onOpen,
			onSelectionChange,
		}: {
			candidates: { referenceId: string }[];
			onOpen: () => void;
			onSelectionChange: (itemIds: number[]) => void;
		}) => (
			<div>
				<button type="button" onClick={onOpen}>
					Open the Work Item picker
				</button>
				<button type="button" onClick={() => onSelectionChange([11, 12])}>
					Choose Work Items 11 and 12
				</button>
				<span data-testid="picker-offers">
					{candidates.map((c) => c.referenceId).join(", ")}
				</span>
			</div>
		),
	}),
);
vi.mock("./PbcOverTimeWidget", () => ({
	default: () => <div data-testid="pbc-over-time" />,
}));
vi.mock("./PercentilesOverTimeWidget", () => ({
	default: () => <div data-testid="percentiles-over-time" />,
}));

const COULD_NOT_LOAD =
	"This chart couldn't be loaded. Change the dates or reload to try again.";

const LAST_7_DAYS = windowOf(7);
const LAST_14_DAYS = windowOf(14);
const LAST_30_DAYS = windowOf(30);
const LAST_60_DAYS = windowOf(60);
const LAST_90_DAYS = windowOf(90);
const THE_30_DAYS_BEFORE_LAST_WEEK = windowOf(30, 7);

const team = (() => {
	const t = new Team();
	t.name = "Team Zenith";
	t.id = 2;
	t.serviceLevelExpectationProbability = 85;
	t.serviceLevelExpectationRange = 10;
	return t;
})();

const portfolio = (() => {
	const p = new Portfolio();
	p.name = "Ocean Explorer";
	p.id = 1;
	return p;
})();

type Owner = "team" | "portfolio";

let held: HeldMetricsService;

function openTheDashboard(
	owner: Owner,
	category: CategoryKey,
	options: { blackoutPeriodsFail?: boolean } = {},
): void {
	held = createHeldMetricsService(owner);
	const entity = owner === "team" ? team : portfolio;
	localStorage.setItem(
		`lighthouse:metrics:${owner}:${entity.id}:category`,
		category,
	);
	const blackoutPeriodService = createMockBlackoutPeriodService();
	blackoutPeriodService.getAll = options.blackoutPeriodsFail
		? vi.fn().mockRejectedValue(new Error("blackout periods failed"))
		: vi.fn().mockResolvedValue([]);

	render(
		<MemoryRouter>
			<QueryClientProvider
				client={
					new QueryClient({ defaultOptions: { queries: { retry: false } } })
				}
			>
				<ApiServiceContext.Provider
					value={createMockApiServiceContext({ blackoutPeriodService })}
				>
					<BaseMetricsView
						entity={entity}
						metricsService={held.service as IMetricsService<IWorkItem>}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["In Progress"]}
						hasForecastFilter
						featuresInProgress={[]}
					/>
				</ApiServiceContext.Provider>
			</QueryClientProvider>
		</MemoryRouter>,
	);
}

const frameOf = (widgetKey: string) =>
	screen.getByTestId(`widget-shell-${widgetKey}`);
const statusOf = (widgetKey: string) =>
	frameOf(widgetKey).getAttribute("data-widget-status");
const throughputChartReads = () =>
	within(frameOf("throughput")).getByTestId("run-chart-reads");

const widgetsOnScreen = () =>
	screen
		.queryAllByTestId(/^dashboard-item-/)
		.map((item) =>
			item.getAttribute("data-testid")?.replace("dashboard-item-", ""),
		);

const placedWidgets = (category: CategoryKey, owner: Owner) =>
	getWidgetsForCategory(category, owner).map((w) => w.widgetKey);

// Charts that fetch their own series are replaced by stand-ins on this page; their frames
// are specified with the real charts in their own file.
const selfFetching = new Set(["percentilesOverTime", "pbcOverTime"]);

async function everythingAsked(
	filter: Parameters<HeldMetricsService["answer"]>[0] = {},
) {
	await act(async () => held.answer(filter));
}

async function everythingHasLoaded(): Promise<void> {
	await everythingAsked();
	await everythingAsked();
}

function theReaderPicks(presetLabel: string): void {
	fireEvent.click(screen.getByRole("button", { name: presetLabel }));
}

function theReaderOpens(category: CategoryKey): void {
	fireEvent.click(screen.getByRole("button", { name: `Open ${category}` }));
}

async function throughputAnswersFor(window: string): Promise<void> {
	await act(async () => held.answer({ method: "getThroughput", window }));
}

async function throughputFailsFor(window: string): Promise<void> {
	await act(async () => held.fail({ method: "getThroughput", window }));
}

function permutationsOf<T>(items: readonly T[]): T[][] {
	if (items.length <= 1) return [[...items]];
	return items.flatMap((item, index) =>
		permutationsOf([...items.slice(0, index), ...items.slice(index + 1)]).map(
			(rest) => [item, ...rest],
		),
	);
}

beforeEach(() => {
	vi.clearAllMocks();
	localStorage.clear();
});

describe.each<Owner>(["team", "portfolio"])(
	"a %s's charts after the reader picks another window",
	(owner) => {
		it.skip("every chart behind the new window dims at once, keeping its older picture until its own data arrives", async () => {
			openTheDashboard(owner, "flow-metrics");
			await everythingHasLoaded();
			expect(throughputChartReads()).toHaveTextContent(
				"Work Items Completed: 30",
			);

			theReaderPicks("Last 90 days");

			expect(statusOf("throughput")).toBe("loading");
			expect(statusOf("arrivals")).toBe("loading");
			expect(throughputChartReads()).toHaveTextContent(
				"Work Items Completed: 30",
			);
		});

		it.skip("each chart comes back on its own, as soon as its own data for the new window arrives", async () => {
			openTheDashboard(owner, "flow-metrics");
			await everythingHasLoaded();
			theReaderPicks("Last 90 days");

			await throughputAnswersFor(LAST_90_DAYS);

			expect(statusOf("throughput")).toBe("ready");
			expect(throughputChartReads()).toHaveTextContent(
				"Work Items Completed: 90",
			);
			expect(statusOf("arrivals")).toBe("loading");
		});
	},
);

describe("a team's charts while the window is changing", () => {
	it.skip("picking an earlier start date dims the charts until the new window arrives", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		fireEvent.click(
			screen.getByRole("button", { name: "Pick a start date a month earlier" }),
		);

		expect(statusOf("throughput")).toBe("loading");
		await act(async () => held.answer({ window: LAST_60_DAYS }));
		expect(statusOf("throughput")).toBe("ready");
	});

	it.skip("stepping the window back dims the charts at once, before the step is even asked for", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		fireEvent.click(screen.getByRole("button", { name: "Previous 7 days" }));

		expect(screen.getByTestId("step-waiting")).toHaveTextContent("true");
		expect(statusOf("throughput")).toBe("loading");
		expect(
			held.pending({
				method: "getThroughput",
				window: THE_30_DAYS_BEFORE_LAST_WEEK,
			}),
		).toHaveLength(0);

		await waitFor(
			() =>
				expect(
					held.pending({
						method: "getThroughput",
						window: THE_30_DAYS_BEFORE_LAST_WEEK,
					}),
				).toHaveLength(1),
			{ timeout: 2000 },
		);
		expect(statusOf("throughput")).toBe("loading");

		await throughputAnswersFor(THE_30_DAYS_BEFORE_LAST_WEEK);
		expect(statusOf("throughput")).toBe("ready");
	});

	it.skip("a chart behind the window cannot be hovered, clicked or opened for its data", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		theReaderPicks("Last 90 days");

		expect(
			within(frameOf("throughput")).getByTestId("widget-view-data-throughput"),
		).toBeDisabled();
		expect(screen.getByTestId("widget-shell-body-throughput")).toHaveAttribute(
			"inert",
		);
	});
});

describe("a team's chart when windows are picked in quick succession", () => {
	it.skip("shows the second window when the first window's answer arrives last", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderPicks("Last 90 days");
		theReaderPicks("Last 7 days");

		await throughputAnswersFor(LAST_7_DAYS);
		await throughputAnswersFor(LAST_90_DAYS);

		expect(throughputChartReads()).toHaveTextContent("Work Items Completed: 7");
		expect(statusOf("throughput")).toBe("ready");
	});

	it.skip("stays loading when the first window answers first, until the second window's answer arrives", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderPicks("Last 90 days");
		theReaderPicks("Last 7 days");

		await throughputAnswersFor(LAST_90_DAYS);
		expect(statusOf("throughput")).toBe("loading");

		await throughputAnswersFor(LAST_7_DAYS);
		expect(statusOf("throughput")).toBe("ready");
		expect(throughputChartReads()).toHaveTextContent("Work Items Completed: 7");
	});

	it.skip.each(permutationsOf([LAST_90_DAYS, LAST_14_DAYS, LAST_7_DAYS]))(
		"answers arriving as %s, then %s, then %s still settle on the last window picked",
		async (...arrivalOrder) => {
			openTheDashboard("team", "flow-metrics");
			await everythingHasLoaded();
			theReaderPicks("Last 90 days");
			theReaderPicks("Last 14 days");
			theReaderPicks("Last 7 days");

			const answeredSoFar: string[] = [];
			for (const window of arrivalOrder) {
				await throughputAnswersFor(window);
				answeredSoFar.push(window);
				const lastPickedHasAnswered = answeredSoFar.includes(LAST_7_DAYS);
				expect(statusOf("throughput")).toBe(
					lastPickedHasAnswered ? "ready" : "loading",
				);
			}
			expect(throughputChartReads()).toHaveTextContent(
				"Work Items Completed: 7",
			);
		},
	);
});

describe("a team's chart whose data cannot be loaded", () => {
	async function givenTheReaderPicked90DaysAfterEverythingLoaded() {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderPicks("Last 90 days");
	}

	async function givenThe90DayThroughputFailed() {
		await givenTheReaderPicked90DaysAfterEverythingLoaded();
		await throughputFailsFor(LAST_90_DAYS);
	}

	it.skip("stops its spinner, removes the older chart and says it couldn't be loaded", async () => {
		await givenThe90DayThroughputFailed();

		expect(statusOf("throughput")).toBe("error");
		expect(
			within(frameOf("throughput")).getByText(COULD_NOT_LOAD),
		).toBeInTheDocument();
		expect(
			within(frameOf("throughput")).queryByRole("progressbar"),
		).not.toBeInTheDocument();
		expect(
			within(frameOf("throughput")).queryByText("Work Items Completed: 30"),
		).toBeNull();
	});

	it.skip("leaves every other chart to come back with its own data", async () => {
		await givenThe90DayThroughputFailed();

		await act(async () =>
			held.answer({ window: LAST_90_DAYS, except: "getThroughput" }),
		);

		expect(statusOf("wipOverTime")).toBe("ready");
		expect(statusOf("throughput")).toBe("error");
	});

	it.skip("comes back once the reader picks a window that loads", async () => {
		await givenThe90DayThroughputFailed();

		theReaderPicks("Last 7 days");
		expect(statusOf("throughput")).toBe("loading");
		expect(
			within(frameOf("throughput")).queryByText(COULD_NOT_LOAD),
		).not.toBeInTheDocument();

		await throughputAnswersFor(LAST_7_DAYS);
		expect(statusOf("throughput")).toBe("ready");
		expect(throughputChartReads()).toHaveTextContent("Work Items Completed: 7");
	});

	it.skip("does not report a failure for a window the reader has already left", async () => {
		await givenTheReaderPicked90DaysAfterEverythingLoaded();
		theReaderPicks("Last 7 days");

		await throughputFailsFor(LAST_90_DAYS);

		expect(statusOf("throughput")).toBe("loading");
		expect(screen.queryByText(COULD_NOT_LOAD)).not.toBeInTheDocument();
	});

	it.skip("one failed request marks only the charts that show it", async () => {
		openTheDashboard("team", "flow-overview");
		await act(async () =>
			held.fail({ method: "getCycleTimePercentiles", window: LAST_30_DAYS }),
		);
		await act(async () => held.answer({ except: "getCycleTimePercentiles" }));
		await act(async () => held.answer({ except: "getCycleTimePercentiles" }));

		expect(statusOf("percentiles")).toBe("error");
		expect(statusOf("workItemAgePercentiles")).toBe("ready");
		expect(statusOf("flowEfficiency")).toBe("ready");
	});
});

describe("charts waiting on something this owner never has", () => {
	it.skip("a portfolio's Work In Progress comes back without waiting for an at-risk answer only teams have", async () => {
		openTheDashboard("portfolio", "flow-overview");
		await everythingHasLoaded();

		expect(statusOf("wipOverview")).toBe("ready");
	});

	it.skip("the cycle time scatterplot comes back even when the blackout periods cannot be read", async () => {
		openTheDashboard("team", "flow-metrics", { blackoutPeriodsFail: true });
		await everythingHasLoaded();

		expect(statusOf("cycleScatter")).toBe("ready");
	});
});

describe("Cumulative Time per State narrowed by the reader, across a window change", () => {
	const countsShown = () => screen.getByTestId("time-per-state-counts");

	it.skip("a stretch chosen for a window the reader has left never shows its numbers", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Count only the In Review stretch" }),
		);

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));
		await act(async () => held.answer({ window: LAST_30_DAYS }));

		expect(countsShown()).toHaveTextContent(`every Work Item, ${LAST_90_DAYS}`);
		expect(statusOf("stateTimeCumulative")).toBe("ready");
	});

	it.skip("Work Items chosen in the picker stay chosen and are counted again for the new window", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Choose Work Items 11 and 12" }),
		);
		await everythingAsked();
		expect(countsShown()).toHaveTextContent(
			`Work Items 11+12, ${LAST_30_DAYS}`,
		);

		theReaderPicks("Last 90 days");
		expect(statusOf("stateTimeCumulative")).toBe("loading");
		await act(async () => held.answer({ window: LAST_90_DAYS }));

		expect(countsShown()).toHaveTextContent(
			`Work Items 11+12, ${LAST_90_DAYS}`,
		);
		expect(statusOf("stateTimeCumulative")).toBe("ready");
	});

	it.skip("a choice of Work Items answered late for the window the reader left never replaces the current one", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Choose Work Items 11 and 12" }),
		);

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));
		await act(async () => held.answer({ window: LAST_30_DAYS }));

		expect(countsShown()).toHaveTextContent(
			`Work Items 11+12, ${LAST_90_DAYS}`,
		);
	});

	it.skip("the picker offers the new window's Work Items once the window changes", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Open the Work Item picker" }),
		);
		await everythingAsked();

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));
		fireEvent.click(
			screen.getByRole("button", { name: "Open the Work Item picker" }),
		);
		await act(async () => held.answer({ window: LAST_90_DAYS }));

		expect(screen.getByTestId("picker-offers")).toHaveTextContent(
			`ITEM-11 (${LAST_90_DAYS})`,
		);
	});
});

describe("the Throughput process behaviour chart's filter", () => {
	it.skip("switching it dims only that chart, not the other process behaviour charts", async () => {
		openTheDashboard("team", "predictability");
		await everythingHasLoaded();

		fireEvent.click(
			within(frameOf("throughputPbc")).getByLabelText(
				"Use filtered Throughput",
			),
		);

		expect(statusOf("throughputPbc")).toBe("loading");
		expect(statusOf("cycleTimePbc")).toBe("ready");
		expect(statusOf("arrivalsPbc")).toBe("ready");
	});
});

const everyCategory: readonly CategoryKey[] = [
	"flow-overview",
	"flow-metrics",
	"predictability",
	"portfolio",
];
const everyOwnerAndCategory = (["team", "portfolio"] as const).flatMap(
	(owner) => everyCategory.map((category) => [owner, category] as const),
);

describe("a dashboard as it opens", () => {
	it.skip.each(everyOwnerAndCategory)(
		"a %s's %s opens with every chart's frame in its place, each with a spinner, before any data arrives",
		(owner, category) => {
			openTheDashboard(owner, category);

			expect(widgetsOnScreen()).toEqual(placedWidgets(category, owner));
			for (const widgetKey of placedWidgets(category, owner)) {
				if (selfFetching.has(widgetKey)) continue;
				expect(statusOf(widgetKey)).toBe("loading");
				expect(
					within(frameOf(widgetKey)).getByRole("progressbar"),
				).toBeInTheDocument();
			}
		},
	);

	it.skip.each(everyOwnerAndCategory)(
		"a %s's %s adds and removes no chart once its data has arrived",
		async (owner, category) => {
			openTheDashboard(owner, category);
			const atFirstSight = widgetsOnScreen();

			await everythingHasLoaded();

			expect(widgetsOnScreen()).toEqual(atFirstSight);
		},
	);

	it.skip("a frame waiting for its first data shows its title and info, with no rating, trend or chart yet", () => {
		openTheDashboard("team", "flow-overview");

		const frame = frameOf("totalThroughput");
		expect(
			within(frame).getByTestId("widget-info-totalThroughput"),
		).toBeVisible();
		expect(
			within(frame).queryByTestId("widget-rag-totalThroughput"),
		).not.toBeInTheDocument();
		expect(
			within(frame).queryByTestId("widget-trend-totalThroughput"),
		).not.toBeInTheDocument();
	});

	it.skip.each(["flowEfficiency", "predictabilityScore", "totalWorkItemAge"])(
		"%s waits under the same single spinner as every other chart",
		(widgetKey) => {
			openTheDashboard("team", "flow-overview");

			expect(statusOf(widgetKey)).toBe("loading");
			expect(
				within(frameOf(widgetKey)).getAllByRole("progressbar"),
			).toHaveLength(1);
		},
	);

	it.skip("opening a category not visited yet frames all of its charts at once", async () => {
		openTheDashboard("team", "flow-overview");
		await everythingHasLoaded();

		theReaderOpens("flow-metrics");

		expect(widgetsOnScreen()).toEqual(placedWidgets("flow-metrics", "team"));
		expect(statusOf("throughput")).toBe("loading");
	});

	it.skip("a first visit whose data cannot be loaded says so in the chart's frame", async () => {
		openTheDashboard("team", "flow-metrics");

		await throughputFailsFor(LAST_30_DAYS);
		await act(async () => held.answer({ except: "getThroughput" }));

		expect(statusOf("throughput")).toBe("error");
		expect(
			within(frameOf("throughput")).getByText(COULD_NOT_LOAD),
		).toBeInTheDocument();
	});
});

describe("charts whose place depends on what the data says", () => {
	const notConfigured = () =>
		({
			...(usualAnswer(
				"getEstimationVsCycleTimeData",
				LAST_30_DAYS,
				[],
			) as object),
			status: "NotConfigured",
		}) as unknown;

	it.skip("Estimation vs. Cycle Time is framed while it loads, then leaves when estimation is not set up", async () => {
		openTheDashboard("portfolio", "portfolio");
		expect(statusOf("estimationVsCycleTime")).toBe("loading");

		await act(async () =>
			held.answer({ method: "getEstimationVsCycleTimeData" }, notConfigured()),
		);

		expect(
			screen.queryByTestId("widget-shell-estimationVsCycleTime"),
		).not.toBeInTheDocument();
	});

	it("Estimation vs. Cycle Time stays away on a window change once it is known not to be set up", async () => {
		openTheDashboard("portfolio", "portfolio");
		await act(async () =>
			held.answer({ method: "getEstimationVsCycleTimeData" }, notConfigured()),
		);
		await everythingHasLoaded();

		theReaderPicks("Last 90 days");

		expect(
			screen.queryByTestId("widget-shell-estimationVsCycleTime"),
		).not.toBeInTheDocument();
	});

	it.skip("Feature Size stays in its place and shows its own empty state when the window holds no Features", async () => {
		openTheDashboard("portfolio", "portfolio");

		await act(async () =>
			held.answer({ method: "getAllFeaturesForSizeChart" }, []),
		);
		await everythingHasLoaded();

		expect(statusOf("featureSize")).toBe("ready");
		expect(
			within(frameOf("featureSize")).getByTestId("feature-size-chart"),
		).toHaveTextContent("0 Features");
	});
});
