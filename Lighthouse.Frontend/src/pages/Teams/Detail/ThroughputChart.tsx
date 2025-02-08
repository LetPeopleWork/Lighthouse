import { CircularProgress } from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import { Throughput } from "../../../models/Forecasts/Throughput";
import type { ITeam } from "../../../models/Team/Team";

interface ThroughputBarChartProps {
	team: ITeam;
}

const ThroughputBarChart: React.FC<ThroughputBarChartProps> = ({ team }) => {
	const throughput = new Throughput(team.throughput);

	const data = Array.from({ length: throughput.history }, (_, index) => {
		const targetDate = new Date(team.throughputStartDate);

		targetDate.setDate(targetDate.getDate() + index);

		return {
			day: targetDate.toLocaleDateString(),
			throughput: throughput.getThroughputOnDay(index),
		};
	});

	return throughput.history > 0 ? (
		<BarChart
			dataset={data}
			xAxis={[{ scaleType: "band", dataKey: "day" }]}
			series={[
				{
					dataKey: "throughput",
					label: "Throughput",
					color: "rgba(48, 87, 78, 1)",
				},
			]}
			height={500}
		/>
	) : (
		<CircularProgress />
	);
};

export default ThroughputBarChart;
