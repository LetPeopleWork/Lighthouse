import {
	Card,
	CardContent,
	Stack,
	type Theme,
	ToggleButton,
	ToggleButtonGroup,
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
import { type JSX, useCallback, useEffect, useMemo, useState } from "react";
import { useChartVisibility } from "../../../hooks/useChartVisibility";
import type { IFeature } from "../../../models/Feature";
import type { IFeatureSizeEstimationResponse } from "../../../models/Metrics/FeatureSizeEstimationData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { StateCategory } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	getMaxYAxisHeight,
	integerValueFormatter,
} from "../../../utils/charts/chartAxisUtils";
import {
	type BaseGroupedItem,
	getBubbleSize,
	renderMarkerButton,
	renderMarkerCircle,
} from "../../../utils/charts/scatterMarkerUtils";
import { getWorkItemName } from "../../../utils/featureName";
import { errorColor, getColorMapForKeys } from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import LegendChip from "./LegendChip";
import PercentileLegend from "./PercentileLegend";

interface IGroupedFeature extends BaseGroupedItem<IFeature> {
	cycleTime: number;
	size: number;
	items: IFeature[];
	state: string;
	hasBlockedItems?: boolean;
	type?: string;
	estimationValue?: number;
}

type ScatterDatum = {
	item?: Record<string, unknown>;
	color?: string;
	groupIndex?: number;
	groupKey?: string;
	x?: number;
	y?: number;
	[key: string]: unknown;
};

type ScatterPropsExtended = ScatterMarkerProps & {
	data?: ScatterDatum;
	datum?: ScatterDatum;
	item?: ScatterDatum;
	dataIndex?: number;
	seriesIndex?: number | string;
	seriesId?: number | string;
	color?: string;
};

type MarkerOptions = {
	seriesGroupedDataMap?: Map<string | number, IGroupedFeature[]>;
	seriesGroupKeyMapMap?: Map<string | number, Map<string, number>>;
	theme: Theme;
	featuresTerm: string;
	colorMap: Record<string, string>;
	onShowItems: (items: IFeature[]) => void;
};

const getDatum = (props: ScatterPropsExtended): ScatterDatum | undefined => {
	return props.data ?? props.datum ?? props.item;
};

const getExplicitGroupByIndex = (
	datumGroupIndex: number | undefined,
	allGroupedDataPoints: IGroupedFeature[],
	seriesGroup: IGroupedFeature,
): IGroupedFeature => {
	if (typeof datumGroupIndex !== "number") return seriesGroup;

	const explicitGroup = allGroupedDataPoints[datumGroupIndex];
	return explicitGroup && explicitGroup !== seriesGroup
		? explicitGroup
		: seriesGroup;
};

const getExplicitGroupByKey = (
	datumGroupKey: string | undefined,
	seriesIndexOrId: string | number,
	seriesGroupKeyMapMap: Map<string | number, Map<string, number>> | undefined,
	allGroupedDataPoints: IGroupedFeature[],
	seriesGroup: IGroupedFeature,
): IGroupedFeature => {
	if (typeof datumGroupKey !== "string" || !seriesGroupKeyMapMap) {
		return seriesGroup;
	}

	const seriesKeyMap = seriesGroupKeyMapMap.get(seriesIndexOrId);
	const explicitIdx = seriesKeyMap?.get(datumGroupKey);

	if (typeof explicitIdx !== "number") return seriesGroup;

	const explicitGroup = allGroupedDataPoints[explicitIdx];
	return explicitGroup && explicitGroup !== seriesGroup
		? explicitGroup
		: seriesGroup;
};

const getGroupFromSeries = (
	props: ScatterPropsExtended,
	datum: ScatterDatum | undefined,
	allGroupedDataPoints: IGroupedFeature[],
	seriesGroupedDataMap?: Map<string | number, IGroupedFeature[]>,
	seriesGroupKeyMapMap?: Map<string | number, Map<string, number>>,
): IGroupedFeature | undefined => {
	if (!seriesGroupedDataMap) return undefined;

	const seriesIndexOrId = props.seriesIndex ?? props.seriesId;
	if (seriesIndexOrId === undefined) return undefined;

	const seriesGrouped = seriesGroupedDataMap.get(seriesIndexOrId);
	const dataIndex = props.dataIndex ?? 0;

	if (!seriesGrouped || typeof dataIndex !== "number") return undefined;

	const seriesGroup = seriesGrouped[dataIndex];
	if (!seriesGroup) return undefined;

	const datumGroupIndex = datum?.groupIndex;
	const datumGroupKey = datum?.groupKey;

	if (typeof datumGroupIndex === "number") {
		return getExplicitGroupByIndex(
			datumGroupIndex,
			allGroupedDataPoints,
			seriesGroup,
		);
	}

	return getExplicitGroupByKey(
		datumGroupKey,
		seriesIndexOrId,
		seriesGroupKeyMapMap,
		allGroupedDataPoints,
		seriesGroup,
	);
};

const getGroupByIndex = (
	datumGroupIndex: number | undefined,
	allGroupedDataPoints: IGroupedFeature[],
): IGroupedFeature | undefined => {
	return typeof datumGroupIndex === "number"
		? allGroupedDataPoints[datumGroupIndex]
		: undefined;
};

const getGroupByKey = (
	datumGroupKey: string | undefined,
	groupKeyMap: Map<string, number>,
	allGroupedDataPoints: IGroupedFeature[],
): IGroupedFeature | undefined => {
	if (typeof datumGroupKey !== "string") return undefined;

	const idx = groupKeyMap.get(datumGroupKey);
	return typeof idx === "number" ? allGroupedDataPoints[idx] : undefined;
};

const getGroupByCoordinates = (
	props: ScatterPropsExtended,
	datum: ScatterDatum | undefined,
	allGroupedDataPoints: IGroupedFeature[],
): IGroupedFeature | undefined => {
	const datumX = datum?.x ?? props.x;
	const datumY = datum?.y ?? props.y;
	return allGroupedDataPoints.find(
		(g) => g.size === datumX && g.cycleTime === datumY,
	);
};

const renderFallbackMarker = (
	props: ScatterPropsExtended,
	theme: Theme,
): JSX.Element => {
	const providedColor = props.color;
	const fallbackColor = providedColor ?? theme.palette.primary.main;
	const fallbackSize = 6;

	return (
		<>
			{renderMarkerCircle({
				x: props.x,
				y: props.y,
				size: fallbackSize,
				color: fallbackColor,
				isHighlighted: props.isHighlighted,
				theme,
				title: "Feature (unknown group) - click for details",
			})}
			{renderMarkerButton({
				x: props.x,
				y: props.y,
				size: fallbackSize,
				ariaLabel: "View feature details",
				onClick: () => {},
			})}
		</>
	);
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

	const propsExtended = props as ScatterPropsExtended;
	const datum = getDatum(propsExtended);

	const group =
		getGroupFromSeries(
			propsExtended,
			datum,
			allGroupedDataPoints,
			seriesGroupedDataMap,
			seriesGroupKeyMapMap,
		) ??
		getGroupByIndex(datum?.groupIndex, allGroupedDataPoints) ??
		getGroupByKey(datum?.groupKey, groupKeyMap, allGroupedDataPoints) ??
		getGroupByCoordinates(propsExtended, datum, allGroupedDataPoints);

	if (!group) {
		return renderFallbackMarker(propsExtended, theme);
	}

	const bubbleSize = getBubbleSize(group.items.length);
	const stateCategory = group.items[0]?.stateCategory || group.state || "ToDo";
	const hasBlockedItems = group.items.some((i) => i.isBlocked);
	const providedColor = propsExtended.color;
	const bubbleColor = hasBlockedItems
		? errorColor
		: (providedColor ?? colorMap[stateCategory] ?? theme.palette.primary.main);

	const handleOpenFeatures = () => {
		if (group.items.length > 0) {
			onShowItems(group.items);
		}
	};

	const itemCountText = group.items.length > 1 ? "s" : "";
	const featureTerm =
		group.items.length > 1 ? featuresTerm : featuresTerm.slice(0, -1);

	return (
		<>
			{renderMarkerCircle({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				color: bubbleColor,
				isHighlighted: props.isHighlighted,
				theme,
				title: `${group.items.length} item${itemCountText} with size ${group.size} child items`,
			})}
			{renderMarkerButton({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				ariaLabel: `View ${group.items.length} ${featureTerm} with size ${group.size} child items`,
				onClick: handleOpenFeatures,
			})}
		</>
	);
};

const getStateCategoryDisplayName = (stateCategory: StateCategory): string => {
	if (stateCategory === "ToDo") return "To Do";
	if (stateCategory === "Doing") return "In Progress";
	if (stateCategory === "Done") return "Done";
	return stateCategory;
};

const groupFeatures = (
	items: IFeature[],
	estimationLookup?: Map<number, number>,
): IGroupedFeature[] => {
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

		const estimationValue = estimationLookup?.get(item.id);

		const key = `${item.stateCategory ?? item.state ?? "Unknown"}-${cycleTime}-${item.size}`;

		if (!groups[key]) {
			groups[key] = {
				cycleTime,
				size: item.size,
				items: [],
				state: item.stateCategory || item.state || "Unknown",
				estimationValue,
			};
		}

		groups[key].items.push(item);
	}

	return Object.values(groups);
};

interface FeatureSizeScatterPlotChartProps {
	sizeDataPoints: IFeature[];
	sizePercentileValues?: IPercentileValue[];
	estimationData?: IFeatureSizeEstimationResponse;
}

const createSeriesDataPoint = (
	group: IGroupedFeature,
	allGroupedDataPoints: IGroupedFeature[],
	getGroupKey: (g: IGroupedFeature) => string,
	stateCategory: StateCategory,
	colorMap: Record<string, string>,
	theme: Theme,
	useEstimationY = false,
) => {
	const hasBlockedItems = group.items.some((i) => i.isBlocked);
	const fillColor = hasBlockedItems
		? errorColor
		: colorMap[stateCategory] || theme.palette.primary.main;
	const idx = allGroupedDataPoints.indexOf(group);

	const yValue =
		useEstimationY && group.estimationValue !== undefined
			? group.estimationValue
			: group.cycleTime;

	return {
		x: group.size,
		y: yValue,
		groupIndex: idx,
		groupKey: getGroupKey(group),
		itemCount: group.items.length,
		color: fillColor,
	};
};

const formatSeriesValue = (
	item: ScatterValueType | null,
	allGroupedDataPoints: IGroupedFeature[],
	groupKeyMap: Map<string, number>,
	featuresTerm: string,
): string => {
	if (!item) return "";

	const groupIndexVal = (item as unknown as { groupIndex?: number })
		?.groupIndex;
	const itemKey = (item as unknown as { groupKey?: string })?.groupKey;

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
};

const FeatureSizeScatterPlotChart: React.FC<
	FeatureSizeScatterPlotChartProps
> = ({ sizeDataPoints, sizePercentileValues = [], estimationData }) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const sizeTerm = "Size";

	// Determine if estimation toggle should be shown
	const showEstimationToggle =
		estimationData?.status === "Ready" && !!estimationData?.estimationUnit;

	type YAxisMode = "cycleTime" | "estimation";
	const [yAxisMode, setYAxisMode] = useState<YAxisMode>(
		showEstimationToggle ? "estimation" : "cycleTime",
	);

	// Build estimation lookup: featureId → estimationNumericValue
	const estimationLookup = useMemo(() => {
		if (!estimationData || estimationData.status !== "Ready") {
			return undefined;
		}
		const lookup = new Map<number, number>();
		for (const fe of estimationData.featureEstimations) {
			lookup.set(fe.featureId, fe.estimationNumericValue);
		}
		return lookup;
	}, [estimationData]);

	const useEstimationYAxis = yAxisMode === "estimation" && showEstimationToggle;

	const percentiles = sizePercentileValues ?? [];

	const [fixedXAxisMax, setFixedXAxisMax] = useState<number | null>(null);
	const [fixedYAxisMax, setFixedYAxisMax] = useState<number | null>(null);

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

	const {
		visiblePercentiles,
		togglePercentileVisibility,
		visibleTypes: visibleStateCategories,
		toggleTypeVisibility: toggleStateCategoryVisibility,
	} = useChartVisibility({
		percentiles,
		types: stateCategories,
		initialTypeVisibility: Object.fromEntries(
			stateCategories.map((state) => [state, state === "Done"]),
		) as Record<StateCategory, boolean>,
	});

	useEffect(() => {
		if (sizeDataPoints.length === 0) {
			setFixedXAxisMax(null);
			setFixedYAxisMax(null);
			return;
		}

		const xMax = getMaxYAxisHeight({
			percentiles,
			dataPoints: sizeDataPoints,
			getDataValue: (item) => item.size,
		});
		setFixedXAxisMax(xMax);

		if (useEstimationYAxis && estimationLookup) {
			const estimationValues = sizeDataPoints
				.map((item) => estimationLookup.get(item.id) ?? 0)
				.filter((v) => v > 0);
			const maxEstimation = Math.max(...estimationValues, 0);
			setFixedYAxisMax(maxEstimation * 1.1);
		} else {
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
		}
	}, [sizeDataPoints, percentiles, useEstimationYAxis, estimationLookup]);

	const allGroupedDataPoints = useMemo(
		() => groupFeatures(sizeDataPoints, estimationLookup),
		[sizeDataPoints, estimationLookup],
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

				const seriesData = seriesGrouped.map((group) =>
					createSeriesDataPoint(
						group,
						allGroupedDataPoints,
						getGroupKey,
						stateCategory,
						colorMap,
						theme,
						useEstimationYAxis,
					),
				);

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
					valueFormatter: (item: ScatterValueType | null) =>
						formatSeriesValue(
							item,
							allGroupedDataPoints,
							groupKeyMap,
							featuresTerm,
						),
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
		useEstimationYAxis,
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
					{(percentiles.length > 0 ||
						stateCategories.length > 0 ||
						showEstimationToggle) && (
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
							<PercentileLegend
								percentiles={percentiles}
								visiblePercentiles={visiblePercentiles}
								onTogglePercentile={togglePercentileVisibility}
							/>
							{showEstimationToggle && (
								<ToggleButtonGroup
									value={yAxisMode}
									exclusive
									onChange={(_e, newMode) => {
										if (newMode !== null) {
											setYAxisMode(newMode as YAxisMode);
										}
									}}
									size="small"
									aria-label="Y-axis mode"
									sx={{
										height: 28,
										"& .MuiToggleButton-root": {
											fontSize: "0.75rem",
											py: 0,
											px: 1,
											textTransform: "none",
										},
									}}
								>
									<ToggleButton
										value="estimation"
										aria-label={estimationData?.estimationUnit ?? "Estimation"}
									>
										{estimationData?.estimationUnit ?? "Estimation"}
									</ToggleButton>
									<ToggleButton value="cycleTime" aria-label={cycleTimeTerm}>
										{cycleTimeTerm}
									</ToggleButton>
								</ToggleButtonGroup>
							)}
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
									getMaxYAxisHeight({
										percentiles,
										dataPoints: sizeDataPoints,
										getDataValue: (item) => item.size,
									}),
								valueFormatter: integerValueFormatter,
							},
						]}
						yAxis={[
							{
								id: "timeAxis",
								scaleType: "linear",
								label: useEstimationYAxis
									? `${estimationData?.estimationUnit ?? "Estimation"}`
									: `${cycleTimeTerm} (days)`,
								min: 0,
								max: fixedYAxisMax ?? undefined,
								valueFormatter:
									useEstimationYAxis &&
									estimationData?.useNonNumericEstimation &&
									estimationData.categoryValues.length > 0
										? (v: unknown) => {
												const index = Math.round(v as number);
												return estimationData.categoryValues[index] ?? "";
											}
										: integerValueFormatter,
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
					highlightColumn={{
						title: sizeTerm,
						description: `Child ${workItemsTerm}`,
						valueGetter: (item) => (item as IFeature).size ?? "",
					}}
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
