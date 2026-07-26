/**
 * One recorded process-behaviour day as served by
 * `GET .../metrics/process-behavior-over-time?type={metric}` (ADR-108).
 *
 * recordedAt: ISO date string (`yyyy-MM-dd`, DateOnly on the backend), one per
 * calendar day, ascending.
 * unpl / average / lnpl: the natural process limit triple recorded for that day.
 * The metric family is carried by the request's `type` parameter, not repeated
 * per row — deliberately NOT the percentile quartet's shape, because a limit
 * triple and a percentile quartet are different contracts that evolve apart.
 */
export interface ProcessBehaviorSnapshot {
	recordedAt: string;
	unpl: number;
	average: number;
	lnpl: number;
}

/**
 * The metric families the process-behaviour recorder persists a series for.
 * Only Throughput is recorded today; later slices append to this list rather
 * than restructuring the widget's toggle row.
 */
export type ProcessBehaviorMetricType = "Throughput";

export const PROCESS_BEHAVIOR_METRIC_TYPES: readonly ProcessBehaviorMetricType[] =
	["Throughput"] as const;

/** The toggle's initial selection — the only family recorded so far. */
export const DEFAULT_PROCESS_BEHAVIOR_METRIC_TYPE: ProcessBehaviorMetricType =
	"Throughput";
