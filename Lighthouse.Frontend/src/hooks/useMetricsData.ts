import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useContext, useEffect, useMemo, useState } from "react";
import type { IBlackoutPeriod } from "../models/BlackoutPeriod";
import type { BlockedCountSnapshot } from "../models/BlockedCountSnapshot";
import type { IFeature } from "../models/Feature";
import type { IForecastPredictabilityScore } from "../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureOwner } from "../models/IFeatureOwner";
import type { ICumulativeStateTimeResponse } from "../models/Metrics/CumulativeStateTime";
import type { IEstimationVsCycleTimeResponse } from "../models/Metrics/EstimationVsCycleTimeData";
import type { IFeatureSizeEstimationResponse } from "../models/Metrics/FeatureSizeEstimationData";
import type { IFlowEfficiencyInfo } from "../models/Metrics/FlowEfficiencyInfo";
import type {
	IArrivalsInfo,
	ICycleTimePercentilesInfo,
	IFeatureSizePercentilesInfo,
	IFeaturesWorkedOnInfo,
	IPredictabilityScoreInfo,
	IThroughputInfo,
	ITotalWorkItemAgeInfo,
	IWipOverviewInfo,
} from "../models/Metrics/InfoWidgetData";
import type { ProcessBehaviourChartData } from "../models/Metrics/ProcessBehaviourChartData";
import type { RunChartData } from "../models/Metrics/RunChartData";
import type { ISleRisk } from "../models/Metrics/SleRisk";
import type { IPercentileValue } from "../models/PercentileValue";
import type { IPerStatePercentileValues } from "../models/PerStatePercentileValues";
import type { IPortfolio } from "../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import type { IWorkItem } from "../models/WorkItem";
import {
	getMetricsFetchKeys,
	type MetricsFetchKey,
} from "../pages/Common/MetricsView/categoryMetadata";
import {
	type FetchKeyStates,
	fetchKeyStateOf,
	type QueryProgress,
} from "../pages/Common/MetricsView/widgetStatus";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type {
	IMetricsService,
	IProjectMetricsService,
	ITeamMetricsService,
} from "../services/Api/MetricsService";
import { useTerminology } from "../services/TerminologyContext";
import { formatLocalDate, parseLocalDate } from "../utils/date/localDate";

const ONE_DAY_MS = 24 * 60 * 60 * 1000;

/**
 * Fetch-everything fallback for callers that do not scope their fetches (tests, and any view that
 * genuinely shows every widget at once). Frozen at module level so the default argument is one
 * stable identity rather than a fresh Set per render.
 */
const allMetricsFetchKeys: ReadonlySet<MetricsFetchKey> = new Set(
	getMetricsFetchKeys(),
);

/**
 * Every metrics request is asked fresh for the window on screen and forgotten once nothing shows
 * it: a dashboard answer is only true for the moment it was asked, so caching it, retrying it or
 * refetching it behind the reader's back would show numbers the reader did not ask for.
 */
export const metricsQueryOptions = {
	staleTime: 0,
	gcTime: 0,
	retry: false,
	refetchOnWindowFocus: false,
} as const;

export interface MetricsData<T> {
	blackoutPeriods: IBlackoutPeriod[];
	throughputData: RunChartData | null;
	wipOverTimeData: RunChartData | null;
	inProgressItems: IWorkItem[];
	blockedItems: IWorkItem[];
	cycleTimeData: T[];
	percentileValues: IPercentileValue[];
	workItemAgePercentilesValues: IPercentileValue[];
	/**
	 * The same snapshot read one period earlier (window ends the day before `startDate`), which is
	 * what the widget's previous-period trend compares against.
	 */
	previousWorkItemAgePercentilesValues: IPercentileValue[];
	perStatePercentileValues: IPerStatePercentileValues[];
	/**
	 * Empty for a portfolio and for a team that published no target — the two cases the risk column
	 * is meant to be absent in, which is why they are not told apart here.
	 */
	sleRiskValues: ISleRisk[];
	cumulativeStateTime: ICumulativeStateTimeResponse | null;
	/** Cumulative Time per State counted for the chosen Work Items only; null while none are chosen. */
	cumulativeStateTimeForSelection: ICumulativeStateTimeResponse | null;
	/** Cumulative Time per State counted for the chosen stretch only; null while none is chosen. */
	cumulativeStateTimeForScope: ICumulativeStateTimeResponse | null;
	sizePercentileValues: IPercentileValue[];
	allFeaturesForSizeChart: IFeature[];
	predictabilityData: IForecastPredictabilityScore | null;
	throughputPbcData: ProcessBehaviourChartData | null;
	wipPbcData: ProcessBehaviourChartData | null;
	totalWorkItemAgePbcData: ProcessBehaviourChartData | null;
	cycleTimePbcData: ProcessBehaviourChartData | null;
	featureSizePbcData: ProcessBehaviourChartData | null;
	estimationVsCycleTimeData: IEstimationVsCycleTimeResponse | null;
	featureSizeEstimationData: IFeatureSizeEstimationResponse | null;
	serviceLevelExpectation: IPercentileValue | null;
	featureSizeTarget: IPercentileValue | null;
	totalWorkItemAge: number | null;
	arrivalsData: RunChartData | null;
	arrivalsPbcData: ProcessBehaviourChartData | null;
	throughputInfo: IThroughputInfo | null;
	arrivalsInfo: IArrivalsInfo | null;
	featureSizePercentilesInfo: IFeatureSizePercentilesInfo | null;
	wipOverviewInfo: IWipOverviewInfo | null;
	featuresWorkedOnInfo: IFeaturesWorkedOnInfo | null;
	totalWorkItemAgeInfo: ITotalWorkItemAgeInfo | null;
	predictabilityScoreInfo: IPredictabilityScoreInfo | null;
	cycleTimePercentilesInfo: ICycleTimePercentilesInfo | null;
	flowEfficiencyInfo: IFlowEfficiencyInfo | null;
	blockedCountHistory: BlockedCountSnapshot[] | null;
	/** How far each request for the selected window has got, by the fetch key that asks for it. */
	fetchStates: FetchKeyStates;
	/** Asks the Throughput process behaviour chart again, for the filtered or the raw view. */
	setThroughputPbcView: (view: "raw" | "filtered") => void;
}

function isProjectMetricsService(
	service: object,
): service is IProjectMetricsService {
	return (
		"getAllFeaturesForSizeChart" in service &&
		"getSizePercentiles" in service &&
		"getFeatureSizePbc" in service &&
		"getFeatureSizeEstimation" in service &&
		"getFeatureSizePercentilesInfo" in service
	);
}

function isTeamMetricsService(service: object): service is ITeamMetricsService {
	return "getFeaturesWorkedOnInfo" in service;
}

// The risk is a team question — a feature can sit in several portfolios, each with its own target —
// so only the team service answers it, and its presence is what says which kind of service this is.
function providesSleRisk(service: object): service is ITeamMetricsService {
	return "getSleRisk" in service;
}

// Owner type is discriminated exactly as BaseMetricsView does it, on `getFeaturesInProgress`.
// Deliberately NOT isTeamMetricsService: that predicate keys off getFeaturesWorkedOnInfo, which
// portfolio-shaped services also expose, so it answers "does this service report features worked
// on", not "is this a team".
function isTeamOwnedMetricsService(service: object): boolean {
	return "getFeaturesInProgress" in service;
}

// Whether this owner has the question at all. A key that does not apply is never asked, and is
// never waited for either. Every key is listed, so a new one does not build until someone says
// which owners can ask it.
function applicabilityFor(
	service: object,
	choices: CumulativeStateTimeChoicesMade,
): Record<MetricsFetchKey, boolean> {
	const isPortfolioShaped = isProjectMetricsService(service);
	return {
		blackoutPeriods: true,
		predictability: true,
		totalWorkItemAge: true,
		throughput: true,
		inProgressItems: true,
		blockedItems: true,
		wipOverTime: true,
		cycleTimeData: true,
		cycleTimePercentiles: true,
		workItemAgePercentiles: true,
		ageInStatePercentiles: true,
		cumulativeStateTime: true,
		flowEfficiency: true,
		estimationVsCycleTime: true,
		arrivals: true,
		throughputInfo: true,
		arrivalsInfo: true,
		wipOverviewInfo: true,
		totalWorkItemAgeInfo: true,
		predictabilityScoreInfo: true,
		cycleTimePercentilesInfo: true,
		blockedCountHistory: true,
		pbcCore: true,
		pbcCharts: true,
		throughputPbc: true,
		sleRisk: providesSleRisk(service),
		featureSizeData: isPortfolioShaped,
		featureSizePbc: isPortfolioShaped,
		featureSizeEstimation: isPortfolioShaped,
		featureSizePercentilesInfo: isPortfolioShaped,
		featuresWorkedOnInfo: isTeamMetricsService(service),
		cumulativeStateTimeSelection: choices.selection,
		cumulativeStateTimeScope: choices.scope,
	};
}

/** How the reader narrowed Cumulative Time per State: to some Work Items, or to one stretch. */
export type CumulativeStateTimeChoices = {
	readonly itemIds: readonly number[];
	readonly scopeDefinitionId: number | null;
};

type CumulativeStateTimeChoicesMade = {
	readonly selection: boolean;
	readonly scope: boolean;
};

const noCumulativeStateTimeChoices: CumulativeStateTimeChoices = {
	itemIds: [],
	scopeDefinitionId: null,
};

type OwnerType = "team" | "portfolio";

/** Whose metrics a request asks for, as every metrics request key names it. */
export type MetricsOwnerKey = {
	readonly ownerId: number;
	readonly ownerUpdatedAt: number;
};

// An update recomputes the owner's metrics, so its moment is in the key: an open dashboard asks
// again once the update is done rather than keeping the answer from before it.
export function metricsOwnerKeyOf(
	owner: Pick<IFeatureOwner, "id" | "lastUpdated">,
): MetricsOwnerKey {
	return {
		ownerId: owner.id,
		ownerUpdatedAt: new Date(owner.lastUpdated).getTime(),
	};
}

type OwnerRequest = MetricsOwnerKey & { readonly ownerType: OwnerType };
type AsOfRequest = OwnerRequest & { readonly asOf: string };
type WindowRequest = OwnerRequest & {
	readonly from: string;
	readonly to: string;
};

function dayOf(localDay: string): Date {
	const day = parseLocalDate(localDay);
	if (day === null) {
		throw new TypeError(`${localDay} is not a calendar day.`);
	}
	return day;
}

type MetricsQuery<V> = {
	readonly progress: QueryProgress;
	/** The answer for the selected window or, while it has none, the previous window's answer. */
	readonly answer: V | null;
};

// The key holds everything the request sends, so an answer for a window the reader has already
// left lands under that window's key and is never read.
function useMetricsQuery<R extends OwnerRequest, V>(
	fetchName: string,
	request: R,
	ask: (request: R) => Promise<V | undefined> | V | undefined,
	enabled: boolean,
	failureMessage?: string,
): MetricsQuery<V> {
	const query = useQuery({
		...metricsQueryOptions,
		queryKey: ["metrics", fetchName, request] as const,
		queryFn: async ({ queryKey }) => {
			try {
				// An empty answer is an answer: undefined would read as still on its way.
				return (await ask(queryKey[2])) ?? null;
			} catch (error) {
				if (failureMessage) console.error(failureMessage, error);
				throw error;
			}
		},
		enabled,
		placeholderData: keepPreviousData,
	});

	return { progress: query, answer: query.data ?? null };
}

function listOf<V>(query: MetricsQuery<V[]>): V[] {
	return query.answer ?? [];
}

export function useMetricsData<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
>(
	entity: E,
	metricsService: IMetricsService<T>,
	startDate: Date,
	endDate: Date,
	activeFetchKeys: ReadonlySet<MetricsFetchKey> = allMetricsFetchKeys,
	cumulativeStateTimeChoices: CumulativeStateTimeChoices = noCumulativeStateTimeChoices,
): MetricsData<T> {
	const { blackoutPeriodService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const [serviceLevelExpectation, setServiceLevelExpectation] =
		useState<IPercentileValue | null>(null);
	const [featureSizeTarget, setFeatureSizeTarget] =
		useState<IPercentileValue | null>(null);
	const [throughputPbcView, setThroughputPbcView] = useState<
		"raw" | "filtered" | undefined
	>(undefined);

	const selectionChosen = cumulativeStateTimeChoices.itemIds.length > 0;
	const scopeChosen = cumulativeStateTimeChoices.scopeDefinitionId !== null;
	const applicable = useMemo(
		() =>
			applicabilityFor(metricsService, {
				selection: selectionChosen,
				scope: scopeChosen,
			}),
		[metricsService, selectionChosen, scopeChosen],
	);
	const isAsked = (key: MetricsFetchKey) =>
		activeFetchKeys.has(key) && applicable[key];

	const ownerType: OwnerType = isTeamOwnedMetricsService(metricsService)
		? "team"
		: "portfolio";
	const owner: OwnerRequest = { ownerType, ...metricsOwnerKeyOf(entity) };
	const selectedWindow: WindowRequest = {
		...owner,
		from: formatLocalDate(startDate),
		to: formatLocalDate(endDate),
	};
	const asOfEnd: AsOfRequest = { ...owner, asOf: selectedWindow.to };

	const blackoutPeriods = useMetricsQuery(
		"getAllBlackoutPeriods",
		owner,
		// Without the blackout periods a chart simply marks none, so failing to read them is not a
		// failed chart.
		async () => {
			try {
				return await blackoutPeriodService.getAll();
			} catch (error) {
				console.error("Error fetching blackout periods:", error);
				return [];
			}
		},
		isAsked("blackoutPeriods"),
	);

	const totalWorkItemAge = useMetricsQuery(
		"getTotalWorkItemAge",
		asOfEnd,
		({ ownerId, asOf }) =>
			metricsService.getTotalWorkItemAge(ownerId, dayOf(asOf)),
		isAsked("totalWorkItemAge"),
		"Error fetching total work item age:",
	);

	const throughput = useMetricsQuery(
		"getThroughput",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getThroughput(ownerId, dayOf(from), dayOf(to)),
		isAsked("throughput"),
		"Error getting throughput:",
	);

	const inProgressItems = useMetricsQuery(
		"getInProgressItems",
		asOfEnd,
		({ ownerId, asOf }) =>
			metricsService.getInProgressItems(ownerId, dayOf(asOf)),
		isAsked("inProgressItems"),
		`Error getting ${workItemsTerm} in progress:`,
	);

	// The blocked overview spans BOTH open state categories (To Do + In Progress) — an item can be
	// stuck in To Do because it is blocked — so it is sourced from the blocked-eligible endpoint,
	// not filtered out of the WIP (in-progress-only) set.
	const blockedItems = useMetricsQuery(
		"getBlockedItemsAtDate",
		asOfEnd,
		({ ownerId, asOf }) =>
			metricsService.getBlockedItemsAtDate(ownerId, dayOf(asOf)),
		isAsked("blockedItems"),
		`Error getting blocked ${workItemsTerm}:`,
	);

	const wipOverTime = useMetricsQuery(
		"getWorkInProgressOverTime",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getWorkInProgressOverTime(ownerId, dayOf(from), dayOf(to)),
		isAsked("wipOverTime"),
		`Error getting ${workItemsTerm} over time:`,
	);

	const cycleTimeFailure = `Error fetching ${cycleTimeTerm} data:`;

	const cycleTimeData = useMetricsQuery(
		"getCycleTimeData",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getCycleTimeData(ownerId, dayOf(from), dayOf(to)),
		isAsked("cycleTimeData"),
		cycleTimeFailure,
	);

	const cycleTimePercentiles = useMetricsQuery(
		"getCycleTimePercentiles",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getCycleTimePercentiles(ownerId, dayOf(from), dayOf(to)),
		isAsked("cycleTimePercentiles"),
		cycleTimeFailure,
	);

	const workItemAgePercentiles = useMetricsQuery(
		"getWorkItemAgePercentiles",
		{ ...selectedWindow, period: "selected" },
		({ ownerId, from, to }) =>
			metricsService.getWorkItemAgePercentiles(ownerId, dayOf(from), dayOf(to)),
		isAsked("workItemAgePercentiles"),
		cycleTimeFailure,
	);

	// The previous-period trend compares against the same window length, ending the day BEFORE the
	// selected range starts. The backend snapshots on the window's end date, so that boundary day is
	// what actually selects the comparison point.
	const previousWorkItemAgePercentiles = useMetricsQuery(
		"getWorkItemAgePercentiles",
		{ ...selectedWindow, period: "previous" },
		({ ownerId, from, to }) => {
			const selectedStart = dayOf(from);
			const previousPeriodEnd = new Date(selectedStart.getTime() - ONE_DAY_MS);
			const previousPeriodStart = new Date(
				previousPeriodEnd.getTime() -
					(dayOf(to).getTime() - selectedStart.getTime()),
			);
			return metricsService.getWorkItemAgePercentiles(
				ownerId,
				previousPeriodStart,
				previousPeriodEnd,
			);
		},
		isAsked("workItemAgePercentiles"),
		cycleTimeFailure,
	);

	const flowEfficiency = useMetricsQuery(
		"getFlowEfficiencyInfo",
		selectedWindow,
		({ ownerType: askedFor, ownerId, from, to }) =>
			askedFor === "team"
				? metricsService.getFlowEfficiencyInfoForTeam(
						ownerId,
						dayOf(from),
						dayOf(to),
					)
				: metricsService.getFlowEfficiencyInfoForPortfolio(
						ownerId,
						dayOf(from),
						dayOf(to),
					),
		isAsked("flowEfficiency"),
		cycleTimeFailure,
	);

	const ageInStatePercentiles = useMetricsQuery(
		"getAgeInStatePercentiles",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getAgeInStatePercentiles(ownerId, dayOf(from), dayOf(to)),
		isAsked("ageInStatePercentiles"),
		"Error fetching per-state percentiles:",
	);

	// No dates in this request. The risk is a claim about today over the team's own configured
	// history, so moving the range picker must not ask for it again.
	const sleRisk = useMetricsQuery(
		"getSleRisk",
		owner,
		({ ownerId }) =>
			(metricsService as unknown as ITeamMetricsService).getSleRisk(ownerId),
		isAsked("sleRisk"),
		"Error fetching SLE risk:",
	);

	const cumulativeStateTime = useMetricsQuery(
		"getCumulativeStateTime",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getCumulativeStateTimeForTeam(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("cumulativeStateTime"),
		"Error fetching cumulative state time:",
	);

	const cumulativeStateTimeSelection = useMetricsQuery(
		"getCumulativeStateTimeForItems",
		{ ...selectedWindow, itemIds: cumulativeStateTimeChoices.itemIds },
		({ ownerId, from, to, itemIds }) =>
			metricsService.getCumulativeStateTimeForTeam(
				ownerId,
				dayOf(from),
				dayOf(to),
				[...itemIds],
			),
		isAsked("cumulativeStateTimeSelection"),
		"Error fetching cumulative state time for the chosen work items:",
	);

	const cumulativeStateTimeScope = useMetricsQuery(
		"getCumulativeStateTimeForScope",
		{
			...selectedWindow,
			definitionId: cumulativeStateTimeChoices.scopeDefinitionId,
		},
		({ ownerId, from, to, definitionId }) =>
			metricsService.getCumulativeStateTimeForTeam(
				ownerId,
				dayOf(from),
				dayOf(to),
				undefined,
				definitionId ?? undefined,
			),
		isAsked("cumulativeStateTimeScope"),
		"Error fetching cumulative state time for the chosen stretch:",
	);

	const arrivals = useMetricsQuery(
		"getArrivals",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getArrivals(ownerId, dayOf(from), dayOf(to)),
		isAsked("arrivals"),
		"Error fetching arrivals data:",
	);

	// The blocked trend looks for its baseline the day before the window starts, while the backend
	// only returns days on or after the start it is given — so the history is asked from one day
	// earlier, or the trend never finds its baseline.
	const blockedCountHistory = useMetricsQuery(
		"getBlockedCountHistory",
		selectedWindow,
		({ ownerId, from, to }) => {
			const baselineStart = dayOf(from);
			baselineStart.setDate(baselineStart.getDate() - 1);
			return metricsService.getBlockedCountHistory(
				ownerId,
				baselineStart,
				dayOf(to),
			);
		},
		isAsked("blockedCountHistory"),
		"Error fetching blocked count history:",
	);

	const coreProcessBehaviourFailure =
		"Error fetching core process behaviour chart data:";

	const wipPbc = useMetricsQuery(
		"getWipPbc",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getWipPbc(ownerId, dayOf(from), dayOf(to)),
		isAsked("pbcCore"),
		coreProcessBehaviourFailure,
	);

	const totalWorkItemAgePbc = useMetricsQuery(
		"getTotalWorkItemAgePbc",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getTotalWorkItemAgePbc(ownerId, dayOf(from), dayOf(to)),
		isAsked("pbcCore"),
		coreProcessBehaviourFailure,
	);

	const predictability = useMetricsQuery(
		"getMultiItemForecastPredictabilityScore",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getMultiItemForecastPredictabilityScore(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("predictability"),
		"Error fetching predictability data:",
	);

	// Only a portfolio is asked these, which is what `applicable` already says.
	const portfolioService = metricsService as unknown as IProjectMetricsService;
	const sizePercentileFailure = "Error fetching Size Percentile Data:";

	const sizePercentiles = useMetricsQuery(
		"getSizePercentiles",
		selectedWindow,
		({ ownerId, from, to }) =>
			portfolioService.getSizePercentiles(ownerId, dayOf(from), dayOf(to)),
		isAsked("featureSizeData"),
		sizePercentileFailure,
	);

	const featuresForSizeChart = useMetricsQuery(
		"getAllFeaturesForSizeChart",
		selectedWindow,
		({ ownerId, from, to }) =>
			portfolioService.getAllFeaturesForSizeChart(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("featureSizeData"),
		sizePercentileFailure,
	);

	const featureSizePbc = useMetricsQuery(
		"getFeatureSizePbc",
		selectedWindow,
		({ ownerId, from, to }) =>
			portfolioService.getFeatureSizePbc(ownerId, dayOf(from), dayOf(to)),
		isAsked("featureSizePbc"),
		"Error fetching feature size PBC data:",
	);

	const featureSizeEstimation = useMetricsQuery(
		"getFeatureSizeEstimation",
		selectedWindow,
		({ ownerId, from, to }) =>
			portfolioService.getFeatureSizeEstimation(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("featureSizeEstimation"),
		"Error fetching feature size estimation data:",
	);

	const featureSizePercentilesInfo = useMetricsQuery(
		"getFeatureSizePercentilesInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			portfolioService.getFeatureSizePercentilesInfo(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("featureSizePercentilesInfo"),
		"Error fetching feature size percentiles info:",
	);

	useEffect(() => {
		if (
			entity.serviceLevelExpectationProbability > 0 &&
			entity.serviceLevelExpectationRange > 0
		) {
			setServiceLevelExpectation({
				value: entity.serviceLevelExpectationRange,
				percentile: entity.serviceLevelExpectationProbability,
			});
		}

		if (entity as unknown as IPortfolio) {
			const portfolio = entity as unknown as IPortfolio;
			if (
				portfolio.featureSizeTargetProbability &&
				portfolio.featureSizeTargetRange
			) {
				setFeatureSizeTarget({
					percentile: portfolio.featureSizeTargetProbability,
					value: portfolio.featureSizeTargetRange,
				});
			}
		}
	}, [entity]);

	const estimationVsCycleTime = useMetricsQuery(
		"getEstimationVsCycleTimeData",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getEstimationVsCycleTimeData(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("estimationVsCycleTime"),
		"Error fetching estimation vs cycle time data:",
	);

	const throughputInfo = useMetricsQuery(
		"getThroughputInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getThroughputInfo(ownerId, dayOf(from), dayOf(to)),
		isAsked("throughputInfo"),
		"Error fetching throughput info:",
	);

	const arrivalsInfo = useMetricsQuery(
		"getArrivalsInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getArrivalsInfo(ownerId, dayOf(from), dayOf(to)),
		isAsked("arrivalsInfo"),
		"Error fetching arrivals info:",
	);

	const wipOverviewInfo = useMetricsQuery(
		"getWipOverviewInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getWipOverviewInfo(ownerId, dayOf(from), dayOf(to)),
		isAsked("wipOverviewInfo"),
		"Error fetching WIP overview info:",
	);

	const totalWorkItemAgeInfo = useMetricsQuery(
		"getTotalWorkItemAgeInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getTotalWorkItemAgeInfo(ownerId, dayOf(from), dayOf(to)),
		isAsked("totalWorkItemAgeInfo"),
		"Error fetching total work item age info:",
	);

	const predictabilityScoreInfo = useMetricsQuery(
		"getPredictabilityScoreInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getPredictabilityScoreInfo(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("predictabilityScoreInfo"),
		"Error fetching predictability score info:",
	);

	const cycleTimePercentilesInfo = useMetricsQuery(
		"getCycleTimePercentilesInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getCycleTimePercentilesInfo(
				ownerId,
				dayOf(from),
				dayOf(to),
			),
		isAsked("cycleTimePercentilesInfo"),
		"Error fetching cycle time percentiles info:",
	);

	const featuresWorkedOnInfo = useMetricsQuery(
		"getFeaturesWorkedOnInfo",
		selectedWindow,
		({ ownerId, from, to }) =>
			(
				metricsService as unknown as ITeamMetricsService
			).getFeaturesWorkedOnInfo(ownerId, dayOf(from), dayOf(to)),
		isAsked("featuresWorkedOnInfo"),
		"Error fetching features worked on info:",
	);

	const processBehaviourFailure =
		"Error fetching process behaviour chart data:";

	const cycleTimePbc = useMetricsQuery(
		"getCycleTimePbc",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getCycleTimePbc(ownerId, dayOf(from), dayOf(to)),
		isAsked("pbcCharts"),
		processBehaviourFailure,
	);

	const arrivalsPbc = useMetricsQuery(
		"getArrivalsPbc",
		selectedWindow,
		({ ownerId, from, to }) =>
			metricsService.getArrivalsPbc(ownerId, dayOf(from), dayOf(to)),
		isAsked("pbcCharts"),
		processBehaviourFailure,
	);

	// The filter is part of the request, so flipping it asks again for this chart alone, and an
	// answer for the view the reader has just left never lands.
	const throughputPbc = useMetricsQuery(
		"getThroughputPbc",
		{ ...selectedWindow, view: throughputPbcView },
		({ ownerId, from, to, view }) =>
			view === undefined
				? metricsService.getThroughputPbc(ownerId, dayOf(from), dayOf(to))
				: metricsService.getThroughputPbc(
						ownerId,
						dayOf(from),
						dayOf(to),
						view,
					),
		isAsked("throughputPbc"),
		processBehaviourFailure,
	);
	const queriesByFetchKey: Record<MetricsFetchKey, readonly QueryProgress[]> = {
		blackoutPeriods: [blackoutPeriods.progress],
		totalWorkItemAge: [totalWorkItemAge.progress],
		throughput: [throughput.progress],
		inProgressItems: [inProgressItems.progress],
		blockedItems: [blockedItems.progress],
		wipOverTime: [wipOverTime.progress],
		cycleTimeData: [cycleTimeData.progress],
		cycleTimePercentiles: [cycleTimePercentiles.progress],
		workItemAgePercentiles: [
			workItemAgePercentiles.progress,
			previousWorkItemAgePercentiles.progress,
		],
		flowEfficiency: [flowEfficiency.progress],
		ageInStatePercentiles: [ageInStatePercentiles.progress],
		sleRisk: [sleRisk.progress],
		cumulativeStateTime: [cumulativeStateTime.progress],
		cumulativeStateTimeSelection: [cumulativeStateTimeSelection.progress],
		cumulativeStateTimeScope: [cumulativeStateTimeScope.progress],
		arrivals: [arrivals.progress],
		blockedCountHistory: [blockedCountHistory.progress],
		pbcCore: [wipPbc.progress, totalWorkItemAgePbc.progress],
		predictability: [predictability.progress],
		featureSizeData: [sizePercentiles.progress, featuresForSizeChart.progress],
		featureSizePbc: [featureSizePbc.progress],
		featureSizeEstimation: [featureSizeEstimation.progress],
		featureSizePercentilesInfo: [featureSizePercentilesInfo.progress],
		estimationVsCycleTime: [estimationVsCycleTime.progress],
		throughputInfo: [throughputInfo.progress],
		arrivalsInfo: [arrivalsInfo.progress],
		wipOverviewInfo: [wipOverviewInfo.progress],
		totalWorkItemAgeInfo: [totalWorkItemAgeInfo.progress],
		predictabilityScoreInfo: [predictabilityScoreInfo.progress],
		cycleTimePercentilesInfo: [cycleTimePercentilesInfo.progress],
		featuresWorkedOnInfo: [featuresWorkedOnInfo.progress],
		pbcCharts: [cycleTimePbc.progress, arrivalsPbc.progress],
		throughputPbc: [throughputPbc.progress],
	};
	const fetchStates: FetchKeyStates = Object.fromEntries(
		Object.entries(queriesByFetchKey).map(([key, queries]) => [
			key,
			fetchKeyStateOf(queries, applicable[key as MetricsFetchKey]),
		]),
	);

	return {
		blackoutPeriods: listOf(blackoutPeriods),
		throughputData: throughput.answer,
		wipOverTimeData: wipOverTime.answer,
		inProgressItems: listOf(inProgressItems),
		blockedItems: listOf(blockedItems),
		cycleTimeData: listOf(cycleTimeData),
		percentileValues: listOf(cycleTimePercentiles),
		workItemAgePercentilesValues: listOf(workItemAgePercentiles),
		previousWorkItemAgePercentilesValues: listOf(
			previousWorkItemAgePercentiles,
		),
		perStatePercentileValues: listOf(ageInStatePercentiles),
		sleRiskValues: listOf(sleRisk),
		cumulativeStateTime: cumulativeStateTime.answer,
		// A choice the reader has let go of must not keep showing what it last counted.
		cumulativeStateTimeForSelection: selectionChosen
			? cumulativeStateTimeSelection.answer
			: null,
		cumulativeStateTimeForScope: scopeChosen
			? cumulativeStateTimeScope.answer
			: null,
		sizePercentileValues: listOf(sizePercentiles),
		allFeaturesForSizeChart: listOf(featuresForSizeChart),
		predictabilityData: predictability.answer,
		throughputPbcData: throughputPbc.answer,
		wipPbcData: wipPbc.answer,
		totalWorkItemAgePbcData: totalWorkItemAgePbc.answer,
		cycleTimePbcData: cycleTimePbc.answer,
		featureSizePbcData: featureSizePbc.answer,
		estimationVsCycleTimeData: estimationVsCycleTime.answer,
		featureSizeEstimationData: featureSizeEstimation.answer,
		serviceLevelExpectation,
		featureSizeTarget,
		totalWorkItemAge: totalWorkItemAge.answer,
		arrivalsData: arrivals.answer,
		arrivalsPbcData: arrivalsPbc.answer,
		throughputInfo: throughputInfo.answer,
		arrivalsInfo: arrivalsInfo.answer,
		featureSizePercentilesInfo: featureSizePercentilesInfo.answer,
		wipOverviewInfo: wipOverviewInfo.answer,
		featuresWorkedOnInfo: featuresWorkedOnInfo.answer,
		totalWorkItemAgeInfo: totalWorkItemAgeInfo.answer,
		predictabilityScoreInfo: predictabilityScoreInfo.answer,
		cycleTimePercentilesInfo: cycleTimePercentilesInfo.answer,
		flowEfficiencyInfo: flowEfficiency.answer,
		blockedCountHistory: blockedCountHistory.answer,
		fetchStates,
		setThroughputPbcView,
	};
}
