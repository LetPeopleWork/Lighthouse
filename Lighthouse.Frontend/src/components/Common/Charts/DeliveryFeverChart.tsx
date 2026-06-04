import type { Theme } from "@mui/material";
import {
	Box,
	Button,
	Card,
	CardContent,
	Typography,
	useTheme,
} from "@mui/material";
import { useXScale, useYScale } from "@mui/x-charts/hooks";
import { ScatterChart } from "@mui/x-charts/ScatterChart";
import type React from "react";
import type { DeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import {
	deriveFeatureFeverChart,
	type FeatureFeverSeries,
	type FeverZone,
	feverZonePolygons,
} from "../../../models/Delivery/FeverTrail";
import { useFeatureFeverReveal } from "./useFeatureFeverReveal";

interface DeliveryFeverChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no feature snapshots recorded yet.";

const ZONE_CAPTION =
	"One bubble per feature at its latest snapshot. Red (top-left) is off track, green (bottom-right) is on track. Run the animation to watch each feature move over time.";

const ZONE_FILL_OPACITY = 0.25;

const FEATURE_COLORS = [
	"#1f77b4",
	"#9467bd",
	"#17becf",
	"#8c564b",
	"#e377c2",
	"#2c3e50",
	"#393b79",
	"#637939",
];

interface ScatterDatum {
	x: number;
	y: number;
	id: number;
}

const zoneColors = (theme: Theme): Record<FeverZone, string> => ({
	green: theme.palette.success.main,
	amber: theme.palette.warning.main,
	red: theme.palette.error.main,
});

const visiblePoints = (
	feature: FeatureFeverSeries,
	frame: number | null,
): ScatterDatum[] => {
	const points =
		frame === null ? [feature.latest] : feature.points.slice(0, frame + 1);
	return points.map((point, index) => ({
		x: point.completion,
		y: point.chanceOfLate,
		id: index,
	}));
};

const FeverZoneBands: React.FC<{ colors: Record<FeverZone, string> }> = ({
	colors,
}) => {
	const xScale = useXScale();
	const yScale = useYScale();
	return (
		<g>
			{feverZonePolygons().map((polygon) => {
				const path = polygon.points
					.map(([x, y]) => `${xScale(x)} ${yScale(y)}`)
					.join(" L ");
				return (
					<path
						key={polygon.zone}
						d={`M ${path} Z`}
						fill={colors[polygon.zone]}
						fillOpacity={ZONE_FILL_OPACITY}
					/>
				);
			})}
		</g>
	);
};

const runButtonLabel = (isRunning: boolean, frame: number | null): string => {
	if (isRunning) {
		return "Running…";
	}
	if (frame === null) {
		return "Run";
	}
	return "Show latest";
};

const DeliveryFeverChart: React.FC<DeliveryFeverChartProps> = ({
	history,
	title = "Delivery Progress",
}) => {
	const theme = useTheme();
	const chart = deriveFeatureFeverChart(history);
	const maxLength = chart.features.reduce(
		(longest, feature) => Math.max(longest, feature.points.length),
		0,
	);
	const { frame, isRunning, run, showLatest } =
		useFeatureFeverReveal(maxLength);

	if (chart.empty) {
		return (
			<Card sx={{ p: 2, borderRadius: 2 }}>
				<CardContent>
					<Typography variant="h6">{title}</Typography>
					<Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
						{FORWARD_ONLY_EMPTY_STATE}
					</Typography>
				</CardContent>
			</Card>
		);
	}

	const series = chart.features.map((feature, index) => ({
		id: feature.referenceId,
		label: feature.name,
		color: FEATURE_COLORS[index % FEATURE_COLORS.length],
		markerSize: 7,
		data: visiblePoints(feature, frame),
	}));

	const canAnimate = maxLength > 1;
	const onRunClick = frame === null ? run : showLatest;

	return (
		<Card
			data-testid="delivery-fever-chart"
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
					}}
				>
					<Typography variant="h6">{title}</Typography>
					{canAnimate ? (
						<Button
							size="small"
							variant="outlined"
							onClick={onRunClick}
							disabled={isRunning}
						>
							{runButtonLabel(isRunning, frame)}
						</Button>
					) : null}
				</Box>
				<ScatterChart
					xAxis={[{ min: 0, max: 100, label: "Completion Rate (%)" }]}
					yAxis={[{ min: 0, max: 100, label: "Chance of Being Late (%)" }]}
					series={series}
					height={320}
				>
					<FeverZoneBands colors={zoneColors(theme)} />
				</ScatterChart>
				<Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
					{ZONE_CAPTION}
				</Typography>
			</CardContent>
		</Card>
	);
};

export default DeliveryFeverChart;
