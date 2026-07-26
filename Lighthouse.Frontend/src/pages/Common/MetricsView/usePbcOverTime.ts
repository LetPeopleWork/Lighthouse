import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import {
	DEFAULT_PROCESS_BEHAVIOR_METRIC_TYPE,
	type ProcessBehaviorMetricType,
	type ProcessBehaviorSnapshot,
} from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

type MetricTypeCache = Partial<
	Record<ProcessBehaviorMetricType, ProcessBehaviorSnapshot[]>
>;

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
): PbcOverTimeState {
	const [metricType, setMetricType] = useState<ProcessBehaviorMetricType>(
		DEFAULT_PROCESS_BEHAVIOR_METRIC_TYPE,
	);
	const [cache, setCache] = useState<MetricTypeCache>({});

	useEffect(() => {
		// Already fetched — re-plot from the persisted series, no recompute.
		if (cache[metricType] !== undefined) {
			return;
		}
		let cancelled = false;
		metricsService
			.getProcessBehaviorOverTime(ownerId, metricType)
			.then((data) => {
				if (!cancelled) {
					setCache((previous) => ({ ...previous, [metricType]: data }));
				}
			})
			.catch((error) =>
				console.error("Error fetching process behavior over time:", error),
			);
		return () => {
			cancelled = true;
		};
	}, [ownerId, metricsService, metricType, cache]);

	return { metricType, setMetricType, series: cache[metricType] ?? null };
}
