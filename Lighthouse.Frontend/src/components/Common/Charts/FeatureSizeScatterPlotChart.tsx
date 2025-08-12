import {
	Card,
	CardContent,
	type Theme,
	Typography,
	useTheme,
} from "@mui/material";
import {
	ChartContainer,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	type ScatterMarkerProps,
	ScatterPlot,
} from "@mui/x-charts";
import type React from "react";
import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { getWorkItemName } from "../../../utils/featureName";
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
	closedDateTimestamp: number;
	size: number;
	items: IFeature[];
}

const groupFeatures = (items: IFeature[]): IGroupedFeature[] => {
	const groups: Record<string, IGroupedFeature> = {};

	for (const item of items) {
		const closedDateTimestamp = getDateOnlyTimestamp(item.closedDate);

		const key = `${closedDateTimestamp}-${item.size}`;

		if (!groups[key]) {
			groups[key] = {
				closedDateTimestamp,
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
}

const getMaxYAxisHeight = (sizeDataPoints: IFeature[]): number => {
	const maxFromData =
		sizeDataPoints.length > 0
			? Math.max(...sizeDataPoints.map((item) => item.size))
			: 0;

	// Add 10% padding to the top
	return maxFromData * 1.1;
};

const FeatureSizeScatterPlotChart: React.FC<
	FeatureSizeScatterPlotChartProps
> = ({ sizeDataPoints }) => {
	const [groupedDataPoints, setGroupedDataPoints] = useState<IGroupedFeature[]>(
		[],
	);
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [selectedItems, setSelectedItems] = useState<IFeature[]>([]);
	const theme = useTheme();

	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const sizeTerm = "Size";

	useEffect(() => {
		setGroupedDataPoints(groupFeatures(sizeDataPoints));
	}, [sizeDataPoints]);

	const handleShowItems = (items: IFeature[]) => {
		setSelectedItems(items);
		setDialogOpen(true);
	};

	return sizeDataPoints.length > 0 ? (
		<>
			<Card sx={{ p: 2, borderRadius: 2 }}>
				<CardContent>
					<Typography variant="h6">
						{featuresTerm} {sizeTerm}
					</Typography>

					<ChartContainer
						height={500}
						xAxis={[
							{
								id: "timeAxis",
								scaleType: "time",
								label: "Closed Date",
								valueFormatter: (value: number) => {
									return new Date(value).toLocaleDateString();
								},
							},
						]}
						yAxis={[
							{
								id: "sizeAxis",
								scaleType: "linear",
								label: `${sizeTerm} (Child Items)`,
								min: 0,
								max: getMaxYAxisHeight(sizeDataPoints),
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
									y: group.size,
									id: index,
									itemCount: group.items.length,
								})),
								xAxisId: "timeAxis",
								yAxisId: "sizeAxis",
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

									return `${numberOfClosedItems} Closed ${featuresTerm} (Click for details)`;
								},
							},
						]}
					>
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
			<WorkItemsDialog
				title={`${featuresTerm} Closed on ${selectedItems[0]?.closedDate.toLocaleDateString()}`}
				items={selectedItems}
				open={dialogOpen}
				additionalColumnTitle={sizeTerm}
				additionalColumnDescription="Child Items"
				additionalColumnContent={(item) => (item as IFeature).size ?? ""}
				onClose={() => setDialogOpen(false)}
			/>
		</>
	) : (
		<Typography variant="body2" color="text.secondary">
			No data available
		</Typography>
	);
};

export default FeatureSizeScatterPlotChart;
