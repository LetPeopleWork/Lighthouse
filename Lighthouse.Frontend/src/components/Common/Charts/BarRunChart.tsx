import { useTheme } from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import type { RunChartData } from "../../../models/Forecasts/RunChartData";
import BaseRunChart from "./BaseRunChart";

interface BarRunChartProps {
	chartData: RunChartData;
	startDate: Date;
	displayTotal?: boolean;
	title?: string;
}

const BarRunChart: React.FC<BarRunChartProps> = ({
	chartData,
	startDate,
	displayTotal = false,
	title = "Bar Chart",
}) => {
	const theme = useTheme();

	return (
		<BaseRunChart
			chartData={chartData}
			startDate={startDate}
			title={title}
			displayTotal={displayTotal}
		>
			{(data) => (
				<BarChart
					dataset={data.map((item) => ({
						day: item.day,
						value: item.value,
					}))}
					yAxis={[
						{
							min: 0,
							valueFormatter: (value: number) => {
								return Number.isInteger(value) ? value.toString() : "";
							},
						},
					]}
					xAxis={[{ scaleType: "band", dataKey: "day" }]}
					series={[
						{
							dataKey: "value",
							color: theme.palette.primary.main,
						},
					]}
					height={500}
				/>
			)}
		</BaseRunChart>
	);
};

export default BarRunChart;
