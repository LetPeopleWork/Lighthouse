import StackedBarChartOutlinedIcon from "@mui/icons-material/StackedBarChartOutlined";
import {
	Box,
	Card,
	CardContent,
	IconButton,
	Stack,
	ToggleButton,
	ToggleButtonGroup,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import type { Theme } from "@mui/material/styles";
import {
	ChartsContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	ScatterPlot,
} from "@mui/x-charts";
import { useXScale, useYScale } from "@mui/x-charts/hooks";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import { useChartVisibility } from "../../../hooks/useChartVisibility";
import { useShowPaceBands } from "../../../hooks/useShowPaceBands";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IPerStatePercentileValues } from "../../../models/PerStatePercentileValues";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	getMaxYAxisHeight,
	integerValueFormatter,
} from "../../../utils/charts/chartAxisUtils";
import {
	type BaseGroupedItem,
	getBubbleSize,
	getMarkerColor,
	renderMarkerButton,
	renderMarkerCircle,
} from "../../../utils/charts/scatterMarkerUtils";
import { getWorkItemName } from "../../../utils/featureName";
import { deriveStaleness } from "../../../utils/staleness/deriveStaleness";
import {
	certainColor,
	confidentColor,
	errorColor,
	getColorMapForKeys,
	realisticColor,
} from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import LegendChip from "./LegendChip";
import PercentileLegend from "./PercentileLegend";

export const STATE_BAND_HALF_WIDTH = 0.5;

export const PACE_BAND_COLORS_LOW_TO_HIGH = [
	certainColor,
	confidentColor,
	"#fbc02d",
	realisticColor,
	errorColor,
] as const;

export interface IPaceBandRect {
	key: string;
	x: number;
	y: number;
	width: number;
	height: number;
	fill: string;
}

interface PaceBandGeometryConfig {
	perStatePercentileValues: IPerStatePercentileValues[];
	doingStates: string[];
	xScale: (value: number) => number;
	yScale: (value: number) => number;
	axisMin: number;
	axisMax: number;
}

const paceBandColorForPosition = (position: number): string => {
	const lastIndex = PACE_BAND_COLORS_LOW_TO_HIGH.length - 1;
	const clamped = Math.min(position, lastIndex);
	return PACE_BAND_COLORS_LOW_TO_HIGH[clamped];
};

export const computePaceBandRects = ({
	perStatePercentileValues,
	doingStates,
	xScale,
	yScale,
	axisMin,
	axisMax,
}: PaceBandGeometryConfig): IPaceBandRect[] => {
	const percentilesByState: Record<string, IPercentileValue[]> = {};
	for (const perState of perStatePercentileValues) {
		if (perState.percentiles.length > 0) {
			percentilesByState[perState.state.toLowerCase()] = perState.percentiles;
		}
	}

	let carriedPercentiles: IPercentileValue[] | undefined;
	return doingStates.flatMap((stateName, stateIndex) => {
		const ownPercentiles = percentilesByState[stateName.toLowerCase()];
		if (ownPercentiles) {
			carriedPercentiles = ownPercentiles;
		}

		const percentiles = carriedPercentiles;
		if (!percentiles) {
			return [];
		}

		const sortedPercentiles = [...percentiles].sort(
			(a, b) => a.value - b.value,
		);

		const leftX = xScale(stateIndex - STATE_BAND_HALF_WIDTH);
		const rightX = xScale(stateIndex + STATE_BAND_HALF_WIDTH);
		const bandWidth = Math.abs(rightX - leftX);
		const x = Math.min(leftX, rightX);

		const upperBoundaries = [
			...sortedPercentiles.map((percentile) => ({
				upperValue: percentile.value,
				key: `${stateName}-${percentile.percentile}`,
			})),
			{ upperValue: axisMax, key: `${stateName}-top` },
		];

		let lowerValue = axisMin;
		const topPosition = upperBoundaries.length - 1;
		const reddest =
			PACE_BAND_COLORS_LOW_TO_HIGH[PACE_BAND_COLORS_LOW_TO_HIGH.length - 1];
		return upperBoundaries.flatMap((boundary, position) => {
			const lowerPixel = yScale(lowerValue);
			const upperPixel = yScale(boundary.upperValue);
			lowerValue = boundary.upperValue;
			const height = Math.abs(upperPixel - lowerPixel);
			if (height === 0) {
				return [];
			}
			return [
				{
					key: boundary.key,
					x,
					y: Math.min(lowerPixel, upperPixel),
					width: bandWidth,
					height,
					fill:
						position === topPosition
							? reddest
							: paceBandColorForPosition(position),
				},
			];
		});
	});
};

export const PaceBandOverlay: React.FC<{
	perStatePercentileValues: IPerStatePercentileValues[];
	doingStates: string[];
}> = ({ perStatePercentileValues, doingStates }) => {
	const xScale = useXScale("stateAxis") as (value: number) => number;
	const yScale = useYScale("ageAxis") as unknown as {
		(value: number): number;
		domain: () => [number, number];
	};

	const [axisMin, axisMax] = yScale.domain();

	const rects = computePaceBandRects({
		perStatePercentileValues,
		doingStates,
		xScale,
		yScale,
		axisMin,
		axisMax,
	});

	return (
		<g>
			{rects.map((rect) => (
				<rect
					key={rect.key}
					data-testid="pace-band"
					x={rect.x}
					y={rect.y}
					width={rect.width}
					height={rect.height}
					fill={rect.fill}
					fillOpacity={0.28}
				/>
			))}
		</g>
	);
};

interface ScatterMarkerProps {
	x: number;
	y: number;
	isHighlighted?: boolean;
	dataIndex?: number;
}

const getAgeInDays = (item: IWorkItem): number => {
	return item.workItemAge;
};

interface IGroupedWorkItem extends BaseGroupedItem<IWorkItem> {
	state: string;
	stateIndex: number;
	age: number;
	items: IWorkItem[];
	hasBlockedItems: boolean;
	hasStaleItems: boolean;
	type: string;
}

interface ScatterMarkerConfig {
	groupedDataPoints: IGroupedWorkItem[];
	theme: Theme;
	workItemTerm: string;
	workItemsTerm: string;
	blockedTerm: string;
	colorMap: Record<string, string>;
	onShowItems: (items: IWorkItem[]) => void;
}

const ScatterMarker = (
	props: ScatterMarkerProps,
	config: ScatterMarkerConfig,
) => {
	const {
		groupedDataPoints,
		theme,
		workItemTerm,
		workItemsTerm,
		blockedTerm,
		colorMap,
		onShowItems,
	} = config;

	const dataIndex = props.dataIndex ?? 0;
	const group = groupedDataPoints[dataIndex];
	if (!group) return null;

	const bubbleSize = getBubbleSize(group.items.length);
	const providedColor = (props as unknown as { color?: string }).color;
	const bubbleColor = getMarkerColor(group, colorMap, theme, providedColor);

	const handleOpenWorkItems = () => {
		if (group.items.length > 0) {
			onShowItems(group.items);
		}
	};

	const blockedTermText = group.hasBlockedItems
		? ` (${blockedTerm} ${workItemsTerm})`
		: "";
	const itemsText = group.items.length > 1 ? workItemsTerm : workItemTerm;

	return (
		<>
			{renderMarkerCircle({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				color: bubbleColor,
				isHighlighted: props.isHighlighted,
				theme,
				title: `${group.items.length} ${itemsText} aged ${group.age} days in ${group.state} ${blockedTermText}`,
			})}
			{renderMarkerButton({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				ariaLabel: `View ${group.items.length} ${itemsText} aged ${group.age} days in ${group.state} ${blockedTermText}`,
				onClick: handleOpenWorkItems,
				testId: group.hasStaleItems ? "aging-bubble-stale" : undefined,
			})}
		</>
	);
};

const groupWorkItems = (
	items: IWorkItem[],
	doingStates: string[],
	stalenessThresholdDays: number | undefined,
	now: Date,
): IGroupedWorkItem[] => {
	const groups: Record<string, IGroupedWorkItem> = {};

	const stateIndexMap: Record<string, number> = {};
	for (let i = 0; i < doingStates.length; i++) {
		stateIndexMap[doingStates[i].toLowerCase()] = i;
	}

	for (const item of items) {
		if (!item.state) continue;

		const age = getAgeInDays(item);
		const stateIndex = stateIndexMap[item.state.toLowerCase()];
		if (stateIndex === undefined) continue;

		const key = `${item.state}-${age}`;

		if (!groups[key]) {
			groups[key] = {
				state: item.state,
				stateIndex,
				age,
				items: [],
				hasBlockedItems: false,
				hasStaleItems: false,
				type: item.type || "Unknown",
			};
		}

		groups[key].items.push(item);

		if (item.isBlocked) {
			groups[key].hasBlockedItems = true;
		}

		if (deriveStaleness(item, stalenessThresholdDays, now)) {
			groups[key].hasStaleItems = true;
		}
	}

	return Object.values(groups);
};

type PercentileSource = "cycleTime" | "workItemAge";

interface WorkItemAgingChartProps {
	inProgressItems: IWorkItem[];
	percentileValues: IPercentileValue[];
	serviceLevelExpectation?: IPercentileValue | null;
	doingStates: string[];
	stalenessThresholdDays?: number;
	now?: Date;
	perStatePercentileValues?: IPerStatePercentileValues[];
	workItemAgePercentileValues?: IPercentileValue[];
}

const WorkItemAgingChart: React.FC<WorkItemAgingChartProps> = ({
	inProgressItems,
	percentileValues,
	serviceLevelExpectation = null,
	doingStates,
	stalenessThresholdDays,
	now: providedNow,
	perStatePercentileValues = [],
	workItemAgePercentileValues = [],
}) => {
	const [groupedDataPoints, setGroupedDataPoints] = useState<
		IGroupedWorkItem[]
	>([]);
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [percentileSource, setPercentileSource] =
		useState<PercentileSource>("cycleTime");
	const { showPaceBands, togglePaceBands } = useShowPaceBands();
	const theme = useTheme();
	const { getTerm } = useTerminology();

	const types = useMemo(() => {
		const typeSet = new Set<string>();
		for (const item of inProgressItems) {
			if (item.type) typeSet.add(item.type);
		}
		return Array.from(typeSet).sort((a, b) => a.localeCompare(b));
	}, [inProgressItems]);

	const colorMap = useMemo(() => getColorMapForKeys(types), [types]);

	const {
		visiblePercentiles,
		togglePercentileVisibility,
		sleVisible,
		toggleSleVisibility,
		visibleTypes,
		toggleTypeVisibility,
	} = useChartVisibility({
		percentiles: percentileValues,
		types,
	});

	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);
	const sleTerm = getTerm(TERMINOLOGY_KEYS.SLE);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);

	const meaningfulWorkItemAgePercentiles = useMemo(
		() => workItemAgePercentileValues.filter((p) => p.value > 0),
		[workItemAgePercentileValues],
	);

	const activePercentiles = useMemo(
		() =>
			percentileSource === "workItemAge"
				? meaningfulWorkItemAgePercentiles
				: percentileValues,
		[percentileSource, meaningfulWorkItemAgePercentiles, percentileValues],
	);

	const now = useMemo(() => providedNow ?? new Date(), [providedNow]);
	const nowTime = now.getTime();

	useEffect(() => {
		const grouped = groupWorkItems(
			inProgressItems,
			doingStates,
			stalenessThresholdDays,
			new Date(nowTime),
		);
		const filtered = grouped.filter((g) => {
			return g.items.some((item) => {
				const itemType = item.type || "";
				if (!itemType) return true;
				return visibleTypes[itemType] !== false;
			});
		});
		setGroupedDataPoints(filtered);
	}, [
		inProgressItems,
		doingStates,
		visibleTypes,
		stalenessThresholdDays,
		nowTime,
	]);

	const handleShowItems = (items: IWorkItem[]) => {
		setSelectedItems(items);
		setDialogOpen(true);
	};

	const getMaxYAxisHeightValue = () => {
		return getMaxYAxisHeight({
			percentiles: activePercentiles,
			serviceLevelExpectation,
			dataPoints: groupedDataPoints,
			getDataValue: (g) => g.age,
			minValue: 5,
		});
	};

	return (
		<>
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ display: "flex", flexDirection: "column", height: "100%" }}
				>
					<Box
						sx={{
							display: "flex",
							alignItems: "flex-start",
							justifyContent: "space-between",
						}}
					>
						<Typography variant="h6">{workItemTerm} Aging</Typography>
						{perStatePercentileValues.length > 0 && (
							<Tooltip title="Toggle pace percentiles">
								<IconButton
									data-testid="pace-bands-toggle"
									size="small"
									aria-pressed={showPaceBands}
									color={showPaceBands ? "primary" : "default"}
									onClick={togglePaceBands}
								>
									<StackedBarChartOutlinedIcon fontSize="small" />
								</IconButton>
							</Tooltip>
						)}
					</Box>

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
							percentiles={percentileValues}
							visiblePercentiles={visiblePercentiles}
							onTogglePercentile={togglePercentileVisibility}
							serviceLevelExpectation={serviceLevelExpectation}
							serviceLevelExpectationLabel={serviceLevelExpectationTerm}
							sleVisible={sleVisible}
							onToggleSle={toggleSleVisibility}
						/>
						<ToggleButtonGroup
							value={percentileSource}
							exclusive
							onChange={(_e, newSource) => {
								if (newSource !== null) {
									setPercentileSource(newSource as PercentileSource);
								}
							}}
							size="small"
							aria-label="Reference line source"
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
							<ToggleButton value="cycleTime" aria-label={cycleTimeTerm}>
								{cycleTimeTerm}
							</ToggleButton>
							<ToggleButton value="workItemAge" aria-label={workItemAgeTerm}>
								{workItemAgeTerm}
							</ToggleButton>
						</ToggleButtonGroup>
					</Stack>
					<Stack
						direction="row"
						spacing={1}
						sx={{ mb: 2, flexWrap: "wrap", gap: 1, ml: "auto" }}
					>
						{types.map((type) => (
							<LegendChip
								key={`legend-type-${type}`}
								label={type}
								color={colorMap[type] ?? theme.palette.primary.main}
								visible={visibleTypes[type] !== false}
								onToggle={() => toggleTypeVisibility(type)}
							/>
						))}
					</Stack>

					{groupedDataPoints.length > 0 ? (
						<ChartsContainer
							sx={{ flex: 1, minHeight: 0, height: "100%" }}
							xAxis={[
								{
									id: "stateAxis",
									scaleType: "linear",
									data: doingStates.map((_, index) => index),
									label: "State",
									height: 56,
									min: -0.5,
									max: doingStates.length - 0.5,
									tickNumber: doingStates.length,
									tickLabelInterval: () => true,
									disableTicks: false,
									valueFormatter: (value: number) => {
										const index = Math.round(value);
										return index >= 0 && index < doingStates.length
											? doingStates[index]
											: "";
									},
								},
							]}
							yAxis={[
								{
									id: "ageAxis",
									scaleType: "linear",
									label: `${workItemAgeTerm} (days)`,
									min: 1,
									max: getMaxYAxisHeightValue(),
									valueFormatter: integerValueFormatter,
								},
							]}
							series={[
								{
									type: "scatter",
									data: groupedDataPoints.map((group, index) => ({
										x: group.stateIndex,
										y: group.age,
										id: index,
										itemCount: group.items.length,
										color: getMarkerColor(group, colorMap, theme),
									})),
									xAxisId: "stateAxis",
									yAxisId: "ageAxis",
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

										const numberOfItems = group.items.length ?? 0;

										if (numberOfItems === 1) {
											const workItem = group.items[0];
											return `${getWorkItemName(workItem.name, workItem.referenceId)} (Click for details)`;
										}

										return `${numberOfItems} ${workItemsTerm} in ${group.state} (Click for details)`;
									},
								},
							]}
						>
							{activePercentiles.map((p) => {
								const forecastLevel = new ForecastLevel(p.percentile);
								const label =
									percentileSource === "workItemAge"
										? `${workItemAgeTerm} ${p.percentile}%`
										: `${p.percentile}%`;
								return visiblePercentiles[p.percentile] === false ? null : (
									<ChartsReferenceLine
										key={`percentile-${percentileSource}-${p.percentile}`}
										y={p.value}
										label={label}
										labelAlign="end"
										lineStyle={{
											stroke: forecastLevel.color,
											strokeWidth: 1,
											strokeDasharray: "5 5",
										}}
									/>
								);
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
							{doingStates.slice(0, -1).map((stateName, index) => (
								<ChartsReferenceLine
									key={`vertical-grid-${stateName}`}
									x={index + 0.5}
									lineStyle={{
										stroke: theme.palette.divider,
										strokeWidth: 1,
										opacity: 0.3,
									}}
								/>
							))}
							<ChartsXAxis axisId="stateAxis" />
							<ChartsYAxis axisId="ageAxis" />
							{showPaceBands && (
								<PaceBandOverlay
									perStatePercentileValues={perStatePercentileValues}
									doingStates={doingStates}
								/>
							)}
							<ScatterPlot
								slots={{
									marker: (props) =>
										ScatterMarker(props, {
											groupedDataPoints,
											theme,
											workItemTerm,
											workItemsTerm,
											blockedTerm,
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
						<Typography variant="body2" color="text.secondary">
							No work items in progress
						</Typography>
					)}
				</CardContent>
			</Card>
			<WorkItemsDialog
				title={
					selectedItems.length > 0
						? `${workItemsTerm} in ${selectedItems[0]?.state} aged ${getAgeInDays(selectedItems[0])} days`
						: workItemsTerm
				}
				items={selectedItems}
				open={dialogOpen}
				onClose={() => setDialogOpen(false)}
				highlightColumn={{
					title: workItemAgeTerm,
					description: "days",
					valueGetter: (item) => item.workItemAge,
				}}
				timeInStateColumn={{
					now,
					stalenessThresholdDays,
				}}
			/>
		</>
	);
};

export default WorkItemAgingChart;
