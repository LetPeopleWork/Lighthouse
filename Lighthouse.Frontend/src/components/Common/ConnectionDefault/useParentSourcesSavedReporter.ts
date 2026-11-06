import { useCallback, useRef } from "react";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { nowListsSeveralSources } from "./parentSources";

// A follower's settings echo the default's list and the server ignores it, so a follower holds no list of its own.
const ownListOf = (settings: IBaseSettings): readonly ParentSourceEntry[] =>
	settings.parentOverrideSource === "ConnectionDefault"
		? []
		: (settings.parentSources ?? []);

/**
 * Reports an accepted save that gives a Team or Portfolio an own list of several parent sources it did not have.
 *
 * The list last saved is kept in a ref rather than in state, because the settings form sends a save that was
 * queued behind another as soon as that one is answered, before the page has drawn the answer.
 */
export const useParentSourcesSavedReporter = () => {
	const reportUsage = useUsageDataReporter();
	const lastSavedList = useRef<readonly ParentSourceEntry[]>([]);

	const settingsLoaded = useCallback(
		<Settings extends IBaseSettings>(settings: Settings) => {
			lastSavedList.current = ownListOf(settings);
			return settings;
		},
		[],
	);

	const settingsSaved = useCallback(
		(settings: IBaseSettings) => {
			const list = ownListOf(settings);
			if (nowListsSeveralSources(list, lastSavedList.current)) {
				reportUsage({
					name: UsageDataEventName.ParentSourcesSavedWithSeveralEntries,
				});
			}
			lastSavedList.current = list;
		},
		[reportUsage],
	);

	return { settingsLoaded, settingsSaved };
};
