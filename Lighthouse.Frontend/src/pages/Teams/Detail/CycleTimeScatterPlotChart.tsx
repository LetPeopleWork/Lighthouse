import { CircularProgress } from "@mui/material";
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
import { useContext, useEffect, useState } from "react";
import type { ITeam } from "../../../models/Team/Team";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface CycleTimeScatterPlotChartProps {
	team: ITeam;
}

interface CycleTimePoint extends IWorkItem {
	cycleTime: number;
}

interface PercentileLine {
	percentile: number;
	value: number;
	color: string;
}

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
	team,
}) => {
	const [cycleTimeData, setCycleTimeData] = useState<CycleTimePoint[]>([]);
	const [percentiles, setPercentiles] = useState<PercentileLine[]>([]);

	const { teamService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchCycleTimeData = async () => {
			const workItems = await teamService.getWorkItems(team.id);

			// Transform data for scatter plot - add cycle time calculation
			const scatterplotData: CycleTimePoint[] = workItems.map((workItem) => {
				const cycleTimeDays =
					Math.floor(
						(workItem.closedDate.getTime() - workItem.startedDate.getTime()) /
							(1000 * 60 * 60 * 24),
					) + 1;

				// Update the work item with cycle time data
				return {
					...workItem,
					cycleTime: cycleTimeDays,
				};
			});

			setCycleTimeData(scatterplotData);
		};

		fetchCycleTimeData();
	}, [teamService, team]);

	useEffect(() => {
		const calculatePercentiles = (data: CycleTimePoint[]) => {
			const values = [...data.map((item) => item.cycleTime)].sort(
				(a, b) => a - b,
			);
			const getPercentile = (p: number) => {
				const pos = (values.length * p) / 100 - 1;
				const index = Math.ceil(pos);

				if (index < 0) return values[0];
				if (index >= values.length) return values[values.length - 1];

				return values[index];
			};

			setPercentiles([
				{ percentile: 50, value: getPercentile(50), color: "green" },
				{ percentile: 70, value: getPercentile(70), color: "blue" },
				{ percentile: 85, value: getPercentile(85), color: "orange" },
				{ percentile: 95, value: getPercentile(95), color: "red" },
			]);
		};

		calculatePercentiles(cycleTimeData);
	}, [cycleTimeData]);

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
						stroke: p.color,
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
	) : (
		<CircularProgress />
	);
};

export default CycleTimeScatterPlotChart;
