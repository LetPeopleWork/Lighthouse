import { Card, CardContent, Typography } from "@mui/material";
import {
	ChartContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	ScatterPlot,
} from "@mui/x-charts";
import type React from "react";
import { useEffect, useState } from "react";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import { ForecastLevel } from "../Forecasts/ForecastLevel";

interface CycleTimeScatterPlotChartProps {
	percentileValues: IPercentileValue[];
	cycleTimeDataPoints: IWorkItem[];
}

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
	percentileValues,
	cycleTimeDataPoints,
}) => {
	const [percentiles, setPercentiles] = useState<IPercentileValue[]>([]);

	useEffect(() => {
		setPercentiles(percentileValues);
	}, [percentileValues]);

	const handleItemClick = (itemId: number) => {
		const item = cycleTimeDataPoints[itemId];

		if (item?.url) {
			window.open(item.url, "_blank");
		}
	};

	return cycleTimeDataPoints.length > 0 ? (
		<Card sx={{ p: 2, borderRadius: 2 }}>
			<CardContent>
				<Typography variant="h6">Cycle Time</Typography>
				<ChartContainer
					height={500}
					xAxis={[
						{
							id: "timeAxis",
							scaleType: "time",
							label: "Date",
							valueFormatter: (value: number) => {
								return new Date(value).toLocaleDateString();
							},
						},
					]}
					yAxis={[
						{
							id: "cycleTimeAxis",
							scaleType: "linear",
							label: "Cycle Time (days)",
							min: 0,
							valueFormatter: (value: number) => {
								return Number.isInteger(value) ? value.toString() : "";
							},
						},
					]}
					series={[
						{
							type: "scatter",
							data: cycleTimeDataPoints.map((point) => ({
								x: point.closedDate.getTime(),
								y: point.cycleTime,
								id: point.id,
							})),
							xAxisId: "timeAxis",
							yAxisId: "cycleTimeAxis",
							color: "rgba(48, 87, 78, 1)",
							markerSize: 6,
							highlightScope: { highlight: "item", fade: "global" },
							valueFormatter: (item) => {
								if (!item?.id) return "";

								const workItem = cycleTimeDataPoints.find(
									(p) => p.id === item.id,
								);
								if (workItem) {
									return `${workItem.name} - Cycle Time: ${workItem.cycleTime} days`;
								}
								return "";
							},
						},
					]}
				>
					{/* Add reference lines for each percentile */}
					{percentiles.map((p) => {
						const forecastLevel = new ForecastLevel(p.percentile);
						return (
							<ChartsReferenceLine
								key={`percentile-${p.percentile}`}
								y={p.value}
								label={`${p.percentile}th percentile: ${p.value} days`}
								labelAlign="start"
								lineStyle={{
									stroke: forecastLevel.color,
									strokeWidth: 1,
									strokeDasharray: "5 5",
								}}
							/>
						);
					})}

					<ChartsXAxis />
					<ChartsYAxis />
					<ScatterPlot
						onItemClick={(_event, itemData) => {
							if (itemData?.dataIndex >= 0) {
								handleItemClick(itemData.dataIndex);
							}
						}}
					/>
					<ChartsTooltip trigger="item" />
				</ChartContainer>
			</CardContent>
		</Card>
	) : (
		<Typography variant="body2" color="text.secondary">
			No data available
		</Typography>
	);
};

export default CycleTimeScatterPlotChart;
