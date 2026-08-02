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
import ChartLegend, { type ChartLegendItem } from "./ChartLegend";
import { deliveryEpicColors } from "./deliveryEpicColors";
import {
	likelihoodTooltip,
	runButtonLabel,
	visiblePoints,
	zoneBandPath,
	zoneColors,
} from "./feverChartView";
import { useFeatureFeverReveal } from "./useFeatureFeverReveal";
import { useLegendFilter } from "./useLegendFilter";

interface DeliveryFeverChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
	height?: number;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no feature snapshots recorded yet.";

const ZONE_FILL_OPACITY = 0.25;

interface ColouredFeature extends FeatureFeverSeries {
	color: string;
}

const FeverZoneBands: React.FC<{ colors: Record<FeverZone, string> }> = ({
	colors,
}) => {
	const xScale = useXScale();
	const yScale = useYScale();
	return (
		<g>
			{feverZonePolygons().map((polygon) => (
				<path
					key={polygon.zone}
					d={zoneBandPath(polygon.points, xScale, yScale)}
					fill={colors[polygon.zone]}
					fillOpacity={ZONE_FILL_OPACITY}
				/>
			))}
		</g>
	);
};

const DeliveryFeverChart: React.FC<DeliveryFeverChartProps> = ({
	history,
	title = "Delivery Progress",
	height = 320,
}) => {
	const theme = useTheme();
	const { selected, isVisible, toggle, showAll } = useLegendFilter();
	const chart = deriveFeatureFeverChart(history);
	const maxLength = chart.features.reduce(
		(longest, feature) => Math.max(longest, feature.points.length),
		0,
	);
	const { frame, isRunning, run } = useFeatureFeverReveal(maxLength);

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

	// Shared with the size chart on the same tab, so an epic is one colour across the tab.
	const colorByReferenceId = deliveryEpicColors(history);
	const colouredFeatures: ColouredFeature[] = chart.features.map((feature) => ({
		...feature,
		color: colorByReferenceId[feature.referenceId],
	}));

	const legendItems: ChartLegendItem[] = colouredFeatures.map((feature) => ({
		id: feature.referenceId,
		label: feature.name,
		color: feature.color,
	}));

	const series = colouredFeatures
		.filter((feature) => isVisible(feature.referenceId))
		.map((feature) => ({
			id: feature.referenceId,
			label: feature.name,
			color: feature.color,
			markerSize: 7,
			valueFormatter: likelihoodTooltip,
			data: visiblePoints(feature, frame),
		}));

	const canAnimate = maxLength > 1;

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
							onClick={run}
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
					height={height}
					hideLegend
				>
					<FeverZoneBands colors={zoneColors(theme)} />
				</ScatterChart>
				<ChartLegend
					items={legendItems}
					selected={selected}
					onToggle={toggle}
					onShowAll={showAll}
				/>
			</CardContent>
		</Card>
	);
};

export default DeliveryFeverChart;
