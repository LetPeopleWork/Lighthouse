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
					xAxis={[{ scaleType: "band", dataKey: "day" }]}
					series={[
						{
							dataKey: "value",
							color: "rgba(48, 87, 78, 1)",
						},
					]}
					height={500}
				/>
			)}
		</BaseRunChart>
	);
};

export default BarRunChart;
