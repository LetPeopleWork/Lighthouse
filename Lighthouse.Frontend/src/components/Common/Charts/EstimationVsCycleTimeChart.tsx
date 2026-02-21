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
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	type ScatterMarkerProps,
	ScatterPlot,
} from "@mui/x-charts";
import type React from "react";
import { useMemo, useState } from "react";
import type {
	IEstimationVsCycleTimeDataPoint,
	IEstimationVsCycleTimeResponse,
} from "../../../models/Metrics/EstimationVsCycleTimeData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	getMaxYAxisHeight,
	integerValueFormatter,
} from "../../../utils/charts/chartAxisUtils";
import {
	getBubbleSize,
	renderMarkerButton,
	renderMarkerCircle,
} from "../../../utils/charts/scatterMarkerUtils";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

interface EstimationVsCycleTimeChartProps {
	data: IEstimationVsCycleTimeResponse;
	workItemLookup: Map<number, IWorkItem>;
}

const ScatterMarker = (
	props: ScatterMarkerProps,
	dataPoints: IEstimationVsCycleTimeDataPoint[],
	theme: Theme,
	cycleTimeTerm: string,
	onShowItems: (items: IWorkItem[]) => void,
	workItemLookup: Map<number, IWorkItem>,
) => {
	const dataIndex = props.dataIndex || 0;
	const point = dataPoints[dataIndex];

	if (!point) return null;

	const bubbleSize = getBubbleSize(point.workItemIds.length);
	const bubbleColor = theme.palette.primary.main;

	const itemsText = point.workItemIds.length > 1 ? "s" : "";

	const handleClick = () => {
		const items: IWorkItem[] = point.workItemIds
			.map((id) => workItemLookup.get(id))
			.filter((item): item is IWorkItem => item !== undefined);

		if (items.length > 0) {
			onShowItems(items);
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
				title: `${point.workItemIds.length} item${itemsText} with estimate ${point.estimationDisplayValue} and ${cycleTimeTerm} ${point.cycleTime} days`,
			})}
			{renderMarkerButton({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				ariaLabel: `View ${point.workItemIds.length} item${itemsText} with estimate ${point.estimationDisplayValue}`,
				onClick: handleClick,
			})}
		</>
	);
};

const EstimationVsCycleTimeChart: React.FC<EstimationVsCycleTimeChartProps> = ({
	data,
	workItemLookup,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const [dialogOpen, setDialogOpen] = useState(false);
	const [selectedItems, setSelectedItems] = useState<IWorkItem[]>([]);

	const handleShowItems = (items: IWorkItem[]) => {
		setSelectedItems(items);
		setDialogOpen(true);
	};

	const xAxisLabel = useMemo(() => {
		if (data.estimationUnit) {
			return `Estimation (${data.estimationUnit})`;
		}
		return "Estimation";
	}, [data.estimationUnit]);

	const xAxisConfig = useMemo(() => {
		const config: Record<string, unknown> = {
			id: "estimationAxis",
			scaleType: "linear" as const,
			label: xAxisLabel,
		};

		if (data.useNonNumericEstimation && data.categoryValues.length > 0) {
			config.min = -0.5;
			config.max = data.categoryValues.length - 0.5;
			config.tickMinStep = 1;
			config.tickValues = data.categoryValues.map((_, i) => i);
			config.valueFormatter = (v: unknown) => {
				const index = Math.round(v as number);
				return data.categoryValues[index] ?? "";
			};
		}

		return config;
	}, [data.useNonNumericEstimation, data.categoryValues, xAxisLabel]);

	const yAxisMax = useMemo(() => {
		return getMaxYAxisHeight({
			percentiles: [],
			serviceLevelExpectation: null,
			dataPoints: data.dataPoints,
			getDataValue: (point) => point.cycleTime,
		});
	}, [data.dataPoints]);

	const hasDiagnosticIssues =
		data.diagnostics.unmappedCount > 0 || data.diagnostics.invalidCount > 0;

	if (data.status === "NotConfigured") {
		return null;
	}

	if (data.status === "NoData" || data.dataPoints.length === 0) {
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
						justifyContent="space-between"
						alignItems="center"
						sx={{ mb: 1 }}
					>
						<Typography variant="h6">Estimation vs. {cycleTimeTerm}</Typography>
					</Stack>

					{hasDiagnosticIssues && (
						<Typography variant="caption" color="text.secondary" sx={{ mb: 1 }}>
							{data.diagnostics.unmappedCount > 0 &&
								`${data.diagnostics.unmappedCount} unmapped`}
							{data.diagnostics.unmappedCount > 0 &&
								data.diagnostics.invalidCount > 0 &&
								", "}
							{data.diagnostics.invalidCount > 0 &&
								`${data.diagnostics.invalidCount} not estimated`}
							{` ${workItemsTerm} excluded`}
						</Typography>
					)}

					<ChartContainer
						sx={{ flex: 1, minHeight: 0, height: "100%" }}
						xAxis={[xAxisConfig]}
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
								data: data.dataPoints.map((point, index) => ({
									x: point.estimationNumericValue,
									y: point.cycleTime,
									id: index,
									itemCount: point.workItemIds.length,
								})),
								xAxisId: "estimationAxis",
								yAxisId: "cycleTimeAxis",
								color: theme.palette.primary.main,
								markerSize: 4,
								highlightScope: {
									highlight: "item",
									fade: "global",
								},
								valueFormatter: (item) => {
									if (item?.id === undefined) return "";

									const point = data.dataPoints[item.id as number];
									if (!point) return "";

									const count = point.workItemIds.length;
									if (count === 1) {
										const workItem = workItemLookup.get(point.workItemIds[0]);
										if (workItem) {
											return `${workItem.name} (Click for details)`;
										}
									}

									return `${count} ${workItemsTerm} with estimate ${point.estimationDisplayValue} (Click for details)`;
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
										data.dataPoints,
										theme,
										cycleTimeTerm,
										handleShowItems,
										workItemLookup,
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
				title={`${workItemsTerm} with estimate ${selectedItems.length > 0 ? (data.dataPoints.find((dp) => dp.workItemIds.includes(selectedItems[0]?.id))?.estimationDisplayValue ?? "") : ""}`}
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
	);
};

export default EstimationVsCycleTimeChart;
