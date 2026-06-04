import type { Theme } from "@mui/material";
import { Card, CardContent, Typography, useTheme } from "@mui/material";
import { useXScale, useYScale } from "@mui/x-charts/hooks";
import { ScatterChart } from "@mui/x-charts/ScatterChart";
import type React from "react";
import type { DeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import {
	deriveFeverTrail,
	type FeverPoint,
	type FeverZone,
	feverZonePolygons,
} from "../../../models/Delivery/FeverTrail";
import { useFeverTrailAnimation } from "./useFeverTrailAnimation";

interface DeliveryFeverChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no likelihood snapshots recorded yet.";

const ZONE_CAPTION =
	"Each bubble is a snapshot; the trail moves left-to-right as work completes. Red (top-left) is off track, green (bottom-right) is on track.";

const TRAIL_SERIES_ID = "trail";
const LATEST_SERIES_ID = "latest";
const ZONE_FILL_OPACITY = 0.25;

interface ScatterDatum {
	x: number;
	y: number;
	id: number;
}

const toDatum = (point: FeverPoint, index: number): ScatterDatum => ({
	x: point.completion,
	y: point.chanceOfLate,
	id: index,
});

const zoneColors = (theme: Theme): Record<FeverZone, string> => ({
	green: theme.palette.success.main,
	amber: theme.palette.warning.main,
	red: theme.palette.error.main,
});

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

const DeliveryFeverChart: React.FC<DeliveryFeverChartProps> = ({
	history,
	title = "Delivery Progress",
}) => {
	const theme = useTheme();
	const trail = deriveFeverTrail(history);
	const visibleCount = useFeverTrailAnimation(trail.points.length);

	if (trail.empty) {
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

	const visible = trail.points.slice(0, visibleCount);
	const latest = visible[visible.length - 1];
	const series = [
		{
			id: TRAIL_SERIES_ID,
			label: "Snapshots",
			color: theme.palette.primary.main,
			markerSize: 5,
			data: visible.map(toDatum),
		},
		{
			id: LATEST_SERIES_ID,
			label: "Latest",
			color: theme.palette.primary.dark,
			markerSize: 11,
			data: [toDatum(latest, visible.length - 1)],
		},
	];

	return (
		<Card
			data-testid="delivery-fever-chart"
			sx={{ p: 2, borderRadius: 2, height: "100%" }}
		>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Typography variant="h6">{title}</Typography>
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
