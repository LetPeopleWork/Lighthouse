import {
	Card,
	CardContent,
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
import { useEffect, useMemo, useState } from "react";
import { useChartVisibility } from "../../../hooks/useChartVisibility";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	dateValueFormatter,
	getDateOnlyTimestamp,
	getMaxYAxisHeight,
	integerValueFormatter,
} from "../../../utils/charts/chartAxisUtils";
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
import LegendChip from "./LegendChip";
import PercentileLegend from "./PercentileLegend";

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
}

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
	percentileValues,
	cycleTimeDataPoints,
	serviceLevelExpectation = null,
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
		percentiles: percentileValues,
		types,
	});

	const [groupedDataPoints, setGroupedDataPoints] = useState<
		IGroupedWorkItem[]
	>([]);
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);
	const [fixedXAxisDomain, setFixedXAxisDomain] = useState<
		[number, number] | null
	>(null);
	const [fixedYAxisMax, setFixedYAxisMax] = useState<number | null>(null);

	// Calculate fixed axis domains
	useEffect(() => {
		if (cycleTimeDataPoints.length === 0) {
			setFixedXAxisDomain(null);
			setFixedYAxisMax(null);
			return;
		}

		// X-axis domain (time range)
		const timestamps = cycleTimeDataPoints.map((item) =>
			getDateOnlyTimestamp(item.closedDate),
		);
		const minTimestamp = Math.min(...timestamps);
		const maxTimestamp = Math.max(...timestamps);
		setFixedXAxisDomain([minTimestamp, maxTimestamp]);

		// Y-axis max - Using utility function!
		const yMax = getMaxYAxisHeight({
			percentiles: percentileValues,
			serviceLevelExpectation,
			dataPoints: cycleTimeDataPoints,
			getDataValue: (item) => item.cycleTime,
		});
		setFixedYAxisMax(yMax);
	}, [cycleTimeDataPoints, percentileValues, serviceLevelExpectation]);

	// Group and filter work items
	useEffect(() => {
		const grouped = groupWorkItems(cycleTimeDataPoints);
		const filtered = grouped.filter((g) => {
			return g.items.some((item) => {
				const itemType = item.type || "";
				if (!itemType) return true;
				return visibleTypes[itemType] !== false;
			});
		});
		setGroupedDataPoints(filtered);
	}, [cycleTimeDataPoints, visibleTypes]);

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
						sx={{
							mb: 2,
							flexWrap: "wrap",
							gap: 1,
							justifyContent: "space-between",
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

					<ChartContainer
						sx={{ flex: 1, minHeight: 0, height: "100%" }}
						xAxis={[
							{
								id: "timeAxis",
								scaleType: "time",
								label: "Date",
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
								max:
									fixedYAxisMax ??
									getMaxYAxisHeight({
										percentiles: percentileValues,
										serviceLevelExpectation,
										dataPoints: cycleTimeDataPoints,
										getDataValue: (item) => item.cycleTime,
									}),
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
										return `${getWorkItemName(workItem)} (Click for details)`;
									}

									return `${numberOfClosedItems} Closed ${workItemsTerm} (Click for details)`;
								},
							},
						]}
					>
						{/* Reference lines for percentiles */}
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

						{/* SLE reference line */}
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
					</ChartContainer>
				</CardContent>
			</Card>
			<WorkItemsDialog
				title={`${workItemsTerm} closed on ${selectedItems[0]?.closedDate.toLocaleDateString()} with ${cycleTimeTerm} of ${selectedItems[0]?.cycleTime} days`}
				items={selectedItems}
				open={dialogOpen}
				onClose={() => setDialogOpen(false)}
				highlightColumn={{
					title: cycleTimeTerm,
					description: "days",
					valueGetter: (item) => item.cycleTime,
				}}
			/>
		</>
	) : (
		<Typography variant="body2" color="text.secondary">
			No data available
		</Typography>
	);
};

export default CycleTimeScatterPlotChart;
