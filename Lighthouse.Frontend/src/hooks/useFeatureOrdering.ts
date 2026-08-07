import { useCallback, useContext, useEffect, useState } from "react";
import {
	type FeatureOrderingPolicy,
	MANUAL_ORDER_COLUMN_LABEL,
	SOURCE_ORDER_COLUMN_LABEL,
} from "../models/FeatureOrdering";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

export interface FeatureOrderingState {
	/** Who decides the order this instance forecasts in. */
	policy: FeatureOrderingPolicy;
	/** What the position column calls itself under that policy. */
	positionColumnLabel: string;
	isLoading: boolean;
	refresh: () => Promise<void>;
}

/**
 * The single place the ordering policy is read on the client (ADR-134 SA-12). Four scattered `if`s over
 * the same question is the frontend twin of the five-`if` backend failure this epic exists to prevent.
 */
export const useFeatureOrdering = (): FeatureOrderingState => {
	const [policy, setPolicy] = useState<FeatureOrderingPolicy>("SourceOrder");
	const [isLoading, setIsLoading] = useState(true);

	const { settingsService } = useContext(ApiServiceContext);

	const refresh = useCallback(async () => {
		try {
			setPolicy(await settingsService.getFeatureOrdering());
		} catch {
			// An instance that cannot answer follows the tracker, which is what it did before anyone
			// could choose. Failing closed here would silently re-sequence every forecast.
			setPolicy("SourceOrder");
		} finally {
			setIsLoading(false);
		}
	}, [settingsService]);

	useEffect(() => {
		refresh();
	}, [refresh]);

	return {
		policy,
		positionColumnLabel:
			policy === "ManualOrder"
				? MANUAL_ORDER_COLUMN_LABEL
				: SOURCE_ORDER_COLUMN_LABEL,
		isLoading,
		refresh,
	};
};
