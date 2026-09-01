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

/** The row in the instance-wide settings store that holds who owns the order. */
const ORDERING_SETTING_KEY = "FeatureOrdering";

export interface FeatureOrderingState {
	/** Who decides the order this instance forecasts in. */
	policy: FeatureOrderingPolicy;
	/** What the position column calls itself under that policy. */
	positionColumnLabel: string;
	/**
	 * Four different reasons produce the same unavailable move action, so all four are decided here
	 * rather than wherever a move happens to be drawn. Spread out, one of them quietly stops being
	 * asked and a move nobody is allowed to make becomes clickable.
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
 * The single place the client reads who owns the order, and the single place the stored on/off answer
 * is turned into that. A second copy of either would be free to drift out of step with this one.
 */
export const useFeatureOrdering = (): FeatureOrderingState => {
	const [policy, setPolicy] = useState<FeatureOrderingPolicy>("SourceOrder");

	const { optionalFeatureService } = useContext(ApiServiceContext);
	const { licenseStatus } = useLicenseRestrictions();
	const canUsePremiumFeatures = licenseStatus?.canUsePremiumFeatures ?? false;

	const refresh = useCallback(async () => {
		try {
			const setting =
				await optionalFeatureService.getFeatureByKey(ORDERING_SETTING_KEY);
			setPolicy(setting?.enabled === true ? "ManualOrder" : "SourceOrder");
		} catch {
			// An instance that cannot answer follows the tracker, which is what it did before anyone
			// could choose. Failing the other way would silently re-sequence every forecast.
			setPolicy("SourceOrder");
		}
	}, [optionalFeatureService]);

	useEffect(() => {
		refresh();
	}, [refresh]);

	const resolveMoveGate = useCallback(
		(
			feature: IFeature,
			options: { isSortActive: boolean },
		): FeatureMoveGate => {
			// Ordered deliberately, not left to whichever `if` came first. An instance-wide reason removes
			// the actions entirely, so it outranks one that only greys them out.
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
			// of those read as permitted when they simply have no answer yet, and a missing answer is not
			// permission.
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
