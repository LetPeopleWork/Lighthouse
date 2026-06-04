import type { Theme } from "@mui/material";
import { Card, CardContent, Typography, useTheme } from "@mui/material";
import { ScatterChart } from "@mui/x-charts/ScatterChart";
import type React from "react";
import type { DeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";
import {
	deriveFeverTrail,
	type FeverPoint,
	type FeverZone,
} from "../../../models/Delivery/FeverTrail";
import { useFeverTrailAnimation } from "./useFeverTrailAnimation";

interface DeliveryFeverChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

const ZONE_CAPTION =
	"Bubbles are coloured by zone: green is on track, amber is at risk, red is off track.";

const ZONES: FeverZone[] = ["green", "amber", "red"];

const LATEST_SERIES_ID = "latest";

interface ScatterDatum {
	x: number;
	y: number;
	id: number;
}

interface ZoneSeries {
	id: FeverZone | typeof LATEST_SERIES_ID;
	label: string;
	color: string;
	markerSize: number;
	data: ScatterDatum[];
}

const colourForZone = (zone: FeverZone, theme: Theme): string => {
	if (zone === "green") {
		return theme.palette.success.main;
	}
	if (zone === "amber") {
		return theme.palette.warning.main;
	}
	return theme.palette.error.main;
};

const toDatum = (point: FeverPoint, index: number): ScatterDatum => ({
	x: point.x,
	y: point.y,
	id: index,
});

const buildZoneSeries = (points: FeverPoint[], theme: Theme): ZoneSeries[] => {
	const zoneSeries = ZONES.map((zone) => ({
		id: zone,
		label: zone,
		color: colourForZone(zone, theme),
		markerSize: 5,
		data: points.map(toDatum).filter((_, index) => points[index].zone === zone),
	})).filter((series) => series.data.length > 0);

	const latest = points[points.length - 1];
	const latestSeries: ZoneSeries = {
		id: LATEST_SERIES_ID,
		label: "Latest",
		color: colourForZone(latest.zone, theme),
		markerSize: 12,
		data: [toDatum(latest, points.length - 1)],
	};

	return [...zoneSeries, latestSeries];
};

const DeliveryFeverChart: React.FC<DeliveryFeverChartProps> = ({
	history,
	title = "Delivery Fever",
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

	const series = buildZoneSeries(trail.points.slice(0, visibleCount), theme);

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
					xAxis={[{ min: 0, max: 100, label: "% schedule consumed" }]}
					yAxis={[{ min: 0, max: 100, label: "% work remaining" }]}
					series={series}
					height={320}
				/>
				<Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
					{ZONE_CAPTION}
				</Typography>
			</CardContent>
		</Card>
	);
};

export default DeliveryFeverChart;
