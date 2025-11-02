import { Grid } from "@mui/material";
import { useEffect, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import FeatureSizeScatterPlotChart from "../../../components/Common/Charts/FeatureSizeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import StackedAreaChart from "../../../components/Common/Charts/StackedAreaChart";
import StartedVsFinishedDisplay from "../../../components/Common/Charts/StartedVsFinishedDisplay";
import TotalWorkItemAgeRunChart from "../../../components/Common/Charts/TotalWorkItemAgeRunChart";
import TotalWorkItemAgeWidget from "../../../components/Common/Charts/TotalWorkItemAgeWidget";
import WorkItemAgingChart from "../../../components/Common/Charts/WorkItemAgingChart";
import type { IFeature } from "../../../models/Feature";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
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

	const [startedItems, setStartedItems] = useState<RunChartData | null>(null);
	const [predictabilityData, setPredictabilityData] =
		useState<IForecastPredictabilityScore | null>(null);

	const [startDate, setStartDate] = useState<Date>(() => {
		const date = new Date();
		date.setDate(date.getDate() - defaultDateRange);
		return date;
	});

	const [serviceLevelExpectation, setServiceLevelExpectation] =
		useState<IPercentileValue | null>(null);

	const [endDate, setEndDate] = useState<Date>(new Date());

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const dashboardId = `${"getFeaturesInProgress" in metricsService ? "Team" : "Project"}_${entity.id}`;

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
			} catch (error) {
				console.error(`Error fetching Size Percentile Data:`, error);
			}
		};

		// Check if the service has the getSizePercentiles method (only ProjectMetricsService has this)
		if (
			"getSizePercentiles" in metricsService &&
			typeof (metricsService as IProjectMetricsService).getSizePercentiles ===
				"function"
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

		items.push({
			id: "itemsInProgress",
			priority: 1,
			size: "small",
			node: <ItemsInProgress entries={inProgressEntries} />,
		});

		items.push({
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
		});

		items.push({
			id: "startedVsFinished",
			priority: 3,
			size: "small",
			node: (
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={throughputData}
				/>
			),
		});

		items.push({
			id: "totalWorkItemAge",
			priority: 4,
			size: "small",
			node: (
				<TotalWorkItemAgeWidget
					entityId={entity.id}
					metricsService={metricsService}
				/>
			),
		});

		items.push({
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
		});

		items.push({
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
		});
		items.push({
			id: "aging",
			priority: 12,
			size: "large",
			node: (
				<WorkItemAgingChart
					inProgressItems={inProgressItems}
					percentileValues={percentileValues}
					serviceLevelExpectation={serviceLevelExpectation}
					doingStates={doingStates}
				/>
			),
		});

		items.push({
			id: "wipOverTime",
			priority: 13,
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
		});

		items.push({
			id: "totalWorkItemAgeOverTime",
			priority: 14,
			size: "large",
			node: wipOverTimeData ? (
				<TotalWorkItemAgeRunChart
					title={`${title} Total Work Item Age Over Time`}
					startDate={startDate}
					wipOverTimeData={wipOverTimeData}
				/>
			) : null,
		});

		items.push({
			id: "stacked",
			priority: 15,
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
		});

		// Feature size chart (conditional)
		if (cycleTimeData.length > 0 && "size" in cycleTimeData[0]) {
			items.push({
				id: "featureSize",
				priority: 16,
				size: "large", // Use standardized large size
				node: (
					<FeatureSizeScatterPlotChart
						sizeDataPoints={cycleTimeData as IFeature[]}
						sizePercentileValues={sizePercentileValues}
					/>
				),
			});
		}

		return items;
	})();

	return (
		<Grid container spacing={2}>
			<DashboardHeader
				startDate={startDate}
				endDate={endDate}
				onStartDateChange={(date) => date && setStartDate(date)}
				onEndDateChange={(date) => date && setEndDate(date)}
				dashboardId={dashboardId}
			/>

			<Dashboard items={dashboardItems} dashboardId={dashboardId} />
		</Grid>
	);
};
