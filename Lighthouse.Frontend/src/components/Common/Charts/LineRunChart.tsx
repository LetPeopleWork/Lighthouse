import { useTheme } from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import type React from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import BaseRunChart from "./BaseRunChart";

interface LineRunChartProps {
	chartData: RunChartData;
	startDate: Date;
	title?: string;
	displayTotal?: boolean;
}

const LineRunChart: React.FC<LineRunChartProps> = ({
	chartData,
	startDate,
	title = "Run Chart",
	displayTotal = false,
}) => {
	const theme = useTheme();

	return (
		<BaseRunChart
			chartData={chartData}
			startDate={startDate}
			title={title}
			displayTotal={displayTotal}
		>
			{(data) => {
				const xLabels = data.map((item) => item.day);
				const yValues = data.map((item) => item.value);

				return (
					<LineChart
						yAxis={[
							{
								min: 0,
								valueFormatter: (value: number) => {
									return Number.isInteger(value) ? value.toString() : "";
								},
							},
						]}
						xAxis={[
							{
								data: xLabels,
								scaleType: "point",
							},
						]}
						series={[
							{
								data: yValues,
								color: theme.palette.primary.main,
							},
						]}
						height={500}
					/>
				);
			}}
		</BaseRunChart>
	);
};

export default LineRunChart;
