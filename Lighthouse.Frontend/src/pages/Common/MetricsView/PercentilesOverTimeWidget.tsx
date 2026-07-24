import {
	Box,
	Card,
	CardContent,
	Stack,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
} from "@mui/material";
import { LineChart } from "@mui/x-charts";
import type React from "react";
import { ForecastLevel } from "../../../components/Common/Forecasts/ForecastLevel";
import type { IFeature } from "../../../models/Feature";
import type {
	PercentilesHorizon,
	PercentilesOverTimeSnapshot,
} from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import { PERCENTILES_HORIZONS } from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { usePercentilesOverTime } from "./usePercentilesOverTime";

interface PercentilesOverTimeWidgetProps {
	ownerId: number;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	title?: string;
}

// Forward-only over-time charts read this on a fresh owner instead of a broken
// axis (D6 — verbatim copy, shared with the delivery-metrics forecast trend).
const EMPTY_MESSAGE = "builds forward from today — no snapshots recorded yet";

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

/**
 * Percentiles Over Time widget (Predictability category, team + portfolio).
 * Wraps the red→green percentile line ramp with a CT-30/60/90 horizon toggle
 * (CT-30 default). The persisted daily series is fetched per horizon through
 * the existing metrics-service abstraction via usePercentilesOverTime; toggling
 * re-plots already-fetched horizons without a backend recompute (US-01).
 */
const PercentilesOverTimeWidget: React.FC<PercentilesOverTimeWidgetProps> = ({
	ownerId,
	metricsService,
	title = "Percentiles Over Time",
}) => {
	const { horizon, setHorizon, series } = usePercentilesOverTime(
		ownerId,
		metricsService,
	);

	const handleHorizonChange = (
		_event: React.MouseEvent<HTMLElement>,
		next: PercentilesHorizon | null,
	): void => {
		if (next !== null) {
			setHorizon(next);
		}
	};

	const lineSeries = PERCENTILE_LINES.map((line) => ({
		label: line.label,
		color: new ForecastLevel(line.percentile).color,
		showMark: true,
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
						value={horizon}
						onChange={handleHorizonChange}
						aria-label="Percentiles horizon"
					>
						{PERCENTILES_HORIZONS.map((option) => (
							<ToggleButton
								key={option}
								value={option}
								data-testid={`percentiles-horizon-${option}`}
							>
								{`CT-${option}`}
							</ToggleButton>
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
							{EMPTY_MESSAGE}
						</Typography>
					)
				)}
			</CardContent>
		</Card>
	);
};

export default PercentilesOverTimeWidget;
