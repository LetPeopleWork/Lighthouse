import { Card, CardContent, Stack, Typography, useTheme } from "@mui/material";
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
import { useChartVisibility } from "../../../hooks/useChartVisibility";
import type { IPercentileValue } from "../../../models/PercentileValue";
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
import { getColorMapForKeys } from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import LegendChip from "./LegendChip";
import PercentileLegend from "./PercentileLegend";

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
			})}
		</>
	);
};

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
	const [groupedDataPoints, setGroupedDataPoints] = useState<
		IGroupedWorkItem[]
	>([]);
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
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

	useEffect(() => {
		const grouped = groupWorkItems(inProgressItems, doingStates);
		const filtered = grouped.filter((g) => {
			return g.items.some((item) => {
				const itemType = item.type || "";
				if (!itemType) return true;
				return visibleTypes[itemType] !== false;
			});
		});
		setGroupedDataPoints(filtered);
	}, [inProgressItems, doingStates, visibleTypes]);

	const handleShowItems = (items: IWorkItem[]) => {
		setSelectedItems(items);
		setDialogOpen(true);
	};

	const getMaxYAxisHeightValue = () => {
		return getMaxYAxisHeight({
			percentiles: percentileValues,
			serviceLevelExpectation,
			dataPoints: groupedDataPoints,
			getDataValue: (g) => g.age,
			minValue: 5,
		});
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
						<PercentileLegend
							percentiles={percentileValues}
							visiblePercentiles={visiblePercentiles}
							onTogglePercentile={togglePercentileVisibility}
							serviceLevelExpectation={serviceLevelExpectation}
							serviceLevelExpectationLabel={serviceLevelExpectationTerm}
							sleVisible={sleVisible}
							onToggleSle={toggleSleVisibility}
						/>
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
										return `${getWorkItemName(workItem)} (Click for details)`;
									}

									return `${numberOfItems} ${workItemsTerm} in ${group.state} (Click for details)`;
								},
							},
						]}
					>
						{percentileValues.map((p) => {
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
				highlightColumn={{
					title: workItemAgeTerm,
					description: "days",
					valueGetter: (item) => item.workItemAge,
				}}
			/>
		</>
	) : (
		<Typography variant="body2" color="text.secondary">
			No items in progress
		</Typography>
	);
};

export default WorkItemAgingChart;
