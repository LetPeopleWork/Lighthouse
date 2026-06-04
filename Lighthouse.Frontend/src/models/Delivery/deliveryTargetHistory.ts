import type { DeliveryMetricsHistoryPoint } from "./DeliveryMetricsHistory";

export interface TargetChange {
	index: number;
	previousTarget: Date;
	newTarget: Date;
}

export function steppedTargetData(
	points: DeliveryMetricsHistoryPoint[],
): Array<number | null> | null {
	if (points.every((point) => point.targetDateAtSnapshot === null)) {
		return null;
	}

	return points.map((point) =>
		point.targetDateAtSnapshot ? point.targetDateAtSnapshot.getTime() : null,
	);
}

export function targetChanges(
	points: DeliveryMetricsHistoryPoint[],
): TargetChange[] {
	return points
		.map((point, index) => ({
			index,
			current: point.targetDateAtSnapshot,
			previous: points[index - 1]?.targetDateAtSnapshot ?? null,
		}))
		.filter(
			(entry): entry is { index: number; current: Date; previous: Date } =>
				entry.current !== null &&
				entry.previous !== null &&
				entry.current.getTime() !== entry.previous.getTime(),
		)
		.map((entry) => ({
			index: entry.index,
			previousTarget: entry.previous,
			newTarget: entry.current,
		}));
}
