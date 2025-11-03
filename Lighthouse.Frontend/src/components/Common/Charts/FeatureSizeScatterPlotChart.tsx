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
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { StateCategory } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";
import { hexToRgba } from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

const getBubbleSize = (count: number): number => {
	return Math.min(5 + Math.sqrt(count) * 3, 20);
};

const ScatterMarker = (
	props: ScatterMarkerProps,
	groupedDataPoints: IGroupedFeature[],
	theme: Theme,
	featuresTerm: string,
	onShowItems: (items: IFeature[]) => void,
) => {
	const dataIndex = props.dataIndex || 0;
	const group = groupedDataPoints[dataIndex];
	if (!group) return null;
	const bubbleSize = getBubbleSize(group.items.length);
	const handleOpenFeatures = () => {
		if (group.items.length > 0) onShowItems(group.items);
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
				pointerEvents="none"
			>
				<title>{`${group.items.length} item${group.items.length > 1 ? "s" : ""} with size ${group.size} child items`}</title>
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
					onClick={handleOpenFeatures}
					aria-label={`View ${group.items.length} ${group.items.length > 1 ? featuresTerm : featuresTerm.slice(0, -1)} with size ${group.size} child items`}
				/>
			</foreignObject>
		</>
	);
};

interface IGroupedFeature {
	cycleTime: number;
	size: number;
	items: IFeature[];
}

const groupFeatures = (items: IFeature[]): IGroupedFeature[] => {
	const groups: Record<string, IGroupedFeature> = {};
	for (const item of items) {
		let cycleTime: number;
		if (item.stateCategory === "Done") {
			cycleTime = item.cycleTime ?? 0;
		} else if (item.stateCategory === "Doing") {
			cycleTime = item.workItemAge ?? 0;
		} else {
			// Items in the 'To Do' state are shown at the 0 line
			cycleTime = 0;
		}
		const key = `${cycleTime}-${item.size}`;
		if (!groups[key]) {
			groups[key] = {
				cycleTime,
				size: item.size,
				items: [],
			};
		}
		groups[key].items.push(item);
	}
	return Object.values(groups);
};

interface FeatureSizeScatterPlotChartProps {
	sizeDataPoints: IFeature[];
	sizePercentileValues?: IPercentileValue[];
}

const getMaxYAxisHeight = (
	sizeDataPoints: IFeature[],
	sizePercentileValues: IPercentileValue[],
): number => {
	const maxFromPercentiles =
		sizePercentileValues.length > 0
			? Math.max(...sizePercentileValues.map((p) => p.value))
			: 0;
	const maxFromData =
		sizeDataPoints.length > 0
			? Math.max(...sizeDataPoints.map((item) => item.size))
			: 0;
	const absoluteMax = Math.max(maxFromPercentiles, maxFromData);
	return absoluteMax * 1.1;
};

const FeatureSizeScatterPlotChart: React.FC<
	FeatureSizeScatterPlotChartProps
> = ({ sizeDataPoints, sizePercentileValues = [] }) => {
	const percentiles = sizePercentileValues ?? [];
	const [visiblePercentiles, setVisiblePercentiles] = useState<
		Record<number, boolean>
	>(() => Object.fromEntries(percentiles.map((p) => [p.percentile, true])));

	// State for controlling which categories to show
	const [showDone, setShowDone] = useState<boolean>(true);
	const [showToDo, setShowToDo] = useState<boolean>(false);
	const [showInProgress, setShowInProgress] = useState<boolean>(false);

	// Determine which state categories exist in the data
	const availableStates = useMemo(() => {
		const states = new Set<StateCategory>();
		for (const item of sizeDataPoints) {
			states.add(item.stateCategory);
		}
		return states;
	}, [sizeDataPoints]);

	const hasDoneFeatures = availableStates.has("Done");
	const hasToDoFeatures = availableStates.has("ToDo");
	const hasInProgressFeatures = availableStates.has("Doing");

	// Filter features based on toggle states
	const filteredDataPoints = useMemo(() => {
		return sizeDataPoints.filter((item) => {
			if (item.stateCategory === "Done") return showDone;
			if (item.stateCategory === "ToDo") return showToDo;
			if (item.stateCategory === "Doing") return showInProgress;
			return false;
		});
	}, [sizeDataPoints, showDone, showToDo, showInProgress]);

	const groupedDataPoints = useMemo(
		() =>
			groupFeatures(
				filteredDataPoints.filter((item) => {
					// Filter out To Do items with size 0 (not worth showing)
					if (
						item.stateCategory === "ToDo" &&
						(item.size === 0 || item.size === null)
					) {
						return false;
					}
					// Keep all other items (Done/Doing with any size, or To Do with size > 0)
					return item.size !== null;
				}),
			),
		[filteredDataPoints],
	);
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [selectedItems, setSelectedItems] = useState<IFeature[]>([]);
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const sizeTerm = "Size";

	const percentilesSig = useMemo(
		() => percentiles.map((p) => `${p.percentile}:${p.value}`).join("|"),
		[percentiles],
	);
	const lastSigRef = useRef(percentilesSig);

	useEffect(() => {
		if (lastSigRef.current !== percentilesSig) {
			lastSigRef.current = percentilesSig;
			const next = Object.fromEntries(
				percentiles.map((p) => [p.percentile, true]),
			);
			setVisiblePercentiles((prev) => {
				const prevKeys = Object.keys(prev);
				const nextKeys = Object.keys(next);
				if (prevKeys.length !== nextKeys.length) return next;
				for (const k of nextKeys) if (prev[+k] !== next[+k]) return next;
				return prev;
			});
		}
	}, [percentilesSig, percentiles]);

	const togglePercentileVisibility = useCallback((percentile: number) => {
		setVisiblePercentiles((prev) => ({
			...prev,
			[percentile]: !prev[percentile],
		}));
	}, []);

	const handleShowItems = useCallback((items: IFeature[]) => {
		setSelectedItems(items);
		setDialogOpen(true);
	}, []);

	const dialogTitle =
		selectedItems.length && selectedItems[0]?.cycleTime !== null
			? `${featuresTerm} Details`
			: `Selected ${featuresTerm}`;
	return sizeDataPoints.length > 0 ? (
		<>
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ height: "100%", display: "flex", flexDirection: "column" }}
				>
					<Typography variant="h6">
						{featuresTerm} {sizeTerm}
					</Typography>
					{(percentiles.length > 0 ||
						hasDoneFeatures ||
						hasToDoFeatures ||
						hasInProgressFeatures) && (
						<Stack
							direction="row"
							spacing={1}
							sx={{
								mb: 2,
								flexWrap: "wrap",
								gap: 1,
								justifyContent: "space-between",
								alignItems: "center",
							}}
						>
							<Stack
								direction="row"
								spacing={1}
								sx={{ flexWrap: "wrap", gap: 1 }}
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
												backgroundColor: visiblePercentiles[p.percentile]
													? hexToRgba(forecastLevel.color, theme.opacity.high)
													: "transparent",
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
							</Stack>
							<Stack
								direction="row"
								spacing={1}
								sx={{ flexWrap: "wrap", gap: 1 }}
							>
								{hasToDoFeatures && (
									<Chip
										label="To Do"
										sx={{
											borderColor: theme.palette.primary.main,
											borderWidth: showToDo ? 2 : 1,
											opacity: showToDo ? 1 : 0.7,
											backgroundColor: showToDo
												? hexToRgba(
														theme.palette.primary.main,
														theme.opacity.high,
													)
												: "transparent",
											"&:hover": {
												borderColor: theme.palette.primary.main,
												borderWidth: 2,
												backgroundColor: hexToRgba(
													theme.palette.primary.main,
													theme.opacity.high + 0.1,
												),
											},
										}}
										variant={showToDo ? "filled" : "outlined"}
										onClick={() => setShowToDo(!showToDo)}
									/>
								)}
								{hasInProgressFeatures && (
									<Chip
										label="In Progress"
										sx={{
											borderColor: theme.palette.primary.main,
											borderWidth: showInProgress ? 2 : 1,
											opacity: showInProgress ? 1 : 0.7,
											backgroundColor: showInProgress
												? hexToRgba(
														theme.palette.primary.main,
														theme.opacity.high,
													)
												: "transparent",
											"&:hover": {
												borderColor: theme.palette.primary.main,
												borderWidth: 2,
												backgroundColor: hexToRgba(
													theme.palette.primary.main,
													theme.opacity.high + 0.1,
												),
											},
										}}
										variant={showInProgress ? "filled" : "outlined"}
										onClick={() => setShowInProgress(!showInProgress)}
									/>
								)}
								{hasDoneFeatures && (
									<Chip
										label="Done"
										sx={{
											borderColor: theme.palette.primary.main,
											borderWidth: showDone ? 2 : 1,
											opacity: showDone ? 1 : 0.7,
											backgroundColor: showDone
												? hexToRgba(
														theme.palette.primary.main,
														theme.opacity.high,
													)
												: "transparent",
											"&:hover": {
												borderColor: theme.palette.primary.main,
												borderWidth: 2,
												backgroundColor: hexToRgba(
													theme.palette.primary.main,
													theme.opacity.high + 0.1,
												),
											},
										}}
										variant={showDone ? "filled" : "outlined"}
										onClick={() => setShowDone(!showDone)}
									/>
								)}
							</Stack>
						</Stack>
					)}
					<ChartContainer
						sx={{ flex: 1, minHeight: 0, height: "100%" }}
						xAxis={[
							{
								id: "sizeAxis",
								scaleType: "linear",
								label: `${sizeTerm} (Child Items)`,
								min: 0,
								max: getMaxYAxisHeight(filteredDataPoints, percentiles),
								valueFormatter: (value: number) => {
									return Number.isInteger(value) ? value.toString() : "";
								},
							},
						]}
						yAxis={[
							{
								id: "timeAxis",
								scaleType: "linear",
								label: `${cycleTimeTerm} (days)`,
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
									x: group.size,
									y: group.cycleTime,
									id: index,
									itemCount: group.items.length,
								})),
								xAxisId: "sizeAxis",
								yAxisId: "timeAxis",
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
										const single = group.items[0];
										return `${getWorkItemName(single)} - ${single.state} (Click for details)`;
									}
									return `${numberOfClosedItems} Closed ${featuresTerm} (Click for details)`;
								},
							},
						]}
					>
						{percentiles.map((p) => {
							const forecastLevel = new ForecastLevel(p.percentile);
							return visiblePercentiles[p.percentile] ? (
								<ChartsReferenceLine
									key={`percentile-${p.percentile}`}
									x={p.value}
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
						<ChartsXAxis />
						<ChartsYAxis />
						<ScatterPlot
							slots={{
								marker: (props) =>
									ScatterMarker(
										props,
										groupedDataPoints,
										theme,
										featuresTerm,
										handleShowItems,
									),
							}}
						/>
						<ChartsTooltip
							trigger="item"
							sx={{
								zIndex: 1200,
								maxWidth: "400px",
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
			{dialogOpen && selectedItems.length > 0 && (
				<WorkItemsDialog
					title={dialogTitle}
					items={selectedItems}
					open={dialogOpen}
					additionalColumnTitle={sizeTerm}
					additionalColumnDescription="Child Items"
					additionalColumnContent={(item) => (item as IFeature).size ?? ""}
					onClose={() => setDialogOpen(false)}
				/>
			)}
		</>
	) : (
		<Typography variant="body2" color="text.secondary">
			No data available
		</Typography>
	);
};

export default FeatureSizeScatterPlotChart;
