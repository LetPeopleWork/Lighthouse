import { Grid } from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import StackedAreaChart from "../../../components/Common/Charts/StackedAreaChart";
import StartedVsFinishedDisplay from "../../../components/Common/Charts/StartedVsFinishedDisplay";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";
import type { IFeature } from "../../../models/Feature";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { appColors } from "../../../utils/theme/colors";
import ItemsInProgress from "../../Teams/Detail/ItemsInProgress";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";

export interface BaseMetricsViewProps<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
> {
	entity: E;
	metricsService: IMetricsService<T>;
	title: string;
	idealWip?: number;
	defaultDateRange?: number;
	renderAdditionalComponents?: () => React.ReactNode;
}

export const BaseMetricsView = <
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
>({
	entity,
	metricsService,
	title,
	idealWip = 0,
	defaultDateRange = 30,
	renderAdditionalComponents,
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
	const [startedItems, setStartedItems] = useState<RunChartData | null>(null);

	const [startDate, setStartDate] = useState<Date>(() => {
		const date = new Date();
		date.setDate(date.getDate() - defaultDateRange);
		return date;
	});

	const [serviceLevelExpectation, setServiceLevelExpectation] =
		useState<IPercentileValue | null>(null);

	const [endDate, setEndDate] = useState<Date>(new Date());

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
				console.error("Error getting started items:", error);
			}
		};

		fetchStartedItems();
	}, [entity, metricsService, startDate, endDate]);

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
				console.error("Error getting items in progress:", error);
			}
		};

		fetchInProgressItems();
	}, [entity, metricsService, startDate, endDate]);

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
				console.error("Error fetching cycle time data:", error);
			}
		};

		fetchCycleTimeData();
	}, [entity, metricsService, startDate, endDate]);

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
			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<DateRangeSelector
					startDate={startDate}
					endDate={endDate}
					onStartDateChange={(date) => date && setStartDate(date)}
					onEndDateChange={(date) => date && setEndDate(date)}
				/>
			</Grid>

			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<ItemsInProgress
					title={`${title} in Progress:`}
					items={inProgressItems}
					idealWip={idealWip}
				/>

				{renderAdditionalComponents?.()}
			</Grid>

			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<CycleTimePercentiles percentileValues={percentileValues} />
			</Grid>

			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={throughputData}
				/>
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				{throughputData && (
					<BarRunChart
						title={`${title} Completed`}
						startDate={startDate}
						chartData={throughputData}
						displayTotal={true}
					/>
				)}
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				<CycleTimeScatterPlotChart
					cycleTimeDataPoints={cycleTimeData}
					percentileValues={percentileValues}
					serviceLevelExpectation={serviceLevelExpectation}
				/>
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				{wipOverTimeData && (
					<LineRunChart
						title={`${title} In Progress Over Time`}
						startDate={startDate}
						chartData={wipOverTimeData}
						displayTotal={false}
					/>
				)}
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				{throughputData && startedItems && (
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
				)}
			</Grid>
		</Grid>
	);
};
