import type { Theme } from "@mui/material";
import {
	Box,
	Button,
	Card,
	CardContent,
	Typography,
	useTheme,
} from "@mui/material";
import type { ScatterValueType } from "@mui/x-charts";
import { useXScale, useYScale } from "@mui/x-charts/hooks";
import { ScatterChart } from "@mui/x-charts/ScatterChart";
import type React from "react";
import { useCallback, useState } from "react";
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
	"One bubble per feature at its latest snapshot. Red (top-left) is off track, green (bottom-right) is on track. Run the animation to watch each feature move over time, or click a feature to show or hide it.";

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

interface ColouredFeature extends FeatureFeverSeries {
	color: string;
}

const zoneColors = (theme: Theme): Record<FeverZone, string> => ({
	green: theme.palette.success.main,
	amber: theme.palette.warning.main,
	red: theme.palette.error.main,
});

const currentPoint = (feature: FeatureFeverSeries, frame: number | null) =>
	frame === null
		? feature.latest
		: feature.points[Math.min(frame, feature.points.length - 1)];

const visiblePoints = (
	feature: FeatureFeverSeries,
	frame: number | null,
): ScatterDatum[] => {
	const point = currentPoint(feature, frame);
	return [{ x: point.completion, y: point.chanceOfLate, id: 0 }];
};

const likelihoodTooltip = (value: ScatterValueType | null): string =>
	value === null ? "" : `${Math.round(100 - value.y)}% Likelihood`;

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

interface FeatureLegendProps {
	features: ColouredFeature[];
	hidden: ReadonlySet<string>;
	onToggle: (referenceId: string) => void;
}

const FeatureLegend: React.FC<FeatureLegendProps> = ({
	features,
	hidden,
	onToggle,
}) => (
	<Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5, mt: 1 }}>
		{features.map((feature) => {
			const isHidden = hidden.has(feature.referenceId);
			return (
				<Box
					key={feature.referenceId}
					component="button"
					type="button"
					onClick={() => onToggle(feature.referenceId)}
					aria-pressed={!isHidden}
					sx={{
						display: "flex",
						alignItems: "center",
						gap: 0.75,
						border: "none",
						background: "none",
						cursor: "pointer",
						p: 0,
						opacity: isHidden ? 0.4 : 1,
					}}
				>
					<Box
						sx={{
							width: 12,
							height: 12,
							borderRadius: "50%",
							backgroundColor: feature.color,
						}}
					/>
					<Typography variant="body2">{feature.name}</Typography>
				</Box>
			);
		})}
	</Box>
);

const runButtonLabel = (isRunning: boolean, frame: number | null): string => {
	if (isRunning) {
		return "Running…";
	}
	return frame === null ? "Run" : "Rerun";
};

const DeliveryFeverChart: React.FC<DeliveryFeverChartProps> = ({
	history,
	title = "Delivery Progress",
}) => {
	const theme = useTheme();
	const [hidden, setHidden] = useState<ReadonlySet<string>>(new Set());
	const chart = deriveFeatureFeverChart(history);
	const maxLength = chart.features.reduce(
		(longest, feature) => Math.max(longest, feature.points.length),
		0,
	);
	const { frame, isRunning, run } = useFeatureFeverReveal(maxLength);

	const toggle = useCallback((referenceId: string) => {
		setHidden((previous) => {
			const next = new Set(previous);
			if (next.has(referenceId)) {
				next.delete(referenceId);
			} else {
				next.add(referenceId);
			}
			return next;
		});
	}, []);

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

	const colouredFeatures: ColouredFeature[] = chart.features.map(
		(feature, index) => ({
			...feature,
			color: FEATURE_COLORS[index % FEATURE_COLORS.length],
		}),
	);

	const series = colouredFeatures
		.filter((feature) => !hidden.has(feature.referenceId))
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
					height={320}
					hideLegend
				>
					<FeverZoneBands colors={zoneColors(theme)} />
				</ScatterChart>
				<FeatureLegend
					features={colouredFeatures}
					hidden={hidden}
					onToggle={toggle}
				/>
				<Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
					{ZONE_CAPTION}
				</Typography>
			</CardContent>
		</Card>
	);
};

export default DeliveryFeverChart;
