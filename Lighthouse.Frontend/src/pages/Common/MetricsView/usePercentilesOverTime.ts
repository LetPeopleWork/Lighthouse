import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type {
	PercentilesOverTimeSnapshot,
	PercentilesSelection,
} from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

/**
 * Keyed by selection AND date range, not by selection alone: the series a request
 * answers with depends on both, so a selection-only key would serve the previous
 * range's series after the dashboard pickers move (US-06 AC4).
 */
type SelectionCache = Record<string, PercentilesOverTimeSnapshot[]>;

/** Single source of the cache key, so the write and the read-back cannot disagree. */
function cacheKey(
	selection: PercentilesSelection,
	startDate: Date,
	endDate: Date,
): string {
	return `${selection}|${startDate.toISOString()}|${endDate.toISOString()}`;
}

export interface PercentilesOverTimeState {
	selection: PercentilesSelection;
	setSelection: (selection: PercentilesSelection) => void;
	/** null while the selected tab is still loading; [] once loaded-but-empty (D6). */
	series: PercentilesOverTimeSnapshot[] | null;
}

/**
 * Fetches the persisted percentiles-over-time series for the selected tab
 * through the existing metrics-service abstraction (no bespoke fetch). Each
 * selection — the horizon-less work-item-age tab as well as each cycle-time
 * horizon — is fetched at most once and cached, so switching Age↔30↔60↔90
 * re-plots from the already-fetched persisted series without a second recompute
 * request (US-01 AC5 — the endpoint is read-only).
 */
export function usePercentilesOverTime(
	ownerId: number,
	metricsService: IMetricsService<IWorkItem | IFeature>,
	startDate: Date,
	endDate: Date,
): PercentilesOverTimeState {
	const [selection, setSelection] = useState<PercentilesSelection>(30);
	const [cache, setCache] = useState<SelectionCache>({});
	const key = cacheKey(selection, startDate, endDate);

	useEffect(() => {
		// Already fetched for this selection AND range — re-plot from the persisted
		// series, no recompute (AC5). A range change is a different key, so it
		// refetches instead of replaying a stale series (US-06 AC4).
		if (cache[key] !== undefined) {
			return;
		}
		let cancelled = false;
		metricsService
			.getPercentilesOverTime(ownerId, selection, startDate, endDate)
			.then((data) => {
				if (!cancelled) {
					setCache((previous) => ({ ...previous, [key]: data }));
				}
			})
			.catch((error) =>
				console.error("Error fetching percentiles over time:", error),
			);
		return () => {
			cancelled = true;
		};
	}, [ownerId, metricsService, selection, startDate, endDate, key, cache]);

	return { selection, setSelection, series: cache[key] ?? null };
}
