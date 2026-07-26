import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import {
	DEFAULT_PROCESS_BEHAVIOR_METRIC_TYPE,
	type ProcessBehaviorMetricType,
	type ProcessBehaviorSnapshot,
} from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

/**
 * Keyed by metric family AND date range, not by family alone: the series a request
 * answers with depends on both, so a family-only key would serve the previous
 * range's series after the dashboard pickers move (US-06 AC4).
 */
type MetricTypeCache = Record<string, ProcessBehaviorSnapshot[]>;

/** Single source of the cache key, so the write and the read-back cannot disagree. */
function cacheKey(
	metricType: ProcessBehaviorMetricType,
	startDate: Date,
	endDate: Date,
): string {
	return `${metricType}|${startDate.toISOString()}|${endDate.toISOString()}`;
}

export interface PbcOverTimeState {
	metricType: ProcessBehaviorMetricType;
	setMetricType: (metricType: ProcessBehaviorMetricType) => void;
	/** null while the selected family is still loading; [] once loaded-but-empty (D6). */
	series: ProcessBehaviorSnapshot[] | null;
}

/**
 * Fetches the persisted process-behaviour limits series for the selected metric
 * family through the existing metrics-service abstraction (no bespoke fetch).
 * Each family is fetched at most once and cached, so toggling re-plots the
 * already-fetched series without a second recompute — the read endpoint is
 * read-only by contract (ADR-108).
 */
export function usePbcOverTime(
	ownerId: number,
	metricsService: IMetricsService<IWorkItem | IFeature>,
	startDate: Date,
	endDate: Date,
): PbcOverTimeState {
	const [metricType, setMetricType] = useState<ProcessBehaviorMetricType>(
		DEFAULT_PROCESS_BEHAVIOR_METRIC_TYPE,
	);
	const [cache, setCache] = useState<MetricTypeCache>({});
	const key = cacheKey(metricType, startDate, endDate);

	useEffect(() => {
		// Already fetched for this family AND range — re-plot from the persisted
		// series, no recompute. A range change is a different key, so it refetches
		// instead of replaying a stale series (US-06 AC4).
		if (cache[key] !== undefined) {
			return;
		}
		let cancelled = false;
		metricsService
			.getProcessBehaviorOverTime(ownerId, metricType, startDate, endDate)
			.then((data) => {
				if (!cancelled) {
					setCache((previous) => ({ ...previous, [key]: data }));
				}
			})
			.catch((error) =>
				console.error("Error fetching process behavior over time:", error),
			);
		return () => {
			cancelled = true;
		};
	}, [ownerId, metricsService, metricType, startDate, endDate, key, cache]);

	return { metricType, setMetricType, series: cache[key] ?? null };
}
