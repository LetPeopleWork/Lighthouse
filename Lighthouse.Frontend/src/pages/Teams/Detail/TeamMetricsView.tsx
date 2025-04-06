import { Grid } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import type { Throughput } from "../../../models/Forecasts/Throughput";
import type { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import ThroughputBarChart from "./ThroughputChart";

interface TeamMetricsViewProps {
	team: Team;
}

const TeamMetricsView: React.FC<TeamMetricsViewProps> = ({ team }) => {
	const [throughput, setThroughput] = useState<Throughput | null>(null);
	const { teamMetricsService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchThroughput = async () => {
			try {
				const throughputData = await teamMetricsService.getThroughput(team.id);
				if (throughputData) {
					setThroughput(throughputData);
				}
			} catch (error) {
				console.error("Error getting throughput:", error);
			}
		};

		fetchThroughput();
	}, [team.id, teamMetricsService]);

	return (
		<Grid container spacing={3}>
			<Grid size={{ xs: 6 }}>
				<InputGroup title="Throughput" initiallyExpanded={true}>
					{throughput && (
						<ThroughputBarChart
							startDate={team.throughputStartDate}
							throughput={throughput}
						/>
					)}
				</InputGroup>
			</Grid>

			{/*
			<Grid size={{xs: 6}}>
				<InputGroup title="Cycle Time" initiallyExpanded={true}>
					<CycleTimeScatterPlotChart team={team} />
				</InputGroup>
			</Grid>*/}
		</Grid>
	);
};

export default TeamMetricsView;
