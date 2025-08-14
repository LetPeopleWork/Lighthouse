import { Grid } from "@mui/material";
import { useEffect, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import FeatureSizeScatterPlotChart from "../../../components/Common/Charts/FeatureSizeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import StackedAreaChart from "../../../components/Common/Charts/StackedAreaChart";
import StartedVsFinishedDisplay from "../../../components/Common/Charts/StartedVsFinishedDisplay";
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
import ItemsInProgress from "../../Teams/Detail/ItemsInProgress";
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
	additionalItems?: DashboardItem[];
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

		if (metricsService as unknown as IProjectMetricsService) {
			fetchSizePercentileData(
				metricsService as unknown as IProjectMetricsService,
			);
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

	return (
		<Grid container spacing={2}>
			<Grid size={{ xs: 12 }}>
				<DashboardHeader
					startDate={startDate}
					endDate={endDate}
					onStartDateChange={(date) => date && setStartDate(date)}
					onEndDateChange={(date) => date && setEndDate(date)}
				/>
			</Grid>

			<Dashboard
				items={((): DashboardItem[] => {
					const items: DashboardItem[] = [];

					items.push({
						id: "inProgress",
						node: (
							<ItemsInProgress
								title={`${title} in Progress:`}
								items={inProgressItems}
								sle={
									entity.serviceLevelExpectationRange > 0
										? entity.serviceLevelExpectationRange
										: undefined
								}
								idealWip={
									entity.systemWIPLimit > 0 ? entity.systemWIPLimit : undefined
								}
							/>
						),
						variant: "small",
					});

					if (additionalItems && additionalItems.length > 0) {
						items.push(...additionalItems);
					}

					items.push({
						id: "blocked",
						node: (
							<ItemsInProgress
								title={`${blockedTerm}:`}
								items={inProgressItems.filter((item) => item.isBlocked)}
								idealWip={0}
							/>
						),
						variant: "small",
					});

					items.push({
						id: "percentiles",
						node: (
							<CycleTimePercentiles
								percentileValues={percentileValues}
								serviceLevelExpectation={serviceLevelExpectation}
								items={cycleTimeData}
							/>
						),
						variant: "small",
					});

					items.push({
						id: "startedVsFinished",
						node: (
							<StartedVsFinishedDisplay
								startedItems={startedItems}
								closedItems={throughputData}
							/>
						),
						variant: "small",
					});

					items.push({
						id: "throughput",
						node: throughputData ? (
							<BarRunChart
								title={`${title} Completed`}
								startDate={startDate}
								chartData={throughputData}
								displayTotal={true}
								predictabilityData={predictabilityData}
							/>
						) : null,
						variant: "large",
					});

					items.push({
						id: "cycleScatter",
						node: (
							<CycleTimeScatterPlotChart
								cycleTimeDataPoints={cycleTimeData}
								percentileValues={percentileValues}
								serviceLevelExpectation={serviceLevelExpectation}
							/>
						),
						variant: "large",
					});

					items.push({
						id: "aging",
						node: (
							<WorkItemAgingChart
								inProgressItems={inProgressItems}
								percentileValues={percentileValues}
								serviceLevelExpectation={serviceLevelExpectation}
								doingStates={doingStates}
							/>
						),
						variant: "large",
					});

					items.push({
						id: "wipOverTime",
						node: wipOverTimeData ? (
							<LineRunChart
								title={`${title} In Progress Over Time`}
								startDate={startDate}
								chartData={wipOverTimeData}
								displayTotal={false}
								wipLimit={entity.systemWIPLimit}
							/>
						) : null,
						variant: "large",
					});

					items.push({
						id: "stacked",
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
						variant: "large",
					});

					// Feature size chart only when features are present
					if (cycleTimeData.length > 0 && "size" in cycleTimeData[0]) {
						items.push({
							id: "featureSize",
							node: (
								<FeatureSizeScatterPlotChart
									sizeDataPoints={cycleTimeData as IFeature[]}
									sizePercentileValues={sizePercentileValues}
								/>
							),
							variant: "large",
						});
					}

					return items;
				})()}
			/>
		</Grid>
	);
};
