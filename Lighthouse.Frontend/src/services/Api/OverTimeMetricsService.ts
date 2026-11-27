import {
	PERCENTILES_AGE_SELECTION,
	type PercentilesOverTimeSnapshot,
	type PercentilesSelection,
} from "../../models/Metrics/PercentilesOverTimeSnapshot";
import type {
	ProcessBehaviorMetricType,
	ProcessBehaviorSnapshot,
} from "../../models/Metrics/ProcessBehaviorSnapshot";
import type {
	IWithinSleOverTime,
	WithinSleHorizon,
} from "../../models/Metrics/WithinSleOverTime";

/**
 * The charts drawn from a daily record the server keeps, one row per day, rather than worked out for the
 * selected range on request.
 */
export interface IOverTimeMetricsService {
	getPercentilesOverTime(
		id: number,
		selection: PercentilesSelection,
		startDate: Date,
		endDate: Date,
	): Promise<PercentilesOverTimeSnapshot[]>;

	getProcessBehaviorOverTime(
		id: number,
		metricType: ProcessBehaviorMetricType,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviorSnapshot[]>;

	getWithinSleOverTime(
		id: number,
		horizon: WithinSleHorizon,
		startDate: Date,
		endDate: Date,
	): Promise<IWithinSleOverTime>;
}

/**
 * Query suffix for the percentiles-over-time read endpoint. Work Item Age is
 * as-of-today and therefore horizon-less — it asks for the metric type and lets
 * the backend resolve the horizon-less sentinel. Cycle time keeps the horizon
 * request shape unchanged.
 */
export function buildPercentilesOverTimeQuery(
	selection: PercentilesSelection,
): string {
	if (selection === PERCENTILES_AGE_SELECTION) {
		return "metricType=WorkItemAge";
	}

	return `horizon=${selection}`;
}
