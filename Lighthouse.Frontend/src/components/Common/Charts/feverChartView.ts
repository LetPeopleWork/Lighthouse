import type { ScatterValueType } from "@mui/x-charts";
import type {
	FeatureFeverSeries,
	FeverZone,
} from "../../../models/Delivery/FeverTrail";

interface ZonePalette {
	palette: {
		success: { main: string };
		warning: { main: string };
		error: { main: string };
	};
}

export const FEATURE_COLORS = [
	"#1f77b4",
	"#9467bd",
	"#17becf",
	"#8c564b",
	"#e377c2",
	"#2c3e50",
	"#393b79",
	"#637939",
];

export interface ScatterDatum {
	x: number;
	y: number;
	id: number;
}

export type AxisScale = (value: number) => number | undefined;

export const featureColor = (index: number): string =>
	FEATURE_COLORS[index % FEATURE_COLORS.length];

export const zoneColors = (theme: ZonePalette): Record<FeverZone, string> => ({
	green: theme.palette.success.main,
	amber: theme.palette.warning.main,
	red: theme.palette.error.main,
});

export const currentPoint = (
	feature: FeatureFeverSeries,
	frame: number | null,
) =>
	frame === null
		? feature.latest
		: feature.points[Math.min(frame, feature.points.length - 1)];

export const visiblePoints = (
	feature: FeatureFeverSeries,
	frame: number | null,
): ScatterDatum[] => {
	const point = currentPoint(feature, frame);
	return [{ x: point.completion, y: point.chanceOfLate, id: 0 }];
};

export const likelihoodTooltip = (value: ScatterValueType | null): string =>
	value === null ? "" : `${Math.round(100 - value.y)}% Likelihood`;

export const runButtonLabel = (
	isRunning: boolean,
	frame: number | null,
): string => {
	if (isRunning) {
		return "Running…";
	}
	return frame === null ? "Run" : "Rerun";
};

export const zoneBandPath = (
	points: Array<[number, number]>,
	xScale: AxisScale,
	yScale: AxisScale,
): string => {
	const path = points.map(([x, y]) => `${xScale(x)} ${yScale(y)}`).join(" L ");
	return `M ${path} Z`;
};
