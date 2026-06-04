import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "./DeliveryMetricsHistory";

export type FeverZone = "green" | "amber" | "red";

export type FeverPoint = {
	date: Date;
	x: number;
	y: number;
	zone: FeverZone;
};

export type FeverTrail = {
	points: FeverPoint[];
	empty: boolean;
};

const GREEN_MAX_DEVIATION = 5;
const AMBER_MAX_DEVIATION = 20;

const clamp = (value: number, min: number, max: number): number =>
	Math.min(Math.max(value, min), max);

const zoneFor = (deviation: number): FeverZone => {
	if (deviation <= GREEN_MAX_DEVIATION) {
		return "green";
	}
	if (deviation <= AMBER_MAX_DEVIATION) {
		return "amber";
	}
	return "red";
};

const emptyTrail: FeverTrail = { points: [], empty: true };

export function deriveFeverTrail(history: DeliveryMetricsHistory): FeverTrail {
	const firstSnapshotDate = history.firstSnapshotDate;
	if (!firstSnapshotDate || history.points.length === 0) {
		return emptyTrail;
	}

	const span = history.deliveryDate.getTime() - firstSnapshotDate.getTime();
	if (span <= 0) {
		return emptyTrail;
	}

	const startTime = firstSnapshotDate.getTime();
	const points = history.points
		.filter((point) => point.totalWork > 0)
		.map((point) => toFeverPoint(point, startTime, span));

	return { points, empty: points.length === 0 };
}

function toFeverPoint(
	point: DeliveryMetricsHistoryPoint,
	startTime: number,
	span: number,
): FeverPoint {
	const scheduleConsumed =
		clamp((point.date.getTime() - startTime) / span, 0, 1) * 100;
	const remaining = (point.remainingWork / point.totalWork) * 100;
	const deviation = remaining - (100 - scheduleConsumed);
	return {
		date: point.date,
		x: scheduleConsumed,
		y: remaining,
		zone: zoneFor(deviation),
	};
}
