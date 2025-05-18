import {
	Card,
	CardContent,
	Chip,
	Stack,
	Typography,
	useTheme,
} from "@mui/material";
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
	serviceLevelExpectation?: IPercentileValue | null;
}

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
	percentileValues,
	cycleTimeDataPoints,
	serviceLevelExpectation = null,
}) => {
	const [percentiles, setPercentiles] = useState<IPercentileValue[]>([]);
	const [visiblePercentiles, setVisiblePercentiles] = useState<
		Record<number, boolean>
	>({});
	const [sleVisible, setSleVisible] = useState<boolean>(false);
	const theme = useTheme();

	useEffect(() => {
		setPercentiles(percentileValues);
		// Initialize all percentiles as visible
		const initialVisibility: Record<number, boolean> = {};
		for (const p of percentileValues) {
			initialVisibility[p.percentile] = true;
		}
		setVisiblePercentiles(initialVisibility);
	}, [percentileValues]);

	const handleItemClick = (itemId: number) => {
		const item = cycleTimeDataPoints[itemId];

		if (item?.url) {
			window.open(item.url, "_blank");
		}
	};

	const togglePercentileVisibility = (percentile: number) => {
		setVisiblePercentiles((prev) => ({
			...prev,
			[percentile]: !prev[percentile],
		}));
	};

	const toggleSleVisibility = () => {
		setSleVisible((prev) => !prev);
	};

	return cycleTimeDataPoints.length > 0 ? (
		<Card sx={{ p: 2, borderRadius: 2 }}>
			<CardContent>
				<Typography variant="h6">Cycle Time</Typography>

				<Stack
					direction="row"
					spacing={1}
					sx={{ mb: 2, flexWrap: "wrap", gap: 1 }}
				>
					{percentiles.map((p) => {
						const forecastLevel = new ForecastLevel(p.percentile);
						return (
							<Chip
								key={`legend-${p.percentile}`}
								label={`${p.percentile}%`}
								sx={{
									borderColor: forecastLevel.color,
									color: visiblePercentiles[p.percentile]
										? forecastLevel.color
										: theme.palette.text.disabled,
									borderWidth: 1,
									borderStyle: "dashed",
									opacity: visiblePercentiles[p.percentile] ? 1 : 0.7,
									"&:hover": {
										borderColor: forecastLevel.color,
									},
								}}
								variant={
									visiblePercentiles[p.percentile] ? "filled" : "outlined"
								}
								onClick={() => togglePercentileVisibility(p.percentile)}
							/>
						);
					})}
					{serviceLevelExpectation && (
						<Chip
							key="legend-sle"
							label="Service Level Expectation"
							sx={{
								borderColor: theme.palette.primary.main,
								color: sleVisible
									? theme.palette.primary.main
									: theme.palette.text.disabled,
								borderWidth: 1,
								borderStyle: "dashed",
								opacity: sleVisible ? 1 : 0.7,
								"&:hover": {
									borderColor: theme.palette.primary.main,
								},
							}}
							variant={sleVisible ? "filled" : "outlined"}
							onClick={toggleSleVisibility}
						/>
					)}
				</Stack>

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
							color: theme.palette.primary.main,
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
					{percentiles.map((p) => {
						const forecastLevel = new ForecastLevel(p.percentile);
						return visiblePercentiles[p.percentile] ? (
							<ChartsReferenceLine
								key={`percentile-${p.percentile}`}
								y={p.value}
								label={`${p.percentile}%`}
								labelAlign="end"
								lineStyle={{
									stroke: forecastLevel.color,
									strokeWidth: 1,
									strokeDasharray: "5 5",
								}}
							/>
						) : null;
					})}
					{sleVisible && serviceLevelExpectation && (
						<ChartsReferenceLine
							key="sle-reference-line"
							y={serviceLevelExpectation.value}
							label={`SLE: ${serviceLevelExpectation.percentile}% @ ${serviceLevelExpectation.value} days or less`}
							labelAlign="start"
							lineStyle={{
								stroke: theme.palette.primary.main,
								strokeWidth: 2,
								strokeDasharray: "3 3",
							}}
						/>
					)}

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
