import { differenceInCalendarDays } from "date-fns";
import { vi } from "vitest";
import { ForecastPredictabilityScore } from "../models/Forecasts/ForecastPredictabilityScore";
import type { ProcessBehaviourChartData } from "../models/Metrics/ProcessBehaviourChartData";
import { RunChartData } from "../models/Metrics/RunChartData";
import type { IPercentileValue } from "../models/PercentileValue";
import type { IWorkItem, StateCategory } from "../models/WorkItem";
import type {
	IProjectMetricsService,
	ITeamMetricsService,
} from "../services/Api/MetricsService";
import { generateWorkItemMapForRunChart } from "./TestDataProvider";

/**
 * A metrics service whose every answer is held until the test settles it by hand, so a
 * test decides which window answers first, which answers last, and which never answers.
 *
 * Every call is recorded with the window it asked for. A window is named the way a reader
 * of the dashboard would describe it: how many days long it is, and how many days ago it
 * ends. `windowOf(90)` is "the last 90 days"; `windowOf(30, 7)` is the 30-day window that
 * ended a week ago.
 */

type Deferred<T> = {
	readonly promise: Promise<T>;
	readonly resolve: (value: T) => void;
	readonly reject: (reason: unknown) => void;
};

/** `Promise.withResolvers` is ES2024; the frontend targets ES2021. */
export function deferred<T>(): Deferred<T> {
	let resolve: (value: T) => void = () => undefined;
	let reject: (reason: unknown) => void = () => undefined;
	const promise = new Promise<T>((res, rej) => {
		resolve = res;
		reject = rej;
	});
	return { promise, resolve, reject };
}

export function windowOf(lengthInDays: number, endsDaysAgo = 0): string {
	return `${lengthInDays} days ending ${endsDaysAgo} days ago`;
}

function asOf(daysAgo: number): string {
	return `as of ${daysAgo} days ago`;
}

function describeWindow(args: readonly unknown[]): string {
	const dates = args.filter((arg): arg is Date => arg instanceof Date);
	const today = new Date();
	if (dates.length >= 2) {
		return windowOf(
			differenceInCalendarDays(dates[1], dates[0]),
			differenceInCalendarDays(today, dates[1]),
		);
	}
	if (dates.length === 1) {
		return asOf(differenceInCalendarDays(today, dates[0]));
	}
	return "no window";
}

function daysIn(window: string): number {
	return Number.parseInt(window, 10) || 0;
}

export type MetricsMethod =
	| keyof ITeamMetricsService
	| keyof IProjectMetricsService;

export type HeldCall = {
	readonly method: MetricsMethod;
	readonly args: readonly unknown[];
	readonly window: string;
	settled: boolean;
	readonly answer: Deferred<unknown>;
};

const sharedMethods = [
	"getThroughput",
	"getWorkInProgressOverTime",
	"getInProgressItems",
	"getCycleTimeData",
	"getCycleTimePercentiles",
	"getWorkItemAgePercentiles",
	"getAgeInStatePercentiles",
	"getCumulativeStateTimeForTeam",
	"getCumulativeStateTimeItemsForTeam",
	"getCumulativeStateTimeItemsForPortfolio",
	"getCumulativeStateTimeCandidatesForTeam",
	"getCumulativeStateTimeCandidatesForPortfolio",
	"getMultiItemForecastPredictabilityScore",
	"getTotalWorkItemAge",
	"getThroughputPbc",
	"getWipPbc",
	"getTotalWorkItemAgePbc",
	"getCycleTimePbc",
	"getEstimationVsCycleTimeData",
	"getArrivals",
	"getArrivalsPbc",
	"getThroughputInfo",
	"getArrivalsInfo",
	"getWipOverviewInfo",
	"getTotalWorkItemAgeInfo",
	"getPredictabilityScoreInfo",
	"getCycleTimePercentilesInfo",
	"getBlockedCountHistory",
	"getPercentilesOverTime",
	"getProcessBehaviorOverTime",
	"getBlockedItemsAtDate",
	"getFlowEfficiencyInfoForTeam",
	"getFlowEfficiencyInfoForPortfolio",
] as const satisfies readonly MetricsMethod[];

const teamOnlyMethods = [
	"getFeaturesInProgress",
	"getSleRisk",
	"getForecastInputCandidates",
	"getFeaturesWorkedOnInfo",
] as const satisfies readonly MetricsMethod[];

const portfolioOnlyMethods = [
	"getSizePercentiles",
	"getAllFeaturesForSizeChart",
	"getFeatureSizePbc",
	"getFeatureSizeEstimation",
	"getFeatureSizePercentilesInfo",
] as const satisfies readonly MetricsMethod[];

const workItem = (id: number, state: string, category: string): IWorkItem => ({
	id,
	name: `Item ${id}`,
	state,
	stateCategory: category as StateCategory,
	type: "Story",
	referenceId: `ITEM-${id}`,
	url: `https://example.com/work/${id}`,
	startedDate: new Date("2026-01-01"),
	closedDate: new Date("2026-01-10"),
	cycleTime: 9,
	workItemAge: 9,
	parentWorkItemReference: "",
	isBlocked: false,
});

const inProgressItems = [
	workItem(1, "In Progress", "Doing"),
	workItem(2, "In Progress", "Doing"),
];
const closedItems = [workItem(3, "Done", "Done"), workItem(4, "Done", "Done")];

const percentiles: IPercentileValue[] = [
	{ percentile: 50, value: 9 },
	{ percentile: 85, value: 12 },
	{ percentile: 95, value: 15 },
];

const baselineMissing: ProcessBehaviourChartData = {
	status: "BaselineMissing",
	statusReason: "No Baseline Configured",
	xAxisKind: "Date",
	average: 0,
	upperNaturalProcessLimit: 0,
	lowerNaturalProcessLimit: 0,
	baselineConfigured: false,
	dataPoints: [],
};

const noComparison = (metricLabel: string) => ({
	direction: "none",
	metricLabel,
});

/** One run-chart total per window: the last 90 days answer with a total of 90. */
function runChartFor(window: string): RunChartData {
	return new RunChartData(
		generateWorkItemMapForRunChart([1, 2]),
		2,
		daysIn(window),
	);
}

function cumulativeStateTimeFor(
	window: string,
	args: readonly unknown[],
): unknown {
	const itemIds = args[3] as number[] | undefined;
	const definitionId = args[4] as number | undefined;
	let scope = "every Work Item";
	if (itemIds && itemIds.length > 0) {
		scope = `Work Items ${itemIds.join("+")}`;
	} else if (definitionId !== undefined) {
		scope = `scope ${definitionId}`;
	}
	return {
		states: [
			{
				state: `${scope}, ${window}`,
				workflowOrder: 0,
				totalDays: 10,
				completedContributionDays: 5,
				ongoingContributionDays: 5,
				itemCount: 2,
				completedItemCount: 1,
				ongoingItemCount: 1,
				meanDays: 5,
				medianDays: 5,
			},
		],
	};
}

export function usualAnswer(
	method: MetricsMethod,
	window: string,
	args: readonly unknown[],
): unknown {
	switch (method) {
		case "getThroughput":
		case "getArrivals":
		case "getWorkInProgressOverTime":
			return runChartFor(window);
		case "getInProgressItems":
			return inProgressItems;
		case "getCycleTimeData":
			return closedItems;
		case "getCycleTimePercentiles":
		case "getWorkItemAgePercentiles":
		case "getSizePercentiles":
			return percentiles;
		case "getMultiItemForecastPredictabilityScore":
			return new ForecastPredictabilityScore(percentiles, 0.73, new Map());
		case "getTotalWorkItemAge":
			return 150;
		case "getThroughputPbc":
		case "getWipPbc":
		case "getTotalWorkItemAgePbc":
		case "getCycleTimePbc":
		case "getArrivalsPbc":
		case "getFeatureSizePbc":
			return baselineMissing;
		case "getEstimationVsCycleTimeData":
			return {
				status: "Ready",
				diagnostics: {
					totalCount: 0,
					mappedCount: 0,
					unmappedCount: 0,
					invalidCount: 0,
				},
				estimationUnit: null,
				useNonNumericEstimation: false,
				categoryValues: [],
				dataPoints: [],
			};
		case "getThroughputInfo":
			return {
				total: daysIn(window),
				dailyAverage: 1,
				comparison: noComparison("Total Throughput"),
			};
		case "getArrivalsInfo":
			return {
				total: daysIn(window),
				dailyAverage: 1,
				comparison: noComparison("Total Arrivals"),
			};
		case "getWipOverviewInfo":
			return { count: 2, comparison: noComparison("WIP") };
		case "getTotalWorkItemAgeInfo":
			return { totalAge: 150, comparison: noComparison("Total Work Item Age") };
		case "getPredictabilityScoreInfo":
			return { score: 0.73, comparison: noComparison("Predictability Score") };
		case "getCycleTimePercentilesInfo":
			return {
				percentiles: [],
				comparison: noComparison("Cycle Time Percentiles"),
			};
		case "getFeaturesWorkedOnInfo":
			return {
				count: 0,
				comparison: noComparison("Features Being Worked On"),
			};
		case "getFeatureSizePercentilesInfo":
			return {
				percentiles: [],
				comparison: noComparison("Feature Size Percentiles"),
			};
		case "getFeatureSizeEstimation":
			return {
				status: "NotConfigured",
				estimationUnit: null,
				useNonNumericEstimation: false,
				categoryValues: [],
				featureEstimations: [],
			};
		case "getAllFeaturesForSizeChart":
			return closedItems.map((item) => ({ ...item, size: 5 }));
		case "getFlowEfficiencyInfoForTeam":
		case "getFlowEfficiencyInfoForPortfolio":
			return {
				isConfigured: true,
				hasDataInScope: true,
				efficiencyPercent: 40,
				totalDoingDays: 10,
				waitDays: 6,
			};
		case "getCumulativeStateTimeForTeam":
			return cumulativeStateTimeFor(window, args);
		case "getCumulativeStateTimeCandidatesForTeam":
		case "getCumulativeStateTimeCandidatesForPortfolio":
			return {
				items: [
					{
						workItemId: 11,
						referenceId: `ITEM-11 (${window})`,
						title: "Item 11",
						workItemType: "Story",
					},
				],
			};
		case "getCumulativeStateTimeItemsForTeam":
		case "getCumulativeStateTimeItemsForPortfolio":
			return { state: "", items: [] };
		default:
			return [];
	}
}

export type HeldMetricsService = {
	readonly service: ITeamMetricsService | IProjectMetricsService;
	readonly calls: readonly HeldCall[];
	/** The calls still waiting for an answer, optionally narrowed to one method and/or window. */
	pending(filter?: CallFilter): HeldCall[];
	/** Answers every matching call still waiting, with the usual answer unless one is given. */
	answer(filter?: CallFilter, value?: unknown): void;
	/** Fails every matching call still waiting. */
	fail(filter?: CallFilter): void;
};

export type CallFilter = {
	readonly method?: MetricsMethod;
	readonly window?: string;
	readonly except?: MetricsMethod;
	/** Only the calls that asked for the forecast-filtered series. */
	readonly filtered?: boolean;
};

function matches(call: HeldCall, filter: CallFilter): boolean {
	if (call.settled) return false;
	if (filter.method !== undefined && call.method !== filter.method)
		return false;
	if (filter.except !== undefined && call.method === filter.except)
		return false;
	if (filter.window !== undefined && call.window !== filter.window)
		return false;
	if (filter.filtered !== undefined) {
		const asksForFiltered = call.args.includes("filtered");
		if (asksForFiltered !== filter.filtered) return false;
	}
	return true;
}

export function createHeldMetricsService(
	ownerType: "team" | "portfolio",
): HeldMetricsService {
	const calls: HeldCall[] = [];
	const methods: readonly MetricsMethod[] = [
		...sharedMethods,
		...(ownerType === "team" ? teamOnlyMethods : portfolioOnlyMethods),
	];

	const service: Record<string, unknown> = {};
	for (const method of methods) {
		service[method] = vi.fn((...args: unknown[]) => {
			const call: HeldCall = {
				method,
				args,
				window: describeWindow(args),
				settled: false,
				answer: deferred<unknown>(),
			};
			calls.push(call);
			return call.answer.promise;
		});
	}

	const pending = (filter: CallFilter = {}) =>
		calls.filter((call) => matches(call, filter));

	return {
		service: service as unknown as ITeamMetricsService | IProjectMetricsService,
		calls,
		pending,
		answer(filter = {}, value) {
			for (const call of pending(filter)) {
				call.settled = true;
				call.answer.resolve(
					value === undefined
						? usualAnswer(call.method, call.window, call.args)
						: value,
				);
			}
		},
		fail(filter = {}) {
			for (const call of pending(filter)) {
				call.settled = true;
				call.answer.reject(new Error(`${call.method} failed`));
			}
		},
	};
}
