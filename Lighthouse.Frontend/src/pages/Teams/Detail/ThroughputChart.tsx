import { Card, CardContent, CircularProgress, Typography } from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import type { Throughput } from "../../../models/Forecasts/Throughput";

interface ThroughputBarChartProps {
	throughput: Throughput;
	startDate: Date;
}

const ThroughputBarChart: React.FC<ThroughputBarChartProps> = ({
	throughput,
	startDate,
}) => {
	const data = Array.from({ length: throughput.history }, (_, index) => {
		const targetDate = new Date(startDate);

		targetDate.setDate(targetDate.getDate() + index);

		return {
			day: targetDate.toLocaleDateString(),
			throughput: throughput.getThroughputOnDay(index),
		};
	});

	return throughput?.history > 0 ? (
		<Card sx={{ p: 2, borderRadius: 2 }}>
			<CardContent>
				<Typography variant="h6">
					Total Throughput: {throughput.totalThroughput} Items
				</Typography>

				<BarChart
					dataset={data}
					xAxis={[{ scaleType: "band", dataKey: "day" }]}
					series={[
						{
							dataKey: "throughput",
							color: "rgba(48, 87, 78, 1)",
						},
					]}
					height={500}
				/>
			</CardContent>
		</Card>
	) : (
		<CircularProgress />
	);
};

export default ThroughputBarChart;
