import {
	Card,
	CardContent,
	FormControlLabel,
	Switch,
	Typography,
} from "@mui/material";
import { LineChart } from "@mui/x-charts/LineChart";
import { addDays } from "date-fns";
import type React from "react";
import { useEffect, useState } from "react";
import type { RunChartData } from "../../../models/Metrics/RunChartData";

export interface AreaChartItem {
	index: number;
	title: string;
	area: RunChartData;
	startOffset?: number;
	color?: string;
}

interface StackedAreaChartProps {
	areas: AreaChartItem[];
	startDate: Date;
	title?: string;
}

const StackedAreaChart: React.FC<StackedAreaChartProps> = ({
	areas,
	startDate,
	title = "Stacked Area Chart",
}) => {
	const [chartData, setChartData] = useState<Date[]>([]);
	const [showTrend, setShowTrend] = useState<boolean>(true);

	const sortedAreas = [...areas].sort((a, b) => a.index - b.index);

	const maxHistory = Math.max(...sortedAreas.map((item) => item.area.history));

	useEffect(() => {
		const data: Date[] = [];
		for (let i = 0; i < maxHistory; i++) {
			const date = addDays(startDate, i);
			data.push(date);
		}
		setChartData(data);
	}, [maxHistory, startDate]);

	if (chartData.length === 0) {
		return null;
	}

	const areaDataArrays = sortedAreas.map((areaItem) => {
		const areaData = Array(chartData.length).fill(0);

		for (let i = 0; i < chartData.length; i++) {
			if (i < areaItem.area.history) {
				areaData[i] = areaItem.area.getValueOnDay(i);
			}
		}

		if (areaItem.startOffset !== undefined && areaData.length > 0) {
			areaData[0] += areaItem.startOffset;
		}

		for (let i = 1; i < areaData.length; i++) {
			areaData[i] += areaData[i - 1];
		}
		return areaData;
	});

	const areaSeries = sortedAreas.map((areaItem, idx) => {
		return {
			data: areaDataArrays[idx],
			label: areaItem.title,
			area: true,
			showMark: false,
			color: areaItem.color ? `${areaItem.color}50` : undefined,
		};
	});

	const lineSeries = sortedAreas
		.map((areaItem, idx) => {
			const areaData = areaDataArrays[idx];
			if (areaData.length < 2) return null;

			const lineData = Array(areaData.length).fill(null);
			lineData[0] = areaData[0];
			lineData[areaData.length - 1] = areaData[areaData.length - 1];

			return {
				data: lineData,
				label: `${areaItem.title} Trend`,
				area: false,
				showMark: false,
				color: areaItem.color,
				curve: "linear",
				lineWidth: 2,
				connectNulls: true,
				id: `trend-${idx}`,
				showInLegend: false,
				skipAnimation: true,
				hideTooltip: true,
				valueFormatter: () => null,
			};
		})
		.filter((item): item is NonNullable<typeof item> => item !== null);

	const series = showTrend ? [...areaSeries, ...lineSeries] : areaSeries;

	return (
		<Card sx={{ p: 2, borderRadius: 2 }}>
			<CardContent>
				<Typography variant="h6">{title}</Typography>
				<FormControlLabel
					control={
						<Switch
							checked={showTrend}
							onChange={(e) => setShowTrend(e.target.checked)}
							color="primary"
						/>
					}
					label="Show Trend"
				/>
				<LineChart
					xAxis={[
						{
							scaleType: "point",
							data: chartData,
							label: "Date",
							valueFormatter: (value: number) => {
								return new Date(value).toLocaleDateString();
							},
						},
					]}
					series={series}
					height={500}
					slotProps={{
						legend: {
							direction: "horizontal",
							position: {
								vertical: "top",
								horizontal: "end",
							},
						},
						tooltip: {
							hidden: true,
						},
					}}
				/>
			</CardContent>
		</Card>
	);
};
export default StackedAreaChart;
