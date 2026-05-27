import {
	Box,
	Card,
	CardContent,
	Stack,
	Typography,
	useTheme,
} from "@mui/material";
import { BarChart } from "@mui/x-charts";
import type React from "react";
import type { ReactNode } from "react";
import type {
	ICumulativeStateTimeResponse,
	ICumulativeStateTimeStateRow,
} from "../../../models/Metrics/CumulativeStateTime";
import {
	chooseDurationUnit,
	type DurationUnit,
	formatDuration,
} from "../../../utils/date/formatDuration";

const HATCH_PATTERN_ID = "cumulative-state-time-ongoing-hatch";

interface CumulativeStateTimeChartProps {
	data: ICumulativeStateTimeResponse;
	onBarClick?: (stateName: string) => void;
	pickerSlot?: ReactNode;
}

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
}) => {
	const theme = useTheme();

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

	const handleBarClick = (dataIndex: number) => {
		const row = orderedStates[dataIndex];
		if (row) {
			onBarClick?.(row.state);
		}
	};

	const valueFormatter = (value: number | null): string =>
		value === null ? "" : formatDuration(value, unit);

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
						minHeight: 56,
						mb: 1,
					}}
				>
					<Typography variant="h6">Cumulative Time per State</Typography>
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
							},
						]}
						yAxis={[{ valueFormatter, label: unitLabel }]}
						series={[
							{
								data: completedData,
								label: `Completed (${unitLabel})`,
								stack: "stateTime",
								color: theme.palette.primary.main,
								valueFormatter,
							},
							{
								data: ongoingData,
								label: `Ongoing (${unitLabel})`,
								stack: "stateTime",
								color: `url(#${HATCH_PATTERN_ID})`,
								valueFormatter,
							},
						]}
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
