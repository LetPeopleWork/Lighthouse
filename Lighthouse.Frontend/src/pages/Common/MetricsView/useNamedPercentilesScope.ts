import { useCallback, useEffect, useRef, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { ICycleTimePercentilesInfo } from "../../../models/Metrics/InfoWidgetData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { fireAndForget } from "../../../utils/fireAndForget";

interface NamedPercentilesScopeInput<T extends IWorkItem | IFeature> {
	metricsService: IMetricsService<T>;
	entityId: IFeatureOwner["id"];
	startDate: Date;
	endDate: Date;
}

export function useNamedPercentilesScope<T extends IWorkItem | IFeature>({
	metricsService,
	entityId,
	startDate,
	endDate,
}: NamedPercentilesScopeInput<T>) {
	const [percentilesScopeDefinitionId, setPercentilesScopeDefinitionId] =
		useState<number | null>(null);
	const [scopedPercentileValues, setScopedPercentileValues] = useState<
		IPercentileValue[] | null
	>(null);
	const [scopedPercentilesInfo, setScopedPercentilesInfo] =
		useState<ICycleTimePercentilesInfo | null>(null);

	const namedPercentilesFetchGeneration = useRef(0);

	const handlePercentilesScopeChange = useCallback(
		(definitionId: number | null) => {
			// Each selection supersedes the one before it. Without this guard a slow
			// response for an earlier definition can land after a fast one for the
			// current definition and overwrite it, leaving the selector and the table
			// disagreeing about which window is on screen.
			const generation = ++namedPercentilesFetchGeneration.current;
			const isCurrent = () =>
				generation === namedPercentilesFetchGeneration.current;

			setPercentilesScopeDefinitionId(definitionId);
			if (definitionId === null) {
				setScopedPercentileValues(null);
				setScopedPercentilesInfo(null);
				return;
			}

			// A failed named fetch must never leave the Default numbers on screen under
			// a named selection - the widget would be presenting the default window's
			// percentiles as if they were the named one's. Snap back to Default instead,
			// mirroring the invalid-definition self-reset the selector already does.
			const revertToDefault = () => {
				if (!isCurrent()) {
					return;
				}
				setPercentilesScopeDefinitionId(null);
				setScopedPercentileValues(null);
				setScopedPercentilesInfo(null);
			};

			fireAndForget(
				metricsService
					.getCycleTimePercentiles(entityId, startDate, endDate, definitionId)
					.then((values) => {
						if (isCurrent()) {
							setScopedPercentileValues(values);
						}
					})
					.catch(revertToDefault),
			);
			fireAndForget(
				metricsService
					.getCycleTimePercentilesInfo(
						entityId,
						startDate,
						endDate,
						definitionId,
					)
					.then((info) => {
						if (isCurrent()) {
							setScopedPercentilesInfo(info);
						}
					})
					.catch(revertToDefault),
			);
		},
		[metricsService, entityId, startDate, endDate],
	);

	// biome-ignore lint/correctness/useExhaustiveDependencies: entity/window are reset triggers, not values read in the body — a scope chosen for one team/window must not leak into the next.
	useEffect(() => {
		// Bumping the generation discards any response still in flight for the
		// previous entity or window - it would otherwise land after the reset and
		// re-populate the scope we just cleared.
		namedPercentilesFetchGeneration.current += 1;
		setPercentilesScopeDefinitionId(null);
		setScopedPercentileValues(null);
		setScopedPercentilesInfo(null);
	}, [entityId, startDate, endDate]);

	return {
		percentilesScopeDefinitionId,
		scopedPercentileValues,
		scopedPercentilesInfo,
		handlePercentilesScopeChange,
	};
}
