import { Grid } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";
import type { RunChartData } from "../../../models/Forecasts/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IProject } from "../../../models/Project/Project";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
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
			<Grid size={{ xs: 2 }}>
				<DateRangeSelector
					startDate={startDate}
					endDate={endDate}
					onStartDateChange={(date) => date && setStartDate(date)}
					onEndDateChange={(date) => date && setEndDate(date)}
				/>
			</Grid>

			<Grid size={{ xs: 4 }}>
				<ItemsInProgress
					title="Features in Progress:"
					items={inProgressFeatures}
					idealWip={0}
				/>
			</Grid>

			<Grid size={{ xs: 4 }} spacing={3}>
				<CycleTimePercentiles percentileValues={percentileValues} />
			</Grid>

			<Grid size={{ xs: 6 }}>
				{featuresCompletedData && (
					<BarRunChart
						title="Features Completed"
						startDate={startDate}
						chartData={featuresCompletedData}
						displayTotal={true}
					/>
				)}
			</Grid>

			<Grid size={{ xs: 6 }}>
				<CycleTimeScatterPlotChart
					cycleTimeDataPoints={cycleTimeData}
					percentileValues={percentileValues}
				/>
			</Grid>

			<Grid size={{ xs: 6 }}>
				{featuresInProgressData && (
					<LineRunChart
						title="Features In Progress Over Time"
						startDate={startDate}
						chartData={featuresInProgressData}
						displayTotal={false}
					/>
				)}
			</Grid>
		</Grid>
	);
};

export default ProjectMetricsView;
