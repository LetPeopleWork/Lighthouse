import { LineChart } from "@mui/x-charts/LineChart";
import type React from "react";
import type { RunChartData } from "../../../models/Forecasts/RunChartData";
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
						xAxis={[
							{
								data: xLabels,
								scaleType: "point",
							},
						]}
						series={[
							{
								data: yValues,
								color: "rgba(48, 87, 78, 1)",
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
