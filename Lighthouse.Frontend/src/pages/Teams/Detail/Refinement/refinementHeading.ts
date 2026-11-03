import type { IRefinementView } from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { describeStageBreakdown } from "./stageBreakdown";

type GetTerm = (key: string) => string;

export const describeReadyByVotes = (refinement: IRefinementView): string =>
	refinement.readyByVotesCount === undefined
		? ""
		: ` · ${refinement.readyByVotesCount} ready by votes`;

// A Team with stage rules reads its Work Items by stage; any other Team by how many there are.
export const describeHeading = (
	refinement: IRefinementView,
	getTerm: GetTerm,
): string => {
	if (refinement.stagesConfigured) {
		return describeStageBreakdown(refinement.workItems);
	}
	const count = refinement.workItems.length;
	const workItemsTerm = getTerm(
		count === 1 ? TERMINOLOGY_KEYS.WORK_ITEM : TERMINOLOGY_KEYS.WORK_ITEMS,
	);
	return `${count} ${workItemsTerm} in ${getTerm(TERMINOLOGY_KEYS.REFINEMENT)}${describeReadyByVotes(refinement)}`;
};
