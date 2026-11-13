import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useContext } from "react";
import type { IOptionalFeature } from "../models/OptionalFeatures/OptionalFeature";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../services/Api/UsageDataService";
import { usageDataOptionalFeatureFor } from "../services/UsageData/usageDataOptionalFeatures";
import { useUsageDataReporter } from "../services/UsageData/usageDataReporter";
import { useLicenseStatusQuery } from "./useLicenseRestrictions";

const OPTIONAL_FEATURES_KEY = ["optionalFeatures"] as const;

/** The behaviour settings a tab shows, once it knows enough to show them truthfully. */
export type BehaviourSettingsState =
	| { status: "loading" }
	| { status: "failed" }
	| {
			status: "ready";
			settings: IOptionalFeature[];
			canUsePremiumFeatures: boolean;
			toggle: (setting: IOptionalFeature) => Promise<void>;
	  };

/**
 * The behaviour settings with the given keys. Rows are picked by key, never by id: the server hands every
 * row the same id. The license is read through the shared query and only matters when a picked row is
 * Premium; an unread license is then loading or failed, never "no Premium".
 */
export const useBehaviourSettings = (
	keys: readonly string[],
): BehaviourSettingsState => {
	const { optionalFeatureService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const reportUsage = useUsageDataReporter();

	const features = useQuery({
		queryKey: OPTIONAL_FEATURES_KEY,
		queryFn: async () => (await optionalFeatureService.getAllFeatures()) ?? [],
	});
	const license = useLicenseStatusQuery();

	const toggle = useCallback(
		async (toggled: IOptionalFeature) => {
			const before =
				queryClient.getQueryData<IOptionalFeature[]>(OPTIONAL_FEATURES_KEY) ??
				[];
			const enabled = !toggled.enabled;
			queryClient.setQueryData<IOptionalFeature[]>(
				OPTIONAL_FEATURES_KEY,
				before.map((setting) =>
					setting.key === toggled.key ? { ...setting, enabled } : setting,
				),
			);

			try {
				await optionalFeatureService.updateFeature({ ...toggled, enabled });
			} catch {
				queryClient.setQueryData(OPTIONAL_FEATURES_KEY, before);
				await queryClient.invalidateQueries({
					queryKey: OPTIONAL_FEATURES_KEY,
				});
				return;
			}

			// Reported only once the server accepted it: the switch moved before the answer, and a refused
			// one never happened.
			const optionalFeature = usageDataOptionalFeatureFor(toggled.key);
			if (optionalFeature) {
				reportUsage({
					name: UsageDataEventName.OptionalFeatureToggled,
					optionalFeature,
					enabled,
				});
			}
		},
		[optionalFeatureService, queryClient, reportUsage],
	);

	if (features.isError) return { status: "failed" };
	if (features.data === undefined) return { status: "loading" };

	const settings = features.data.filter((setting) =>
		keys.includes(setting.key),
	);
	const needsLicense = settings.some((setting) => setting.isPremium);
	if (needsLicense) {
		if (license.isError || license.data === null) return { status: "failed" };
		if (license.data === undefined) return { status: "loading" };
	}

	return {
		status: "ready",
		settings,
		canUsePremiumFeatures: license.data?.canUsePremiumFeatures ?? false,
		toggle,
	};
};
