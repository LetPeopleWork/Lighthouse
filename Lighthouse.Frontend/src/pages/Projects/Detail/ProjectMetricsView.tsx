import { Grid } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import StackedAreaChart from "../../../components/Common/Charts/StackedAreaChart";
import StartedVsFinishedDisplay from "../../../components/Common/Charts/StartedVsFinishedDisplay";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IProject } from "../../../models/Project/Project";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { appColors } from "../../../utils/theme/colors";
import ItemsInProgress from "../../Teams/Detail/ItemsInProgress";

interface ProjectMetricsViewProps {
	project: IProject;
}

const ProjectMetricsView: React.FC<ProjectMetricsViewProps> = ({ project }) => {
	const [featuresCompletedData, setFeaturesCompletedData] =
		useState<RunChartData | null>(null);
	const [featuresInProgressData, setFeaturesInProgressData] =
		useState<RunChartData | null>(null);
	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const [cycleTimeData, setCycleTimeData] = useState<IWorkItem[]>([]);
	const [percentileValues, setPercentileValues] = useState<IPercentileValue[]>(
		[],
	);

	const [startedItems, setStartedItems] = useState<RunChartData | null>(null);

	const { projectMetricsService } = useContext(ApiServiceContext);

	const [startDate, setStartDate] = useState<Date>(() => {
		const date = new Date();
		date.setDate(date.getDate() - 91);
		return date;
	});
	const [endDate, setEndDate] = useState<Date>(new Date());

	useEffect(() => {
		const fetchFeaturesCompleted = async () => {
			try {
				const data = await projectMetricsService.getThroughputForProject(
					project.id,
					startDate,
					endDate,
				);
				setFeaturesCompletedData(data);
			} catch (error) {
				console.error("Error getting features completed:", error);
			}
		};

		fetchFeaturesCompleted();
	}, [project.id, projectMetricsService, startDate, endDate]);

	useEffect(() => {
		const fetchStartedItems = async () => {
			try {
				const startedItemsData = await projectMetricsService.getStartedItems(
					project.id,
					startDate,
					endDate,
				);
				if (startedItemsData) {
					setStartedItems(startedItemsData);
				}
			} catch (error) {
				console.error("Error getting throughput:", error);
			}
		};

		fetchStartedItems();
	}, [project.id, projectMetricsService, startDate, endDate]);

	useEffect(() => {
		const fetchFeaturesInProgress = async () => {
			try {
				const wipData =
					await projectMetricsService.getInProgressFeaturesForProject(
						project.id,
					);
				setInProgressFeatures(wipData);

				const data =
					await projectMetricsService.getFeaturesInProgressOverTimeForProject(
						project.id,
						startDate,
						endDate,
					);
				setFeaturesInProgressData(data);
			} catch (error) {
				console.error("Error getting features in progress:", error);
			}
		};

		fetchFeaturesInProgress();
	}, [project.id, projectMetricsService, startDate, endDate]);

	useEffect(() => {
		const fetchCycleTimeData = async () => {
			try {
				const cycleTimeData =
					await projectMetricsService.getCycleTimeDataForProject(
						project.id,
						startDate,
						endDate,
					);
				setCycleTimeData(cycleTimeData);

				const percentiles =
					await projectMetricsService.getCycleTimePercentilesForProject(
						project.id,
						startDate,
						endDate,
					);
				setPercentileValues(percentiles);
			} catch (err) {
				console.error("Error fetching cycle time data:", err);
			}
		};

		fetchCycleTimeData();
	}, [project.id, projectMetricsService, startDate, endDate]);

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
					title="Features in Progress:"
					items={inProgressFeatures}
					idealWip={0}
				/>
			</Grid>

			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<CycleTimePercentiles percentileValues={percentileValues} />
			</Grid>

			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<StartedVsFinishedDisplay
					startedItems={startedItems}
					closedItems={featuresCompletedData}
				/>
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				{featuresCompletedData && (
					<BarRunChart
						title="Features Completed"
						startDate={startDate}
						chartData={featuresCompletedData}
						displayTotal={true}
					/>
				)}
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				<CycleTimeScatterPlotChart
					cycleTimeDataPoints={cycleTimeData}
					percentileValues={percentileValues}
				/>
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				{featuresInProgressData && (
					<LineRunChart
						title="Features In Progress Over Time"
						startDate={startDate}
						chartData={featuresInProgressData}
						displayTotal={false}
					/>
				)}
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				{featuresCompletedData && startedItems && (
					<StackedAreaChart
						title="Simplified Cumulative Flow Diagram"
						startDate={startDate}
						areas={[
							{
								index: 1,
								title: "Doing",
								area: startedItems,
								color: appColors.primary.light,
								startOffset: featuresInProgressData?.getValueOnDay(0) ?? 0,
							},
							{
								index: 2,
								title: "Done",
								area: featuresCompletedData,
								color: appColors.secondary.light,
							},
						]}
					/>
				)}
			</Grid>
		</Grid>
	);
};

export default ProjectMetricsView;
