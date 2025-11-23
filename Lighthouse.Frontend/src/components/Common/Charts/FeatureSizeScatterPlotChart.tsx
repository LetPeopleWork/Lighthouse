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
	type ScatterValueType,
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
	return Math.min(5 + count * 3, 20);
};

type MarkerOptions = {
	seriesGroupedDataMap?: Map<string | number, IGroupedFeature[]>;
	seriesGroupKeyMapMap?: Map<string | number, Map<string, number>>;
	theme: Theme;
	featuresTerm: string;
	colorMap: Record<string, string>;
	onShowItems: (items: IFeature[]) => void;
};

const ScatterMarker = (
	props: ScatterMarkerProps,
	allGroupedDataPoints: IGroupedFeature[],
	groupKeyMap: Map<string, number>,
	options: MarkerOptions,
) => {
	const {
		seriesGroupedDataMap,
		seriesGroupKeyMapMap,
		theme,
		featuresTerm,
		colorMap,
		onShowItems,
	} = options;

	type ScatterDatum = {
		item?: Record<string, unknown>;
		color?: string;
		[key: string]: unknown;
	};

	const datum =
		(
			props as unknown as {
				data?: ScatterDatum;
				datum?: ScatterDatum;
				item?: ScatterDatum;
			}
		).data ??
		(
			props as unknown as {
				data?: ScatterDatum;
				datum?: ScatterDatum;
				item?: ScatterDatum;
			}
		).datum ??
		(
			props as unknown as {
				data?: ScatterDatum;
				datum?: ScatterDatum;
				item?: ScatterDatum;
			}
		).item;

	const datumGroupIndex = (datum as unknown as { groupIndex?: number })
		?.groupIndex;
	const datumGroupKey = (datum as unknown as { groupKey?: string })?.groupKey;
	const seriesIndexOrId =
		(
			props as unknown as {
				seriesIndex?: number | string;
				seriesId?: number | string;
			}
		)?.seriesIndex ??
		(props as unknown as { seriesId?: number | string })?.seriesId;

	let group: IGroupedFeature | undefined;

	// 1) Prefer per-series mapping if available
	if (seriesGroupedDataMap && seriesIndexOrId !== undefined) {
		const seriesGrouped = seriesGroupedDataMap.get(seriesIndexOrId);
		const dataIndex =
			(props as unknown as { dataIndex?: number })?.dataIndex ?? 0;
		if (seriesGrouped && typeof dataIndex === "number") {
			const seriesGroup = seriesGrouped[dataIndex];
			if (seriesGroup) {
				if (typeof datumGroupIndex === "number") {
					const explicitGroup = allGroupedDataPoints[datumGroupIndex];
					if (explicitGroup && explicitGroup !== seriesGroup) {
						group = explicitGroup;
					} else {
						group = seriesGroup;
					}
				} else if (typeof datumGroupKey === "string" && seriesGroupKeyMapMap) {
					const seriesKeyMap = seriesGroupKeyMapMap.get(seriesIndexOrId);
					const explicitIdx = seriesKeyMap?.get(datumGroupKey);
					const explicitGroup =
						typeof explicitIdx === "number"
							? allGroupedDataPoints[explicitIdx]
							: undefined;
					if (explicitGroup && explicitGroup !== seriesGroup) {
						group = explicitGroup;
					} else {
						group = seriesGroup;
					}
				} else {
					group = seriesGroup;
				}
			}
		}
	}

	// 2) Prefer explicit groupIndex if present
	if (!group && typeof datumGroupIndex === "number") {
		group = allGroupedDataPoints[datumGroupIndex];
	}

	// 3) Then try groupKey map
	if (!group && typeof datumGroupKey === "string" && groupKeyMap) {
		const idx = groupKeyMap.get(datumGroupKey);
		if (typeof idx === "number") {
			group = allGroupedDataPoints[idx];
		}
	}

	// 4) As a last resort, try matching by coordinates (x/y)
	if (!group) {
		const datumX = (datum as unknown as { x?: number })?.x ?? props.x;
		const datumY = (datum as unknown as { y?: number })?.y ?? props.y;
		group = allGroupedDataPoints.find(
			(g) => g.size === datumX && g.cycleTime === datumY,
		);
	}

	// Fallback if we still don't have a group
	if (!group) {
		const providedColor = (props as unknown as { color?: string })?.color;
		const fallbackColor = providedColor ?? theme.palette.primary.main;
		const fallbackSize = 6;
		return (
			<>
				<circle
					cx={props.x}
					cy={props.y}
					r={fallbackSize}
					fill={fallbackColor}
					opacity={props.isHighlighted ? 1 : 0.8}
					stroke={
						props.isHighlighted
							? theme.palette.background.paper
							: hexToRgba(fallbackColor, 0.12)
					}
					strokeWidth={props.isHighlighted ? 2 : 1}
					pointerEvents="none"
				>
					<title>{`Feature (unknown group) - click for details`}</title>
				</circle>
				<foreignObject
					x={props.x - fallbackSize}
					y={props.y - fallbackSize}
					width={fallbackSize * 2}
					height={fallbackSize * 2}
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
						onClick={() => {}}
						aria-label={`View feature details`}
					/>
				</foreignObject>
			</>
		);
	}

	const bubbleSize = getBubbleSize(group.items.length);
	const providedColor = (props as unknown as { color?: string }).color;
	const stateCategory = group.items[0]?.stateCategory || group.state || "ToDo";
	const hasBlockedItems = group.items.some((i) => i.isBlocked);
	const bubbleColor = hasBlockedItems
		? errorColor
		: (providedColor ?? colorMap[stateCategory] ?? theme.palette.primary.main);

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
				stroke={
					props.isHighlighted
						? theme.palette.background.paper
						: hexToRgba(bubbleColor, 0.12)
				}
				strokeWidth={props.isHighlighted ? 2 : 1}
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
			cycleTime = 0;
		}

		const key = `${item.stateCategory ?? item.state ?? "Unknown"}-${cycleTime}-${item.size}`;

		if (!groups[key]) {
			groups[key] = {
				cycleTime,
				size: item.size,
				items: [],
				state: item.stateCategory || item.state || "Unknown",
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

	useEffect(() => {
		const newVisibility = Object.fromEntries(
			percentiles.map((p) => [p.percentile, true]),
		);
		setVisiblePercentiles((prev) => {
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

	const stateCategories = useMemo(() => {
		const stateCategorySet = new Set<StateCategory>();
		for (const item of sizeDataPoints) {
			stateCategorySet.add(item.stateCategory);
		}
		const logicalOrder: StateCategory[] = ["ToDo", "Doing", "Done"];
		return logicalOrder.filter((state) => stateCategorySet.has(state));
	}, [sizeDataPoints]);

	const colorMap = useMemo(
		() => getColorMapForKeys(stateCategories),
		[stateCategories],
	);

	const [visibleStateCategories, setVisibleStateCategories] = useState<
		Partial<Record<StateCategory, boolean>>
	>({});

	useEffect(() => {
		const newVisibility = Object.fromEntries(
			stateCategories.map((s) => [s, s === "Done"]),
		) as Partial<Record<StateCategory, boolean>>;
		setVisibleStateCategories((prev) => {
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

	useEffect(() => {
		if (sizeDataPoints.length === 0) {
			setFixedXAxisMax(null);
			setFixedYAxisMax(null);
			return;
		}

		const xMax = getMaxYAxisHeight(sizeDataPoints, percentiles);
		setFixedXAxisMax(xMax);

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

	const allGroupedDataPoints = useMemo(
		() => groupFeatures(sizeDataPoints),
		[sizeDataPoints],
	);

	const getGroupKey = useCallback(
		(g: IGroupedFeature) => `${g.state}-${g.cycleTime}-${g.size}`,
		[],
	);

	const groupKeyMap = useMemo(
		() => new Map(allGroupedDataPoints.map((g, idx) => [getGroupKey(g), idx])),
		[allGroupedDataPoints, getGroupKey],
	);

	const groupedDataPoints = useMemo(() => {
		return allGroupedDataPoints.filter((group) => {
			return group.items.some((item) => {
				if (visibleStateCategories[item.stateCategory] === false) return false;

				if (
					item.stateCategory === "ToDo" &&
					(item.size === 0 || item.size === null)
				) {
					return false;
				}

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
			const visibleCount = Object.values(prev).filter(
				(v) => v !== false,
			).length;

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

	const { series, seriesGroupedDataMap, seriesGroupKeyMapMap } = useMemo(() => {
		const dataMap = new Map<number, IGroupedFeature[]>();
		const keyMapMap = new Map<number, Map<string, number>>();

		const s = stateCategories
			.filter((state) => visibleStateCategories[state] !== false)
			.map((stateCategory, seriesIndex) => {
				const seriesGrouped = groupedDataPoints.filter(
					(g) =>
						(g.items[0]?.stateCategory || g.state || "ToDo") === stateCategory,
				);

				dataMap.set(seriesIndex, seriesGrouped);

				const seriesKeyMap = new Map<string, number>();
				for (const group of seriesGrouped) {
					const globalIdx = allGroupedDataPoints.indexOf(group);
					seriesKeyMap.set(getGroupKey(group), globalIdx);
				}

				keyMapMap.set(seriesIndex, seriesKeyMap);

				const seriesData = seriesGrouped.map((group) => {
					const hasBlockedItems = group.items.some((i) => i.isBlocked);
					const fillColor = hasBlockedItems
						? errorColor
						: colorMap[stateCategory] || theme.palette.primary.main;
					const idx = allGroupedDataPoints.indexOf(group);
					return {
						x: group.size,
						y: group.cycleTime,
						groupIndex: idx,
						groupKey: getGroupKey(group),
						itemCount: group.items.length,
						color: fillColor,
					};
				});

				return {
					type: "scatter" as const,
					id: seriesIndex,
					label: getStateCategoryDisplayName(stateCategory),
					data: seriesData,
					xAxisId: "sizeAxis",
					yAxisId: "timeAxis",
					color: colorMap[stateCategory] || theme.palette.primary.main,
					markerSize: 4,
					highlightScope: {
						highlight: "item" as const,
						fade: "global" as const,
					},
					valueFormatter: (item: ScatterValueType | null) => {
						if (!item) return "";
						const groupIndexVal = (item as unknown as { groupIndex?: number })
							?.groupIndex;
						const itemKey = (item as unknown as { groupKey?: string })
							?.groupKey;
						let group: IGroupedFeature | undefined;
						if (typeof groupIndexVal === "number") {
							group = allGroupedDataPoints[groupIndexVal];
						}
						if (!group && typeof itemKey === "string") {
							const idx = groupKeyMap.get(itemKey);
							if (typeof idx === "number") {
								group = allGroupedDataPoints[idx];
							}
						}
						if (!group) return "";
						const numberOfClosedItems = group.items.length ?? 0;
						if (numberOfClosedItems === 1) {
							const single = group.items[0];
							return `${getWorkItemName(single)} - ${single.state} (Click for details)`;
						}
						return `${numberOfClosedItems} Closed ${featuresTerm} (Click for details)`;
					},
				};
			});
		return {
			series: s,
			seriesGroupedDataMap: dataMap,
			seriesGroupKeyMapMap: keyMapMap,
		};
	}, [
		stateCategories,
		visibleStateCategories,
		groupedDataPoints,
		colorMap,
		theme,
		allGroupedDataPoints,
		getGroupKey,
		groupKeyMap,
		featuresTerm,
	]);

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
						series={series}
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
									ScatterMarker(props, allGroupedDataPoints, groupKeyMap, {
										seriesGroupedDataMap,
										seriesGroupKeyMapMap,
										theme,
										featuresTerm,
										colorMap,
										onShowItems: handleShowItems,
									}),
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
