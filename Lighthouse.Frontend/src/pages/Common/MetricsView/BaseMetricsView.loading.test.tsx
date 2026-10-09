import { createTheme, ThemeProvider } from "@mui/material/styles";
import {
	notifyManager,
	QueryClient,
	QueryClientProvider,
} from "@tanstack/react-query";
import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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
import {
	type CategoryKey,
	getCategories,
	getFetchKeysForCategories,
	getFetchRequirementsForWidget,
	getWidgetsForCategory,
} from "./categoryMetadata";
import type { DateWindowPreset } from "./dateWindow";
import { COULD_NOT_LOAD_MESSAGE } from "./WidgetShell";
import type { WidgetStatus } from "./widgetStatus";

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

vi.mock("@mui/x-charts", async (importOriginal) => ({
	...(await importOriginal<typeof import("@mui/x-charts")>()),
	LineChart: () => <div data-testid="over-time-chart" />,
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
type Theme = "light" | "dark";

let held: HeldMetricsService;

function openTheDashboard(
	owner: Owner,
	category: CategoryKey,
	options: {
		blackoutPeriodsFail?: boolean;
		theme?: Theme;
		queryClient?: QueryClient;
	} = {},
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
	const queryClient =
		options.queryClient ??
		new QueryClient({ defaultOptions: { queries: { retry: false } } });

	render(
		<ThemeProvider
			theme={createTheme({ palette: { mode: options.theme ?? "light" } })}
		>
			<MemoryRouter>
				<QueryClientProvider client={queryClient}>
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
			</MemoryRouter>
		</ThemeProvider>,
	);
}

const frameOf = (widgetKey: string) =>
	screen.getByTestId(`widget-shell-${widgetKey}`);
const statusOf = (widgetKey: string) =>
	frameOf(widgetKey).getAttribute("data-widget-status");

function expectThroughputToRead(total: number): void {
	expect(
		within(frameOf("throughput")).getByTestId("run-chart-reads"),
	).toHaveTextContent(new RegExp(`^Work Items Completed: ${total}$`));
}

const widgetsOnScreen = () =>
	screen
		.queryAllByTestId(/^dashboard-item-/)
		.map((item) =>
			item.getAttribute("data-testid")?.replace("dashboard-item-", ""),
		);

// These two keep today's rule: they appear only once their data says to show them.
const placedOnlyOnceTheirDataSaysSo = new Set([
	"estimationVsCycleTime",
	"featureSize",
]);

const framedBeforeAnyData = (category: CategoryKey, owner: Owner) =>
	getWidgetsForCategory(category, owner)
		.map((w) => w.widgetKey)
		.filter((key) => !placedOnlyOnceTheirDataSaysSo.has(key));

const everyOwnerAndCategory = (["team", "portfolio"] as const).flatMap(
	(owner) =>
		getCategories().map(
			(category) => [owner, category.displayName, category.key] as const,
		),
);

const fetchesForItself = new Set(["percentilesOverTime", "pbcOverTime"]);

const chartsThePageFeedsOn = (category: CategoryKey, owner: Owner) =>
	framedBeforeAnyData(category, owner).filter(
		(key) => !fetchesForItself.has(key),
	);

function expectEveryChartToRead(
	widgetKeys: readonly string[],
	status: WidgetStatus,
): void {
	expect(widgetKeys.length).toBeGreaterThan(0);
	for (const widgetKey of widgetKeys) {
		expect(statusOf(widgetKey), widgetKey).toBe(status);
	}
}

const bodyOf = (widgetKey: string) =>
	within(frameOf(widgetKey)).getByTestId(`widget-shell-body-${widgetKey}`);

const withoutThoseThatWaitForTheirData = (
	keys: readonly (string | undefined)[],
) =>
	keys.filter(
		(key) => key !== undefined && !placedOnlyOnceTheirDataSaysSo.has(key),
	);

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

// The query library tells components about an answer on a later timer tick, which act() does not
// wait for. Telling them in the same tick lets each answered request show before the next step.
notifyManager.setScheduler(queueMicrotask);

beforeEach(() => {
	vi.clearAllMocks();
	localStorage.clear();
});

describe.each<[Owner, Theme]>([
	["team", "light"],
	["team", "dark"],
	["portfolio", "light"],
	["portfolio", "dark"],
])(
	"a %s's charts in the %s theme after the reader picks another window",
	(owner, theme) => {
		it("every chart behind the new window dims at once, keeping its older picture until its own data arrives", async () => {
			openTheDashboard(owner, "flow-metrics", { theme });
			await everythingHasLoaded();
			expectThroughputToRead(30);

			theReaderPicks("Last 90 days");

			expectEveryChartToRead(
				chartsThePageFeedsOn("flow-metrics", owner),
				"loading",
			);
			expect(bodyOf("throughput")).toHaveStyle({ opacity: "0.4" });
			expectThroughputToRead(30);
		});

		it("each chart comes back on its own, as soon as its own data for the new window arrives", async () => {
			openTheDashboard(owner, "flow-metrics", { theme });
			await everythingHasLoaded();
			theReaderPicks("Last 90 days");

			await throughputAnswersFor(LAST_90_DAYS);

			expect(statusOf("throughput")).toBe("ready");
			expectThroughputToRead(90);
			expect(statusOf("arrivals")).toBe("loading");
		});
	},
);

describe("a team's charts while the window is changing", () => {
	it("picking an earlier start date dims the charts until the new window arrives", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		fireEvent.click(
			screen.getByRole("button", { name: "Pick a start date a month earlier" }),
		);

		expect(statusOf("throughput")).toBe("loading");
		await act(async () => held.answer({ window: LAST_60_DAYS }));
		expect(statusOf("throughput")).toBe("ready");
	});

	it("stepping the window back dims the charts at once, before the step is even asked for", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		fireEvent.click(screen.getByRole("button", { name: "Previous 7 days" }));

		expect(screen.getByTestId("step-waiting")).toHaveTextContent(/^true$/);
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

	it("picking the window that is already showing leaves every chart as it is", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		theReaderPicks("Last 30 days");

		expectEveryChartToRead(
			framedBeforeAnyData("flow-metrics", "team"),
			"ready",
		);
		expectThroughputToRead(30);
	});

	it("a chart behind the window cannot be pointed at or opened for its data", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		theReaderPicks("Last 90 days");

		expect(
			within(frameOf("throughput")).getByTestId("widget-view-data-throughput"),
		).toBeDisabled();
		await expect(
			userEvent
				.setup()
				.click(within(frameOf("throughput")).getByTestId("run-chart")),
		).rejects.toThrow(/pointer-events: none/);
	});

	it("the Throughput filter switch inside a chart behind the window does not respond", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderPicks("Last 90 days");

		expect(bodyOf("throughput")).toHaveStyle({ pointerEvents: "none" });
		await expect(
			userEvent
				.setup()
				.click(
					within(frameOf("throughput")).getByLabelText(
						"Use filtered Throughput",
					),
				),
		).rejects.toThrow(/pointer-events: none/);
		expect(
			held.pending({ method: "getThroughput", filtered: true }),
		).toHaveLength(0);
	});
});

describe("a team's chart when windows are picked in quick succession", () => {
	it("shows the second window when the first window's answer arrives last", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderPicks("Last 90 days");
		theReaderPicks("Last 7 days");

		await throughputAnswersFor(LAST_7_DAYS);
		await throughputAnswersFor(LAST_90_DAYS);

		expectThroughputToRead(7);
	});

	async function answersArrivingInThisOrderSettleOnTheLastWindowPicked(
		...arrivalOrder: string[]
	) {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderPicks("Last 90 days");
		theReaderPicks("Last 14 days");
		theReaderPicks("Last 7 days");

		for (const window of arrivalOrder) {
			expect(held.pending({ method: "getThroughput", window })).toHaveLength(1);
		}
		for (const window of arrivalOrder) {
			await throughputAnswersFor(window);
		}

		expectThroughputToRead(7);
	}

	const everyArrivalOrder = permutationsOf([
		LAST_90_DAYS,
		LAST_14_DAYS,
		LAST_7_DAYS,
	]);
	const lastPickedAnswersLast = (order: readonly string[]) =>
		order[order.length - 1] === LAST_7_DAYS;

	it.each(everyArrivalOrder.filter((o) => !lastPickedAnswersLast(o)))(
		"answers arriving as %s, then %s, then %s still settle on the last window picked",
		answersArrivingInThisOrderSettleOnTheLastWindowPicked,
	);

	it.each(everyArrivalOrder.filter(lastPickedAnswersLast))(
		"answers arriving as %s, then %s, then %s settle on the last window picked, which answered last",
		answersArrivingInThisOrderSettleOnTheLastWindowPicked,
	);

	it("stays loading when the first window answers first, until the second window's answer arrives", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderPicks("Last 90 days");
		theReaderPicks("Last 7 days");

		await throughputAnswersFor(LAST_90_DAYS);
		expect(statusOf("throughput")).toBe("loading");

		await throughputAnswersFor(LAST_7_DAYS);
		expect(statusOf("throughput")).toBe("ready");
		expectThroughputToRead(7);
	});

	it.each<[string, string, string, WidgetStatus, WidgetStatus, WidgetStatus]>([
		[LAST_90_DAYS, LAST_14_DAYS, LAST_7_DAYS, "loading", "loading", "ready"],
		[LAST_90_DAYS, LAST_7_DAYS, LAST_14_DAYS, "loading", "ready", "ready"],
		[LAST_14_DAYS, LAST_90_DAYS, LAST_7_DAYS, "loading", "loading", "ready"],
		[LAST_14_DAYS, LAST_7_DAYS, LAST_90_DAYS, "loading", "ready", "ready"],
		[LAST_7_DAYS, LAST_90_DAYS, LAST_14_DAYS, "ready", "ready", "ready"],
		[LAST_7_DAYS, LAST_14_DAYS, LAST_90_DAYS, "ready", "ready", "ready"],
	])(
		"answers arriving as %s, then %s, then %s read %s, then %s, then %s",
		async (first, second, third, afterFirst, afterSecond, afterThird) => {
			openTheDashboard("team", "flow-metrics");
			await everythingHasLoaded();
			theReaderPicks("Last 90 days");
			theReaderPicks("Last 14 days");
			theReaderPicks("Last 7 days");

			await throughputAnswersFor(first);
			expect(statusOf("throughput")).toBe(afterFirst);
			await throughputAnswersFor(second);
			expect(statusOf("throughput")).toBe(afterSecond);
			await throughputAnswersFor(third);
			expect(statusOf("throughput")).toBe(afterThird);
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

	it("stops its spinner, takes the older chart out of sight and says it couldn't be loaded", async () => {
		await givenThe90DayThroughputFailed();

		expect(statusOf("throughput")).toBe("error");
		expect(
			within(frameOf("throughput")).getByText(COULD_NOT_LOAD_MESSAGE),
		).toBeVisible();
		expect(
			within(frameOf("throughput")).queryByRole("progressbar"),
		).not.toBeInTheDocument();
		expect(
			within(frameOf("throughput")).getByText(/^Work Items Completed: 30$/),
		).not.toBeVisible();
	});

	it("leaves every other chart to come back with its own data", async () => {
		await givenThe90DayThroughputFailed();

		await everythingAsked({ except: "getThroughput" });
		await everythingAsked({ except: "getThroughput" });

		const showsThroughput = (widgetKey: string) =>
			(getFetchRequirementsForWidget(widgetKey) ?? []).includes("throughput");
		expectEveryChartToRead(
			framedBeforeAnyData("flow-metrics", "team").filter(
				(key) => !showsThroughput(key),
			),
			"ready",
		);
		expect(statusOf("throughput")).toBe("error");
	});

	it("comes back once the reader picks a window that loads", async () => {
		await givenThe90DayThroughputFailed();

		theReaderPicks("Last 7 days");
		expect(statusOf("throughput")).toBe("loading");
		expect(
			within(frameOf("throughput")).queryByText(COULD_NOT_LOAD_MESSAGE),
		).not.toBeInTheDocument();

		await throughputAnswersFor(LAST_7_DAYS);
		expect(statusOf("throughput")).toBe("ready");
		expectThroughputToRead(7);
	});

	it("stepping the window after a failure shows the chart loading at once, before the step is asked for", async () => {
		await givenThe90DayThroughputFailed();

		fireEvent.click(screen.getByRole("button", { name: "Previous 7 days" }));

		expect(screen.getByTestId("step-waiting")).toHaveTextContent(/^true$/);
		expect(statusOf("throughput")).toBe("loading");
		expect(
			within(frameOf("throughput")).queryByText(COULD_NOT_LOAD_MESSAGE),
		).not.toBeInTheDocument();
	});

	it("does not report a failure for a window the reader has already left", async () => {
		await givenTheReaderPicked90DaysAfterEverythingLoaded();
		theReaderPicks("Last 7 days");

		await throughputFailsFor(LAST_90_DAYS);

		expect(statusOf("throughput")).toBe("loading");
		expect(screen.queryByText(COULD_NOT_LOAD_MESSAGE)).not.toBeInTheDocument();
	});

	it("says it couldn't be loaded as soon as one of its requests fails, while its others are still on their way", async () => {
		await givenTheReaderPicked90DaysAfterEverythingLoaded();

		await act(async () =>
			held.fail({ method: "getCycleTimePercentiles", window: LAST_90_DAYS }),
		);

		expect(
			held.pending({ method: "getCycleTimeData", window: LAST_90_DAYS }),
		).toHaveLength(1);
		expect(statusOf("cycleScatter")).toBe("error");
		expect(
			within(frameOf("cycleScatter")).queryByRole("progressbar"),
		).not.toBeInTheDocument();
		expect(
			within(frameOf("cycleScatter")).getByText(COULD_NOT_LOAD_MESSAGE),
		).toBeInTheDocument();
	});

	it("one failed request marks only the charts that show it", async () => {
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

	it("a chart whose answer is simply empty comes back ready, not failed", async () => {
		openTheDashboard("team", "flow-overview");
		await act(async () =>
			held.answerWithNothing({ method: "getFlowEfficiencyInfoForTeam" }),
		);
		await everythingHasLoaded();

		expect(statusOf("flowEfficiency")).toBe("ready");
		expect(
			within(frameOf("flowEfficiency")).queryByText(COULD_NOT_LOAD_MESSAGE),
		).not.toBeInTheDocument();
	});
});

describe("charts waiting on something this owner never has", () => {
	it("a portfolio's Work In Progress comes back without waiting for an at-risk answer only teams have", async () => {
		openTheDashboard("portfolio", "flow-overview");
		await everythingHasLoaded();

		expect(statusOf("wipOverview")).toBe("ready");
	});

	it("the cycle time scatterplot comes back even when the blackout periods cannot be read", async () => {
		openTheDashboard("team", "flow-metrics", { blackoutPeriodsFail: true });
		await everythingHasLoaded();

		expect(statusOf("cycleScatter")).toBe("ready");
	});
});

describe("every chart request on the dashboard", () => {
	it.each(everyOwnerAndCategory)(
		"on a %s's %s is asked for without retrying, and kept for no other window",
		async (owner, _name, category) => {
			const withTheAppDefaults = new QueryClient({
				defaultOptions: {
					queries: { staleTime: 300_000, gcTime: 1_800_000, retry: 2 },
				},
			});
			openTheDashboard(owner, category, {
				queryClient: withTheAppDefaults,
			});
			await everythingHasLoaded();

			const queries = withTheAppDefaults.getQueryCache().getAll();
			expect(queries.length).toBeGreaterThan(0);
			for (const query of queries) {
				expect(query.options.retry).toBe(false);
				expect(query.options.gcTime).toBe(0);
				for (const observer of query.observers) {
					expect(observer.options.staleTime).toBe(0);
					expect(observer.options.refetchOnWindowFocus).toBe(false);
				}
			}
		},
	);
});

describe("Cumulative Time per State narrowed by the reader, across a window change", () => {
	const expectCountsToRead = (text: string) =>
		expect(screen.getByTestId("time-per-state-counts")).toHaveTextContent(
			new RegExp(`^${text.replace("+", "\\+")}$`),
		);
	const theReaderOpensThePicker = () =>
		fireEvent.click(
			screen.getByRole("button", { name: "Open the Work Item picker" }),
		);
	const candidatesAskedFor = (window: string) =>
		held.calls.filter(
			(call) =>
				call.method === "getCumulativeStateTimeCandidatesForTeam" &&
				call.window === window,
		);

	it("a window change drops the chosen stretch, as it does today, and a late answer for it never shows", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Count only the In Review stretch" }),
		);

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));
		await act(async () => held.answer({ window: LAST_30_DAYS }));

		expectCountsToRead(`every Work Item, ${LAST_90_DAYS}`);
	});

	it("Work Items chosen in the picker stay chosen and are counted again for the new window", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Choose Work Items 11 and 12" }),
		);
		await everythingAsked();
		expectCountsToRead(`Work Items 11+12, ${LAST_30_DAYS}`);

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));

		expectCountsToRead(`Work Items 11+12, ${LAST_90_DAYS}`);
	});

	it("a choice of Work Items answered late for the window the reader left never replaces the current one", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Choose Work Items 11 and 12" }),
		);

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));
		await act(async () => held.answer({ window: LAST_30_DAYS }));

		expectCountsToRead(`Work Items 11+12, ${LAST_90_DAYS}`);
	});

	it("the picker offers the new window's Work Items once the window changes", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderOpensThePicker();
		await everythingAsked();

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));
		theReaderOpensThePicker();
		await act(async () => held.answer({ window: LAST_90_DAYS }));

		expect(screen.getByTestId("picker-offers")).toHaveTextContent(
			new RegExp(`^ITEM-11 \\(${LAST_90_DAYS}\\)$`),
		);
	});

	it("the picker asks for no Work Items of the new window until the reader opens it again", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderOpensThePicker();
		await everythingAsked();

		theReaderPicks("Last 90 days");
		await act(async () => held.answer({ window: LAST_90_DAYS }));
		expect(candidatesAskedFor(LAST_90_DAYS)).toHaveLength(0);

		theReaderOpensThePicker();
		expect(candidatesAskedFor(LAST_90_DAYS)).toHaveLength(1);
	});

	it("is loading while chosen Work Items are counted again for the new window", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Choose Work Items 11 and 12" }),
		);
		await everythingAsked();

		theReaderPicks("Last 90 days");
		expect(statusOf("stateTimeCumulative")).toBe("loading");

		await act(async () => held.answer({ window: LAST_90_DAYS }));
		expect(statusOf("stateTimeCumulative")).toBe("ready");
	});

	it("says it couldn't be loaded when the chosen stretch's numbers cannot be loaded", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		fireEvent.click(
			screen.getByRole("button", { name: "Count only the In Review stretch" }),
		);

		await act(async () =>
			held.fail({ method: "getCumulativeStateTimeForTeam" }),
		);

		expect(statusOf("stateTimeCumulative")).toBe("error");
		expect(
			within(frameOf("stateTimeCumulative")).getByText(COULD_NOT_LOAD_MESSAGE),
		).toBeInTheDocument();
	});

	it.each([
		["Work Items", "Choose Work Items 11 and 12"],
		["stretch", "Count only the In Review stretch"],
	])(
		"the reader's first choice of %s keeps the chart and its controls on screen, dimmed while it is counted",
		async (_choice, control) => {
			openTheDashboard("team", "flow-metrics");
			await everythingHasLoaded();
			const chart = within(frameOf("stateTimeCumulative")).getByTestId(
				"time-per-state-chart",
			);
			const chosenWith = screen.getByRole("button", { name: control });

			fireEvent.click(chosenWith);

			expect(statusOf("stateTimeCumulative")).toBe("loading");
			expect(chart).toBeInTheDocument();
			expect(chosenWith).toBeInTheDocument();
			expect(bodyOf("stateTimeCumulative")).toHaveStyle({ opacity: "0.4" });
		},
	);

	it("keeps showing its numbers when the picker's Work Items cannot be loaded", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		theReaderOpensThePicker();

		await act(async () =>
			held.fail({ method: "getCumulativeStateTimeCandidatesForTeam" }),
		);

		expectCountsToRead(`every Work Item, ${LAST_30_DAYS}`);
		expect(screen.queryByText(COULD_NOT_LOAD_MESSAGE)).not.toBeInTheDocument();
	});
});

describe("filters inside a chart", () => {
	it("switching the Throughput process behaviour chart's filter dims only that chart", async () => {
		openTheDashboard("team", "predictability");
		await everythingHasLoaded();

		fireEvent.click(
			within(frameOf("throughputPbc")).getByLabelText(
				"Use filtered Throughput",
			),
		);

		expect(statusOf("throughputPbc")).toBe("loading");
		expectEveryChartToRead(
			framedBeforeAnyData("predictability", "team").filter(
				(key) => key !== "throughputPbc",
			),
			"ready",
		);
	});

	it("switching the Throughput run chart's filter puts only that chart into loading", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();

		fireEvent.click(
			within(frameOf("throughput")).getByLabelText("Use filtered Throughput"),
		);

		expect(statusOf("throughput")).toBe("loading");
		expectEveryChartToRead(
			framedBeforeAnyData("flow-metrics", "team").filter(
				(key) => key !== "throughput",
			),
			"ready",
		);
	});

	it("the Throughput run chart's filter is still on once the chart comes back from a window that could not be loaded", async () => {
		openTheDashboard("team", "flow-metrics");
		await everythingHasLoaded();
		const filterSwitch = within(frameOf("throughput")).getByLabelText(
			"Use filtered Throughput",
		);
		fireEvent.click(filterSwitch);
		await everythingAsked();

		theReaderPicks("Last 90 days");
		await throughputFailsFor(LAST_90_DAYS);
		expect(statusOf("throughput")).toBe("error");

		theReaderPicks("Last 7 days");
		await everythingHasLoaded();

		expect(statusOf("throughput")).toBe("ready");
		expect(filterSwitch).toBeInTheDocument();
		expect(
			within(frameOf("throughput")).getByLabelText("Use filtered Throughput"),
		).toBeChecked();
	});
});

const chartsThatFetchForThemselves = [
	["Percentiles Over Time", "percentilesOverTime", "getPercentilesOverTime"],
	["PBC Over Time", "pbcOverTime", "getProcessBehaviorOverTime"],
] as const;

describe.each(chartsThatFetchForThemselves)(
	"%s on the Predictability dashboard",
	(_name, widgetKey, method) => {
		it("shows its spinner in its own frame while its series is on its way", async () => {
			openTheDashboard("team", "predictability");

			await everythingAsked({ except: method });
			await everythingAsked({ except: method });

			expect(statusOf(widgetKey)).toBe("loading");
			expect(
				within(frameOf(widgetKey)).getByRole("progressbar"),
			).toBeInTheDocument();
		});

		it("says it couldn't be loaded when its series cannot be loaded", async () => {
			openTheDashboard("team", "predictability");

			await act(async () => held.fail({ method }));
			await everythingHasLoaded();

			expect(statusOf(widgetKey)).toBe("error");
			expect(
				within(frameOf(widgetKey)).getByText(COULD_NOT_LOAD_MESSAGE),
			).toBeInTheDocument();
		});

		it("still says it couldn't be loaded while a stepped window waits to be committed", async () => {
			openTheDashboard("team", "predictability");
			await act(async () => held.fail({ method }));
			await everythingHasLoaded();

			fireEvent.click(screen.getByRole("button", { name: "Previous 7 days" }));

			expect(screen.getByTestId("step-waiting")).toHaveTextContent(/^true$/);
			expect(statusOf(widgetKey)).toBe("error");
		});
	},
);

describe("a dashboard as it opens", () => {
	it.each(everyOwnerAndCategory)(
		"a %s's %s opens with every chart's frame in its place, each with a spinner, before any data arrives",
		(owner, _name, category) => {
			openTheDashboard(owner, category);

			expect(widgetsOnScreen()).toEqual(framedBeforeAnyData(category, owner));
			for (const widgetKey of framedBeforeAnyData(category, owner)) {
				expect(statusOf(widgetKey)).toBe("loading");
				expect(
					within(frameOf(widgetKey)).getByRole("progressbar"),
				).toBeInTheDocument();
			}
		},
	);

	it("a team's Flow Metrics opens with its ten charts framed in their places", () => {
		openTheDashboard("team", "flow-metrics");

		expect(widgetsOnScreen()).toEqual([
			"cycleScatter",
			"aging",
			"throughput",
			"wipOverTime",
			"totalWorkItemAgeOverTime",
			"arrivals",
			"stacked",
			"loadBalanceMatrix",
			"stateTimeCumulative",
			"blockedCountHistory",
		]);
	});

	it("a portfolio's Portfolio & Features opens with only the Work Item distribution framed", () => {
		openTheDashboard("portfolio", "portfolio");

		expect(widgetsOnScreen()).toEqual(["workDistribution"]);
		expect(statusOf("workDistribution")).toBe("loading");
	});

	async function addsAndRemovesNoChartOnceItsDataHasArrived(
		owner: Owner,
		_name: string,
		category: CategoryKey,
	) {
		openTheDashboard(owner, category);
		const atFirstSight = widgetsOnScreen();

		await everythingHasLoaded();

		expect(withoutThoseThatWaitForTheirData(widgetsOnScreen())).toEqual(
			atFirstSight,
		);
	}

	it.each(
		everyOwnerAndCategory.filter(([, , category]) => category !== "portfolio"),
	)(
		"a %s's %s adds and removes no chart once its data has arrived",
		addsAndRemovesNoChartOnceItsDataHasArrived,
	);

	it.each(
		everyOwnerAndCategory.filter(([, , category]) => category === "portfolio"),
	)(
		"a %s's %s adds and removes no chart once its data has arrived",
		addsAndRemovesNoChartOnceItsDataHasArrived,
	);

	it("a frame waiting for its first data shows its info button, with no rating or trend yet", () => {
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

	it.each([
		["Flow Efficiency", "flowEfficiency"],
		["Predictability Score", "predictabilityScore"],
		["Total Work Item Age", "totalWorkItemAge"],
	])(
		"%s waits under the same single spinner as every other chart",
		(_name, widgetKey) => {
			openTheDashboard("team", "flow-overview");

			expect(statusOf(widgetKey)).toBe("loading");
			expect(
				within(frameOf(widgetKey)).getAllByRole("progressbar"),
			).toHaveLength(1);
		},
	);

	it("opening a category not visited yet frames all of its charts at once", async () => {
		openTheDashboard("team", "flow-overview");
		await everythingHasLoaded();

		theReaderOpens("flow-metrics");

		expect(widgetsOnScreen()).toEqual(
			framedBeforeAnyData("flow-metrics", "team"),
		);
		const answeredOnFlowOverview = getFetchKeysForCategories(
			["flow-overview"],
			"team",
		);
		const asksForSomethingNew = (widgetKey: string) =>
			(getFetchRequirementsForWidget(widgetKey) ?? []).some(
				(fetchKey) => !answeredOnFlowOverview.has(fetchKey),
			);
		expectEveryChartToRead(
			chartsThePageFeedsOn("flow-metrics", "team").filter(asksForSomethingNew),
			"loading",
		);
		expect(statusOf("throughput")).toBe("ready");
	});

	it("a first visit whose data cannot be loaded says so in the chart's frame", async () => {
		openTheDashboard("team", "flow-metrics");

		await throughputFailsFor(LAST_30_DAYS);
		await act(async () => held.answer({ except: "getThroughput" }));

		expect(statusOf("throughput")).toBe("error");
		expect(
			within(frameOf("throughput")).getByText(COULD_NOT_LOAD_MESSAGE),
		).toBeInTheDocument();
	});
});

describe("charts that appear only once their data says to show them", () => {
	const notConfigured = () =>
		({
			...(usualAnswer(
				"getEstimationVsCycleTimeData",
				LAST_30_DAYS,
				[],
			) as object),
			status: "NotConfigured",
		}) as unknown;
	const isFramed = (widgetKey: string) =>
		screen.queryByTestId(`widget-shell-${widgetKey}`) !== null;

	it("Estimation vs. Cycle Time is not framed while its first answer is on its way", () => {
		openTheDashboard("portfolio", "portfolio");

		expect(isFramed("estimationVsCycleTime")).toBe(false);
	});

	it("Estimation vs. Cycle Time stays away once its answer says estimation is not set up", async () => {
		openTheDashboard("portfolio", "portfolio");

		await act(async () =>
			held.answer({ method: "getEstimationVsCycleTimeData" }, notConfigured()),
		);
		await everythingHasLoaded();

		expect(isFramed("estimationVsCycleTime")).toBe(false);
	});

	it("Estimation vs. Cycle Time stays away on a window change once it is known not to be set up", async () => {
		openTheDashboard("portfolio", "portfolio");
		await act(async () =>
			held.answer({ method: "getEstimationVsCycleTimeData" }, notConfigured()),
		);
		await everythingHasLoaded();

		theReaderPicks("Last 90 days");

		expect(isFramed("estimationVsCycleTime")).toBe(false);
	});

	it("Feature Size is not framed until the window's Features arrive, then takes its place", async () => {
		openTheDashboard("portfolio", "portfolio");
		expect(isFramed("featureSize")).toBe(false);

		await everythingHasLoaded();

		expect(isFramed("featureSize")).toBe(true);
	});

	it("Feature Size stays away when the window holds no Features", async () => {
		openTheDashboard("portfolio", "portfolio");

		await act(async () =>
			held.answer({ method: "getAllFeaturesForSizeChart" }, []),
		);
		await everythingHasLoaded();

		expect(isFramed("featureSize")).toBe(false);
	});

	const theirOwnRequests = [
		[
			"Estimation vs. Cycle Time",
			"estimationVsCycleTime",
			"getEstimationVsCycleTimeData",
		],
		["Feature Size", "featureSize", "getAllFeaturesForSizeChart"],
	] as const;

	function expectCouldNotLoadWithNoSpinner(widgetKey: string): void {
		expect(statusOf(widgetKey)).toBe("error");
		expect(
			within(frameOf(widgetKey)).getByText(COULD_NOT_LOAD_MESSAGE),
		).toBeInTheDocument();
		expect(
			within(frameOf(widgetKey)).queryByRole("progressbar"),
		).not.toBeInTheDocument();
	}

	it.each(theirOwnRequests)(
		"%s says it couldn't be loaded when its first answer fails, with no spinner",
		async (_name, widgetKey, method) => {
			openTheDashboard("portfolio", "portfolio");

			await act(async () => held.fail({ method }));
			await everythingHasLoaded();

			expectCouldNotLoadWithNoSpinner(widgetKey);
		},
	);

	it.each(theirOwnRequests)(
		"%s, once shown, says it couldn't be loaded when the next window's answer fails",
		async (_name, widgetKey, method) => {
			openTheDashboard("portfolio", "portfolio");
			await everythingHasLoaded();
			theReaderPicks("Last 90 days");

			await act(async () => held.fail({ method }));
			await everythingHasLoaded();

			expectCouldNotLoadWithNoSpinner(widgetKey);
		},
	);

	it.each([
		["Estimation vs. Cycle Time", "estimationVsCycleTime"],
		["Feature Size", "featureSize"],
	])(
		"%s, once shown, dims like every other chart when the reader picks another window",
		async (_name, widgetKey) => {
			openTheDashboard("portfolio", "portfolio");
			await everythingHasLoaded();

			theReaderPicks("Last 90 days");

			expect(statusOf(widgetKey)).toBe("loading");
		},
	);
});
