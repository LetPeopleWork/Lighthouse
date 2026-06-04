import type { DeliveryMetricsHistory } from "./DeliveryMetricsHistory";

export type FeverZone = "green" | "amber" | "red";

export type FeverPoint = {
	date: Date;
	completion: number;
	chanceOfLate: number;
};

export type FeatureFeverSeries = {
	referenceId: string;
	name: string;
	points: FeverPoint[];
	latest: FeverPoint;
};

export type FeatureFeverChart = {
	features: FeatureFeverSeries[];
	empty: boolean;
};

export const BAND_SLOPE = 2 / 3;
export const GREEN_AMBER_INTERCEPT = 0;
export const AMBER_RED_INTERCEPT = 100 / 3;

const clamp = (value: number, min: number, max: number): number =>
	Math.min(Math.max(value, min), max);

const greenAmberBoundary = (completion: number): number =>
	GREEN_AMBER_INTERCEPT + BAND_SLOPE * completion;

const amberRedBoundary = (completion: number): number =>
	AMBER_RED_INTERCEPT + BAND_SLOPE * completion;

export function deriveFeatureFeverChart(
	history: DeliveryMetricsHistory,
): FeatureFeverChart {
	const dated = history.points.flatMap((point) =>
		point.featureBreakdown.map((metric) => ({ metric, date: point.date })),
	);
	const referenceIds = [
		...new Set(dated.map((entry) => entry.metric.referenceId)),
	];

	const features = referenceIds.map((referenceId) => {
		const entries = dated.filter(
			(entry) => entry.metric.referenceId === referenceId,
		);
		const points = entries.map(({ metric, date }) => ({
			date,
			completion: clamp(metric.completion, 0, 100),
			chanceOfLate: clamp(100 - metric.likelihood, 0, 100),
		}));
		return {
			referenceId,
			name: entries[entries.length - 1].metric.name,
			points,
			latest: points[points.length - 1],
		};
	});

	return { features, empty: features.length === 0 };
}

export type ZonePolygon = {
	zone: FeverZone;
	points: Array<[number, number]>;
};

export function feverZonePolygons(): ZonePolygon[] {
	const greenAmberAtFull = greenAmberBoundary(100);
	const amberRedAtZero = amberRedBoundary(0);
	return [
		{
			zone: "green",
			points: [
				[0, 0],
				[100, 0],
				[100, greenAmberAtFull],
			],
		},
		{
			zone: "amber",
			points: [
				[0, 0],
				[100, greenAmberAtFull],
				[100, 100],
				[0, amberRedAtZero],
			],
		},
		{
			zone: "red",
			points: [
				[0, amberRedAtZero],
				[100, 100],
				[0, 100],
			],
		},
	];
}
