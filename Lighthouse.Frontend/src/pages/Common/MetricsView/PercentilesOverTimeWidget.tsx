import {
	Box,
	Card,
	CardContent,
	Stack,
	ToggleButton,
	ToggleButtonGroup,
	Tooltip,
	Typography,
} from "@mui/material";
import { LineChart } from "@mui/x-charts";
import type React from "react";
import { ForecastLevel } from "../../../components/Common/Forecasts/ForecastLevel";
import type { IFeature } from "../../../models/Feature";
import type {
	PercentilesOverTimeSnapshot,
	PercentilesSelection,
} from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import {
	PERCENTILES_AGE_SELECTION,
	PERCENTILES_SELECTIONS,
} from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { resolveOverTimeEmptyCopy } from "./overTimeEmptyState";
import { usePercentilesOverTime } from "./usePercentilesOverTime";

interface PercentilesOverTimeWidgetProps {
	ownerId: number;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	startDate: Date;
	endDate: Date;
	title?: string;
}

/**
 * Re-exported so tests and POMs assert the shipped string rather than a duplicated
 * copy of the prose. Which of the two sentences an empty chart shows is decided by
 * resolveOverTimeEmptyCopy (D10 / DDD-13).
 */
export {
	OVER_TIME_FORWARD_ONLY_EMPTY_COPY as PERCENTILES_OVER_TIME_EMPTY_COPY,
	OVER_TIME_RANGE_EMPTY_COPY as PERCENTILES_OVER_TIME_RANGE_EMPTY_COPY,
} from "./overTimeEmptyState";

// The 50/70/85/95 lines keep the point-in-time percentile red→green ramp (D7):
// ForecastLevel maps 50→red (risky) … 95→green (certain).
const PERCENTILE_LINES: readonly {
	percentile: number;
	label: string;
	accessor: (snapshot: PercentilesOverTimeSnapshot) => number;
}[] = [
	{ percentile: 50, label: "50th", accessor: (s) => s.p50 },
	{ percentile: 70, label: "70th", accessor: (s) => s.p70 },
	{ percentile: 85, label: "85th", accessor: (s) => s.p85 },
	{ percentile: 95, label: "95th", accessor: (s) => s.p95 },
];

interface SelectionChip {
	selection: PercentilesSelection;
	label: string;
	tooltip: string;
	testId: string;
}

/**
 * Chip copy for one tab of the toggle row. Work Item Age is as-of-today, so it
 * reads as its own tab with no horizon in the label; the cycle-time chips keep
 * their shipped "{n} days" wording.
 */
function describeSelection(selection: PercentilesSelection): SelectionChip {
	if (selection === PERCENTILES_AGE_SELECTION) {
		return {
			selection,
			label: "Age",
			tooltip: "Work Item Age of items in progress today",
			testId: "percentiles-selection-age",
		};
	}

	return {
		selection,
		label: `${selection} days`,
		tooltip: `Cycle Time over the last ${selection} days`,
		testId: `percentiles-horizon-${selection}`,
	};
}

/**
 * Percentiles Over Time widget (Predictability category, team + portfolio).
 * Wraps the red→green percentile line ramp with an Age / 30 / 60 / 90-day
 * toggle row (30 days default). The persisted daily series is fetched per
 * selection through the existing metrics-service abstraction via
 * usePercentilesOverTime; toggling re-plots already-fetched selections without
 * a backend recompute (US-01, US-03).
 */
const PercentilesOverTimeWidget: React.FC<PercentilesOverTimeWidgetProps> = ({
	ownerId,
	metricsService,
	startDate,
	endDate,
	title = "Percentiles Over Time",
}) => {
	const { selection, setSelection, series } = usePercentilesOverTime(
		ownerId,
		metricsService,
		startDate,
		endDate,
	);

	const chips = PERCENTILES_SELECTIONS.map(describeSelection);

	// All four percentile lines share one plain-circle marker — the red→green
	// colour is the only thing that distinguishes them (uniform marks, not the
	// per-series shape cycle MUI-X applies by default).
	const lineSeries = PERCENTILE_LINES.map((line) => ({
		label: line.label,
		color: new ForecastLevel(line.percentile).color,
		showMark: true,
		shape: "circle" as const,
		data: (series ?? []).map(line.accessor),
	}));

	const dates = (series ?? []).map((snapshot) => snapshot.recordedAt);
	const hasData = series !== null && series.length > 0;

	return (
		<Card
			data-testid="percentiles-over-time-widget"
			sx={{ p: 2, borderRadius: 2, height: "100%" }}
		>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Box
					sx={{
						display: "flex",
						justifyContent: "space-between",
						alignItems: "center",
						gap: 1,
						flexWrap: "wrap",
					}}
				>
					<Typography variant="h6">{title}</Typography>
					<ToggleButtonGroup
						size="small"
						exclusive
						value={selection}
						aria-label="Percentiles selection"
					>
						{chips.map((chip) => (
							// Tooltip wraps the button, so it — not the ToggleButton — is the
							// group's direct child and the group's selected/onChange injection
							// no longer reaches the button. We set selected + onClick
							// explicitly per button so pressed styling and aria-pressed hold
							// regardless of group injection — the Age chip included.
							<Tooltip key={chip.testId} title={chip.tooltip} arrow>
								<ToggleButton
									size="small"
									value={chip.selection}
									selected={selection === chip.selection}
									onClick={() => setSelection(chip.selection)}
									data-testid={chip.testId}
								>
									{chip.label}
								</ToggleButton>
							</Tooltip>
						))}
					</ToggleButtonGroup>
				</Box>

				{hasData ? (
					<>
						<Stack
							direction="row"
							spacing={2}
							sx={{ mt: 1, flexWrap: "wrap" }}
							data-testid="percentiles-over-time-legend"
						>
							{PERCENTILE_LINES.map((line) => (
								<Box
									key={line.percentile}
									data-testid={`percentile-line-${line.percentile}`}
									sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
								>
									<Box
										sx={{
											width: 12,
											height: 12,
											borderRadius: "2px",
											backgroundColor: new ForecastLevel(line.percentile).color,
										}}
									/>
									<Typography variant="caption">{line.label}</Typography>
								</Box>
							))}
						</Stack>
						<Box sx={{ flex: 1, minHeight: 0 }}>
							<LineChart
								style={{ height: "100%", width: "100%" }}
								xAxis={[{ data: dates, scaleType: "point" }]}
								series={lineSeries}
								hideLegend
							/>
						</Box>
					</>
				) : (
					series !== null && (
						<Typography
							data-testid="percentiles-over-time-empty"
							variant="body2"
							color="text.secondary"
							sx={{ py: 4, textAlign: "center" }}
						>
							{resolveOverTimeEmptyCopy(endDate)}
						</Typography>
					)
				)}
			</CardContent>
		</Card>
	);
};

export default PercentilesOverTimeWidget;
