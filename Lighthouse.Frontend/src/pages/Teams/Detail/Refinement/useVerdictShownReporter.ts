import { useEffect, useRef } from "react";
import type { IRefinementNeed } from "../../../../models/Refinement/Refinement";
import type { UsageDataRefinementVerdict } from "../../../../models/UsageData/UsageData";
import { UsageDataEventName } from "../../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../../services/UsageData/usageDataReporter";
import type { ShownRefinement } from "./useRefinement";

/** The verdict worth reporting, or nothing: only a Refinement day reports, and a missing number is `None`. */
export const refinementDayVerdict = (
	isRefinementDay: boolean | undefined,
	need: IRefinementNeed | undefined,
): UsageDataRefinementVerdict | undefined => {
	if (isRefinementDay !== true) {
		return undefined;
	}
	return need?.verdict ?? "None";
};

interface Opening {
	teamId: number;
	hasReported: boolean;
}

/**
 * Reports which verdict the tab showed on a Refinement day, once per Team the tab is opened on. A vote that
 * reads the Refinement again does not report again: usage data counts openings, not reads. Until the
 * Refinement of the Team the tab moved to arrives, what is on screen is the Team before's and reports
 * nothing.
 */
export const useVerdictShownReporter = (
	teamId: number,
	shown: ShownRefinement | null,
): void => {
	const reportUsage = useUsageDataReporter();
	const opening = useRef<Opening>({ teamId, hasReported: false });

	useEffect(() => {
		if (opening.current.teamId !== teamId) {
			opening.current = { teamId, hasReported: false };
		}

		const isShowingThisTeam =
			shown?.teamId === teamId && shown.view.workItems.length > 0;
		if (opening.current.hasReported || !isShowingThisTeam) {
			return;
		}

		const verdict = refinementDayVerdict(
			shown.view.isRefinementDay,
			shown.view.need,
		);
		// Consent can arrive after the verdict is shown; until the event could go, the opening is not used up.
		opening.current.hasReported =
			verdict === undefined ||
			reportUsage({
				name: UsageDataEventName.TeamRefinementDayVerdictShown,
				refinementVerdict: verdict,
			});
	}, [teamId, shown, reportUsage]);
};
