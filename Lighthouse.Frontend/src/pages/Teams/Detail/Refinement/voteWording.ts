import type { RowReadiness } from "../../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { ApiError } from "../../../../services/Api/ApiError";
import { LONGEST_VOTER_NAME } from "./VoterNamePrompt";

export const describeVoteCount = (voteCount: number): string => {
	if (voteCount === 0) {
		return "No votes";
	}

	return voteCount === 1 ? "1 vote" : `${voteCount} votes`;
};

export const LONGEST_COMMENT = 2000;

const TOO_MANY_REQUESTS = 429;
const BAD_REQUEST = 400;
const NAME_REFUSALS = new Set(["voter-name-required", "voter-name-too-long"]);
const COMMENT_REFUSALS = new Map([
	["comment-too-long", `A comment is at most ${LONGEST_COMMENT} characters.`],
	["comment-required", "A comment needs some text."],
]);

const isARefusedName = ({ code, problemCode, fieldName }: ApiError): boolean =>
	(problemCode !== undefined && NAME_REFUSALS.has(problemCode)) ||
	(code === BAD_REQUEST && fieldName?.toLowerCase() === "votername");

/** Why the server would not take a vote or a comment, in words the voter can act on. */
export const describeVoteRefusal = (
	error: unknown,
	getTerm: (key: string) => string,
): string => {
	if (!(error instanceof ApiError)) {
		return error instanceof Error ? error.message : String(error);
	}

	if (error.code === TOO_MANY_REQUESTS) {
		return "Too many votes or comments from this browser. Try again in a minute.";
	}

	const commentRefusal = COMMENT_REFUSALS.get(error.problemCode ?? "");
	if (commentRefusal !== undefined) {
		return commentRefusal;
	}

	if (error.problemCode === "work-item-not-in-refinement") {
		return `This ${getTerm(TERMINOLOGY_KEYS.WORK_ITEM).toLowerCase()} is no longer in ${getTerm(TERMINOLOGY_KEYS.REFINEMENT).toLowerCase()}.`;
	}

	if (isARefusedName(error)) {
		return `Please give your name (at most ${LONGEST_VOTER_NAME} characters).`;
	}

	return error.message;
};

const moreVoters = (missing: number): string =>
	`${missing} more ${missing === 1 ? "voter" : "voters"} needed`;

const READINESS_WORDING: Record<RowReadiness, (missing: number) => string> = {
	Ready: () => "Ready",
	MoreYesNeeded: (missing) => `${missing} more Yes needed`,
	MoreVotersNeeded: moreVoters,
	NeedsDiscussion: () => "Needs discussion",
};

export const describeReadiness = (
	readiness: RowReadiness,
	missingVotes: number | null,
): string => READINESS_WORDING[readiness](missingVotes ?? 0);
