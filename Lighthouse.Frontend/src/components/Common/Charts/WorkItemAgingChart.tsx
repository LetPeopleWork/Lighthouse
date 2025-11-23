import {
	Card,
	CardContent,
	Chip,
	Stack,
	Typography,
	useTheme,
} from "@mui/material";
import type { Theme } from "@mui/material/styles";
import {
	ChartContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	ScatterPlot,
} from "@mui/x-charts";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
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

interface ScatterMarkerProps {
	x: number;
	y: number;
	isHighlighted?: boolean;
	dataIndex?: number;
	color?: string;
}

const getAgeInDays = (item: IWorkItem): number => {
	return item.workItemAge;
};

const getBubbleSize = (count: number): number => {
	return Math.min(5 + Math.sqrt(count) * 3, 20);
};

const ScatterMarker = (
	props: ScatterMarkerProps,
	groupedDataPoints: IGroupedWorkItem[],
	theme: Theme,
	workItemTerm: string,
	workItemsTerm: string,
	blockedTerm: string,
	onShowItems: (items: IWorkItem[]) => void,
) => {
	const dataIndex = props.dataIndex ?? 0;
	const group = groupedDataPoints[dataIndex];

	if (!group) return null;

	const bubbleSize = getBubbleSize(group.items.length);
	// Use the color provided on the data item if present (series/data.color), otherwise fallback to theme
	const providedColor = (props as unknown as { color?: string }).color;
	const bubbleColor = group.hasBlockedItems
		? errorColor
		: (providedColor ?? theme.palette.primary.main);

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
				<title>{`${group.items.length} ${itemsText} aged ${group.age} days in ${group.state} ${blockedTermText}`}</title>
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
					aria-label={`View ${group.items.length} ${itemsText} aged ${group.age} days in ${group.state} ${blockedTermText}`}
				/>
			</foreignObject>
		</>
	);
};

interface IGroupedWorkItem {
	state: string;
	stateIndex: number;
	age: number;
	items: IWorkItem[];
	hasBlockedItems: boolean;
	type: string;
}

const groupWorkItems = (
	items: IWorkItem[],
	doingStates: string[],
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
				type: item.type || "Unknown",
			};
		}

		groups[key].items.push(item);

		if (item.isBlocked) {
			groups[key].hasBlockedItems = true;
		}
	}

	return Object.values(groups);
};

interface WorkItemAgingChartProps {
	inProgressItems: IWorkItem[];
	percentileValues: IPercentileValue[];
	serviceLevelExpectation?: IPercentileValue | null;
	doingStates: string[];
}

const WorkItemAgingChart: React.FC<WorkItemAgingChartProps> = ({
	inProgressItems,
	percentileValues,
	serviceLevelExpectation = null,
	doingStates,
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

	// Extract unique types and create color map
	const types = useMemo(() => {
		const typeSet = new Set<string>();
		for (const item of inProgressItems) {
			if (item.type) typeSet.add(item.type);
		}
		return Array.from(typeSet).sort((a, b) => a.localeCompare(b));
	}, [inProgressItems]);

	const colorMap = useMemo(
		() => getColorMapForKeys(types, theme.palette.primary.main),
		[types, theme.palette.primary.main],
	);

	const [visibleTypes, setVisibleTypes] = useState<Record<string, boolean>>({});

	// Initialize visible types when types change
	useEffect(() => {
		setVisibleTypes(Object.fromEntries(types.map((type) => [type, true])));
	}, [types]);

	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);
	const sleTerm = getTerm(TERMINOLOGY_KEYS.SLE);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);

	useEffect(() => {
		setPercentiles(percentileValues);
		const initialVisibility: Record<number, boolean> = {};
		for (const p of percentileValues) {
			initialVisibility[p.percentile] = true;
		}
		setVisiblePercentiles(initialVisibility);
	}, [percentileValues]);

	useEffect(() => {
		const grouped = groupWorkItems(inProgressItems, doingStates);
		// Filter based on type visibility - show group if at least one item has a visible type
		const filtered = grouped.filter((g) => {
			// Check if any item in the group has a visible type
			return g.items.some((item) => {
				const itemType = item.type || "";
				if (!itemType) return true;
				return visibleTypes[itemType] !== false;
			});
		});
		setGroupedDataPoints(filtered);
	}, [inProgressItems, doingStates, visibleTypes]);

	const togglePercentileVisibility = (percentile: number) => {
		setVisiblePercentiles((prev) => ({
			...prev,
			[percentile]: !prev[percentile],
		}));
	};

	const toggleSleVisibility = () => {
		setSleVisible((prev) => !prev);
	};

	const toggleTypeVisibility = (type: string) => {
		setVisibleTypes((prev) => {
			// Count how many types are currently visible
			const visibleCount = Object.values(prev).filter(
				(v) => v !== false,
			).length;

			// If trying to hide the last visible type, prevent it
			if (prev[type] !== false && visibleCount <= 1) {
				return prev;
			}

			return {
				...prev,
				[type]: !prev[type],
			};
		});
	};

	const handleShowItems = (items: IWorkItem[]) => {
		setSelectedItems(items);
		setDialogOpen(true);
	};

	const getMaxYAxisHeight = () => {
		const percentileMax =
			percentileValues.length > 0
				? Math.max(...percentileValues.map((p) => p.value))
				: 0;
		const sleMax = serviceLevelExpectation ? serviceLevelExpectation.value : 0;
		const dataMax =
			groupedDataPoints.length > 0
				? Math.max(...groupedDataPoints.map((g) => g.age))
				: 0;

		const maxValue = Math.max(percentileMax, sleMax, dataMax, 5);
		return Math.ceil(maxValue * 1.1);
	};

	return inProgressItems.length > 0 && groupedDataPoints.length > 0 ? (
		<>
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ display: "flex", flexDirection: "column", height: "100%" }}
				>
					<Typography variant="h6">{workItemTerm} Aging</Typography>

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
						{serviceLevelExpectation && (
							<Chip
								key="legend-sle"
								label={serviceLevelExpectationTerm}
								sx={{
									borderColor: theme.palette.primary.main,
									borderWidth: sleVisible ? 2 : 1,
									borderStyle: "dashed",
									opacity: sleVisible ? 1 : 0.7,
									backgroundColor: sleVisible
										? hexToRgba(
												theme.palette.primary.main,
												theme.opacity.medium,
											)
										: "transparent",
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

					<ChartContainer
						sx={{ flex: 1, minHeight: 0, height: "100%" }}
						xAxis={[
							{
								id: "stateAxis",
								scaleType: "linear",
								data: doingStates.map((_, index) => index),
								label: "State",
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
								max: getMaxYAxisHeight(),
								valueFormatter: (value: number) => {
									return Number.isInteger(value) ? value.toString() : "";
								},
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
									color: group.hasBlockedItems
										? errorColor
										: colorMap[group.type] || theme.palette.primary.main,
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
										return `${getWorkItemName(workItem)} (Click for details)`;
									}

									return `${numberOfItems} ${workItemsTerm} in ${group.state} (Click for details)`;
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
						<ChartsXAxis />
						<ChartsYAxis />
						<ScatterPlot
							slots={{
								marker: (props) =>
									ScatterMarker(
										props,
										groupedDataPoints,
										theme,
										workItemTerm,
										workItemsTerm,
										blockedTerm,
										handleShowItems,
									),
							}}
						/>{" "}
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
				title={
					selectedItems.length > 0
						? `${workItemsTerm} in ${selectedItems[0]?.state} aged ${getAgeInDays(selectedItems[0])} days`
						: workItemsTerm
				}
				items={selectedItems}
				open={dialogOpen}
				onClose={() => setDialogOpen(false)}
				additionalColumnTitle={workItemAgeTerm}
				additionalColumnDescription="days"
				additionalColumnContent={(item) => item.workItemAge}
			/>
		</>
	) : (
		<Typography variant="body2" color="text.secondary">
			No items in progress
		</Typography>
	);
};

export default WorkItemAgingChart;
