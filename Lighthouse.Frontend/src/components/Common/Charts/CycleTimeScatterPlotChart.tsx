import {
	Card,
	CardContent,
	FormControl,
	MenuItem,
	Select,
	Stack,
	type Theme,
	Typography,
	useTheme,
} from "@mui/material";
import {
	ChartsContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	type ScatterMarkerProps,
	ScatterPlot,
} from "@mui/x-charts";
import type React from "react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useChartVisibility } from "../../../hooks/useChartVisibility";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import {
	dateValueFormatter,
	getDateOnlyTimestamp,
	integerValueFormatter,
} from "../../../utils/charts/chartAxisUtils";
import {
	linesAboveAxis,
	markerSpecs,
} from "../../../utils/charts/offScaleLines";
import { axisTop, findOutliers } from "../../../utils/charts/outliers";
import { outlierWording } from "../../../utils/charts/outlierWording";
import {
	getBubbleSize,
	getMarkerColor,
	renderMarkerButton,
	renderMarkerCircle,
} from "../../../utils/charts/scatterMarkerUtils";
import { getWorkItemName } from "../../../utils/featureName";
import { getColorMapForKeys } from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import type { WorkItemsDialogOwnerKind } from "../WorkItemsDialog/workItemsDialogContexts";
import LegendChip from "./LegendChip";
import OffScaleLineMarkers, { MARKER_ROW_HEIGHT } from "./OffScaleLineMarkers";
import { outlierHeaderChip } from "./outlierHeaderChip";
import {
	allLineValues,
	buildPlotLines,
	currentNamedPercentiles,
	type INamedAnswer,
	isOfVisibleType,
	reservedMarkerRows,
} from "./outlierPlot";
import PercentileLegend from "./PercentileLegend";
import TimeBlackoutOverlay from "./TimeBlackoutOverlay";
import { useFrameIsReady, useHeaderSlotContent } from "./widgetHeaderSlot";

interface IGroupedWorkItem {
	closedDateTimestamp: number;
	cycleTime: number;
	items: IWorkItem[];
	type: string;
	hasBlockedItems: boolean;
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
				type: item.type || "Unknown",
				hasBlockedItems: false,
			};
		}

		groups[key].items.push(item);

		if (item.isBlocked) {
			groups[key].hasBlockedItems = true;
		}
	}

	return Object.values(groups).map((g) => ({
		...g,
		type: g.items?.[0]?.type || g.type || "Unknown",
	}));
};

const ScatterMarker = (
	props: ScatterMarkerProps,
	groupedDataPoints: IGroupedWorkItem[],
	theme: Theme,
	workItemsTerm: string,
	blockedTerm: string,
	colorMap: Record<string, string>,
	onShowItems: (items: IWorkItem[]) => void,
) => {
	const dataIndex = props.dataIndex || 0;
	const group = groupedDataPoints[dataIndex];

	if (!group) return null;

	const bubbleSize = getBubbleSize(group.items.length);
	const providedColor = (props as unknown as { color?: string }).color;
	const bubbleColor = getMarkerColor(group, colorMap, theme, providedColor);

	const blockedSuffix = group.hasBlockedItems ? ` (${blockedTerm})` : "";
	const itemsText = group.items.length > 1 ? "s" : "";

	const handleOpenWorkItems = () => {
		if (group.items.length > 0) {
			onShowItems(group.items);
		}
	};

	return (
		<>
			{renderMarkerCircle({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				color: bubbleColor,
				isHighlighted: props.isHighlighted,
				theme,
				title: `${group.items.length} item${itemsText} with cycle time ${group.cycleTime} days${blockedSuffix}`,
			})}
			{renderMarkerButton({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				ariaLabel: `View ${group.items.length} ${workItemsTerm}${itemsText} with cycle time ${group.cycleTime} days${blockedSuffix}`,
				onClick: handleOpenWorkItems,
			})}
		</>
	);
};

interface CycleTimeScatterPlotChartProps {
	percentileValues: IPercentileValue[];
	cycleTimeDataPoints: IWorkItem[];
	serviceLevelExpectation?: IPercentileValue | null;
	blackoutPeriods?: IBlackoutPeriod[];
	namedCycleTimeDefinitions?: INamedCycleTimeDefinition[];
	onFetchNamedCycleTimePercentiles?: (
		definitionId: number,
	) => Promise<IPercentileValue[]>;
	ownerKind: WorkItemsDialogOwnerKind;
}

const DEFAULT_SELECTION = "default";
const PLOT_TOP_MARGIN = 8;

const plotMargin = (markerRows: number) =>
	markerRows > 0
		? { top: PLOT_TOP_MARGIN + markerRows * MARKER_ROW_HEIGHT }
		: undefined;

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
	percentileValues,
	cycleTimeDataPoints,
	serviceLevelExpectation = null,
	blackoutPeriods = [],
	namedCycleTimeDefinitions = [],
	onFetchNamedCycleTimePercentiles,
	ownerKind,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();

	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);
	const sleTerm = getTerm(TERMINOLOGY_KEYS.SLE);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const [selectedDefinitionId, setSelectedDefinitionId] = useState<
		number | null
	>(null);
	const [namedAnswer, setNamedAnswer] = useState<INamedAnswer | null>(null);
	const frameIsReady = useFrameIsReady();
	const reportUsage = useUsageDataReporter();

	useEffect(() => {
		if (selectedDefinitionId === null) {
			return;
		}
		const selected = namedCycleTimeDefinitions.find(
			(definition) => definition.id === selectedDefinitionId,
		);
		if (!selected || selected.isValid === false) {
			setSelectedDefinitionId(null);
		}
	}, [selectedDefinitionId, namedCycleTimeDefinitions]);

	useEffect(() => {
		if (selectedDefinitionId === null || !onFetchNamedCycleTimePercentiles) {
			setNamedAnswer(null);
			return;
		}

		let cancelled = false;
		onFetchNamedCycleTimePercentiles(selectedDefinitionId)
			.then((percentiles) => {
				if (!cancelled)
					setNamedAnswer({
						definitionId: selectedDefinitionId,
						fetcher: onFetchNamedCycleTimePercentiles,
						percentiles,
					});
			})
			.catch(() => {
				if (!cancelled) setNamedAnswer(null);
			});
		return () => {
			cancelled = true;
		};
	}, [selectedDefinitionId, onFetchNamedCycleTimePercentiles]);

	const effectiveDataPoints = useMemo(() => {
		if (selectedDefinitionId === null) {
			return cycleTimeDataPoints;
		}

		return cycleTimeDataPoints.flatMap((item) => {
			const named = item.namedCycleTimes?.find(
				(value) => value.definitionId === selectedDefinitionId,
			);
			return named ? [{ ...item, cycleTime: named.days }] : [];
		});
	}, [cycleTimeDataPoints, selectedDefinitionId]);

	// An answer fetched for another definition, or by an older callback, is not the current one.
	const namedPercentiles = currentNamedPercentiles(
		namedAnswer,
		selectedDefinitionId,
		onFetchNamedCycleTimePercentiles,
	);

	const effectivePercentiles =
		selectedDefinitionId === null ? percentileValues : namedPercentiles;

	const types = useMemo(() => {
		const typeSet = new Set<string>();
		for (const item of cycleTimeDataPoints) {
			if (item.type) typeSet.add(item.type);
		}
		return Array.from(typeSet).sort((a, b) => a.localeCompare(b));
	}, [cycleTimeDataPoints]);

	const colorMap = useMemo(() => getColorMapForKeys(types), [types]);

	const {
		visiblePercentiles,
		togglePercentileVisibility,
		sleVisible,
		toggleSleVisibility,
		visibleTypes,
		toggleTypeVisibility,
	} = useChartVisibility({
		percentiles: effectivePercentiles,
		types,
		canHideEveryType: true,
		sleInitiallyVisible: true,
	});

	const chipRef = useRef<HTMLDivElement>(null);
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [dialogContent, setDialogContent] = useState<{
		title: string;
		items: IWorkItem[];
	}>({ title: "", items: [] });

	// The cut and the axis come from every item, so hiding a type never moves them.
	const analysis = useMemo(
		() => findOutliers(effectiveDataPoints, (item) => item.cycleTime),
		[effectiveDataPoints],
	);

	const fixedXAxisDomain = useMemo((): [number, number] | null => {
		if (effectiveDataPoints.length === 0) return null;
		const timestamps = effectiveDataPoints.map((item) =>
			getDateOnlyTimestamp(item.closedDate),
		);
		return [Math.min(...timestamps), Math.max(...timestamps)];
	}, [effectiveDataPoints]);

	const yAxisMax = useMemo(
		() =>
			axisTop({
				typicalValues: analysis.typical.map((item) => item.cycleTime),
				lineValues: allLineValues(
					effectivePercentiles,
					serviceLevelExpectation,
				),
				cut: analysis.cut,
				hasOutliers: analysis.outliers.length > 0,
			}),
		[analysis, effectivePercentiles, serviceLevelExpectation],
	);

	const groupedDataPoints = useMemo(
		() =>
			groupWorkItems(analysis.typical).filter((group) =>
				group.items.some((item) => isOfVisibleType(item, visibleTypes)),
			),
		[analysis, visibleTypes],
	);

	const visibleOutliers = analysis.outliers.filter((item) =>
		isOfVisibleType(item, visibleTypes),
	);

	const plotLines = useMemo(
		() =>
			buildPlotLines({
				percentiles: effectivePercentiles,
				visiblePercentiles,
				sle: serviceLevelExpectation,
				sleVisible,
				sleTerm,
				sleColor: theme.palette.primary.main,
			}),
		[
			effectivePercentiles,
			visiblePercentiles,
			sleVisible,
			serviceLevelExpectation,
			sleTerm,
			theme.palette.primary.main,
		],
	);

	const offScaleLines = linesAboveAxis(plotLines, yAxisMax);
	const markerRows = reservedMarkerRows(
		effectivePercentiles,
		serviceLevelExpectation,
		yAxisMax,
		analysis.outliers.length > 0,
	);

	const handleShowItems = (items: IWorkItem[]) => {
		setDialogContent({
			title: `${workItemsTerm} closed on ${items[0]?.closedDate.toLocaleDateString()} with ${cycleTimeTerm} of ${items[0]?.cycleTime} days`,
			items,
		});
		setDialogOpen(true);
	};

	const wording = useMemo(
		() =>
			outlierWording({
				workItems: workItemsTerm,
				sle: sleTerm,
				cycleTime: cycleTimeTerm,
			}),
		[workItemsTerm, sleTerm, cycleTimeTerm],
	);

	useHeaderSlotContent(
		outlierHeaderChip({
			wording,
			outliers: visibleOutliers,
			offScaleLines,
			disabled: !frameIsReady,
			onOpen: () => {
				reportUsage({ name: UsageDataEventName.OutlierListOpened });
				setDialogContent({
					title: wording.listTitle,
					items: [...visibleOutliers],
				});
				setDialogOpen(true);
			},
			chipRef,
		}),
	);

	const cycleTimeSelector =
		namedCycleTimeDefinitions.length > 0 ? (
			<FormControl size="small" sx={{ minWidth: 200 }}>
				<Select
					value={
						selectedDefinitionId === null
							? DEFAULT_SELECTION
							: String(selectedDefinitionId)
					}
					onChange={(event) => {
						const value = event.target.value;
						setSelectedDefinitionId(
							value === DEFAULT_SELECTION ? null : Number(value),
						);
					}}
					SelectDisplayProps={{ "aria-label": cycleTimeTerm }}
				>
					<MenuItem value={DEFAULT_SELECTION}>Default</MenuItem>
					{namedCycleTimeDefinitions.map((definition) => (
						<MenuItem
							key={definition.id}
							value={String(definition.id)}
							disabled={definition.isValid === false}
						>
							{definition.isValid === false
								? `${definition.name} (invalid — fix its states)`
								: definition.name}
						</MenuItem>
					))}
				</Select>
			</FormControl>
		) : null;

	if (cycleTimeDataPoints.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				No data available
			</Typography>
		);
	}

	return (
		<>
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ display: "flex", flexDirection: "column", height: "100%" }}
				>
					<Stack
						direction="row"
						spacing={2}
						sx={{
							alignItems: "center",
							mb: 1,
						}}
					>
						<Typography variant="h6">{cycleTimeTerm}</Typography>
						{cycleTimeSelector}
					</Stack>

					{effectiveDataPoints.length > 0 ? (
						<>
							<Stack
								direction="row"
								spacing={1}
								sx={{
									mb: 2,
									flexWrap: "wrap",
									gap: 1,
									justifyContent: "space-between",
								}}
							>
								<PercentileLegend
									percentiles={effectivePercentiles}
									visiblePercentiles={visiblePercentiles}
									onTogglePercentile={togglePercentileVisibility}
									serviceLevelExpectation={serviceLevelExpectation}
									serviceLevelExpectationLabel={serviceLevelExpectationTerm}
									sleVisible={sleVisible}
									onToggleSle={toggleSleVisibility}
								/>

								{types.length > 0 && (
									<Stack
										direction="row"
										spacing={1}
										sx={{ flexWrap: "wrap", gap: 1 }}
									>
										{types.map((type) => (
											<LegendChip
												key={`legend-type-${type}`}
												label={type}
												color={colorMap[type]}
												visible={visibleTypes[type] !== false}
												onToggle={() => toggleTypeVisibility(type)}
											/>
										))}
									</Stack>
								)}
							</Stack>

							{groupedDataPoints.length > 0 ? (
								<ChartsContainer
									sx={{ flex: 1, minHeight: 0, height: "100%" }}
									margin={plotMargin(markerRows)}
									xAxis={[
										{
											id: "timeAxis",
											scaleType: "time",
											label: "Date",
											height: 56,
											min: fixedXAxisDomain?.[0],
											max: fixedXAxisDomain?.[1],
											valueFormatter: dateValueFormatter,
										},
									]}
									yAxis={[
										{
											id: "cycleTimeAxis",
											scaleType: "linear",
											label: `${cycleTimeTerm} (days)`,
											min: 1,
											max: yAxisMax,
											valueFormatter: integerValueFormatter,
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
												color: getMarkerColor(group, colorMap, theme),
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
													const workItem = group.items[0];
													return `${getWorkItemName(workItem.name, workItem.referenceId)} (Click for details)`;
												}

												return `${numberOfClosedItems} Closed ${workItemsTerm} (Click for details)`;
											},
										},
									]}
								>
									{/* Reference lines for percentiles */}
									{effectivePercentiles.map((p) => {
										const forecastLevel = new ForecastLevel(p.percentile);
										return visiblePercentiles[p.percentile] &&
											p.value <= yAxisMax ? (
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

									{/* SLE reference line */}
									{sleVisible &&
										serviceLevelExpectation &&
										serviceLevelExpectation.value <= yAxisMax && (
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

									{offScaleLines.length > 0 && (
										<OffScaleLineMarkers markers={markerSpecs(offScaleLines)} />
									)}
									<ChartsXAxis axisId="timeAxis" />
									<ChartsYAxis axisId="cycleTimeAxis" />
									<TimeBlackoutOverlay
										blackoutPeriods={blackoutPeriods}
										axisId="timeAxis"
									/>
									<ScatterPlot
										slots={{
											marker: (props) =>
												ScatterMarker(
													props,
													groupedDataPoints,
													theme,
													workItemsTerm,
													blockedTerm,
													colorMap,
													handleShowItems,
												),
										}}
									/>

									<ChartsTooltip
										trigger="item"
										sx={{
											zIndex: 1200,
											maxWidth: "400px",
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
								</ChartsContainer>
							) : (
								<Typography
									variant="body2"
									color="text.secondary"
									sx={{ flex: 1 }}
								>
									{visibleOutliers.length > 0
										? wording.allVisibleAreOutliers
										: wording.allTypesHidden}
								</Typography>
							)}
						</>
					) : (
						<Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
							No data available
						</Typography>
					)}
				</CardContent>
			</Card>
			<WorkItemsDialog
				title={dialogContent.title}
				items={dialogContent.items}
				open={dialogOpen}
				onClose={() => {
					setDialogOpen(false);
					chipRef.current?.focus();
				}}
				context="closedItems"
				ownerKind={ownerKind}
			/>
		</>
	);
};

export default CycleTimeScatterPlotChart;
