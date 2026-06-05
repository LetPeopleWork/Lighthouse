import {
	Box,
	Card,
	CardContent,
	Stack,
	Typography,
	useTheme,
} from "@mui/material";
import { BarChart } from "@mui/x-charts";
import {
	ChartsTooltipContainer,
	useItemTooltip,
} from "@mui/x-charts/ChartsTooltip";
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

const HATCH_PATTERN_ID = "cumulative-state-time-ongoing-hatch";
const WAIT_HATCH_PATTERN_ID = "cumulative-state-time-wait-ongoing-hatch";

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

export const CumulativeStateBarTooltip: React.FC<{
	row: ICumulativeStateTimeStateRow;
	color: string;
	unit: DurationUnit;
}> = ({ row, color, unit }) => (
	<Box data-testid="cumulative-state-bar-tooltip" sx={{ p: 1, minWidth: 160 }}>
		<Typography variant="subtitle2" sx={{ mb: 0.5 }}>
			{row.state}
		</Typography>
		<Box
			data-testid="cumulative-state-bar-tooltip-row-completed"
			sx={{ display: "flex", alignItems: "center", gap: 1 }}
		>
			<TooltipMark color={color} />
			<Typography variant="caption">
				Completed: {formatDuration(row.completedContributionDays, unit)}
			</Typography>
		</Box>
		<Box
			data-testid="cumulative-state-bar-tooltip-row-ongoing"
			sx={{ display: "flex", alignItems: "center", gap: 1 }}
		>
			<TooltipMark color={color} />
			<Typography variant="caption">
				Ongoing: {formatDuration(row.ongoingContributionDays, unit)}
			</Typography>
		</Box>
	</Box>
);

const createBarTooltipSlot = (
	states: ICumulativeStateTimeStateRow[],
	unit: DurationUnit,
	solidColorFor: (state: string) => string,
): React.FC => {
	const BarTooltipSlot: React.FC = () => {
		const tooltipData = useItemTooltip();
		const dataIndex = tooltipData?.identifier?.dataIndex;
		if (dataIndex === undefined) {
			return null;
		}
		const row = states[dataIndex];
		if (!row) {
			return null;
		}
		return (
			<ChartsTooltipContainer trigger="item">
				<CumulativeStateBarTooltip
					row={row}
					color={solidColorFor(row.state)}
					unit={unit}
				/>
			</ChartsTooltipContainer>
		);
	};
	return BarTooltipSlot;
};

const TooltipMark: React.FC<{ color: string }> = ({ color }) => (
	<Box
		data-testid="cumulative-state-bar-tooltip-mark"
		sx={{
			width: 12,
			height: 12,
			borderRadius: 0.5,
			backgroundColor: color,
			flexShrink: 0,
		}}
	/>
);

const LegendSwatch: React.FC<{ color: string; testId?: string }> = ({
	color,
	testId,
}) => (
	<Box
		data-testid={testId}
		sx={{
			width: 14,
			height: 14,
			borderRadius: 0.5,
			backgroundColor: color,
			flexShrink: 0,
		}}
	/>
);

interface CompletionLegendButtonProps {
	label: string;
	color: string;
	visible: boolean;
	onToggle: () => void;
}

const CompletionLegendButton: React.FC<CompletionLegendButtonProps> = ({
	label,
	color,
	visible,
	onToggle,
}) => (
	<Box
		component="button"
		type="button"
		onClick={onToggle}
		aria-pressed={visible}
		aria-label={`${label} visibility toggle`}
		sx={{
			display: "flex",
			alignItems: "center",
			gap: 0.75,
			border: "none",
			background: "none",
			cursor: "pointer",
			p: 0,
			color: "text.primary",
			opacity: visible ? 1 : 0.4,
		}}
	>
		<LegendSwatch color={color} />
		<Typography variant="body2">{label}</Typography>
	</Box>
);

const WaitColourKey: React.FC<{ color: string }> = ({ color }) => (
	<Box
		data-testid="cumulative-state-time-wait-legend"
		sx={{ display: "flex", alignItems: "center", gap: 0.75 }}
	>
		<LegendSwatch
			color={color}
			testId="cumulative-state-time-wait-legend-swatch"
		/>
		<Typography variant="body2" color="text.secondary">
			Wait
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

	const waitRawStates = new Set(
		resolveWaitRawStates(waitStates, stateMappings).map((state) =>
			state.toLowerCase(),
		),
	);
	const isWaitState = (state: string): boolean =>
		waitRawStates.has(state.toLowerCase());
	const hasWaitBars = orderedStates.some((row) => isWaitState(row.state));

	const completedFor = (wait: boolean): (number | null)[] =>
		orderedStates.map((row) =>
			isWaitState(row.state) === wait ? row.completedContributionDays : null,
		);
	const ongoingFor = (wait: boolean): (number | null)[] =>
		orderedStates.map((row) =>
			isWaitState(row.state) === wait ? row.ongoingContributionDays : null,
		);

	const handleBarClick = (dataIndex: number) => {
		const row = orderedStates[dataIndex];
		if (row) {
			onBarClick?.(row.state);
		}
	};

	const valueFormatter = (value: number | null): string =>
		value === null ? "" : formatDuration(value, unit);

	const solidColorFor = (state: string): string =>
		isWaitState(state) ? theme.palette.error.main : theme.palette.primary.main;

	const BarTooltipSlot = createBarTooltipSlot(
		orderedStates,
		unit,
		solidColorFor,
	);

	const allSeries = [
		{
			id: "completedNonWait",
			completionClass: COMPLETED_CLASS,
			wait: false,
			data: completedFor(false),
			label: `Completed (${unitLabel})`,
			stack: "stateTime",
			color: theme.palette.primary.main,
			valueFormatter,
		},
		{
			id: "completedWait",
			completionClass: COMPLETED_CLASS,
			wait: true,
			data: completedFor(true),
			label: `Completed (${unitLabel})`,
			stack: "stateTime",
			color: theme.palette.error.main,
			valueFormatter,
		},
		{
			id: "ongoingNonWait",
			completionClass: ONGOING_CLASS,
			wait: false,
			data: ongoingFor(false),
			label: `Ongoing (${unitLabel})`,
			stack: "stateTime",
			color: `url(#${HATCH_PATTERN_ID})`,
			valueFormatter,
		},
		{
			id: "ongoingWait",
			completionClass: ONGOING_CLASS,
			wait: true,
			data: ongoingFor(true),
			label: `Ongoing (${unitLabel})`,
			stack: "stateTime",
			color: `url(#${WAIT_HATCH_PATTERN_ID})`,
			valueFormatter,
		},
	];

	const series = allSeries
		.filter((entry) => hasWaitBars || !entry.wait)
		.filter(
			(entry) =>
				!completionFilterEnabled ||
				visibleTypes[entry.completionClass] !== false,
		)
		.map(
			({ completionClass: _completionClass, wait: _wait, ...entry }) => entry,
		);

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ display: "flex", flexDirection: "column", height: "100%" }}
			>
				<Stack
					direction="row"
					sx={{
						justifyContent: "space-between",
						alignItems: "flex-start",
						mb: 1,
					}}
				>
					<Stack
						direction="column"
						spacing={0.25}
						data-testid="cumulative-state-time-title-block"
					>
						<Typography variant="h6">Cumulative Time per State</Typography>
						<FlowEfficiencyFigure
							rows={orderedStates}
							waitStates={waitStates}
							stateMappings={stateMappings}
						/>
					</Stack>
					{pickerSlot}
				</Stack>

				<Stack
					direction="row"
					spacing={2}
					sx={{ alignItems: "center", flexWrap: "wrap", mb: 1, rowGap: 1 }}
				>
					{completionFilterEnabled && (
						<>
							<CompletionLegendButton
								label={COMPLETED_CLASS}
								color={theme.palette.primary.main}
								visible={visibleTypes[COMPLETED_CLASS] !== false}
								onToggle={() => toggleTypeVisibility(COMPLETED_CLASS)}
							/>
							<CompletionLegendButton
								label={ONGOING_CLASS}
								color={theme.palette.primary.light}
								visible={visibleTypes[ONGOING_CLASS] !== false}
								onToggle={() => toggleTypeVisibility(ONGOING_CLASS)}
							/>
						</>
					)}
					{hasWaitBars && <WaitColourKey color={theme.palette.error.main} />}
				</Stack>

				<Box sx={{ flex: 1, minHeight: 0 }}>
					<BarChart
						style={{ height: "100%", width: "100%" }}
						hideLegend
						slots={{ tooltip: BarTooltipSlot }}
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
								id={WAIT_HATCH_PATTERN_ID}
								patternUnits="userSpaceOnUse"
								width={6}
								height={6}
								patternTransform="rotate(45)"
							>
								<rect width={6} height={6} fill={theme.palette.error.light} />
								<line
									x1={0}
									y1={0}
									x2={0}
									y2={6}
									stroke={theme.palette.error.dark}
									strokeWidth={2}
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
