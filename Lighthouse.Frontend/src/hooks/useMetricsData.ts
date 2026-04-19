import { useContext, useEffect, useState } from "react";
import type { IBlackoutPeriod } from "../models/BlackoutPeriod";
import type { IFeature } from "../models/Feature";
import type { IForecastPredictabilityScore } from "../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureOwner } from "../models/IFeatureOwner";
import type { IEstimationVsCycleTimeResponse } from "../models/Metrics/EstimationVsCycleTimeData";
import type { IFeatureSizeEstimationResponse } from "../models/Metrics/FeatureSizeEstimationData";
import type {
	IArrivalsInfo,
	IThroughputInfo,
} from "../models/Metrics/InfoWidgetData";
import type { ProcessBehaviourChartData } from "../models/Metrics/ProcessBehaviourChartData";
import type { RunChartData } from "../models/Metrics/RunChartData";
import type { IPercentileValue } from "../models/PercentileValue";
import type { IPortfolio } from "../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import type { IWorkItem } from "../models/WorkItem";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type {
	IMetricsService,
	IProjectMetricsService,
} from "../services/Api/MetricsService";
import { useTerminology } from "../services/TerminologyContext";

export interface MetricsData<T> {
	blackoutPeriods: IBlackoutPeriod[];
	throughputData: RunChartData | null;
	wipOverTimeData: RunChartData | null;
	inProgressItems: IWorkItem[];
	cycleTimeData: T[];
	percentileValues: IPercentileValue[];
	sizePercentileValues: IPercentileValue[];
	allFeaturesForSizeChart: IFeature[];
	startedItems: RunChartData | null;
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
}

function isProjectMetricsService(
	service: object,
): service is IProjectMetricsService {
	return (
		"getAllFeaturesForSizeChart" in service &&
		"getSizePercentiles" in service &&
		"getFeatureSizePbc" in service &&
		"getFeatureSizeEstimation" in service
	);
}

export function useMetricsData<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
>(
	entity: E,
	metricsService: IMetricsService<T>,
	startDate: Date,
	endDate: Date,
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
	const [cycleTimeData, setCycleTimeData] = useState<T[]>([]);
	const [percentileValues, setPercentileValues] = useState<IPercentileValue[]>(
		[],
	);
	const [sizePercentileValues, setSizePercentileValues] = useState<
		IPercentileValue[]
	>([]);
	const [allFeaturesForSizeChart, setAllFeaturesForSizeChart] = useState<
		IFeature[]
	>([]);
	const [startedItems, setStartedItems] = useState<RunChartData | null>(null);
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
	useEffect(() => {
		blackoutPeriodService
			.getAll()
			.then(setBlackoutPeriods)
			.catch(() => {
				/* optional — fall back to empty */
			});
	}, [blackoutPeriodService]);

	useEffect(() => {
		metricsService
			.getMultiItemForecastPredictabilityScore(entity.id, startDate, endDate)
			.then(setPredictabilityData)
			.catch((error) =>
				console.error("Error fetching predictability data:", error),
			);
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		metricsService
			.getTotalWorkItemAge(entity.id)
			.then(setTotalWorkItemAge)
			.catch((error) =>
				console.error("Error fetching total work item age:", error),
			);
	}, [entity, metricsService]);

	useEffect(() => {
		metricsService
			.getThroughput(entity.id, startDate, endDate)
			.then(setThroughputData)
			.catch((error) => console.error("Error getting throughput:", error));
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		metricsService
			.getStartedItems(entity.id, startDate, endDate)
			.then(setStartedItems)
			.catch((error) =>
				console.error(`Error getting started ${workItemsTerm}:`, error),
			);
	}, [entity, metricsService, startDate, endDate, workItemsTerm]);

	useEffect(() => {
		const fetch = async () => {
			const items = await metricsService.getInProgressItems(entity.id);
			setInProgressItems(items);
			const wipData = await metricsService.getWorkInProgressOverTime(
				entity.id,
				startDate,
				endDate,
			);
			setWipOverTimeData(wipData);
		};
		fetch().catch((error) =>
			console.error(`Error getting ${workItemsTerm} in progress:`, error),
		);
	}, [entity, metricsService, startDate, endDate, workItemsTerm]);

	useEffect(() => {
		const fetch = async () => {
			const data = await metricsService.getCycleTimeData(
				entity.id,
				startDate,
				endDate,
			);
			setCycleTimeData(data);
			const percentiles = await metricsService.getCycleTimePercentiles(
				entity.id,
				startDate,
				endDate,
			);
			setPercentileValues(percentiles);
		};
		fetch().catch((error) =>
			console.error(`Error fetching ${cycleTimeTerm} data:`, error),
		);
	}, [entity, metricsService, startDate, endDate, cycleTimeTerm]);

	useEffect(() => {
		if (!isProjectMetricsService(metricsService)) return;
		const svc = metricsService as IProjectMetricsService;
		const fetch = async () => {
			setSizePercentileValues(
				await svc.getSizePercentiles(entity.id, startDate, endDate),
			);
			setAllFeaturesForSizeChart(
				await svc.getAllFeaturesForSizeChart(entity.id, startDate, endDate),
			);
			setFeatureSizePbcData(
				await svc.getFeatureSizePbc(entity.id, startDate, endDate),
			);
			setFeatureSizeEstimationData(
				await svc.getFeatureSizeEstimation(entity.id, startDate, endDate),
			);
		};
		fetch().catch((error) =>
			console.error("Error fetching Size Percentile Data:", error),
		);
	}, [metricsService, entity, startDate, endDate]);

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
		metricsService
			.getEstimationVsCycleTimeData(entity.id, startDate, endDate)
			.then(setEstimationVsCycleTimeData)
			.catch((error) =>
				console.error("Error fetching estimation vs cycle time data:", error),
			);
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		metricsService
			.getArrivals(entity.id, startDate, endDate)
			.then(setArrivalsData)
			.catch((error) => console.error("Error fetching arrivals data:", error));
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		metricsService
			.getThroughputInfo(entity.id, startDate, endDate)
			.then(setThroughputInfo)
			.catch((error) =>
				console.error("Error fetching throughput info:", error),
			);
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		metricsService
			.getArrivalsInfo(entity.id, startDate, endDate)
			.then(setArrivalsInfo)
			.catch((error) => console.error("Error fetching arrivals info:", error));
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		const fetch = async () => {
			const [
				throughputPbc,
				wipPbc,
				totalWorkItemAgePbc,
				cycleTimePbc,
				arrivalsPbc,
			] = await Promise.all([
				metricsService.getThroughputPbc(entity.id, startDate, endDate),
				metricsService.getWipPbc(entity.id, startDate, endDate),
				metricsService.getTotalWorkItemAgePbc(entity.id, startDate, endDate),
				metricsService.getCycleTimePbc(entity.id, startDate, endDate),
				metricsService.getArrivalsPbc(entity.id, startDate, endDate),
			]);
			setThroughputPbcData(throughputPbc);
			setWipPbcData(wipPbc);
			setTotalWorkItemAgePbcData(totalWorkItemAgePbc);
			setCycleTimePbcData(cycleTimePbc);
			setArrivalsPbcData(arrivalsPbc);
		};
		fetch().catch((error) =>
			console.error("Error fetching process behaviour chart data:", error),
		);
	}, [entity, metricsService, startDate, endDate]);

	return {
		blackoutPeriods,
		throughputData,
		wipOverTimeData,
		inProgressItems,
		cycleTimeData,
		percentileValues,
		sizePercentileValues,
		allFeaturesForSizeChart,
		startedItems,
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
	};
}
