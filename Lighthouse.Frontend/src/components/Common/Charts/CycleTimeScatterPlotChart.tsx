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
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";
import { hexToRgba } from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

const getDateOnlyTimestamp = (date: Date): number => {
	const dateOnly = new Date(date);
	dateOnly.setHours(0, 0, 0, 0);
	return dateOnly.getTime();
};

const getBubbleSize = (count: number): number => {
	return Math.min(5 + Math.sqrt(count) * 3, 20);
};

const ScatterMarker = (
	props: ScatterMarkerProps,
	groupedDataPoints: IGroupedWorkItem[],
	theme: Theme,
	workItemsTerm: string,
	onShowItems: (items: IWorkItem[]) => void,
) => {
	const dataIndex = props.dataIndex || 0;
	const group = groupedDataPoints[dataIndex];

	if (!group) return null;

	const bubbleSize = getBubbleSize(group.items.length);

	const handleOpenWorkItems = () => {
		if (group.items.length > 0) {
			onShowItems(group.items);
		}
	};

	return (
		<>
			<circle
				cx={props.x}
				cy={props.y}
				r={bubbleSize}
				fill={props.color}
				opacity={props.isHighlighted ? 1 : 0.8}
				stroke={props.isHighlighted ? theme.palette.background.paper : "none"}
				strokeWidth={props.isHighlighted ? 2 : 0}
				pointerEvents="none" // Disable pointer events as the button will handle clicks
			>
				<title>{`${group.items.length} item${group.items.length > 1 ? "s" : ""} with cycle time ${group.cycleTime} days`}</title>
			</circle>

			<foreignObject
				x={props.x - bubbleSize}
				y={props.y - bubbleSize}
				width={bubbleSize * 2}
				height={bubbleSize * 2}
			>
				<button
					type="button"
					style={{
						width: "100%",
						height: "100%",
						cursor: "pointer",
						background: "transparent",
						border: "none",
						padding: 0,
						borderRadius: "50%",
					}}
					onClick={handleOpenWorkItems}
					aria-label={`View ${group.items.length} ${workItemsTerm}${group.items.length > 1 ? "s" : ""} with cycle time ${group.cycleTime} days`}
				/>
			</foreignObject>
		</>
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

const getMaxYAxisHeight = (
	percentiles: IPercentileValue[],
	serviceLevelExpectation: IPercentileValue | null | undefined,
	cycleTimeDataPoints: IWorkItem[],
): number => {
	const maxFromPercentiles =
		percentiles.length > 0 ? Math.max(...percentiles.map((p) => p.value)) : 0;

	const maxFromSle = serviceLevelExpectation?.value ?? 0;

	const maxFromData =
		cycleTimeDataPoints.length > 0
			? Math.max(...cycleTimeDataPoints.map((item) => item.cycleTime))
			: 0;

	const absoluteMax = Math.max(maxFromPercentiles, maxFromSle, maxFromData);

	// Add 10% padding to the top
	return absoluteMax * 1.1;
};

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
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const theme = useTheme();

	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);
	const sleTerm = getTerm(TERMINOLOGY_KEYS.SLE);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	useEffect(() => {
		setPercentiles(percentileValues);
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

	const handleShowItems = (items: IWorkItem[]) => {
		setSelectedItems(items);
		setDialogOpen(true);
	};

	return cycleTimeDataPoints.length > 0 && groupedDataPoints.length > 0 ? (
		<>
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ display: "flex", flexDirection: "column", height: "100%" }}
				>
					<Typography variant="h6">{cycleTimeTerm}</Typography>

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
										borderWidth: visiblePercentiles[p.percentile] ? 2 : 1,
										borderStyle: "dashed",
										opacity: visiblePercentiles[p.percentile] ? 1 : 0.7,
										backgroundColor: !visiblePercentiles[p.percentile]
											? "transparent"
											: hexToRgba(forecastLevel.color, theme.opacity.high),
										"&:hover": {
											borderColor: forecastLevel.color,
											borderWidth: 2,
											backgroundColor: hexToRgba(
												forecastLevel.color,
												theme.opacity.high + 0.1,
											),
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
								label={serviceLevelExpectationTerm}
								sx={{
									borderColor: theme.palette.primary.main,
									borderWidth: sleVisible ? 2 : 1,
									borderStyle: "dashed",
									opacity: sleVisible ? 1 : 0.7,
									backgroundColor: !sleVisible
										? "transparent"
										: hexToRgba(
												theme.palette.primary.main,
												theme.opacity.medium,
											),
									"&:hover": {
										borderColor: theme.palette.primary.main,
										borderWidth: 2,
										backgroundColor: hexToRgba(
											theme.palette.primary.main,
											theme.opacity.high,
										),
									},
								}}
								variant={sleVisible ? "filled" : "outlined"}
								onClick={toggleSleVisibility}
							/>
						)}
					</Stack>

					<ChartContainer
						sx={{ flex: 1, minHeight: 0, height: "100%" }}
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
								label: `${cycleTimeTerm} (days)`,
								min: 1,
								max: getMaxYAxisHeight(
									percentiles,
									serviceLevelExpectation,
									cycleTimeDataPoints,
								),
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
								markerSize: 4,
								highlightScope: {
									highlight: "item",
									fade: "global",
								},
								valueFormatter: (item) => {
									if (item?.id === undefined) return "";

									const group = groupedDataPoints[item.id as number];
									if (!group) return "";

									const numberOfClosedItems = group.items.length ?? 0;

									if (numberOfClosedItems === 1) {
										const item = group.items[0];
										return `${getWorkItemName(item)} (Click for details)`;
									}

									return `${numberOfClosedItems} Closed ${workItemsTerm} (Click for details)`;
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
								label={`${sleTerm}: ${serviceLevelExpectation.percentile}% @ ${serviceLevelExpectation.value} days or less`}
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
								marker: (props) =>
									ScatterMarker(
										props,
										groupedDataPoints,
										theme,
										workItemsTerm,
										handleShowItems,
									),
							}}
						/>

						<ChartsTooltip
							trigger="item"
							sx={{
								zIndex: 1200,
								maxWidth: "400px",
								// Hide the tooltip marker/dot
								"& .MuiChartsTooltip-mark": {
									display: "none",
								},
								"& .MuiChartsTooltip-markContainer": {
									display: "none",
								},
								"& .MuiChartsTooltip-table": {
									maxWidth: "100%",
									wordBreak: "break-word",
								},
								"& .MuiChartsTooltip-valueCell": {
									whiteSpace: "pre-line",
									maxWidth: "300px",
									overflowWrap: "break-word",
								},
								"& .MuiPopper-root": {
									transition: "opacity 0.2s ease-in-out",
									transitionDelay: "150ms",
								},
							}}
						/>
					</ChartContainer>
				</CardContent>
			</Card>
			<WorkItemsDialog
				title={`${workItemsTerm} closed on ${selectedItems[0]?.closedDate.toLocaleDateString()} with ${cycleTimeTerm} of ${selectedItems[0]?.cycleTime} days`}
				items={selectedItems}
				open={dialogOpen}
				onClose={() => setDialogOpen(false)}
				additionalColumnTitle={cycleTimeTerm}
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.cycleTime}
			/>
		</>
	) : (
		<Typography variant="body2" color="text.secondary">
			No data available
		</Typography>
	);
};

export default CycleTimeScatterPlotChart;
