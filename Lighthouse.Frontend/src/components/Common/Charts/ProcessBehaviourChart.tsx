import {
	Box,
	Card,
	CardContent,
	Chip,
	Stack,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import {
	ChartContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	LinePlot,
	MarkPlot,
} from "@mui/x-charts";
import type React from "react";
import {
	createContext,
	useCallback,
	useContext,
	useMemo,
	useState,
} from "react";
import type {
	ProcessBehaviourChartData,
	ProcessBehaviourChartDataPoint,
	SpecialCauseType,
} from "../../../models/Metrics/ProcessBehaviourChartData";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { useTerminology } from "../../../services/TerminologyContext";
import { calculateHistoricalAge } from "../../../utils/date/age";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";

const specialCauseColors: Record<SpecialCauseType, string> = {
	None: "",
	LargeChange: "#f44336",
	ModerateChange: "#ff9800",
	ModerateShift: "#ff9800",
	SmallShift: "#2196f3",
};

const specialCausePriority: SpecialCauseType[] = [
	"LargeChange",
	"ModerateChange",
	"ModerateShift",
	"SmallShift",
];

const specialCauseLabels: Record<SpecialCauseType, string> = {
	None: "",
	LargeChange: "Large Change",
	ModerateChange: "Moderate Change",
	ModerateShift: "Moderate Shift",
	SmallShift: "Small Shift",
};

const specialCauseTooltips: Record<SpecialCauseType, string> = {
	None: "",
	LargeChange:
		"A single point outside the natural process limits indicates an assignable cause with a dominant effect.",
	ModerateChange:
		"Two out of three successive values beyond one of the two sigma lines (on the same side of the average) signal a moderate process change.",
	ModerateShift:
		"Four out of five successive values beyond one of the one sigma lines (on the same side of the average) signal a moderate, sustained shift.",
	SmallShift:
		"Eight successive values on the same side of the average signal a small, sustained shift in the process.",
};

type SpecialCauseMarkContextValue = {
	readonly dataPoints: readonly ProcessBehaviourChartDataPoint[];
	readonly selectedCause: SpecialCauseType | null;
	readonly defaultColor: string;
};

const SpecialCauseMarkContext = createContext<SpecialCauseMarkContextValue>({
	dataPoints: [],
	selectedCause: null,
	defaultColor: "",
});

const getHighestPriorityCause = (
	causes: readonly SpecialCauseType[],
): SpecialCauseType | null => {
	for (const p of specialCausePriority) {
		if (causes.includes(p)) return p;
	}
	return null;
};

const SpecialCauseMark = (props: Record<string, unknown>) => {
	const { dataPoints, selectedCause, defaultColor } = useContext(
		SpecialCauseMarkContext,
	);
	const {
		x,
		y,
		dataIndex,
		color: _color,
		id: _id,
		...rest
	} = props as {
		x: number;
		y: number;
		dataIndex: number;
		color: string;
		id: string;
	} & Record<string, unknown>;

	const point = dataPoints[dataIndex];
	const causes = point?.specialCauses ?? [];
	const highestCause = getHighestPriorityCause(causes);

	const isHighlighted =
		selectedCause == null ? false : causes.includes(selectedCause);

	const displayCause =
		selectedCause != null && causes.includes(selectedCause)
			? selectedCause
			: highestCause;

	const fill =
		isHighlighted && displayCause
			? specialCauseColors[displayCause]
			: defaultColor;
	const radius = isHighlighted ? 6 : 4;

	return (
		<circle
			{...(rest as React.SVGProps<SVGCircleElement>)}
			cx={x}
			cy={y}
			r={radius}
			fill={fill}
			stroke={fill}
			strokeWidth={1}
			style={{ cursor: "pointer" }}
		/>
	);
};

interface ProcessBehaviourChartProps {
	data: ProcessBehaviourChartData;
	title: string;
	workItemLookup?: Map<number, IWorkItem>;
	useEqualSpacing?: boolean;
	showHistoricalAge?: boolean;
}

const ProcessBehaviourChart: React.FC<ProcessBehaviourChartProps> = ({
	data,
	title,
	workItemLookup,
	useEqualSpacing = false,
	showHistoricalAge = false,
}) => {
	const theme = useTheme();

	const [selectedSpecialCause, setSelectedSpecialCause] =
		useState<SpecialCauseType | null>(null);
	const [isDefaultApplied, setIsDefaultApplied] = useState(false);

	const [dialogOpen, setDialogOpen] = useState(false);
	const [dialogItems, setDialogItems] = useState<IWorkItem[]>([]);
	const [selectedDate, setSelectedDate] = useState<Date | null>(null);
	const [dialogTitle, setDialogTitle] = useState("");

	const { getTerm } = useTerminology();
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);

	const availableSpecialCauses = useMemo(() => {
		if (data.status !== "Ready") return new Set<SpecialCauseType>();
		const types = new Set<SpecialCauseType>();
		for (const point of data.dataPoints) {
			for (const cause of point.specialCauses) {
				if (cause !== "None") {
					types.add(cause);
				}
			}
		}
		return types;
	}, [data]);

	const hasAnySpecialCause = availableSpecialCauses.size > 0;

	if (hasAnySpecialCause && !isDefaultApplied) {
		const defaultCause = specialCausePriority.find((cause) =>
			availableSpecialCauses.has(cause),
		);
		if (defaultCause) {
			setSelectedSpecialCause(defaultCause);
		}
		setIsDefaultApplied(true);
	}

	const handleChipClick = useCallback(
		(cause: SpecialCauseType) => {
			if (!availableSpecialCauses.has(cause)) return;
			setSelectedSpecialCause((prev) => (prev === cause ? null : cause));
		},
		[availableSpecialCauses],
	);

	const neutralColor = theme.palette.text.secondary;
	const defaultColor = theme.palette.primary.main;

	const markContext = useMemo<SpecialCauseMarkContextValue>(
		() => ({
			dataPoints: data.dataPoints,
			selectedCause: selectedSpecialCause,
			defaultColor,
		}),
		[data.dataPoints, selectedSpecialCause, defaultColor],
	);

	const chartData = useMemo(() => {
		if (data.status !== "Ready" || data.dataPoints.length === 0) {
			return {
				xValues: [] as number[],
				yValues: [] as number[],
			};
		}

		const xValues = useEqualSpacing
			? data.dataPoints.map((_, index) => index)
			: data.dataPoints.map((p) => new Date(p.xValue).getTime());
		const yValues = data.dataPoints.map((p) => p.yValue);

		return { xValues, yValues };
	}, [data, useEqualSpacing]);

	const yAxisMax = useMemo(() => {
		if (chartData.yValues.length === 0) {
			return 10;
		}

		const maxValue = Math.max(
			...chartData.yValues,
			data.upperNaturalProcessLimit,
			data.average,
		);

		return maxValue * 1.1;
	}, [chartData.yValues, data.upperNaturalProcessLimit, data.average]);

	const handleDotClick = useCallback(
		(_event: React.MouseEvent, params: { dataIndex?: number }) => {
			const dataIndex = params.dataIndex;
			if (
				dataIndex == null ||
				dataIndex < 0 ||
				dataIndex >= data.dataPoints.length
			)
				return;

			const point = data.dataPoints[dataIndex];
			if (!point.workItemIds || point.workItemIds.length === 0) return;
			if (!workItemLookup || workItemLookup.size === 0) return;

			const resolvedItems: IWorkItem[] = [];
			for (const id of point.workItemIds) {
				const item = workItemLookup.get(id);
				if (item) {
					resolvedItems.push(item);
				}
			}

			if (resolvedItems.length === 0) return;

			const day = new Date(point.xValue);
			setDialogTitle(`${title} — ${day.toLocaleDateString()}`);

			setSelectedDate(day);

			setDialogItems(resolvedItems);
			setDialogOpen(true);
		},
		[data, workItemLookup, title],
	);

	if (data.status !== "Ready") {
		return (
			<Card sx={{ p: 2, borderRadius: 2 }}>
				<CardContent>
					<Typography variant="h6">{`${title} Process Behaviour Chart`}</Typography>
					<Typography variant="body2" color="text.secondary">
						{data.statusReason}
					</Typography>
				</CardContent>
			</Card>
		);
	}

	if (data.dataPoints.length === 0) {
		return (
			<Card sx={{ p: 2, borderRadius: 2 }}>
				<CardContent>
					<Typography variant="h6">{`${title} Process Behaviour Chart`}</Typography>
					<Typography variant="body2" color="text.secondary">
						No data available
					</Typography>
				</CardContent>
			</Card>
		);
	}

	return (
		<>
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ height: "100%", display: "flex", flexDirection: "column" }}
				>
					<Stack
						direction="row"
						justifyContent="space-between"
						alignItems="center"
						sx={{ mb: 1 }}
					>
						<Typography variant="h6">{`${title} Process Behaviour Chart`}</Typography>

						{hasAnySpecialCause && (
							<Stack direction="row" spacing={0.5}>
								{specialCausePriority.map((cause) => {
									const isAvailable = availableSpecialCauses.has(cause);
									const isSelected = selectedSpecialCause === cause;
									const chipColor = specialCauseColors[cause];

									const chip = (
										<Chip
											key={cause}
											label={specialCauseLabels[cause]}
											size="small"
											disabled={!isAvailable}
											onClick={() => handleChipClick(cause)}
											aria-pressed={isSelected}
											aria-disabled={!isAvailable}
											sx={{
												backgroundColor: isSelected ? chipColor : "transparent",
												color: isSelected
													? theme.palette.common.white
													: chipColor,
												border: `1px solid ${chipColor}`,
												opacity: isAvailable ? 1 : 0.5,
												"&:hover": {
													backgroundColor: isSelected
														? chipColor
														: `${chipColor}22`,
												},
											}}
										/>
									);

									return isAvailable ? (
										<Tooltip key={cause} title={specialCauseTooltips[cause]}>
											{chip}
										</Tooltip>
									) : (
										<Tooltip key={cause} title="No matching data points">
											<span>{chip}</span>
										</Tooltip>
									);
								})}
							</Stack>
						)}
					</Stack>

					<Box sx={{ flex: 1, minHeight: 0 }}>
						<SpecialCauseMarkContext.Provider value={markContext}>
							<ChartContainer
								xAxis={[
									useEqualSpacing
										? {
												id: "xAxis" as const,
												data: chartData.xValues,
												scaleType: "band" as const,
												valueFormatter: (value: number) => {
													if (value >= 0 && value < data.dataPoints.length) {
														return data.xAxisKind === "DateTime"
															? new Date(
																	data.dataPoints[value].xValue,
																).toLocaleString()
															: new Date(
																	data.dataPoints[value].xValue,
																).toLocaleDateString();
													}
													return String(value);
												},
											}
										: {
												id: "xAxis" as const,
												data: chartData.xValues,
												scaleType: "time" as const,
												valueFormatter: (value: number) =>
													data.xAxisKind === "DateTime"
														? new Date(value).toLocaleString()
														: new Date(value).toLocaleDateString(),
											},
								]}
								yAxis={[
									{
										id: "yAxis",
										min: 0,
										max: yAxisMax,
									},
								]}
								series={[
									{
										type: "line",
										data: chartData.yValues,
										color: defaultColor,
									},
								]}
								sx={{ flex: 1, minHeight: 0, height: "100%" }}
							>
								<ChartsXAxis axisId="xAxis" />
								<ChartsYAxis axisId="yAxis" />
								<LinePlot />
								<MarkPlot
									slots={{ mark: SpecialCauseMark }}
									onItemClick={handleDotClick}
								/>
								<ChartsTooltip />

								<ChartsReferenceLine
									y={data.average}
									label={`Average = ${data.average.toFixed(1)}`}
									labelAlign="end"
									lineStyle={{
										stroke: neutralColor,
										strokeWidth: 1.5,
										strokeDasharray: "5 5",
									}}
									labelStyle={{
										fill: neutralColor,
									}}
								/>

								<ChartsReferenceLine
									y={data.upperNaturalProcessLimit}
									label={`UNPL = ${data.upperNaturalProcessLimit.toFixed(1)}`}
									labelAlign="end"
									lineStyle={{
										stroke: neutralColor,
										strokeWidth: 1.5,
										strokeDasharray: "3 3",
									}}
									labelStyle={{
										fill: neutralColor,
									}}
								/>

								{data.lowerNaturalProcessLimit > 0 && (
									<ChartsReferenceLine
										y={data.lowerNaturalProcessLimit}
										label={`LNPL = ${data.lowerNaturalProcessLimit.toFixed(1)}`}
										labelAlign="end"
										lineStyle={{
											stroke: neutralColor,
											strokeWidth: 1.5,
											strokeDasharray: "3 3",
										}}
										labelStyle={{
											fill: neutralColor,
										}}
									/>
								)}
							</ChartContainer>
						</SpecialCauseMarkContext.Provider>
					</Box>
				</CardContent>
			</Card>

			<WorkItemsDialog
				title={dialogTitle}
				items={dialogItems}
				open={dialogOpen}
				onClose={() => setDialogOpen(false)}
				additionalColumnTitle={
					showHistoricalAge ? `${workItemAgeTerm}` : cycleTimeTerm
				}
				additionalColumnDescription="days"
				additionalColumnContent={(item) =>
					showHistoricalAge
						? calculateHistoricalAge(item, selectedDate ?? new Date())
						: item.cycleTime
				}
			/>
		</>
	);
};

export default ProcessBehaviourChart;
