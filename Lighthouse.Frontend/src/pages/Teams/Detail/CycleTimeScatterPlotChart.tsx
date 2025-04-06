import { Card, CardContent, CircularProgress, Typography } from "@mui/material";
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

interface CycleTimeScatterPlotChartProps {
	percentileValues: IPercentileValue[];
	cycleTimeDataPoints: IWorkItem[];
}

interface CycleTimePoint extends IWorkItem {
	cycleTime: number;
}

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
	percentileValues,
	cycleTimeDataPoints,
}) => {
	const [cycleTimeData, setCycleTimeData] = useState<CycleTimePoint[]>([]);
	const [percentiles, setPercentiles] = useState<IPercentileValue[]>([]);

	useEffect(() => {
		const fetchCycleTimeData = async () => {
			// Transform data for scatter plot - add cycle time calculation
			const scatterplotData: CycleTimePoint[] = cycleTimeDataPoints.map(
				(workItem) => {
					const cycleTimeDays =
						Math.floor(
							(workItem.closedDate.getTime() - workItem.startedDate.getTime()) /
								(1000 * 60 * 60 * 24),
						) + 1;

					return {
						...workItem,
						cycleTime: cycleTimeDays,
					};
				},
			);

			setCycleTimeData(scatterplotData);
		};

		fetchCycleTimeData();
	}, [cycleTimeDataPoints]);

	useEffect(() => {
		setPercentiles(percentileValues);
	}, [percentileValues]);

	const handleItemClick = (itemId: number) => {
		const item = cycleTimeData.find((d) => d.id === itemId);
		if (item?.url) {
			window.open(item.url, "_blank");
		}
	};

	const formatValue = (value: ScatterValueType) => {
		const point = cycleTimeData.find((d) => d.id === value.id);
		if (point) {
			return `${point.name} - Cycle Time: ${point.cycleTime} days`;
		}

		return "";
	};

	return cycleTimeData.length > 0 ? (
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
						},
					]}
					series={[
						{
							type: "scatter",
							data: cycleTimeData.map((point) => ({
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
					{percentiles.map((p) => (
						<ChartsReferenceLine
							key={`percentile-${p.percentile}`}
							y={p.value}
							label={`${p.percentile}th percentile: ${p.value} days`}
							labelAlign="start"
							lineStyle={{
								stroke: "orange",
								strokeWidth: 1,
								strokeDasharray: "5 5",
							}}
						/>
					))}

					<ChartsXAxis />
					<ChartsYAxis />
					<ScatterPlot
						onItemClick={(_event, itemData) => {
							if (itemData?.dataIndex) {
								handleItemClick(itemData.dataIndex);
							}
						}}
					/>
					<ChartsTooltip trigger="item" />
				</ResponsiveChartContainer>
			</CardContent>
		</Card>
	) : (
		<CircularProgress />
	);
};

export default CycleTimeScatterPlotChart;
