import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type {
	PercentilesHorizon,
	PercentilesOverTimeSnapshot,
} from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

type HorizonCache = Partial<
	Record<PercentilesHorizon, PercentilesOverTimeSnapshot[]>
>;

export interface PercentilesOverTimeState {
	horizon: PercentilesHorizon;
	setHorizon: (horizon: PercentilesHorizon) => void;
	/** null while the selected horizon is still loading; [] once loaded-but-empty (D6). */
	series: PercentilesOverTimeSnapshot[] | null;
}

/**
 * Fetches the persisted percentiles-over-time series for the selected horizon
 * through the existing metrics-service abstraction (no bespoke fetch). Each
 * horizon is fetched at most once and cached, so switching 30↔60↔90 re-plots
 * from the already-fetched persisted series without a second recompute request
 * (US-01 AC5 — the endpoint is read-only).
 */
export function usePercentilesOverTime(
	ownerId: number,
	metricsService: IMetricsService<IWorkItem | IFeature>,
): PercentilesOverTimeState {
	const [horizon, setHorizon] = useState<PercentilesHorizon>(30);
	const [cache, setCache] = useState<HorizonCache>({});

	useEffect(() => {
		// Already fetched — re-plot from the persisted series, no recompute (AC5).
		if (cache[horizon] !== undefined) {
			return;
		}
		let cancelled = false;
		metricsService
			.getPercentilesOverTime(ownerId, horizon)
			.then((data) => {
				if (!cancelled) {
					setCache((previous) => ({ ...previous, [horizon]: data }));
				}
			})
			.catch((error) =>
				console.error("Error fetching percentiles over time:", error),
			);
		return () => {
			cancelled = true;
		};
	}, [ownerId, metricsService, horizon, cache]);

	return { horizon, setHorizon, series: cache[horizon] ?? null };
}
