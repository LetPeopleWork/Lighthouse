import type { ISizingSplit } from "../../../../models/Refinement/Refinement";

export const describeVoteCount = (voteCount: number): string => {
	if (voteCount === 0) {
		return "No votes";
	}

	return voteCount === 1 ? "1 vote" : `${voteCount} votes`;
};

export const describeSplit = ({ yes, yesBut, no }: ISizingSplit): string =>
	`${yes} Yes · ${yesBut} Yes, if… · ${no} No`;
