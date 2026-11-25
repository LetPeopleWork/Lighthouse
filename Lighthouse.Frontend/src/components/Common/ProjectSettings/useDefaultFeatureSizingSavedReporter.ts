import { useCallback, useRef } from "react";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";

// A Portfolio saved before the switch existed reads as on.
const infersSize = (settings: IPortfolioSettings) =>
	settings.infersDefaultFeatureSize ?? true;

/**
 * Reports an accepted save that switches default Feature sizing from on to off.
 *
 * The switch last saved is kept in a ref rather than in state, because the settings form sends a save that was
 * queued behind another as soon as that one is answered, before the page has drawn the answer.
 */
export const useDefaultFeatureSizingSavedReporter = () => {
	const reportUsage = useUsageDataReporter();
	const lastSavedInfers = useRef(true);

	const settingsLoaded = useCallback(
		<Settings extends IPortfolioSettings>(settings: Settings) => {
			lastSavedInfers.current = infersSize(settings);
			return settings;
		},
		[],
	);

	const settingsSaved = useCallback(
		(settings: IPortfolioSettings) => {
			const infers = infersSize(settings);
			if (lastSavedInfers.current && !infers) {
				reportUsage({
					name: UsageDataEventName.DefaultFeatureSizingSwitchedOff,
				});
			}
			lastSavedInfers.current = infers;
		},
		[reportUsage],
	);

	return { settingsLoaded, settingsSaved };
};
