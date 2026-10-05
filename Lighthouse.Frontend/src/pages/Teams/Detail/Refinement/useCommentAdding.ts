import { useCallback, useContext, useState } from "react";
import type { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type { IVotedRow } from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";

export interface IPendingComment {
	referenceId: string;
	comment: string;
	onSent: () => void;
}

type CommenterIdentity = Pick<
	ReturnType<typeof useVoterIdentity>,
	"voter" | "asksForName" | "declareName" | "commentFor"
>;

/**
 * Adding a comment to a Work Item without voting. Whether it is a plain comment or an open question is the
 * server's call: it knows whether the commenter holds a vote. A commenter without sign-in who has not named
 * themselves yet is asked first, and the comment waits until they have.
 */
export const useCommentAdding = (
	teamId: number,
	{ voter, asksForName, declareName, commentFor }: CommenterIdentity,
	onAnswered: (answeredRow: IVotedRow) => void,
	onFailure: (error: unknown) => void,
) => {
	const { sizingLogService } = useContext(ApiServiceContext);
	const [pendingComment, setPendingComment] = useState<IPendingComment | null>(
		null,
	);

	const send = useCallback(
		(
			{ referenceId, comment, onSent }: IPendingComment,
			declared: IStoredVoter | null,
		) => {
			const signed = commentFor(comment, declared);
			sizingLogService
				.addComment(teamId, referenceId, signed.comment, signed.voterKey)
				.then((answeredRow) => {
					// A comment never makes a Work Item ready; only a vote can.
					onAnswered({ ...answeredRow, madeReady: false });
					onSent();
				})
				.catch(onFailure);
		},
		[commentFor, sizingLogService, teamId, onAnswered, onFailure],
	);

	const addComment = useCallback(
		(pending: IPendingComment) => {
			if (asksForName) {
				setPendingComment(pending);
				return;
			}

			send(pending, voter);
		},
		[asksForName, send, voter],
	);

	const commentUnderName = (name: string) => {
		if (pendingComment !== null) {
			send(pendingComment, declareName(name));
		}
		setPendingComment(null);
	};

	return {
		addComment,
		isAskingForName: pendingComment !== null,
		commentUnderName,
		cancelComment: () => setPendingComment(null),
	};
};
