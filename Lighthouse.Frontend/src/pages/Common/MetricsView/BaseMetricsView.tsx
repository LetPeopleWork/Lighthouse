import { Grid } from "@mui/material";
import { useEffect, useMemo, useState } from "react";
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
import type {
	IMetricsService,
	IProjectMetricsService,
} from "../../../services/Api/MetricsService";
import { useTerminology } from "../../../services/TerminologyContext";
import { appColors } from "../../../utils/theme/colors";
import ItemsInProgress, {
	type InProgressEntry,
} from "../../Teams/Detail/ItemsInProgress";
import type { DashboardItem } from "./Dashboard";
import Dashboard from "./Dashboard";
import DashboardHeader from "./DashboardHeader";

export interface BaseMetricsViewProps<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
> {
	entity: E;
	metricsService: IMetricsService<T>;
	title: string;
	defaultDateRange?: number;
	additionalItems?: InProgressEntry[];
	doingStates: string[];
}

export const BaseMetricsView = <
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
>({
	entity,
	metricsService,
	title,
	defaultDateRange = 30,
	additionalItems = [],
	doingStates,
}: BaseMetricsViewProps<T, E>) => {
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

	// URL state management for dates
	const [searchParams, setSearchParams] = useSearchParams();

	// Helper function to format date as YYYY-MM-DD
	const formatDate = (date: Date): string => {
		return date.toISOString().split("T")[0];
	};

	// Helper function to parse date from string
	const parseDate = (dateString: string): Date | null => {
		const date = new Date(dateString);
		return Number.isNaN(date.getTime()) ? null : date;
	};

	// Calculate default start date
	const getDefaultStartDate = (): Date => {
		const date = new Date();
		date.setDate(date.getDate() - defaultDateRange);
		return date;
	};

	// Initialize dates from URL or defaults
	const [startDate, setStartDate] = useState<Date>(() => {
		const urlStartDate = searchParams.get("startDate");
		if (urlStartDate) {
			const parsed = parseDate(urlStartDate);
			if (parsed) return parsed;
		}
		return getDefaultStartDate();
	});

	const [serviceLevelExpectation, setServiceLevelExpectation] =
		useState<IPercentileValue | null>(null);

	const [endDate, setEndDate] = useState<Date>(() => {
		const urlEndDate = searchParams.get("endDate");
		if (urlEndDate) {
			const parsed = parseDate(urlEndDate);
			if (parsed) return parsed;
		}
		return new Date();
	});

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const workInProgressTerm = getTerm(TERMINOLOGY_KEYS.WORK_IN_PROGRESS);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const dashboardId = `${"getFeaturesInProgress" in metricsService ? "Team" : "Project"}_${entity.id}`;

	// Helper to update URL with both date parameters
	const updateDateParams = (start: Date, end: Date) => {
		const newParams = new URLSearchParams(searchParams);
		newParams.set("startDate", formatDate(start));
		newParams.set("endDate", formatDate(end));
		setSearchParams(newParams, { replace: true });
	};

	// Handler for date changes that updates URL params
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

	useEffect(() => {
		const fetchPredictabilityData = async () => {
			try {
				const data =
					await metricsService.getMultiItemForecastPredictabilityScore(
						entity.id,
						startDate,
						endDate,
					);
				setPredictabilityData(data);
			} catch (error) {
				console.error("Error fetching predictability data:", error);
			}
		};

		fetchPredictabilityData();
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		const fetchThroughput = async () => {
			try {
				const data = await metricsService.getThroughput(
					entity.id,
					startDate,
					endDate,
				);
				setThroughputData(data);
			} catch (error) {
				console.error("Error getting throughput:", error);
			}
		};

		fetchThroughput();
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		const fetchStartedItems = async () => {
			try {
				const data = await metricsService.getStartedItems(
					entity.id,
					startDate,
					endDate,
				);
				setStartedItems(data);
			} catch (error) {
				console.error(`Error getting started ${workItemsTerm}:`, error);
			}
		};

		fetchStartedItems();
	}, [entity, metricsService, startDate, endDate, workItemsTerm]);

	useEffect(() => {
		const fetchInProgressItems = async () => {
			try {
				const items = await metricsService.getInProgressItems(entity.id);
				setInProgressItems(items);

				const wipData = await metricsService.getWorkInProgressOverTime(
					entity.id,
					startDate,
					endDate,
				);
				setWipOverTimeData(wipData);
			} catch (error) {
				console.error(`Error getting ${workItemsTerm} in progress:`, error);
			}
		};

		fetchInProgressItems();
	}, [entity, metricsService, startDate, endDate, workItemsTerm]);

	useEffect(() => {
		const fetchCycleTimeData = async () => {
			try {
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
			} catch (error) {
				console.error(`Error fetching ${cycleTimeTerm} data:`, error);
			}
		};

		fetchCycleTimeData();
	}, [entity, metricsService, startDate, endDate, cycleTimeTerm]);

	useEffect(() => {
		const fetchSizePercentileData = async (
			projectMetricsService: IProjectMetricsService,
		) => {
			try {
				const percentiles = await projectMetricsService.getSizePercentiles(
					entity.id,
					startDate,
					endDate,
				);
				setSizePercentileValues(percentiles);

				const allFeatures =
					await projectMetricsService.getAllFeaturesForSizeChart(
						entity.id,
						startDate,
						endDate,
					);
				setAllFeaturesForSizeChart(allFeatures);

				const featureSizePbc = await projectMetricsService.getFeatureSizePbc(
					entity.id,
					startDate,
					endDate,
				);
				setFeatureSizePbcData(featureSizePbc);

				const featureSizeEstimation =
					await projectMetricsService.getFeatureSizeEstimation(
						entity.id,
						startDate,
						endDate,
					);
				setFeatureSizeEstimationData(featureSizeEstimation);
			} catch (error) {
				console.error(`Error fetching Size Percentile Data:`, error);
			}
		};

		// Check if the service has the getAllFeaturesForSizeChart method (only ProjectMetricsService has this)
		if (
			"getAllFeaturesForSizeChart" in metricsService &&
			"getSizePercentiles" in metricsService &&
			"getFeatureSizePbc" in metricsService &&
			"getFeatureSizeEstimation" in metricsService
		) {
			fetchSizePercentileData(metricsService as IProjectMetricsService);
		}
	}, [metricsService, entity, startDate, endDate]);

	useEffect(() => {
		if (
			entity.serviceLevelExpectationProbability > 0 &&
			entity.serviceLevelExpectationRange > 0
		) {
			const sle: IPercentileValue = {
				value: entity.serviceLevelExpectationRange,
				percentile: entity.serviceLevelExpectationProbability,
			};
			setServiceLevelExpectation(sle);
		}
	}, [entity]);

	useEffect(() => {
		const fetchEstimationVsCycleTimeData = async () => {
			try {
				const data = await metricsService.getEstimationVsCycleTimeData(
					entity.id,
					startDate,
					endDate,
				);
				setEstimationVsCycleTimeData(data);
			} catch (error) {
				console.error("Error fetching estimation vs cycle time data:", error);
			}
		};

		fetchEstimationVsCycleTimeData();
	}, [entity, metricsService, startDate, endDate]);

	useEffect(() => {
		const fetchPbcData = async () => {
			try {
				const [throughputPbc, wipPbc, totalWorkItemAgePbc, cycleTimePbc] =
					await Promise.all([
						metricsService.getThroughputPbc(entity.id, startDate, endDate),
						metricsService.getWipPbc(entity.id, startDate, endDate),
						metricsService.getTotalWorkItemAgePbc(
							entity.id,
							startDate,
							endDate,
						),
						metricsService.getCycleTimePbc(entity.id, startDate, endDate),
					]);

				setThroughputPbcData(throughputPbc);
				setWipPbcData(wipPbc);
				setTotalWorkItemAgePbcData(totalWorkItemAgePbc);
				setCycleTimePbcData(cycleTimePbc);
			} catch (error) {
				console.error("Error fetching process behaviour chart data:", error);
			}
		};

		fetchPbcData();
	}, [entity, metricsService, startDate, endDate]);

	const workItemLookup = useMemo(() => {
		const lookup = new Map<number, IWorkItem>();

		// Helper to add items without duplicates
		const addItems = (items: IWorkItem[]) => {
			for (const item of items) {
				if (!lookup.has(item.id)) {
					lookup.set(item.id, item);
				}
			}
		};

		// Helper to extract all items from workItemsPerUnitOfTime structure
		const extractWorkItems = (
			workItemsPerUnitOfTime?: Record<string, IWorkItem[]>,
		) => {
			if (!workItemsPerUnitOfTime) return [];
			return Object.values(workItemsPerUnitOfTime).flat();
		};

		// Add throughput items (these take priority, so added first without duplicate check)
		const throughputItems = extractWorkItems(
			throughputData?.workItemsPerUnitOfTime,
		);
		for (const item of throughputItems) {
			lookup.set(item.id, item);
		}

		// Add remaining items (skip duplicates)
		addItems(extractWorkItems(wipOverTimeData?.workItemsPerUnitOfTime));
		addItems(cycleTimeData as unknown as IWorkItem[]);
		addItems(inProgressItems);

		// Add work items from estimation vs cycle time data
		if (estimationVsCycleTimeData?.status === "Ready") {
			for (const point of estimationVsCycleTimeData.dataPoints) {
				for (const id of point.workItemIds) {
					if (!lookup.has(id)) {
						// Items will be populated from other sources (throughput/cycleTime)
						// since they're closed items in the same date range
					}
				}
			}
		}

		for (const item of allFeaturesForSizeChart) {
			lookup.set(item.id, item as unknown as IWorkItem);
		}

		return lookup;
	}, [
		throughputData,
		wipOverTimeData,
		cycleTimeData,
		inProgressItems,
		allFeaturesForSizeChart,
		estimationVsCycleTimeData,
	]);

	const getPbcDashboardItems = (): DashboardItem[] => {
		const pbcItems: DashboardItem[] = [];

		const pbcConfigs: Array<{
			id: string;
			priority: number;
			data: ProcessBehaviourChartData | null;
			titleSuffix: string;
			type: ProcessBehaviourChartType;
		}> = [
			{
				id: "throughputPbc",
				priority: 18,
				data: throughputPbcData,
				titleSuffix: throughputTerm,
				type: ProcessBehaviourChartType.Throughput,
			},
			{
				id: "wipPbc",
				priority: 19,
				data: wipPbcData,
				titleSuffix: workInProgressTerm,
				type: ProcessBehaviourChartType.WorkInProgress,
			},
			{
				id: "totalWorkItemAgePbc",
				priority: 20,
				data: totalWorkItemAgePbcData,
				titleSuffix: `Total ${workItemAgeTerm}`,
				type: ProcessBehaviourChartType.TotalWorkItemAge,
			},
			{
				id: "cycleTimePbc",
				priority: 21,
				data: cycleTimePbcData,
				titleSuffix: cycleTimeTerm,
				type: ProcessBehaviourChartType.CycleTime,
			},
			{
				id: "featureSizePbc",
				priority: 22,
				data: featureSizePbcData,
				titleSuffix: `${featureTerm} Size`,
				type: ProcessBehaviourChartType.FeatureSize,
			},
		];

		for (const config of pbcConfigs) {
			if (config.data) {
				pbcItems.push({
					id: config.id,
					priority: config.priority,
					size: "large",
					node: (
						<ProcessBehaviourChart
							data={config.data}
							title={`${config.titleSuffix}`}
							workItemLookup={workItemLookup}
							type={config.type}
						/>
					),
				});
			}
		}

		return pbcItems;
	};

	const dashboardItems: DashboardItem[] = (() => {
		const items: DashboardItem[] = [];

		const inProgressEntries: InProgressEntry[] = [
			{
				title: `${title} in Progress:`,
				items: inProgressItems,
				sle:
					entity.serviceLevelExpectationRange > 0
						? entity.serviceLevelExpectationRange
						: undefined,
				idealWip: entity.systemWIPLimit > 0 ? entity.systemWIPLimit : undefined,
			},
			{
				title: `${blockedTerm}:`,
				items: inProgressItems.filter((item) => item.isBlocked),
				idealWip: 0,
			},
		];

		if (additionalItems && additionalItems.length > 0) {
			additionalItems.forEach((item) => {
				inProgressEntries.push(item);
			});
		}

		items.push(
			{
				id: "itemsInProgress",
				priority: 1,
				size: "small",
				node: <ItemsInProgress entries={inProgressEntries} />,
			},
			{
				id: "percentiles",
				priority: 2,
				size: "small",
				node: (
					<CycleTimePercentiles
						percentileValues={percentileValues}
						serviceLevelExpectation={serviceLevelExpectation}
						items={cycleTimeData}
					/>
				),
			},
			{
				id: "startedVsFinished",
				priority: 3,
				size: "small",
				node: (
					<StartedVsFinishedDisplay
						startedItems={startedItems}
						closedItems={throughputData}
					/>
				),
			},
			{
				id: "totalWorkItemAge",
				priority: 4,
				size: "small",
				node: (
					<TotalWorkItemAgeWidget
						entityId={entity.id}
						metricsService={metricsService}
					/>
				),
			},
			{
				id: "throughput",
				priority: 10,
				size: "large",
				node: throughputData ? (
					<BarRunChart
						title={`${title} Completed`}
						startDate={startDate}
						chartData={throughputData}
						displayTotal={true}
						predictabilityData={predictabilityData}
					/>
				) : null,
			},
			{
				id: "cycleScatter",
				priority: 11,
				size: "large",
				node: (
					<CycleTimeScatterPlotChart
						cycleTimeDataPoints={cycleTimeData}
						percentileValues={percentileValues}
						serviceLevelExpectation={serviceLevelExpectation}
					/>
				),
			},
			{
				id: "workDistribution",
				priority: 12,
				size: "large",
				node: (
					<WorkDistributionChart
						workItems={[...cycleTimeData, ...inProgressItems] as IWorkItem[]}
						title="Work Distribution"
					/>
				),
			},
			{
				id: "aging",
				priority: 13,
				size: "large",
				node: (
					<WorkItemAgingChart
						inProgressItems={inProgressItems}
						percentileValues={percentileValues}
						serviceLevelExpectation={serviceLevelExpectation}
						doingStates={doingStates}
					/>
				),
			},
			{
				id: "wipOverTime",
				priority: 14,
				size: "large",
				node: wipOverTimeData ? (
					<LineRunChart
						title={`${title} In Progress Over Time`}
						startDate={startDate}
						chartData={wipOverTimeData}
						displayTotal={false}
						wipLimit={entity.systemWIPLimit}
					/>
				) : null,
			},
			{
				id: "totalWorkItemAgeOverTime",
				priority: 15,
				size: "large",
				node: wipOverTimeData ? (
					<TotalWorkItemAgeRunChart
						title={`${title} Total Work Item Age Over Time`}
						startDate={startDate}
						wipOverTimeData={wipOverTimeData}
					/>
				) : null,
			},
			{
				id: "stacked",
				priority: 16,
				size: "large",
				node:
					throughputData && startedItems ? (
						<StackedAreaChart
							title="Simplified Cumulative Flow Diagram"
							startDate={startDate}
							areas={[
								{
									index: 1,
									title: "Doing",
									area: startedItems,
									color: appColors.primary.light,
									startOffset: wipOverTimeData?.getValueOnDay(0) ?? 0,
								},
								{
									index: 2,
									title: "Done",
									area: throughputData,
									color: appColors.secondary.light,
								},
							]}
						/>
					) : null,
			},
		);

		// Estimation vs Cycle Time chart (conditional — hidden when not configured)
		if (
			estimationVsCycleTimeData &&
			estimationVsCycleTimeData.status !== "NotConfigured"
		) {
			items.push({
				id: "estimationVsCycleTime",
				priority: 11.5,
				size: "large",
				node: (
					<EstimationVsCycleTimeChart
						data={estimationVsCycleTimeData}
						workItemLookup={workItemLookup}
					/>
				),
			});
		}

		// Feature size chart (conditional)
		if (allFeaturesForSizeChart.length > 0) {
			items.push({
				id: "featureSize",
				priority: 17,
				size: "large", // Use standardized large size
				node: (
					<FeatureSizeScatterPlotChart
						sizeDataPoints={allFeaturesForSizeChart}
						sizePercentileValues={sizePercentileValues}
						estimationData={featureSizeEstimationData ?? undefined}
					/>
				),
			});
		}

		// Process Behaviour Charts (conditional — hidden when baseline is not configured)
		items.push(...getPbcDashboardItems());

		return items;
	})();

	return (
		<Grid container spacing={2}>
			<DashboardHeader
				startDate={startDate}
				endDate={endDate}
				onStartDateChange={handleStartDateChange}
				onEndDateChange={handleEndDateChange}
				dashboardId={dashboardId}
			/>

			<Dashboard items={dashboardItems} dashboardId={dashboardId} />
		</Grid>
	);
};
