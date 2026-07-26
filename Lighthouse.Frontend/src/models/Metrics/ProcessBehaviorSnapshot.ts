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
 * These are the backend `ProcessBehaviorMetricType` enum MEMBER names verbatim,
 * because the value is interpolated straight into the request's `?type=` — the
 * human wording lives in the widget's label map, never on the wire.
 */
export type ProcessBehaviorMetricType =
	| "Throughput"
	| "WorkItemAge"
	| "Wip"
	| "CycleTime"
	| "Arrivals"
	| "FeatureSize";

/**
 * A team has no feature sizes to chart, so Feature Size is not offered there —
 * the portfolio list is this one plus that single family (D8). The wire stays
 * permissive on purpose (a team asking for FeatureSize gets an empty series);
 * the toggle is the one place that withholds the option.
 */
const TEAM_PROCESS_BEHAVIOR_METRIC_TYPES: readonly ProcessBehaviorMetricType[] =
	["Throughput", "WorkItemAge", "Wip", "CycleTime", "Arrivals"] as const;

const PORTFOLIO_PROCESS_BEHAVIOR_METRIC_TYPES: readonly ProcessBehaviorMetricType[] =
	[...TEAM_PROCESS_BEHAVIOR_METRIC_TYPES, "FeatureSize"];

/** The families a dashboard offers at the given scope (D8). */
export function processBehaviorMetricTypesFor(
	ownerType: "team" | "portfolio",
): readonly ProcessBehaviorMetricType[] {
	return ownerType === "portfolio"
		? PORTFOLIO_PROCESS_BEHAVIOR_METRIC_TYPES
		: TEAM_PROCESS_BEHAVIOR_METRIC_TYPES;
}

/** The toggle's initial selection — offered at every scope. */
export const DEFAULT_PROCESS_BEHAVIOR_METRIC_TYPE: ProcessBehaviorMetricType =
	"Throughput";
