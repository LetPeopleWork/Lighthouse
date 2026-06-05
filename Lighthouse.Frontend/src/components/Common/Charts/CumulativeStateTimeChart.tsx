import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import {
	Box,
	Card,
	CardContent,
	Chip,
	Stack,
	Typography,
	useTheme,
} from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import { type ReactNode, useMemo } from "react";
import { useTypesVisibility } from "../../../hooks/useChartVisibility";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type {
	ICumulativeStateTimeResponse,
	ICumulativeStateTimeStateRow,
} from "../../../models/Metrics/CumulativeStateTime";
import {
	chooseDurationUnit,
	type DurationUnit,
	formatDuration,
} from "../../../utils/date/formatDuration";
import {
	flowEfficiency,
	resolveWaitRawStates,
} from "../../../utils/flowEfficiency";
import LegendChip from "./LegendChip";

const HATCH_PATTERN_ID = "cumulative-state-time-ongoing-hatch";
const WAIT_PATTERN_ID = "cumulative-state-time-wait-pattern";

const COMPLETED_CLASS = "Completed";
const ONGOING_CLASS = "Ongoing";
const COMPLETION_CLASSES = [COMPLETED_CLASS, ONGOING_CLASS];
const NO_COMPLETION_CLASSES: string[] = [];

interface CumulativeStateTimeChartProps {
	data: ICumulativeStateTimeResponse;
	onBarClick?: (stateName: string) => void;
	pickerSlot?: ReactNode;
	completionFilterEnabled?: boolean;
	waitStates?: string[];
	stateMappings?: IStateMapping[];
}

const FlowEfficiencyFigure: React.FC<{
	rows: ICumulativeStateTimeStateRow[];
	waitStates: string[];
	stateMappings: IStateMapping[];
}> = ({ rows, waitStates, stateMappings }) => {
	const result = flowEfficiency(rows, waitStates, stateMappings);

	if (result.status === "not-configured") {
		return null;
	}

	const label =
		result.status === "no-data"
			? "No data in scope"
			: `Flow Efficiency: ${Math.round(result.efficiencyPercent)}%`;

	return (
		<Typography
			variant="body2"
			color="text.secondary"
			data-testid="cumulative-state-time-flow-efficiency"
		>
			{label}
		</Typography>
	);
};

const orderByWorkflow = (
	states: ICumulativeStateTimeStateRow[],
): ICumulativeStateTimeStateRow[] =>
	[...states].sort((a, b) => a.workflowOrder - b.workflowOrder);

const formatMedian = (
	medianDays: number | null,
	unit: DurationUnit,
): string => {
	if (medianDays === null) {
		return "—";
	}
	return formatDuration(medianDays, unit);
};

const StateTooltipContent: React.FC<{
	row: ICumulativeStateTimeStateRow;
	unit: DurationUnit;
}> = ({ row, unit }) => (
	<Box
		data-testid={`cumulative-state-tooltip-${row.state}`}
		sx={{ display: "none" }}
	>
		<Typography variant="subtitle2">{row.state}</Typography>
		<Typography variant="caption" data-testid="tooltip-total">
			Total: {formatDuration(row.totalDays, unit)}
		</Typography>
		<Typography variant="caption" data-testid="tooltip-completed">
			Completed: {formatDuration(row.completedContributionDays, unit)}
		</Typography>
		<Typography variant="caption" data-testid="tooltip-ongoing">
			Ongoing: {formatDuration(row.ongoingContributionDays, unit)}
		</Typography>
		<Typography variant="caption" data-testid="tooltip-mean">
			Mean: {formatDuration(row.meanDays, unit)}
		</Typography>
		<Typography variant="caption" data-testid="tooltip-median">
			Median: {formatMedian(row.medianDays, unit)}
		</Typography>
		<Typography variant="caption" data-testid="tooltip-item-count">
			Items: {row.itemCount}
		</Typography>
		<Typography variant="caption" data-testid="tooltip-completed-count">
			Completed items: {row.completedItemCount}
		</Typography>
		<Typography variant="caption" data-testid="tooltip-ongoing-count">
			Ongoing items: {row.ongoingItemCount}
		</Typography>
	</Box>
);

const CumulativeStateTimeChart: React.FC<CumulativeStateTimeChartProps> = ({
	data,
	onBarClick,
	pickerSlot,
	completionFilterEnabled = false,
	waitStates = [],
	stateMappings = [],
}) => {
	const theme = useTheme();

	const completionClasses = useMemo(
		() =>
			completionFilterEnabled ? COMPLETION_CLASSES : NO_COMPLETION_CLASSES,
		[completionFilterEnabled],
	);
	const { visibleTypes, toggleTypeVisibility } =
		useTypesVisibility(completionClasses);

	if (data.states.length === 0) {
		return (
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent>
					<Typography
						variant="body2"
						color="text.secondary"
						data-testid="cumulative-state-time-empty"
					>
						No states to display. Adjust the date range or filter to bring data
						into scope.
					</Typography>
				</CardContent>
			</Card>
		);
	}

	const orderedStates = orderByWorkflow(data.states);
	const maxTotalDays = Math.max(...orderedStates.map((row) => row.totalDays));

	if (maxTotalDays <= 0) {
		return (
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent>
					<Typography
						variant="body2"
						color="text.secondary"
						data-testid="cumulative-state-time-zero"
					>
						No time has been recorded across these states yet.
					</Typography>
				</CardContent>
			</Card>
		);
	}

	const unit = chooseDurationUnit(maxTotalDays);
	const unitLabel = formatDuration(maxTotalDays, unit).split(" ")[1];

	const stateLabels = orderedStates.map((row) => row.state);
	const completedData = orderedStates.map(
		(row) => row.completedContributionDays,
	);
	const ongoingData = orderedStates.map((row) => row.ongoingContributionDays);

	const waitRawStates = new Set(
		resolveWaitRawStates(waitStates, stateMappings).map((state) =>
			state.toLowerCase(),
		),
	);
	const isWaitState = (state: string): boolean =>
		waitRawStates.has(state.toLowerCase());
	const hasWaitHighlight = orderedStates.some((row) => isWaitState(row.state));
	const waitColorMap = hasWaitHighlight
		? {
				type: "ordinal" as const,
				values: stateLabels,
				colors: orderedStates.map((row) =>
					isWaitState(row.state) ? `url(#${WAIT_PATTERN_ID})` : "",
				),
			}
		: undefined;

	const handleBarClick = (dataIndex: number) => {
		const row = orderedStates[dataIndex];
		if (row) {
			onBarClick?.(row.state);
		}
	};

	const valueFormatter = (value: number | null): string =>
		value === null ? "" : formatDuration(value, unit);

	const allSeries = [
		{
			completionClass: COMPLETED_CLASS,
			data: completedData,
			label: `Completed (${unitLabel})`,
			stack: "stateTime",
			color: theme.palette.primary.main,
			valueFormatter,
		},
		{
			completionClass: ONGOING_CLASS,
			data: ongoingData,
			label: `Ongoing (${unitLabel})`,
			stack: "stateTime",
			color: `url(#${HATCH_PATTERN_ID})`,
			valueFormatter,
		},
	];

	const series = allSeries
		.filter(
			(entry) =>
				!completionFilterEnabled ||
				visibleTypes[entry.completionClass] !== false,
		)
		.map(({ completionClass: _completionClass, ...entry }) => entry);

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ display: "flex", flexDirection: "column", height: "100%" }}
			>
				<Stack
					direction="row"
					sx={{
						justifyContent: "space-between",
						alignItems: "center",
						minHeight: 56,
						mb: 1,
					}}
				>
					<Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
						<Typography variant="h6">Cumulative Time per State</Typography>
						<FlowEfficiencyFigure
							rows={orderedStates}
							waitStates={waitStates}
							stateMappings={stateMappings}
						/>
						{hasWaitHighlight && (
							<Chip
								size="small"
								variant="outlined"
								icon={<HourglassEmptyIcon fontSize="small" />}
								label="Wait"
								data-testid="cumulative-state-time-wait-legend"
								sx={{ borderColor: theme.palette.secondary.main }}
							/>
						)}
						{completionFilterEnabled && (
							<>
								<LegendChip
									label={COMPLETED_CLASS}
									color={theme.palette.primary.main}
									visible={visibleTypes[COMPLETED_CLASS] !== false}
									onToggle={() => toggleTypeVisibility(COMPLETED_CLASS)}
								/>
								<LegendChip
									label={ONGOING_CLASS}
									color={theme.palette.primary.light}
									visible={visibleTypes[ONGOING_CLASS] !== false}
									onToggle={() => toggleTypeVisibility(ONGOING_CLASS)}
								/>
							</>
						)}
					</Stack>
					{pickerSlot}
				</Stack>

				<Box sx={{ flex: 1, minHeight: 0 }}>
					<BarChart
						style={{ height: "100%", width: "100%" }}
						onItemClick={(_event, params) =>
							handleBarClick(params?.dataIndex ?? -1)
						}
						xAxis={[
							{
								scaleType: "band",
								data: stateLabels,
								label: `Time per state (${unitLabel})`,
								height: 64,
								tickLabelInterval: () => true,
								tickLabelStyle: {
									angle: -25,
									textAnchor: "end",
								},
								colorMap: waitColorMap,
							},
						]}
						yAxis={[{ valueFormatter, label: unitLabel }]}
						series={series}
					>
						<defs>
							<pattern
								id={HATCH_PATTERN_ID}
								patternUnits="userSpaceOnUse"
								width={6}
								height={6}
								patternTransform="rotate(45)"
							>
								<rect width={6} height={6} fill={theme.palette.primary.light} />
								<line
									x1={0}
									y1={0}
									x2={0}
									y2={6}
									stroke={theme.palette.primary.dark}
									strokeWidth={2}
								/>
							</pattern>
							<pattern
								id={WAIT_PATTERN_ID}
								patternUnits="userSpaceOnUse"
								width={8}
								height={8}
							>
								<rect
									width={8}
									height={8}
									fill={theme.palette.secondary.main}
								/>
								<circle
									cx={4}
									cy={4}
									r={1.5}
									fill={theme.palette.secondary.dark}
								/>
							</pattern>
						</defs>
					</BarChart>
				</Box>

				{orderedStates.map((row) => (
					<StateTooltipContent
						key={`tooltip-${row.state}`}
						row={row}
						unit={unit}
					/>
				))}
			</CardContent>
		</Card>
	);
};

export default CumulativeStateTimeChart;
