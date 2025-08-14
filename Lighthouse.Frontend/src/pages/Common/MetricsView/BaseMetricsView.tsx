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
			<DashboardHeader
				startDate={startDate}
				endDate={endDate}
				onStartDateChange={(date) => date && setStartDate(date)}
				onEndDateChange={(date) => date && setEndDate(date)}
			/>

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
						// place top-left (row 1, first of 4 columns)
						colStart: 1,
						colSpan: 3,
						rowStart: 1,
						rowSpan: 1,
					});

					if (additionalItems && additionalItems.length > 0) {
						// Map additional items into the left column of row 2 by default
						// (e.g. "Features being worked on"). If an additional item already
						// specifies placement, keep it as-is.
						const mapped = additionalItems.map((it) =>
							typeof it.colStart === "number" || typeof it.rowStart === "number"
								? it
								: {
										...it,
										colStart: it.colStart ?? 1,
										colSpan: it.colSpan ?? 3,
										rowStart: it.rowStart ?? 2,
									},
						);

						items.push(...mapped);
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
						// top-right (4th column)
						colStart: 10,
						colSpan: 3,
						rowStart: 1,
						rowSpan: 1,
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
						// place in top row, second column and span two rows
						colStart: 4,
						colSpan: 3,
						rowStart: 1,
						rowSpan: 2,
					});

					items.push({
						id: "startedVsFinished",
						node: (
							<StartedVsFinishedDisplay
								startedItems={startedItems}
								closedItems={throughputData}
							/>
						),
						// place in top row, third column and span two rows
						colStart: 7,
						colSpan: 3,
						rowStart: 1,
						rowSpan: 2,
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
						// place as left half on row 3 (6 cols)
						colStart: 1,
						colSpan: 6,
						rowStart: 3,
						rowSpan: 4,
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
						// right half of row 3
						colStart: 7,
						colSpan: 6,
						rowStart: 3,
						rowSpan: 2,
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
						// left half of row 4
						colStart: 1,
						colSpan: 6,
						rowStart: 4,
						rowSpan: 2,
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
						// right half of row 4
						colStart: 7,
						colSpan: 6,
						rowStart: 4,
						rowSpan: 2,
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
						// simplified CFD as left half of row 5
						colStart: 1,
						colSpan: 6,
						rowStart: 5,
						rowSpan: 2,
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
						});
					}

					return items;
				})()}
			/>
		</Grid>
	);
};
