import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "./DeliveryMetricsHistory";

export type FeverZone = "green" | "amber" | "red";

export type FeverPoint = {
	date: Date;
	completion: number;
	chanceOfLate: number;
	zone: FeverZone;
};

export type FeverTrail = {
	points: FeverPoint[];
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

const zoneFor = (completion: number, chanceOfLate: number): FeverZone => {
	if (chanceOfLate >= amberRedBoundary(completion)) {
		return "red";
	}
	if (chanceOfLate >= greenAmberBoundary(completion)) {
		return "amber";
	}
	return "green";
};

const isPlottable = (point: DeliveryMetricsHistoryPoint): boolean =>
	point.totalWork > 0 && point.likelihoodPercentage !== null;

const toFeverPoint = (point: DeliveryMetricsHistoryPoint): FeverPoint => {
	const completion = clamp((point.doneWork / point.totalWork) * 100, 0, 100);
	const chanceOfLate = clamp(100 - (point.likelihoodPercentage ?? 0), 0, 100);
	return {
		date: point.date,
		completion,
		chanceOfLate,
		zone: zoneFor(completion, chanceOfLate),
	};
};

export function deriveFeverTrail(history: DeliveryMetricsHistory): FeverTrail {
	const points = history.points.filter(isPlottable).map(toFeverPoint);
	return { points, empty: points.length === 0 };
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
