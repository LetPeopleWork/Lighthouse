import { Grid } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";
import type { RunChartData } from "../../../models/Forecasts/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import CycleTimePercentiles from "./CycleTimePercentiles";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";
import ItemsInProgress from "./ItemsInProgress";

interface TeamMetricsViewProps {
	team: Team;
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [throughputRunChartData, setThroughputRunChartData] =
		useState<RunChartData | null>(null);
	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const [inProgressItems, setInProgressItems] = useState<IWorkItem[]>([]);
	const [cycleTimeData, setCycleTimeData] = useState<IWorkItem[]>([]);
	const [percentileValues, setPercentileValues] = useState<IPercentileValue[]>(
		[],
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
			} catch (err) {
				console.error("Error fetching items in progress:", err);
			}
		};

		fetchInProgressItems();
	}, [team.id, teamMetricsService]);

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
			<Grid size={{ xs: 4 }}>
				<DateRangeSelector
					startDate={startDate}
					endDate={endDate}
					onStartDateChange={(date) => date && setStartDate(date)}
					onEndDateChange={(date) => date && setEndDate(date)}
				/>
			</Grid>
			<Grid size={{ xs: 4 }}>
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
			<Grid size={{ xs: 4 }} spacing={3}>
				<CycleTimePercentiles percentileValues={percentileValues} />
			</Grid>

			<Grid size={{ xs: 6 }}>
				{throughputRunChartData && (
					<BarRunChart
						title="Throughput"
						startDate={startDate}
						chartData={throughputRunChartData}
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
		</Grid>
	);
};

export default TeamMetricsView;
