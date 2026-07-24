/**
 * A single persisted percentiles-over-time snapshot as returned by
 * `GET .../metrics/percentiles-over-time?horizon={30|60|90}` (epic-5427).
 * recordedAt: ISO date string (DateOnly on the backend), one per calendar day.
 * metricType: the metric the percentiles describe (e.g. "CycleTime").
 * p50/p70/p85/p95: the persisted percentile values for that day.
 */
export interface PercentilesOverTimeSnapshot {
	recordedAt: string;
	metricType: string;
	p50: number;
	p70: number;
	p85: number;
	p95: number;
}

/** The only horizons the backend persists series for (D-contract). */
export type PercentilesHorizon = 30 | 60 | 90;

export const PERCENTILES_HORIZONS: readonly PercentilesHorizon[] = [
	30, 60, 90,
] as const;
