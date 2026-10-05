import { useEffect, useRef } from "react";
import type {
	IRefinementNeed,
	IRefinementView,
} from "../../../../models/Refinement/Refinement";
import type { UsageDataRefinementVerdict } from "../../../../models/UsageData/UsageData";
import { UsageDataEventName } from "../../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../../services/UsageData/usageDataReporter";

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
	// What was on screen when the tab moved to this Team still belongs to the Team before it.
	previousTeamsRefinement: IRefinementView | null;
	hasReported: boolean;
}

/**
 * Reports which verdict the tab showed on a Refinement day, once per Team the tab is opened on. A vote that
 * reads the Refinement again does not report again: usage data counts openings, not reads.
 */
export const useVerdictShownReporter = (
	teamId: number,
	refinement: IRefinementView | null,
): void => {
	const reportUsage = useUsageDataReporter();
	const opening = useRef<Opening>({
		teamId,
		previousTeamsRefinement: null,
		hasReported: false,
	});

	useEffect(() => {
		if (opening.current.teamId !== teamId) {
			opening.current = {
				teamId,
				previousTeamsRefinement: refinement,
				hasReported: false,
			};
		}

		const isShowingThisTeam =
			refinement !== null &&
			refinement !== opening.current.previousTeamsRefinement &&
			refinement.workItems.length > 0;
		if (opening.current.hasReported || !isShowingThisTeam) {
			return;
		}

		opening.current.hasReported = true;
		const verdict = refinementDayVerdict(
			refinement.isRefinementDay,
			refinement.need,
		);
		if (verdict !== undefined) {
			reportUsage({
				name: UsageDataEventName.TeamRefinementDayVerdictShown,
				refinementVerdict: verdict,
			});
		}
	}, [teamId, refinement, reportUsage]);
};
