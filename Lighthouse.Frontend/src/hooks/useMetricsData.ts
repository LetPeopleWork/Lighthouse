import { useCallback, useContext, useEffect, useState } from "react";
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
import type { IPercentileValue } from "../models/PercentileValue";
import type { IPerStatePercentileValues } from "../models/PerStatePercentileValues";
import type { IPortfolio } from "../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import type { IWorkItem } from "../models/WorkItem";
import {
	getMetricsFetchKeys,
	type MetricsFetchKey,
} from "../pages/Common/MetricsView/categoryMetadata";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type {
	IMetricsService,
	IProjectMetricsService,
	ITeamMetricsService,
} from "../services/Api/MetricsService";
import { useTerminology } from "../services/TerminologyContext";

const ONE_DAY_MS = 24 * 60 * 60 * 1000;

/**
 * Fetch-everything fallback for callers that do not scope their fetches (tests, and any view that
 * genuinely shows every widget at once). Frozen at module level so the default argument is one
 * stable identity rather than a fresh Set per render.
 */
const allMetricsFetchKeys: ReadonlySet<MetricsFetchKey> = new Set(
	getMetricsFetchKeys(),
);

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
	 * what the widget's previous-period trend compares against (D5).
	 */
	previousWorkItemAgePercentilesValues: IPercentileValue[];
	perStatePercentileValues: IPerStatePercentileValues[];
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

// Owner type is discriminated exactly as BaseMetricsView does it, on `getFeaturesInProgress`.
// Deliberately NOT isTeamMetricsService: that predicate keys off getFeaturesWorkedOnInfo, which
// portfolio-shaped services also expose, so it answers "does this service report features worked
// on", not "is this a team".
function isTeamOwnedMetricsService(service: object): boolean {
	return "getFeaturesInProgress" in service;
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

	const [blackoutPeriods, setBlackoutPeriods] = useState<IBlackoutPeriod[]>([]);
	const [throughputData, setThroughputData] = useState<RunChartData | null>(
		null,
	);
	const [wipOverTimeData, setWipOverTimeData] = useState<RunChartData | null>(
		null,
	);
	const [inProgressItems, setInProgressItems] = useState<IWorkItem[]>([]);
	const [blockedItems, setBlockedItems] = useState<IWorkItem[]>([]);
	const [cycleTimeData, setCycleTimeData] = useState<T[]>([]);
	const [percentileValues, setPercentileValues] = useState<IPercentileValue[]>(
		[],
	);
	const [workItemAgePercentilesValues, setWorkItemAgePercentilesValues] =
		useState<IPercentileValue[]>([]);
	const [
		previousWorkItemAgePercentilesValues,
		setPreviousWorkItemAgePercentilesValues,
	] = useState<IPercentileValue[]>([]);
	const [perStatePercentileValues, setPerStatePercentileValues] = useState<
		IPerStatePercentileValues[]
	>([]);
	const [cumulativeStateTime, setCumulativeStateTime] =
		useState<ICumulativeStateTimeResponse | null>(null);
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
	const [wipPbcData, setWipPbcData] =
		useState<ProcessBehaviourChartData | null>(null);
	const [totalWorkItemAgePbcData, setTotalWorkItemAgePbcData] =
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
	const [totalWorkItemAge, setTotalWorkItemAge] = useState<number | null>(null);
	const [arrivalsData, setArrivalsData] = useState<RunChartData | null>(null);
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
	const [flowEfficiencyInfo, setFlowEfficiencyInfo] =
		useState<IFlowEfficiencyInfo | null>(null);
	const [blockedCountHistory, setBlockedCountHistory] = useState<
		BlockedCountSnapshot[] | null
	>(null);
	// One primitive per fetch key. Primitives are compared by value, so an effect listing its own
	// flag re-runs only when that flag flips — never because a caller handed us a new Set with the
	// same contents. Callers grow the key set monotonically within an (entity, window), which makes
	// false→true happen at most once and therefore fetches at most once, with no refs or cache
	// (Bug #5571).
	const needsBlackoutPeriods = activeFetchKeys.has("blackoutPeriods");
	const needsPredictability = activeFetchKeys.has("predictability");
	const needsTotalWorkItemAge = activeFetchKeys.has("totalWorkItemAge");
	const needsThroughput = activeFetchKeys.has("throughput");
	const needsInProgressItems = activeFetchKeys.has("inProgressItems");
	const needsBlockedItems = activeFetchKeys.has("blockedItems");
	const needsWipOverTime = activeFetchKeys.has("wipOverTime");
	const needsCycleTimeData = activeFetchKeys.has("cycleTimeData");
	const needsCycleTimePercentiles = activeFetchKeys.has("cycleTimePercentiles");
	const needsWorkItemAgePercentiles = activeFetchKeys.has(
		"workItemAgePercentiles",
	);
	const needsAgeInStatePercentiles = activeFetchKeys.has(
		"ageInStatePercentiles",
	);
	const needsCumulativeStateTime = activeFetchKeys.has("cumulativeStateTime");
	const needsFlowEfficiency = activeFetchKeys.has("flowEfficiency");
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
	const needsArrivals = activeFetchKeys.has("arrivals");
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
	const needsBlockedCountHistory = activeFetchKeys.has("blockedCountHistory");
	const needsFeaturesWorkedOnInfo = activeFetchKeys.has("featuresWorkedOnInfo");
	const needsPbcCore = activeFetchKeys.has("pbcCore");
	const needsPbcCharts = activeFetchKeys.has("pbcCharts");

	// The one batch whose members are still fetched together: they share a window derivation and,
	// on the default Flow Overview, every one of them is required anyway (see the batch below).
	const needsCycleTimeBatch =
		needsCycleTimeData ||
		needsCycleTimePercentiles ||
		needsWorkItemAgePercentiles ||
		needsFlowEfficiency;

	useEffect(() => {
		if (!needsBlackoutPeriods) return;
		blackoutPeriodService
			.getAll()
			.then(setBlackoutPeriods)
			.catch(() => {
				/* optional — fall back to empty */
			});
	}, [blackoutPeriodService, needsBlackoutPeriods]);

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
		if (!needsTotalWorkItemAge) return;
		metricsService
			.getTotalWorkItemAge(entity.id, endDate)
			.then(setTotalWorkItemAge)
			.catch((error) =>
				console.error("Error fetching total work item age:", error),
			);
	}, [entity, metricsService, endDate, needsTotalWorkItemAge]);

	useEffect(() => {
		if (!needsThroughput) return;
		metricsService
			.getThroughput(entity.id, startDate, endDate)
			.then(setThroughputData)
			.catch((error) => console.error("Error getting throughput:", error));
	}, [entity, metricsService, startDate, endDate, needsThroughput]);

	useEffect(() => {
		if (!needsInProgressItems) return;
		metricsService
			.getInProgressItems(entity.id, endDate)
			.then(setInProgressItems)
			.catch((error) =>
				console.error(`Error getting ${workItemsTerm} in progress:`, error),
			);
	}, [entity, metricsService, endDate, workItemsTerm, needsInProgressItems]);

	useEffect(() => {
		if (!needsBlockedItems) return;
		// The blocked overview spans BOTH open state categories (To Do + In Progress) — an item
		// can be stuck in To Do because it is blocked — so it is sourced from the blocked-eligible
		// endpoint, not filtered out of the WIP (in-progress-only) set.
		metricsService
			.getBlockedItemsAtDate(entity.id, endDate)
			.then(setBlockedItems)
			.catch((error) =>
				console.error(`Error getting blocked ${workItemsTerm}:`, error),
			);
	}, [entity, metricsService, endDate, workItemsTerm, needsBlockedItems]);

	useEffect(() => {
		if (!needsWipOverTime) return;
		metricsService
			.getWorkInProgressOverTime(entity.id, startDate, endDate)
			.then(setWipOverTimeData)
			.catch((error) =>
				console.error(`Error getting ${workItemsTerm} over time:`, error),
			);
	}, [
		entity,
		metricsService,
		startDate,
		endDate,
		workItemsTerm,
		needsWipOverTime,
	]);

	useEffect(() => {
		if (!needsCycleTimeBatch) return;
		// Every call below shares the same dependency signature, so they all belong in one
		// parallel batch: getCycleTimeData used to be awaited sequentially ahead of the batch,
		// which needlessly gated the rest of the view — including flow efficiency, which does
		// not depend on cycle-time data at all (D18). The batch's cross-category members
		// (per-state percentiles, cumulative state time) have since moved to their own gated
		// effects; siblings still dispatch in the same commit, so those stay parallel too.
		const fetchFlowEfficiency = () =>
			isTeamOwnedMetricsService(metricsService)
				? metricsService.getFlowEfficiencyInfoForTeam(
						entity.id,
						startDate,
						endDate,
					)
				: metricsService.getFlowEfficiencyInfoForPortfolio(
						entity.id,
						startDate,
						endDate,
					);

		// Previous-period window for the Work Item Age Percentiles trend (D5): the same window
		// length, ending the day BEFORE the selected range starts. The backend snapshots on the
		// window's endDate, so that boundary day is what actually selects the comparison point.
		//
		// Derived INSIDE the effect on purpose. As a component-scope `new Date(...)` it would be a
		// fresh identity on every render and, once in this effect's dependency list, an endless
		// re-render loop (React #185 — see docs/ci-learnings.md, 2026-05-25). Here it depends on
		// nothing the effect does not already depend on.
		const previousPeriodEnd = new Date(startDate.getTime() - ONE_DAY_MS);
		const previousPeriodStart = new Date(
			previousPeriodEnd.getTime() - (endDate.getTime() - startDate.getTime()),
		);

		const fetch = async () => {
			const [
				data,
				percentiles,
				workItemAgePercentiles,
				previousWorkItemAgePercentiles,
				flowEfficiency,
			] = await Promise.all([
				metricsService.getCycleTimeData(entity.id, startDate, endDate),
				metricsService.getCycleTimePercentiles(entity.id, startDate, endDate),
				metricsService.getWorkItemAgePercentiles(entity.id, startDate, endDate),
				metricsService.getWorkItemAgePercentiles(
					entity.id,
					previousPeriodStart,
					previousPeriodEnd,
				),
				fetchFlowEfficiency(),
			]);
			setCycleTimeData(data);
			setPercentileValues(percentiles);
			setWorkItemAgePercentilesValues(workItemAgePercentiles);
			setPreviousWorkItemAgePercentilesValues(previousWorkItemAgePercentiles);
			setFlowEfficiencyInfo(flowEfficiency ?? null);
		};
		fetch().catch((error) =>
			console.error(`Error fetching ${cycleTimeTerm} data:`, error),
		);
	}, [
		entity,
		metricsService,
		startDate,
		endDate,
		cycleTimeTerm,
		needsCycleTimeBatch,
	]);

	useEffect(() => {
		if (!needsAgeInStatePercentiles) return;
		metricsService
			.getAgeInStatePercentiles(entity.id, startDate, endDate)
			.then(setPerStatePercentileValues)
			.catch((error) =>
				console.error("Error fetching per-state percentiles:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsAgeInStatePercentiles]);

	useEffect(() => {
		if (!needsCumulativeStateTime) return;
		metricsService
			.getCumulativeStateTimeForTeam(entity.id, startDate, endDate)
			.then(setCumulativeStateTime)
			.catch((error) =>
				console.error("Error fetching cumulative state time:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsCumulativeStateTime]);

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
		if (!needsArrivals) return;
		metricsService
			.getArrivals(entity.id, startDate, endDate)
			.then(setArrivalsData)
			.catch((error) => console.error("Error fetching arrivals data:", error));
	}, [entity, metricsService, startDate, endDate, needsArrivals]);

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
		if (!needsBlockedCountHistory) return;
		// US-03 AC0 / Bug #5521: computeBlockedTrend looks for its baseline at
		// startDate − 1 day, but the controller filters `RecordedAt >= start`. Fetching
		// with the dashboard's own startDate therefore put the baseline day exactly one
		// day outside the returned window, so the trend never found one and rendered the
		// neutral placeholder on every instance for every range. Fetch one day earlier.
		const baselineStart = new Date(startDate);
		baselineStart.setDate(baselineStart.getDate() - 1);

		metricsService
			.getBlockedCountHistory(entity.id, baselineStart, endDate)
			.then(setBlockedCountHistory)
			.catch((error) =>
				console.error("Error fetching blocked count history:", error),
			);
	}, [entity, metricsService, startDate, endDate, needsBlockedCountHistory]);

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
		if (!needsPbcCore) return;
		const fetch = async () => {
			const [wipPbc, totalWorkItemAgePbc] = await Promise.all([
				metricsService.getWipPbc(entity.id, startDate, endDate),
				metricsService.getTotalWorkItemAgePbc(entity.id, startDate, endDate),
			]);
			setWipPbcData(wipPbc);
			setTotalWorkItemAgePbcData(totalWorkItemAgePbc);
		};
		fetch().catch((error) =>
			console.error("Error fetching core process behaviour chart data:", error),
		);
	}, [entity, metricsService, startDate, endDate, needsPbcCore]);

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
		blackoutPeriods,
		throughputData,
		wipOverTimeData,
		inProgressItems,
		blockedItems,
		cycleTimeData,
		percentileValues,
		workItemAgePercentilesValues,
		previousWorkItemAgePercentilesValues,
		perStatePercentileValues,
		cumulativeStateTime,
		sizePercentileValues,
		allFeaturesForSizeChart,
		predictabilityData,
		throughputPbcData,
		wipPbcData,
		totalWorkItemAgePbcData,
		cycleTimePbcData,
		featureSizePbcData,
		estimationVsCycleTimeData,
		featureSizeEstimationData,
		serviceLevelExpectation,
		featureSizeTarget,
		totalWorkItemAge,
		arrivalsData,
		arrivalsPbcData,
		throughputInfo,
		arrivalsInfo,
		featureSizePercentilesInfo,
		wipOverviewInfo,
		featuresWorkedOnInfo,
		totalWorkItemAgeInfo,
		predictabilityScoreInfo,
		cycleTimePercentilesInfo,
		flowEfficiencyInfo,
		blockedCountHistory,
		refetchThroughputPbc,
	};
}
