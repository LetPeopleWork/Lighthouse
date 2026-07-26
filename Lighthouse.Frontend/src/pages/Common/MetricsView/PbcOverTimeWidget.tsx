import type { Theme } from "@mui/material";
import {
	Box,
	Card,
	CardContent,
	Stack,
	ToggleButton,
	ToggleButtonGroup,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import { LineChart } from "@mui/x-charts";
import type React from "react";
import type { IFeature } from "../../../models/Feature";
import type {
	ProcessBehaviorMetricType,
	ProcessBehaviorSnapshot,
} from "../../../models/Metrics/ProcessBehaviorSnapshot";
import { PROCESS_BEHAVIOR_METRIC_TYPES } from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { resolveOverTimeEmptyCopy } from "./overTimeEmptyState";
import { usePbcOverTime } from "./usePbcOverTime";

interface PbcOverTimeWidgetProps {
	ownerId: number;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	startDate: Date;
	endDate: Date;
	title?: string;
}

/**
 * Re-exported so the E2E asserts the shipped string rather than a duplicated copy
 * of the prose. Which of the two sentences an empty chart shows is decided by
 * resolveOverTimeEmptyCopy (D10 / DDD-13).
 */
export {
	OVER_TIME_FORWARD_ONLY_EMPTY_COPY as PBC_OVER_TIME_EMPTY_COPY,
	OVER_TIME_RANGE_EMPTY_COPY as PBC_OVER_TIME_RANGE_EMPTY_COPY,
} from "./overTimeEmptyState";

/**
 * The three limit lines, in the point-in-time chart's vocabulary
 * (average / upperNaturalProcessLimit / lowerNaturalProcessLimit) rather than
 * new names for the same concepts (D7).
 *
 * Deliberate, user-approved deviation from D7 on the *styling*: the
 * point-in-time chart draws its limits neutral-and-dashed because there they
 * are reference lines laid over a measured series. Over time there is no
 * measured series — the three limits ARE the series — so dashes would leave
 * three near-identical greys, unreadable in dark mode. Colour is the
 * differentiating channel instead, and every line renders solid.
 */
const LIMIT_LINES: readonly {
	id: string;
	label: string;
	color: (theme: Theme) => string;
	accessor: (snapshot: ProcessBehaviorSnapshot) => number;
}[] = [
	{
		id: "unpl",
		label: "UNPL",
		color: (theme) => theme.palette.error.main,
		accessor: (s) => s.unpl,
	},
	{
		id: "average",
		label: "Average",
		color: (theme) => theme.palette.info.main,
		accessor: (s) => s.average,
	},
	{
		id: "lnpl",
		label: "LNPL",
		color: (theme) => theme.palette.warning.main,
		accessor: (s) => s.lnpl,
	},
];

/**
 * Stable per-metric test locator, exported so the E2E POM targets the shipped
 * id rather than re-deriving the convention.
 */
export function processBehaviorMetricTestId(
	metricType: ProcessBehaviorMetricType,
): string {
	return `pbc-metric-${metricType.toLowerCase()}`;
}

/** Toggle-row copy for one metric family. Later slices append to the list. */
function describeMetricType(metricType: ProcessBehaviorMetricType): {
	metricType: ProcessBehaviorMetricType;
	label: string;
	tooltip: string;
	testId: string;
} {
	return {
		metricType,
		label: metricType,
		tooltip: `${metricType} natural process limits per recorded day`,
		testId: processBehaviorMetricTestId(metricType),
	};
}

/**
 * PBC Over Time widget (Predictability category, team + portfolio). Plots the
 * dated UNPL / Average / LNPL triple the recorder persisted, one point per
 * recorded day, in the point-in-time process-behaviour chart's vocabulary and
 * each limit in its own theme colour (see LIMIT_LINES for the D7 deviation).
 * A fresh owner legitimately has no history — it gets the honest forward-only
 * copy, never a fabricated or broken axis (D6).
 */
const PbcOverTimeWidget: React.FC<PbcOverTimeWidgetProps> = ({
	ownerId,
	metricsService,
	startDate,
	endDate,
	title = "PBC Over Time",
}) => {
	const theme = useTheme();
	const { metricType, setMetricType, series } = usePbcOverTime(
		ownerId,
		metricsService,
		startDate,
		endDate,
	);

	const tabs = PROCESS_BEHAVIOR_METRIC_TYPES.map(describeMetricType);

	// Colour is the only channel separating the three limits here — they render
	// solid, each in its own theme colour, so the band stays readable in dark
	// mode where three dashed greys collapse into one.
	const lineSeries = LIMIT_LINES.map((line) => ({
		id: line.id,
		label: line.label,
		color: line.color(theme),
		// Limits are a band, not a measured series — no per-day markers.
		showMark: false,
		data: (series ?? []).map(line.accessor),
	}));

	const dates = (series ?? []).map((snapshot) => snapshot.recordedAt);
	const hasData = series !== null && series.length > 0;

	return (
		<Card
			data-testid="pbc-over-time-widget"
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
						value={metricType}
						aria-label="Process behaviour metric type"
					>
						{tabs.map((tab) => (
							// Tooltip wraps the button, so it — not the ToggleButton — is the
							// group's direct child and the group's selected/onChange injection
							// no longer reaches the button. We set selected + onClick
							// explicitly per button so pressed styling and aria-pressed hold.
							<Tooltip key={tab.testId} title={tab.tooltip} arrow>
								<ToggleButton
									size="small"
									value={tab.metricType}
									selected={metricType === tab.metricType}
									onClick={() => setMetricType(tab.metricType)}
									data-testid={tab.testId}
								>
									{tab.label}
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
							data-testid="pbc-over-time-legend"
						>
							{LIMIT_LINES.map((line) => (
								<Box
									key={line.id}
									data-testid={`pbc-line-${line.id}`}
									sx={{ display: "flex", alignItems: "center", gap: 0.5 }}
								>
									<Box
										data-testid={`pbc-swatch-${line.id}`}
										sx={{
											width: 12,
											borderTop: `2px solid ${line.color(theme)}`,
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
							data-testid="pbc-over-time-empty"
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

export default PbcOverTimeWidget;
