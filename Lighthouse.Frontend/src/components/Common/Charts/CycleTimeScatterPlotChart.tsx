import {
	Card,
	CardContent,
	Chip,
	Stack,
	type Theme,
	Typography,
	useTheme,
} from "@mui/material";
import {
	ChartContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	type ScatterMarkerProps,
	ScatterPlot,
} from "@mui/x-charts";
import type React from "react";
import { useEffect, useState } from "react";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import { ForecastLevel } from "../Forecasts/ForecastLevel";

const getDateOnlyTimestamp = (date: Date): number => {
	const dateOnly = new Date(date);
	dateOnly.setHours(0, 0, 0, 0);
	return dateOnly.getTime();
};

const getBubbleSize = (count: number): number => {
	return Math.min(5 + Math.sqrt(count) * 3, 20);
};

// Extracted marker component to avoid ESLint warning
const ScatterMarker = (
	props: ScatterMarkerProps,
	groupedDataPoints: IGroupedWorkItem[],
	theme: Theme,
) => {
	const dataIndex = props.dataIndex || 0;
	const group = groupedDataPoints[dataIndex];

	if (!group) return null;

	const bubbleSize = getBubbleSize(group.items.length);

	return (
		<circle
			cx={props.x}
			cy={props.y}
			r={bubbleSize}
			fill={props.color}
			opacity={props.isHighlighted ? 1 : 0.8}
			stroke={props.isHighlighted ? theme.palette.background.paper : "none"}
			strokeWidth={props.isHighlighted ? 2 : 0}
			style={{ cursor: "pointer" }}
			onClick={() => {
				for (const item of group.items) {
					if (item.url) {
						window.open(item.url, "_blank");
					}
				}
			}}
			onKeyDown={(e) => {
				if (e.key === "Enter" || e.key === " ") {
					e.preventDefault();
					for (const item of group.items) {
						if (item.url) {
							window.open(item.url, "_blank");
						}
					}
				}
			}}
			tabIndex={0}
			role="button"
			aria-label={`View ${group.items.length} item${group.items.length > 1 ? "s" : ""} with cycle time ${group.cycleTime} days`}
		/>
	);
};

interface IGroupedWorkItem {
	closedDateTimestamp: number;
	cycleTime: number;
	items: IWorkItem[];
}

const groupWorkItems = (items: IWorkItem[]): IGroupedWorkItem[] => {
	const groups: Record<string, IGroupedWorkItem> = {};

	for (const item of items) {
		const closedDateTimestamp = getDateOnlyTimestamp(item.closedDate);
		// Create a key combining date and cycle time
		const key = `${closedDateTimestamp}-${item.cycleTime}`;

		if (!groups[key]) {
			groups[key] = {
				closedDateTimestamp,
				cycleTime: item.cycleTime,
				items: [],
			};
		}

		groups[key].items.push(item);
	}

	return Object.values(groups);
};

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
	const [groupedDataPoints, setGroupedDataPoints] = useState<
		IGroupedWorkItem[]
	>([]);
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

	useEffect(() => {
		setGroupedDataPoints(groupWorkItems(cycleTimeDataPoints));
	}, [cycleTimeDataPoints]);

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
							data: groupedDataPoints.map((group, index) => ({
								x: group.closedDateTimestamp,
								y: group.cycleTime,
								id: index,
								itemCount: group.items.length,
							})),
							xAxisId: "timeAxis",
							yAxisId: "cycleTimeAxis",
							color: theme.palette.primary.main,
							// Use marker size to control the size of points
							markerSize: 4, // Smaller than default to make tooltip less sensitive
							highlightScope: {
								highlight: "item",
								fade: "global",
							},
							valueFormatter: (item) => {
								if (item?.id === undefined) return "";

								const group = groupedDataPoints[item.id as number];
								if (!group) return "";

								const itemsList = group.items.map((wi) => `• ${wi.name}`);

								return itemsList.join("\n");
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
						slots={{
							marker: (props) => ScatterMarker(props, groupedDataPoints, theme),
						}}
					/>

					<ChartsTooltip
						trigger="item"
						sx={{
							zIndex: 1200,
							"& .MuiChartsTooltip-valueCell": {
								whiteSpace: "pre-line",
							},
							// Adding a small delay to prevent accidental tooltip displays
							"& .MuiPopper-root": {
								transition: "opacity 0.2s ease-in-out",
								transitionDelay: "150ms",
							},
						}}
					/>
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
