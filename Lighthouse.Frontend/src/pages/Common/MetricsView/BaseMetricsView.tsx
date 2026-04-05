import { Grid } from "@mui/material";
import {
	type ReactNode,
	useContext,
	useEffect,
	useMemo,
	useState,
} from "react";
import { useSearchParams } from "react-router-dom";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import EstimationVsCycleTimeChart from "../../../components/Common/Charts/EstimationVsCycleTimeChart";
import FeatureSizeScatterPlotChart from "../../../components/Common/Charts/FeatureSizeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import ProcessBehaviourChart, {
	ProcessBehaviourChartType,
} from "../../../components/Common/Charts/ProcessBehaviourChart";
import StackedAreaChart from "../../../components/Common/Charts/StackedAreaChart";
import StartedVsFinishedDisplay from "../../../components/Common/Charts/StartedVsFinishedDisplay";
import TotalWorkItemAgeRunChart from "../../../components/Common/Charts/TotalWorkItemAgeRunChart";
import TotalWorkItemAgeWidget from "../../../components/Common/Charts/TotalWorkItemAgeWidget";
import WorkDistributionChart from "../../../components/Common/Charts/WorkDistributionChart";
import WorkItemAgingChart from "../../../components/Common/Charts/WorkItemAgingChart";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import type { IFeature } from "../../../models/Feature";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IEstimationVsCycleTimeResponse } from "../../../models/Metrics/EstimationVsCycleTimeData";
import type { IFeatureSizeEstimationResponse } from "../../../models/Metrics/FeatureSizeEstimationData";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type {
	IMetricsService,
	IProjectMetricsService,
} from "../../../services/Api/MetricsService";
import { useTerminology } from "../../../services/TerminologyContext";
import { appColors } from "../../../utils/theme/colors";
import BlockedOverviewWidget from "./BlockedOverviewWidget";
import { getWidgetsForCategory } from "./categoryMetadata";
import type { DashboardItem } from "./Dashboard";
import Dashboard from "./Dashboard";
import DashboardHeader from "./DashboardHeader";
import FeaturesWorkedOnWidget from "./FeaturesWorkedOnWidget";
import PredictabilityScoreWidget from "./PredictabilityScoreWidget";
import {
	computeBlockedOverviewRag,
	computeCycleTimePercentilesRag,
	computeCycleTimeScatterplotRag,
	computeFeaturesWorkedOnRag,
	computePredictabilityScoreRag,
	computeStartedVsClosedRag,
	computeThroughputRag,
	computeTotalWorkItemAgeRag,
	computeWipOverviewRag,
} from "./ragRules";
import { useCategorySelection } from "./useCategorySelection";
import { useShowTips } from "./useShowTips";
import WidgetShell from "./WidgetShell";
import WipOverviewWidget from "./WipOverviewWidget";

export interface BaseMetricsViewProps<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
> {
	entity: E;
	metricsService: IMetricsService<T>;
	title: string;
	defaultDateRange?: number;
	featuresInProgress?: IWorkItem[];
	featureWip?: number;
	hasBlockedConfig?: boolean;
	doingStates: string[];
}

// ---------------------------------------------------------------------------
// Pure helpers (outside component — zero cognitive complexity cost)
// ---------------------------------------------------------------------------

function formatDate(date: Date): string {
	return date.toISOString().split("T")[0];
}

function parseDate(dateString: string): Date | null {
	const date = new Date(dateString);
	return Number.isNaN(date.getTime()) ? null : date;
}

function getDefaultStartDate(defaultDateRange: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - defaultDateRange);
	return date;
}

function extractWorkItems(
	workItemsPerUnitOfTime?: Record<string, IWorkItem[]>,
): IWorkItem[] {
	if (!workItemsPerUnitOfTime) return [];
	return Object.values(workItemsPerUnitOfTime).flat();
}

function addItemsWithoutDuplicates(
	lookup: Map<number, IWorkItem>,
	items: IWorkItem[],
): void {
	for (const item of items) {
		if (!lookup.has(item.id)) {
			lookup.set(item.id, item);
		}
	}
}

function buildWorkItemLookup(
	throughputData: RunChartData | null,
	wipOverTimeData: RunChartData | null,
	cycleTimeData: IWorkItem[],
	inProgressItems: IWorkItem[],
	allFeaturesForSizeChart: IFeature[],
): Map<number, IWorkItem> {
	const lookup = new Map<number, IWorkItem>();

	for (const item of extractWorkItems(throughputData?.workItemsPerUnitOfTime)) {
		lookup.set(item.id, item);
	}

	addItemsWithoutDuplicates(
		lookup,
		extractWorkItems(wipOverTimeData?.workItemsPerUnitOfTime),
	);
	addItemsWithoutDuplicates(lookup, cycleTimeData);
	addItemsWithoutDuplicates(lookup, inProgressItems);

	for (const item of allFeaturesForSizeChart) {
		lookup.set(item.id, item as unknown as IWorkItem);
	}

	return lookup;
}

type RagFooter = {
	readonly ragStatus: "red" | "amber" | "green";
	readonly tipText: string;
};

type RagInputs = {
	readonly wipCount: number;
	readonly systemWipLimit: number | undefined;
	readonly blockedCount: number;
	readonly hasBlockedConfig: boolean;
	readonly featuresInProgress: IWorkItem[] | undefined;
	readonly featureWip: number | undefined;
	readonly predictabilityScore: number | null;
	readonly sle: IPercentileValue | null;
	readonly percentileValues: IPercentileValue[];
	readonly startedTotal: number;
	readonly closedTotal: number;
	readonly totalWorkItemAge: number | null;
	readonly currentWip: number;
	readonly sleDays: number | undefined;
	readonly throughputValues: ReadonlyArray<number>;
	readonly blackoutDayIndices: ReadonlyArray<number>;
	readonly cycleTimes: ReadonlyArray<number>;
};

function buildWidgetFooters(
	inputs: RagInputs,
): Record<string, RagFooter | undefined> {
	return {
		wipOverview: computeWipOverviewRag(inputs.wipCount, inputs.systemWipLimit),
		blockedOverview: computeBlockedOverviewRag(
			inputs.blockedCount,
			inputs.hasBlockedConfig,
		),
		featuresWorkedOnOverview: inputs.featuresInProgress
			? computeFeaturesWorkedOnRag(
					inputs.featuresInProgress.length,
					inputs.featureWip,
				)
			: undefined,
		predictabilityScore: computePredictabilityScoreRag(
			inputs.predictabilityScore,
		),
		percentiles: computeCycleTimePercentilesRag(
			inputs.sle,
			inputs.percentileValues,
		),
		startedVsFinished: computeStartedVsClosedRag(
			inputs.startedTotal,
			inputs.closedTotal,
			inputs.systemWipLimit,
		),
		totalWorkItemAge:
			inputs.totalWorkItemAge === null
				? undefined
				: computeTotalWorkItemAgeRag(
						inputs.totalWorkItemAge,
						inputs.currentWip,
						inputs.systemWipLimit,
						inputs.sleDays,
					),
		throughput: computeThroughputRag(
			inputs.throughputValues,
			inputs.blackoutDayIndices,
		),
		cycleScatter: computeCycleTimeScatterplotRag(inputs.sle, inputs.cycleTimes),
	};
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- generic helper avoids coupling to specific entity/metrics type
function buildWidgetNodes(ctx: {
	entity: IFeatureOwner;
	title: string;
	startDate: Date;
	inProgressItems: IWorkItem[];
	blockedItems: IWorkItem[];
	blockedTerm: string;
	featuresInProgress: IWorkItem[] | undefined;
	featureWip: number | undefined;
	featureTerm: string;
	predictabilityData: IForecastPredictabilityScore | null;
	percentileValues: IPercentileValue[];
	serviceLevelExpectation: IPercentileValue | null;
	cycleTimeData: IWorkItem[];
	startedItems: RunChartData | null;
	throughputData: RunChartData | null;
	wipOverTimeData: RunChartData | null;
	allFeaturesForSizeChart: IFeature[];
	sizePercentileValues: IPercentileValue[];
	featureSizeEstimationData: IFeatureSizeEstimationResponse | null;
	estimationVsCycleTimeData: IEstimationVsCycleTimeResponse | null;
	blackoutPeriods: IBlackoutPeriod[];
	doingStates: string[];
	workItemLookup: Map<number, IWorkItem>;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	throughputTerm: string;
	workItemAgeTerm: string;
	workInProgressTerm: string;
	getTerm: (key: string) => string;
	totalWorkItemAge: number | null;
	throughputPbcData: ProcessBehaviourChartData | null;
	wipPbcData: ProcessBehaviourChartData | null;
	totalWorkItemAgePbcData: ProcessBehaviourChartData | null;
	cycleTimePbcData: ProcessBehaviourChartData | null;
	featureSizePbcData: ProcessBehaviourChartData | null;
}): Record<string, ReactNode | null> {
	const nodes: Record<string, ReactNode | null> = {
		wipOverview: (
			<WipOverviewWidget
				wipCount={ctx.inProgressItems.length}
				systemWipLimit={
					ctx.entity.systemWIPLimit > 0 ? ctx.entity.systemWIPLimit : undefined
				}
				title={`${ctx.title} in Progress`}
			/>
		),
		blockedOverview: (
			<BlockedOverviewWidget
				blockedCount={ctx.blockedItems.length}
				title={ctx.blockedTerm}
			/>
		),
		featuresWorkedOnOverview: ctx.featuresInProgress ? (
			<FeaturesWorkedOnWidget
				featureCount={ctx.featuresInProgress.length}
				featureWip={ctx.featureWip}
				title={`${ctx.featureTerm} being Worked On`}
			/>
		) : null,
		predictabilityScore: (
			<PredictabilityScoreWidget
				score={ctx.predictabilityData?.predictabilityScore ?? null}
			/>
		),
		percentiles: (
			<CycleTimePercentiles
				percentileValues={ctx.percentileValues}
				serviceLevelExpectation={ctx.serviceLevelExpectation}
				items={ctx.cycleTimeData}
			/>
		),
		startedVsFinished: (
			<StartedVsFinishedDisplay
				startedItems={ctx.startedItems}
				closedItems={ctx.throughputData}
			/>
		),
		totalWorkItemAge: (
			<TotalWorkItemAgeWidget
				entityId={ctx.entity.id}
				metricsService={ctx.metricsService}
			/>
		),
		throughput: ctx.throughputData ? (
			<BarRunChart
				title={`${ctx.title} Completed`}
				startDate={ctx.startDate}
				chartData={ctx.throughputData}
				displayTotal={true}
				predictabilityData={ctx.predictabilityData}
			/>
		) : null,
		cycleScatter: (
			<CycleTimeScatterPlotChart
				cycleTimeDataPoints={ctx.cycleTimeData}
				percentileValues={ctx.percentileValues}
				serviceLevelExpectation={ctx.serviceLevelExpectation}
				blackoutPeriods={ctx.blackoutPeriods}
			/>
		),
		workDistribution: (
			<WorkDistributionChart
				workItems={
					[...ctx.cycleTimeData, ...ctx.inProgressItems] as IWorkItem[]
				}
				title="Work Distribution"
			/>
		),
		aging: (
			<WorkItemAgingChart
				inProgressItems={ctx.inProgressItems}
				percentileValues={ctx.percentileValues}
				serviceLevelExpectation={ctx.serviceLevelExpectation}
				doingStates={ctx.doingStates}
			/>
		),
		wipOverTime: ctx.wipOverTimeData ? (
			<LineRunChart
				title={`${ctx.title} In Progress Over Time`}
				startDate={ctx.startDate}
				chartData={ctx.wipOverTimeData}
				displayTotal={false}
				wipLimit={ctx.entity.systemWIPLimit}
			/>
		) : null,
		totalWorkItemAgeOverTime: ctx.wipOverTimeData ? (
			<TotalWorkItemAgeRunChart
				title={`${ctx.title} Total Work Item Age Over Time`}
				startDate={ctx.startDate}
				wipOverTimeData={ctx.wipOverTimeData}
			/>
		) : null,
		stacked:
			ctx.throughputData && ctx.startedItems ? (
				<StackedAreaChart
					title="Simplified Cumulative Flow Diagram"
					startDate={ctx.startDate}
					areas={[
						{
							index: 1,
							title: "Doing",
							area: ctx.startedItems,
							color: appColors.primary.light,
							startOffset: ctx.wipOverTimeData?.getValueOnDay(0) ?? 0,
						},
						{
							index: 2,
							title: "Done",
							area: ctx.throughputData,
							color: appColors.secondary.light,
						},
					]}
				/>
			) : null,
		estimationVsCycleTime:
			ctx.estimationVsCycleTimeData?.status !== "NotConfigured" &&
			ctx.estimationVsCycleTimeData ? (
				<EstimationVsCycleTimeChart
					data={ctx.estimationVsCycleTimeData}
					workItemLookup={ctx.workItemLookup}
				/>
			) : null,
		featureSize:
			ctx.allFeaturesForSizeChart.length > 0 ? (
				<FeatureSizeScatterPlotChart
					sizeDataPoints={ctx.allFeaturesForSizeChart}
					sizePercentileValues={ctx.sizePercentileValues}
					estimationData={ctx.featureSizeEstimationData ?? undefined}
				/>
			) : null,
	};

	const pbcConfigs = [
		{
			id: "throughputPbc",
			data: ctx.throughputPbcData,
			titleSuffix: ctx.throughputTerm,
			type: ProcessBehaviourChartType.Throughput,
		},
		{
			id: "wipPbc",
			data: ctx.wipPbcData,
			titleSuffix: ctx.workInProgressTerm,
			type: ProcessBehaviourChartType.WorkInProgress,
		},
		{
			id: "totalWorkItemAgePbc",
			data: ctx.totalWorkItemAgePbcData,
			titleSuffix: `Total ${ctx.workItemAgeTerm}`,
			type: ProcessBehaviourChartType.TotalWorkItemAge,
		},
		{
			id: "cycleTimePbc",
			data: ctx.cycleTimePbcData,
			titleSuffix: ctx.getTerm(TERMINOLOGY_KEYS.CYCLE_TIME),
			type: ProcessBehaviourChartType.CycleTime,
		},
		{
			id: "featureSizePbc",
			data: ctx.featureSizePbcData,
			titleSuffix: `${ctx.featureTerm} Size`,
			type: ProcessBehaviourChartType.FeatureSize,
		},
	];

	for (const config of pbcConfigs) {
		if (config.data) {
			nodes[config.id] = (
				<ProcessBehaviourChart
					data={config.data}
					title={config.titleSuffix}
					workItemLookup={ctx.workItemLookup}
					type={config.type}
				/>
			);
		}
	}

	return nodes;
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

// ---------------------------------------------------------------------------
// Custom hook — pulls all data-fetching out of the component
// ---------------------------------------------------------------------------

interface MetricsData<T> {
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
	totalWorkItemAge: number | null;
}

function useMetricsData<
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
	const [totalWorkItemAge, setTotalWorkItemAge] = useState<number | null>(null);

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
		const fetch = async () => {
			const [throughputPbc, wipPbc, totalWorkItemAgePbc, cycleTimePbc] =
				await Promise.all([
					metricsService.getThroughputPbc(entity.id, startDate, endDate),
					metricsService.getWipPbc(entity.id, startDate, endDate),
					metricsService.getTotalWorkItemAgePbc(entity.id, startDate, endDate),
					metricsService.getCycleTimePbc(entity.id, startDate, endDate),
				]);
			setThroughputPbcData(throughputPbc);
			setWipPbcData(wipPbc);
			setTotalWorkItemAgePbcData(totalWorkItemAgePbc);
			setCycleTimePbcData(cycleTimePbc);
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
		totalWorkItemAge,
	};
}

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export const BaseMetricsView = <
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
>({
	entity,
	metricsService,
	title,
	defaultDateRange = 30,
	featuresInProgress,
	featureWip,
	hasBlockedConfig = false,
	doingStates,
}: BaseMetricsViewProps<T, E>) => {
	const [searchParams, setSearchParams] = useSearchParams();

	const [startDate, setStartDate] = useState<Date>(() => {
		const parsed = parseDate(searchParams.get("startDate") ?? "");
		return parsed ?? getDefaultStartDate(defaultDateRange);
	});

	const [endDate, setEndDate] = useState<Date>(() => {
		const parsed = parseDate(searchParams.get("endDate") ?? "");
		return parsed ?? new Date();
	});

	const updateDateParams = (start: Date, end: Date) => {
		const newParams = new URLSearchParams(searchParams);
		newParams.set("startDate", formatDate(start));
		newParams.set("endDate", formatDate(end));
		setSearchParams(newParams, { replace: true });
	};

	const handleStartDateChange = (date: Date | null) => {
		if (!date) return;
		setStartDate(date);
		updateDateParams(date, endDate);
	};

	const handleEndDateChange = (date: Date | null) => {
		if (!date) return;
		setEndDate(date);
		updateDateParams(startDate, date);
	};

	const {
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
		totalWorkItemAge,
	} = useMetricsData(entity, metricsService, startDate, endDate);

	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const workInProgressTerm = getTerm(TERMINOLOGY_KEYS.WORK_IN_PROGRESS);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const ownerType: "team" | "portfolio" =
		"getFeaturesInProgress" in metricsService ? "team" : "portfolio";
	const { selectedCategory, setSelectedCategory } = useCategorySelection(
		ownerType,
		entity.id,
	);
	const { showTips, toggleShowTips } = useShowTips(ownerType, entity.id);

	const workItemLookup = useMemo(
		() =>
			buildWorkItemLookup(
				throughputData,
				wipOverTimeData,
				cycleTimeData as unknown as IWorkItem[],
				inProgressItems,
				allFeaturesForSizeChart,
			),
		[
			throughputData,
			wipOverTimeData,
			cycleTimeData,
			inProgressItems,
			allFeaturesForSizeChart,
		],
	);

	const blockedItems = inProgressItems.filter((item) => item.isBlocked);

	const widgetNodes = buildWidgetNodes({
		entity,
		title,
		startDate,
		inProgressItems,
		blockedItems,
		blockedTerm,
		featuresInProgress,
		featureWip,
		featureTerm,
		predictabilityData,
		percentileValues,
		serviceLevelExpectation,
		cycleTimeData: cycleTimeData as unknown as IWorkItem[],
		startedItems,
		throughputData,
		wipOverTimeData,
		allFeaturesForSizeChart,
		sizePercentileValues,
		featureSizeEstimationData,
		estimationVsCycleTimeData,
		blackoutPeriods,
		doingStates,
		workItemLookup,
		metricsService,
		throughputTerm,
		workItemAgeTerm,
		workInProgressTerm,
		getTerm,
		totalWorkItemAge,
		throughputPbcData,
		wipPbcData,
		totalWorkItemAgePbcData,
		cycleTimePbcData,
		featureSizePbcData,
	});

	const widgetFooters = buildWidgetFooters({
		wipCount: inProgressItems.length,
		systemWipLimit:
			entity.systemWIPLimit > 0 ? entity.systemWIPLimit : undefined,
		blockedCount: blockedItems.length,
		hasBlockedConfig,
		featuresInProgress,
		featureWip,
		predictabilityScore: predictabilityData?.predictabilityScore ?? null,
		sle: serviceLevelExpectation,
		percentileValues,
		startedTotal: startedItems?.total ?? 0,
		closedTotal: throughputData?.total ?? 0,
		totalWorkItemAge,
		currentWip: inProgressItems.length,
		sleDays:
			entity.serviceLevelExpectationRange > 0
				? entity.serviceLevelExpectationRange
				: undefined,
		throughputValues: throughputData
			? Array.from({ length: throughputData.history }, (_, i) =>
					throughputData.getValueOnDay(i),
				)
			: [],
		blackoutDayIndices: throughputData?.blackoutDayIndices ?? [],
		cycleTimes: (cycleTimeData as unknown as IWorkItem[]).map(
			(item) => item.cycleTime,
		),
	});

	const activeWidgets = getWidgetsForCategory(selectedCategory, ownerType);
	const dashboardItems: DashboardItem[] = activeWidgets
		.filter((w) => widgetNodes[w.widgetKey] != null)
		.map((w) => ({
			id: w.widgetKey,
			size: w.size,
			node: (
				<WidgetShell
					widgetKey={w.widgetKey}
					showTips={showTips}
					footer={widgetFooters[w.widgetKey]}
				>
					{widgetNodes[w.widgetKey]}
				</WidgetShell>
			),
		}));

	return (
		<Grid container spacing={2}>
			<DashboardHeader
				startDate={startDate}
				endDate={endDate}
				onStartDateChange={handleStartDateChange}
				onEndDateChange={handleEndDateChange}
				selectedCategory={selectedCategory}
				onSelectCategory={setSelectedCategory}
				showTips={showTips}
				onToggleTips={toggleShowTips}
			/>

			<Dashboard items={dashboardItems} />
		</Grid>
	);
};
