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
import { useCallback, useEffect, useMemo, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { StateCategory } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";
import {
	errorColor,
	getColorMapForKeys,
	hexToRgba,
} from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import LegendChip from "./LegendChip";

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
	const providedColor = (props as unknown as { color?: string }).color;
	const bubbleColor = providedColor ?? theme.palette.primary.main;

	const handleOpenFeatures = () => {
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
				fill={bubbleColor}
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
	state: string;
}

const getStateCategoryDisplayName = (stateCategory: StateCategory): string => {
	if (stateCategory === "ToDo") return "To Do";
	if (stateCategory === "Doing") return "In Progress";
	if (stateCategory === "Done") return "Done";
	return stateCategory;
};

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
				state: item.state || "Unknown",
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
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const sizeTerm = "Size";

	const percentiles = sizePercentileValues ?? [];

	const [visiblePercentiles, setVisiblePercentiles] = useState<
		Record<number, boolean>
	>({});
	const [fixedXAxisMax, setFixedXAxisMax] = useState<number | null>(null);
	const [fixedYAxisMax, setFixedYAxisMax] = useState<number | null>(null);

	// Initialize percentile visibility
	useEffect(() => {
		const newVisibility = Object.fromEntries(
			percentiles.map((p) => [p.percentile, true]),
		);
		setVisiblePercentiles((prev) => {
			// Only update if keys have actually changed
			const prevKeys = Object.keys(prev)
				.sort((a, b) => a.localeCompare(b))
				.join(",");
			const newKeys = Object.keys(newVisibility)
				.sort((a, b) => a.localeCompare(b))
				.join(",");
			if (prevKeys !== newKeys) {
				return newVisibility;
			}
			return prev;
		});
	}, [percentiles]);

	// Extract unique state categories and create color map
	const stateCategories = useMemo(() => {
		const stateCategorySet = new Set<StateCategory>();
		for (const item of sizeDataPoints) {
			stateCategorySet.add(item.stateCategory);
		}
		// Use logical workflow order: To Do -> In Progress -> Done
		const logicalOrder: StateCategory[] = ["ToDo", "Doing", "Done"];
		return logicalOrder.filter((state) => stateCategorySet.has(state));
	}, [sizeDataPoints]);

	const colorMap = useMemo(
		() => getColorMapForKeys(stateCategories, theme.palette.primary.main),
		[stateCategories, theme.palette.primary.main],
	);

	const [visibleStateCategories, setVisibleStateCategories] = useState<
		Partial<Record<StateCategory, boolean>>
	>({});

	useEffect(() => {
		const newVisibility = Object.fromEntries(
			stateCategories.map((s) => [s, s === "Done"]), // Only Done is visible by default
		) as Partial<Record<StateCategory, boolean>>;
		setVisibleStateCategories((prev) => {
			// Only update if keys have actually changed
			const prevKeys = Object.keys(prev)
				.sort((a, b) => a.localeCompare(b))
				.join(",");
			const newKeys = Object.keys(newVisibility)
				.sort((a, b) => a.localeCompare(b))
				.join(",");
			if (prevKeys !== newKeys) {
				return newVisibility;
			}
			return prev;
		});
	}, [stateCategories]);

	// Calculate fixed axis domains from initial data to prevent axis changes on filtering
	useEffect(() => {
		if (sizeDataPoints.length === 0) {
			setFixedXAxisMax(null);
			setFixedYAxisMax(null);
			return;
		}

		// Calculate X-axis max (size)
		const xMax = getMaxYAxisHeight(sizeDataPoints, percentiles);
		setFixedXAxisMax(xMax);

		// Calculate Y-axis max (cycle time)
		const cycleTimes = sizeDataPoints.map((item) => {
			if (item.stateCategory === "Done") {
				return item.cycleTime ?? 0;
			}
			if (item.stateCategory === "Doing") {
				return item.workItemAge ?? 0;
			}
			return 0;
		});
		const maxCycleTime = Math.max(...cycleTimes, 0);
		setFixedYAxisMax(maxCycleTime * 1.1);
	}, [sizeDataPoints, percentiles]);

	// First group all features, then filter groups based on state category toggles
	const allGroupedDataPoints = useMemo(
		() => groupFeatures(sizeDataPoints),
		[sizeDataPoints],
	);

	const groupedDataPoints = useMemo(() => {
		return allGroupedDataPoints.filter((group) => {
			// Show group if at least one item has a visible state category
			return group.items.some((item) => {
				// Filter by state category visibility
				if (visibleStateCategories[item.stateCategory] === false) return false;

				// Filter out To Do items with size 0 (not worth showing)
				if (
					item.stateCategory === "ToDo" &&
					(item.size === 0 || item.size === null)
				) {
					return false;
				}

				// Keep all other items (Done/Doing with any size, or To Do with size > 0)
				return item.size !== null;
			});
		});
	}, [allGroupedDataPoints, visibleStateCategories]);

	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [selectedItems, setSelectedItems] = useState<IFeature[]>([]);

	const togglePercentileVisibility = useCallback((percentile: number) => {
		setVisiblePercentiles((prev) => ({
			...prev,
			[percentile]: !prev[percentile],
		}));
	}, []);

	const toggleStateCategoryVisibility = (stateCategory: StateCategory) => {
		setVisibleStateCategories((prev) => {
			// Count how many state categories are currently visible
			const visibleCount = Object.values(prev).filter(
				(v) => v !== false,
			).length;

			// If trying to hide the last visible state category, prevent it
			if (prev[stateCategory] !== false && visibleCount <= 1) {
				return prev;
			}

			return {
				...prev,
				[stateCategory]: !prev[stateCategory],
			};
		});
	};

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
					{(percentiles.length > 0 || stateCategories.length > 0) && (
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
							{stateCategories.length > 0 && (
								<Stack
									direction="row"
									spacing={1}
									sx={{ flexWrap: "wrap", gap: 1, ml: "auto" }}
								>
									{stateCategories.map((stateCategory) => (
										<LegendChip
											key={`legend-state-category-${stateCategory}`}
											label={getStateCategoryDisplayName(stateCategory)}
											color={
												colorMap[stateCategory] ?? theme.palette.primary.main
											}
											visible={visibleStateCategories[stateCategory] !== false}
											onToggle={() =>
												toggleStateCategoryVisibility(stateCategory)
											}
										/>
									))}
								</Stack>
							)}
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
								max:
									fixedXAxisMax ??
									getMaxYAxisHeight(sizeDataPoints, percentiles),
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
								max: fixedYAxisMax ?? undefined,
								valueFormatter: (value: number) => {
									return Number.isInteger(value) ? value.toString() : "";
								},
							},
						]}
						series={[
							{
								type: "scatter",
								data: groupedDataPoints.map((group, index) => {
									const hasBlockedItems = group.items.some((i) => i.isBlocked);
									const stateCategory = group.items[0]?.stateCategory || "ToDo";
									const fillColor = hasBlockedItems
										? errorColor
										: colorMap[stateCategory] || theme.palette.primary.main;
									return {
										x: group.size,
										y: group.cycleTime,
										id: index,
										itemCount: group.items.length,
										color: fillColor,
									};
								}),
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
