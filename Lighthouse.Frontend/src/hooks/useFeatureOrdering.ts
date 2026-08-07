import { useCallback, useContext, useEffect, useState } from "react";
import type { IFeature } from "../models/Feature";
import {
	type FeatureMoveBlockReason,
	type FeatureMoveGate,
	type FeatureOrderingPolicy,
	MANUAL_ORDER_COLUMN_LABEL,
	SOURCE_ORDER_COLUMN_LABEL,
} from "../models/FeatureOrdering";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { useLicenseRestrictions } from "./useLicenseRestrictions";

export interface FeatureOrderingState {
	/** Who decides the order this instance forecasts in. */
	policy: FeatureOrderingPolicy;
	/** What the position column calls itself under that policy. */
	positionColumnLabel: string;
	/**
	 * AC-3.7, AC-3.8, AC-3.9 and AC-3.10 are four reasons for one visual state, so they resolve in one
	 * place (ADR-134 SA-12). Four scattered `if`s over the same question is the frontend twin of the
	 * backend failure this epic exists to prevent.
	 */
	resolveMoveGate: (
		feature: IFeature,
		options: { isSortActive: boolean },
	) => FeatureMoveGate;
	refresh: () => Promise<void>;
}

const refused = (
	reason: FeatureMoveBlockReason,
	blockingPortfolios: string[] = [],
): FeatureMoveGate => ({ enabled: false, reason, blockingPortfolios });

/**
 * The single place the ordering policy is read on the client (ADR-134 SA-12). Four scattered `if`s over
 * the same question is the frontend twin of the five-`if` backend failure this epic exists to prevent.
 */
export const useFeatureOrdering = (): FeatureOrderingState => {
	const [policy, setPolicy] = useState<FeatureOrderingPolicy>("SourceOrder");

	const { settingsService } = useContext(ApiServiceContext);
	const { licenseStatus } = useLicenseRestrictions();
	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;

	const refresh = useCallback(async () => {
		try {
			setPolicy(await settingsService.getFeatureOrdering());
		} catch {
			// An instance that cannot answer follows the tracker, which is what it did before anyone
			// could choose. Failing closed here would silently re-sequence every forecast.
			setPolicy("SourceOrder");
		}
	}, [settingsService]);

	useEffect(() => {
		refresh();
	}, [refresh]);

	const resolveMoveGate = useCallback(
		(
			feature: IFeature,
			options: { isSortActive: boolean },
		): FeatureMoveGate => {
			// Ordered deliberately, not left to whichever `if` came first. An instance-wide reason removes
			// the actions entirely (AC-3.10), so it outranks one that only greys them out.
			if (!canUsePremiumFeatures) {
				return refused("not-premium");
			}

			if (policy !== "ManualOrder") {
				return refused("policy-off");
			}

			if (options.isSortActive) {
				return refused("sorted");
			}

			// The server's word, carried through. Nothing here consults RBAC or looks at `projects`: both
			// fail open, and an absent verdict is not permission (ADR-136 SA-10).
			if (feature.canMove !== true) {
				return refused(
					feature.moveBlockReason === "orphan" ? "orphan" : "no-write",
					(feature.blockingPortfolios ?? []).map((portfolio) => portfolio.name),
				);
			}

			return { enabled: true };
		},
		[canUsePremiumFeatures, policy],
	);

	return {
		policy,
		resolveMoveGate,
		positionColumnLabel:
			policy === "ManualOrder"
				? MANUAL_ORDER_COLUMN_LABEL
				: SOURCE_ORDER_COLUMN_LABEL,
		refresh,
	};
};
