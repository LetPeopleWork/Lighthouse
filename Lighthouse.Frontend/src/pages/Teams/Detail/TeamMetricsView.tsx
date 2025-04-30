import { Grid } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import StartedVsFinishedDisplay from "../../../components/Common/Charts/StartedVsFinishedDisplay";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import ItemsInProgress from "./ItemsInProgress";

interface TeamMetricsViewProps {
	team: Team;
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [throughputRunChartData, setThroughputRunChartData] =
		useState<RunChartData | null>(null);

	const [wipOverTimeData, setWipOverTimeData] = useState<RunChartData | null>(
		null,
	);

	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const [inProgressItems, setInProgressItems] = useState<IWorkItem[]>([]);
	const [cycleTimeData, setCycleTimeData] = useState<IWorkItem[]>([]);
	const [percentileValues, setPercentileValues] = useState<IPercentileValue[]>(
		[],
	);

	const [startedItemsData, setStartedItemsData] = useState<RunChartData | null>(
		() => {
			const valuePerUnitOfTime = Array(30)
				.fill(0)
				.map(() => Math.floor(Math.random() * 5) + 1);
			const total = valuePerUnitOfTime.reduce((sum, value) => sum + value, 0);
			return new RunChartData(valuePerUnitOfTime, 30, total);
		},
	);

	const { teamMetricsService } = useContext(ApiServiceContext);

	const [startDate, setStartDate] = useState<Date>(() => {
		const date = new Date();
		date.setDate(date.getDate() - 30);
		return date;
	});
	const [endDate, setEndDate] = useState<Date>(new Date());

	useEffect(() => {
		const fetchThroughput = async () => {
			try {
				const throughputData = await teamMetricsService.getThroughput(
					team.id,
					startDate,
					endDate,
				);
				if (throughputData) {
					setThroughputRunChartData(throughputData);
				}
			} catch (error) {
				console.error("Error getting throughput:", error);
			}
		};

		fetchThroughput();
	}, [team.id, teamMetricsService, startDate, endDate]);

	useEffect(() => {
		const fetchFeatures = async () => {
			try {
				const featuresData = await teamMetricsService.getFeaturesInProgress(
					team.id,
				);
				setInProgressFeatures(featuresData);
			} catch (err) {
				console.error("Error fetching features in progress:", err);
			}
		};

		fetchFeatures();
	}, [team.id, teamMetricsService]);

	useEffect(() => {
		const fetchInProgressItems = async () => {
			try {
				const wipData = await teamMetricsService.getInProgressItems(team.id);
				setInProgressItems(wipData);

				const wipOverTimeData =
					await teamMetricsService.getWorkInProgressOverTime(
						team.id,
						startDate,
						endDate,
					);
				setWipOverTimeData(wipOverTimeData);
			} catch (err) {
				console.error("Error fetching items in progress:", err);
			}
		};

		fetchInProgressItems();
	}, [team.id, teamMetricsService, startDate, endDate]);

	useEffect(() => {
		const fetchCycleTimeData = async () => {
			try {
				const cycleTimeData = await teamMetricsService.getCycleTimeData(
					team.id,
					startDate,
					endDate,
				);
				setCycleTimeData(cycleTimeData);

				const percentiles = await teamMetricsService.getCycleTimePercentiles(
					team.id,
					startDate,
					endDate,
				);
				setPercentileValues(percentiles);
			} catch (err) {
				console.error("Error fetching cycle time data:", err);
			}
		};

		fetchCycleTimeData();
	}, [team.id, teamMetricsService, startDate, endDate]);

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
					title="Work Items In Progress:"
					items={inProgressItems}
					idealWip={0}
				/>
				<ItemsInProgress
					title="Features being Worked On:"
					items={inProgressFeatures}
					idealWip={team.featureWip}
				/>
			</Grid>
			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<CycleTimePercentiles percentileValues={percentileValues} />
			</Grid>
			<Grid size={{ xs: 12, sm: 8, md: 6, lg: 4, xl: 3 }}>
				<StartedVsFinishedDisplay
					startedItems={startedItemsData}
					closedItems={throughputRunChartData}
				/>
			</Grid>

			<Grid size={{ xs: 12, sm: 12, md: 12, lg: 9, xl: 6 }}>
				{throughputRunChartData && (
					<BarRunChart
						title="Throughput"
						startDate={startDate}
						chartData={throughputRunChartData}
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
				{wipOverTimeData && (
					<LineRunChart
						title="WIP Over Time"
						startDate={startDate}
						chartData={wipOverTimeData}
						displayTotal={false}
					/>
				)}
			</Grid>
		</Grid>
	);
};

export default TeamMetricsView;
