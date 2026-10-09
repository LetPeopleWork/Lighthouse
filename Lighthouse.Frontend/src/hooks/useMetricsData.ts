import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
	useCallback,
	useContext,
	useEffect,
	useMemo,
	useRef,
	useState,
} from "react";
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
	refetchThroughputPbc: (view?: "raw" | "filtered") => Promise<void>;
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

const everyFetchKeyApplies = Object.fromEntries(
	getMetricsFetchKeys().map((key) => [key, true]),
) as Record<MetricsFetchKey, boolean>;

// Whether this owner has the question at all. A key that does not apply is never asked, and is
// never waited for either.
function applicabilityFor(service: object): Record<MetricsFetchKey, boolean> {
	const isPortfolioShaped = isProjectMetricsService(service);
	return {
		...everyFetchKeyApplies,
		sleRisk: providesSleRisk(service),
		featureSizeData: isPortfolioShaped,
		featureSizePbc: isPortfolioShaped,
		featureSizeEstimation: isPortfolioShaped,
		featureSizePercentilesInfo: isPortfolioShaped,
		featuresWorkedOnInfo: isTeamMetricsService(service),
	};
}

type OwnerType = "team" | "portfolio";

type OwnerRequest = { readonly ownerType: OwnerType; readonly ownerId: number };
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
	/** The answer for the selected window or, while it has none, the last one for this owner. */
	readonly answer: V | null;
};

// The key holds everything the request sends, so an answer for a window the reader has already
// left lands under that window's key and is never read. A window that fails keeps showing the
// last good answer, but never one that belongs to the owner shown before.
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

	const owner = `${request.ownerType}:${request.ownerId}`;
	const lastGood = useRef<{ owner: string; answer: V | null } | null>(null);
	if (lastGood.current?.owner !== owner) lastGood.current = null;
	if (query.data !== undefined && !query.isPlaceholderData) {
		lastGood.current = { owner, answer: query.data };
	}

	return {
		progress: query,
		answer:
			query.data === undefined
				? (lastGood.current?.answer ?? null)
				: query.data,
	};
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
): MetricsData<T> {
	const { blackoutPeriodService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const [sizePercentileValues, setSizePercentileValues] = useState<
		IPercentileValue[]
	>([]);
	const [allFeaturesForSizeChart, setAllFeaturesForSizeChart] = useState<
		IFeature[]
	>([]);
	const [predictabilityData, setPredictabilityData] =
		useState<IForecastPredictabilityScore | null>(null);
	const [throughputPbcData, setThroughputPbcData] =
		useState<ProcessBehaviourChartData | null>(null);
	const [cycleTimePbcData, setCycleTimePbcData] =
		useState<ProcessBehaviourChartData | null>(null);
	const [featureSizePbcData, setFeatureSizePbcData] =
		useState<ProcessBehaviourChartData | null>(null);
	const [estimationVsCycleTimeData, setEstimationVsCycleTimeData] =
		useState<IEstimationVsCycleTimeResponse | null>(null);
	const [featureSizeEstimationData, setFeatureSizeEstimationData] =
		useState<IFeatureSizeEstimationResponse | null>(null);
	const [serviceLevelExpectation, setServiceLevelExpectation] =
		useState<IPercentileValue | null>(null);
	const [featureSizeTarget, setFeatureSizeTarget] =
		useState<IPercentileValue | null>(null);
	const [arrivalsPbcData, setArrivalsPbcData] =
		useState<ProcessBehaviourChartData | null>(null);
	const [throughputInfo, setThroughputInfo] = useState<IThroughputInfo | null>(
		null,
	);
	const [arrivalsInfo, setArrivalsInfo] = useState<IArrivalsInfo | null>(null);
	const [featureSizePercentilesInfo, setFeatureSizePercentilesInfo] =
		useState<IFeatureSizePercentilesInfo | null>(null);
	const [wipOverviewInfo, setWipOverviewInfo] =
		useState<IWipOverviewInfo | null>(null);
	const [featuresWorkedOnInfo, setFeaturesWorkedOnInfo] =
		useState<IFeaturesWorkedOnInfo | null>(null);
	const [totalWorkItemAgeInfo, setTotalWorkItemAgeInfo] =
		useState<ITotalWorkItemAgeInfo | null>(null);
	const [predictabilityScoreInfo, setPredictabilityScoreInfo] =
		useState<IPredictabilityScoreInfo | null>(null);
	const [cycleTimePercentilesInfo, setCycleTimePercentilesInfo] =
		useState<ICycleTimePercentilesInfo | null>(null);
	// One primitive per fetch key. Primitives are compared by value, so an effect listing its own
	// flag re-runs only when that flag flips — never because a caller handed us a new Set with the
	// same contents. Callers grow the key set monotonically within an (entity, window), which makes
	// false→true happen at most once and therefore fetches at most once, with no refs or cache.
	const needsPredictability = activeFetchKeys.has("predictability");
	const needsFeatureSizeData = activeFetchKeys.has("featureSizeData");
	const needsFeatureSizePbc = activeFetchKeys.has("featureSizePbc");
	const needsFeatureSizeEstimation = activeFetchKeys.has(
		"featureSizeEstimation",
	);
	const needsFeatureSizePercentilesInfo = activeFetchKeys.has(
		"featureSizePercentilesInfo",
	);
	const needsEstimationVsCycleTime = activeFetchKeys.has(
		"estimationVsCycleTime",
	);
	const needsThroughputInfo = activeFetchKeys.has("throughputInfo");
	const needsArrivalsInfo = activeFetchKeys.has("arrivalsInfo");
	const needsWipOverviewInfo = activeFetchKeys.has("wipOverviewInfo");
	const needsTotalWorkItemAgeInfo = activeFetchKeys.has("totalWorkItemAgeInfo");
	const needsPredictabilityScoreInfo = activeFetchKeys.has(
		"predictabilityScoreInfo",
	);
	const needsCycleTimePercentilesInfo = activeFetchKeys.has(
		"cycleTimePercentilesInfo",
	);
	const needsFeaturesWorkedOnInfo = activeFetchKeys.has("featuresWorkedOnInfo");
	const needsPbcCharts = activeFetchKeys.has("pbcCharts");

	const applicable = useMemo(
		() => applicabilityFor(metricsService),
		[metricsService],
	);
	const isAsked = (key: MetricsFetchKey) =>
		activeFetchKeys.has(key) && applicable[key];

	const ownerType: OwnerType = isTeamOwnedMetricsService(metricsService)
		? "team"
		: "portfolio";
	const owner: OwnerRequest = { ownerType, ownerId: entity.id };
	const selectedWindow: WindowRequest = {
		...owner,
		from: formatLocalDate(startDate),
		to: formatLocalDate(endDate),
	};
	const asOfEnd: AsOfRequest = { ...owner, asOf: selectedWindow.to };

	const blackoutPeriods = useMetricsQuery(
		"getAllBlackoutPeriods",
		owner,
		() => blackoutPeriodService.getAll(),
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

	const queriesByFetchKey: Partial<
		Record<MetricsFetchKey, readonly QueryProgress[]>
	> = {
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
		arrivals: [arrivals.progress],
		blockedCountHistory: [blockedCountHistory.progress],
		pbcCore: [wipPbc.progress, totalWorkItemAgePbc.progress],
	};
	const fetchStates: FetchKeyStates = Object.fromEntries(
		Object.entries(queriesByFetchKey).map(([key, queries]) => [
			key,
			fetchKeyStateOf(queries, applicable[key as MetricsFetchKey]),
		]),
	);

	useEffect(() => {
		if (!needsPredictability) return;
		metricsService
			.getMultiItemForecastPredictabilityScore(entity.id, startDate, endDate)
			.then(setPredictabilityData)
			.catch((error) =>
				console.error("Error fetching predictability data:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsPredictability]);

	useEffect(() => {
		if (!needsFeatureSizeData) return;
		if (!isProjectMetricsService(metricsService)) return;
		const svc = metricsService;
		const fetch = async () => {
			const [percentiles, features] = await Promise.all([
				svc.getSizePercentiles(entity.id, startDate, endDate),
				svc.getAllFeaturesForSizeChart(entity.id, startDate, endDate),
			]);
			setSizePercentileValues(percentiles);
			setAllFeaturesForSizeChart(features);
		};
		fetch().catch((error) =>
			console.error("Error fetching Size Percentile Data:", error),
		);
	}, [metricsService, entity, startDate, endDate, needsFeatureSizeData]);

	useEffect(() => {
		if (!needsFeatureSizePbc) return;
		if (!isProjectMetricsService(metricsService)) return;
		metricsService
			.getFeatureSizePbc(entity.id, startDate, endDate)
			.then(setFeatureSizePbcData)
			.catch((error) =>
				console.error("Error fetching feature size PBC data:", error),
			);
	}, [metricsService, entity, startDate, endDate, needsFeatureSizePbc]);

	useEffect(() => {
		if (!needsFeatureSizeEstimation) return;
		if (!isProjectMetricsService(metricsService)) return;
		metricsService
			.getFeatureSizeEstimation(entity.id, startDate, endDate)
			.then(setFeatureSizeEstimationData)
			.catch((error) =>
				console.error("Error fetching feature size estimation data:", error),
			);
	}, [metricsService, entity, startDate, endDate, needsFeatureSizeEstimation]);

	useEffect(() => {
		if (!needsFeatureSizePercentilesInfo) return;
		if (!isProjectMetricsService(metricsService)) return;
		metricsService
			.getFeatureSizePercentilesInfo(entity.id, startDate, endDate)
			.then(setFeatureSizePercentilesInfo)
			.catch((error) =>
				console.error("Error fetching feature size percentiles info:", error),
			);
	}, [
		metricsService,
		entity,
		startDate,
		endDate,
		needsFeatureSizePercentilesInfo,
	]);

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

	useEffect(() => {
		if (!needsEstimationVsCycleTime) return;
		metricsService
			.getEstimationVsCycleTimeData(entity.id, startDate, endDate)
			.then(setEstimationVsCycleTimeData)
			.catch((error) =>
				console.error("Error fetching estimation vs cycle time data:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsEstimationVsCycleTime]);

	useEffect(() => {
		if (!needsThroughputInfo) return;
		metricsService
			.getThroughputInfo(entity.id, startDate, endDate)
			.then(setThroughputInfo)
			.catch((error) =>
				console.error("Error fetching throughput info:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsThroughputInfo]);

	useEffect(() => {
		if (!needsArrivalsInfo) return;
		metricsService
			.getArrivalsInfo(entity.id, startDate, endDate)
			.then(setArrivalsInfo)
			.catch((error) => console.error("Error fetching arrivals info:", error));
	}, [entity, metricsService, startDate, endDate, needsArrivalsInfo]);

	useEffect(() => {
		if (!needsWipOverviewInfo) return;
		metricsService
			.getWipOverviewInfo(entity.id, startDate, endDate)
			.then(setWipOverviewInfo)
			.catch((error) =>
				console.error("Error fetching WIP overview info:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsWipOverviewInfo]);

	useEffect(() => {
		if (!needsTotalWorkItemAgeInfo) return;
		metricsService
			.getTotalWorkItemAgeInfo(entity.id, startDate, endDate)
			.then(setTotalWorkItemAgeInfo)
			.catch((error) =>
				console.error("Error fetching total work item age info:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsTotalWorkItemAgeInfo]);

	useEffect(() => {
		if (!needsPredictabilityScoreInfo) return;
		metricsService
			.getPredictabilityScoreInfo(entity.id, startDate, endDate)
			.then(setPredictabilityScoreInfo)
			.catch((error) =>
				console.error("Error fetching predictability score info:", error),
			);
	}, [
		entity,
		metricsService,
		startDate,
		endDate,
		needsPredictabilityScoreInfo,
	]);

	useEffect(() => {
		if (!needsCycleTimePercentilesInfo) return;
		metricsService
			.getCycleTimePercentilesInfo(entity.id, startDate, endDate)
			.then(setCycleTimePercentilesInfo)
			.catch((error) =>
				console.error("Error fetching cycle time percentiles info:", error),
			);
	}, [
		entity,
		metricsService,
		startDate,
		endDate,
		needsCycleTimePercentilesInfo,
	]);

	useEffect(() => {
		if (!needsFeaturesWorkedOnInfo) return;
		if (!isTeamMetricsService(metricsService)) return;
		metricsService
			.getFeaturesWorkedOnInfo(entity.id, startDate, endDate)
			.then(setFeaturesWorkedOnInfo)
			.catch((error) =>
				console.error("Error fetching features worked on info:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsFeaturesWorkedOnInfo]);

	useEffect(() => {
		if (!needsPbcCharts) return;
		const fetch = async () => {
			const [throughputPbc, cycleTimePbc, arrivalsPbc] = await Promise.all([
				metricsService.getThroughputPbc(entity.id, startDate, endDate),
				metricsService.getCycleTimePbc(entity.id, startDate, endDate),
				metricsService.getArrivalsPbc(entity.id, startDate, endDate),
			]);
			setThroughputPbcData(throughputPbc);
			setCycleTimePbcData(cycleTimePbc);
			setArrivalsPbcData(arrivalsPbc);
		};
		fetch().catch((error) =>
			console.error("Error fetching process behaviour chart data:", error),
		);
	}, [entity, metricsService, startDate, endDate, needsPbcCharts]);

	const refetchThroughputPbc = useCallback(
		async (view?: "raw" | "filtered"): Promise<void> => {
			try {
				const data = await metricsService.getThroughputPbc(
					entity.id,
					startDate,
					endDate,
					view,
				);
				setThroughputPbcData(data);
			} catch (error) {
				console.error("Error refetching throughput PBC data:", error);
			}
		},
		[entity, metricsService, startDate, endDate],
	);
	return {
		blackoutPeriods: blackoutPeriods.answer ?? [],
		throughputData: throughput.answer,
		wipOverTimeData: wipOverTime.answer,
		inProgressItems: inProgressItems.answer ?? [],
		blockedItems: blockedItems.answer ?? [],
		cycleTimeData: cycleTimeData.answer ?? [],
		percentileValues: cycleTimePercentiles.answer ?? [],
		workItemAgePercentilesValues: workItemAgePercentiles.answer ?? [],
		previousWorkItemAgePercentilesValues:
			previousWorkItemAgePercentiles.answer ?? [],
		perStatePercentileValues: ageInStatePercentiles.answer ?? [],
		sleRiskValues: sleRisk.answer ?? [],
		cumulativeStateTime: cumulativeStateTime.answer,
		sizePercentileValues,
		allFeaturesForSizeChart,
		predictabilityData,
		throughputPbcData,
		wipPbcData: wipPbc.answer,
		totalWorkItemAgePbcData: totalWorkItemAgePbc.answer,
		cycleTimePbcData,
		featureSizePbcData,
		estimationVsCycleTimeData,
		featureSizeEstimationData,
		serviceLevelExpectation,
		featureSizeTarget,
		totalWorkItemAge: totalWorkItemAge.answer,
		arrivalsData: arrivals.answer,
		arrivalsPbcData,
		throughputInfo,
		arrivalsInfo,
		featureSizePercentilesInfo,
		wipOverviewInfo,
		featuresWorkedOnInfo,
		totalWorkItemAgeInfo,
		predictabilityScoreInfo,
		cycleTimePercentilesInfo,
		flowEfficiencyInfo: flowEfficiency.answer,
		blockedCountHistory: blockedCountHistory.answer,
		fetchStates,
		refetchThroughputPbc,
	};
}
