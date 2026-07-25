import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type {
	PercentilesOverTimeSnapshot,
	PercentilesSelection,
} from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

type SelectionCache = Partial<
	Record<PercentilesSelection, PercentilesOverTimeSnapshot[]>
>;

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
): PercentilesOverTimeState {
	const [selection, setSelection] = useState<PercentilesSelection>(30);
	const [cache, setCache] = useState<SelectionCache>({});

	useEffect(() => {
		// Already fetched — re-plot from the persisted series, no recompute (AC5).
		if (cache[selection] !== undefined) {
			return;
		}
		let cancelled = false;
		metricsService
			.getPercentilesOverTime(ownerId, selection)
			.then((data) => {
				if (!cancelled) {
					setCache((previous) => ({ ...previous, [selection]: data }));
				}
			})
			.catch((error) =>
				console.error("Error fetching percentiles over time:", error),
			);
		return () => {
			cancelled = true;
		};
	}, [ownerId, metricsService, selection, cache]);

	return { selection, setSelection, series: cache[selection] ?? null };
}
