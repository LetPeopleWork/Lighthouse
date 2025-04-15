import { Card, CardContent, Typography } from "@mui/material";
import {
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	ResponsiveChartContainer,
	ScatterPlot,
	type ScatterValueType,
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

	const formatValue = (value: ScatterValueType) => {
		const point = cycleTimeDataPoints.find((d) => d.id === value.id);
		if (point) {
			return `${point.name} - Cycle Time: ${point.cycleTime} days`;
		}

		return "";
	};

	return cycleTimeDataPoints.length > 0 ? (
		<Card sx={{ p: 2, borderRadius: 2 }}>
			<CardContent>
				<Typography variant="h6">Cycle Time</Typography>
				<ResponsiveChartContainer
					height={500}
					xAxis={[
						{
							id: "timeAxis",
							scaleType: "time",
							label: "Date",
							valueFormatter: (value) => {
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
							valueFormatter: (value) => {
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
								data: point,
							})),
							xAxisId: "timeAxis",
							yAxisId: "cycleTimeAxis",
							color: "rgba(48, 87, 78, 1)",
							valueFormatter: formatValue,
							markerSize: 6,
							highlightScope: { highlighted: "item", faded: "global" },
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
				</ResponsiveChartContainer>
			</CardContent>
		</Card>
	) : (
		<Typography variant="body2" color="text.secondary">
			No data available
		</Typography>
	);
};

export default CycleTimeScatterPlotChart;
