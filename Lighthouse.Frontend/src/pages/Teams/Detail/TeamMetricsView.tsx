import { Grid } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";
import type { Throughput } from "../../../models/Forecasts/Throughput";
import type { Team } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import ItemsInProgress from "./ItemsInProgress";
import ThroughputBarChart from "./ThroughputChart";

interface TeamMetricsViewProps {
	team: Team;
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [throughput, setThroughput] = useState<Throughput | null>(null);
	const [inProgressFeatures, setInProgressFeatures] = useState<IWorkItem[]>([]);
	const [inProgressItems, setInProgressItems] = useState<IWorkItem[]>([]);
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
					setThroughput(throughputData);
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

	return (
		<>
			<DateRangeSelector
				startDate={startDate}
				endDate={endDate}
				onStartDateChange={(date) => date && setStartDate(date)}
				onEndDateChange={(date) => date && setEndDate(date)}
			/>

			<Grid container spacing={3}>
				<Grid size={{ xs: 3 }} spacing={3}>
					<ItemsInProgress
						title="Features In Progress"
						items={inProgressFeatures}
						idealWip={team.featureWip}
					/>
				</Grid>
				<Grid size={{ xs: 3 }} spacing={3}>
					<ItemsInProgress
						title="Work Items In Progress"
						items={inProgressItems}
						idealWip={0}
					/>
				</Grid>
				<Grid size={{ xs: 6 }}>
					{throughput && (
						<ThroughputBarChart startDate={startDate} throughput={throughput} />
					)}
				</Grid>

				{/*
				<Grid size={{xs: 6}}>
					<InputGroup title="Cycle Time" initiallyExpanded={true}>
						<CycleTimeScatterPlotChart team={team} />
					</InputGroup>
				</Grid>*/}
			</Grid>
		</>
	);
};

export default TeamMetricsView;
