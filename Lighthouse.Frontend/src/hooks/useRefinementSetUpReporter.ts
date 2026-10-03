import { useCallback, useRef } from "react";
import type { ITeamSettings } from "../models/Team/TeamSettings";
import { UsageDataEventName } from "../services/Api/UsageDataService";
import { useUsageDataReporter } from "../services/UsageData/usageDataReporter";

const hasRefinementStates = (settings: ITeamSettings): boolean =>
	(settings.refinement?.states.length ?? 0) > 0;

/**
 * Reports a Team getting refinement set up: the accepted save that gives refinement states to a Team the
 * server last said had none. Usage data counts Teams, not saves, so later saves never report again.
 *
 * What the server last said is kept in a ref rather than in state, because the settings form sends a save
 * that was queued behind another as soon as that one is answered, before the page has drawn the answer.
 */
export const useRefinementSetUpReporter = () => {
	const reportUsage = useUsageDataReporter();
	const hadRefinementStates = useRef<boolean | undefined>(undefined);

	const settingsLoaded = useCallback((settings: ITeamSettings) => {
		hadRefinementStates.current = hasRefinementStates(settings);
		return settings;
	}, []);

	const settingsSaved = useCallback(
		(settings: ITeamSettings) => {
			const hasThemNow = hasRefinementStates(settings);
			if (hadRefinementStates.current === false && hasThemNow) {
				reportUsage({ name: UsageDataEventName.TeamRefinementConfigured });
			}
			hadRefinementStates.current = hasThemNow;
			return settings;
		},
		[reportUsage],
	);

	return { settingsLoaded, settingsSaved };
};
