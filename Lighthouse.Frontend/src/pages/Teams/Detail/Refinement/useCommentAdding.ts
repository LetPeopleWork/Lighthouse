import { useCallback, useContext, useRef, useState } from "react";
import type { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type { IVotedRow } from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";
import { useNameFirst } from "./useNameFirst";

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
 * server's call: it knows whether the commenter holds a vote.
 */
export const useCommentAdding = (
	teamId: number,
	{ commentFor, ...naming }: CommenterIdentity,
	onAnswered: (answeredRow: IVotedRow) => void,
	onFailure: (error: unknown) => void,
) => {
	const { sizingLogService } = useContext(ApiServiceContext);
	const sending = useRef(new Set<string>());
	const [commentsBeingSent, setCommentsBeingSent] = useState<
		ReadonlySet<string>
	>(() => new Set());

	// Pressing Send again before the server answers would post the same comment twice.
	const showSending = useCallback(() => {
		setCommentsBeingSent(new Set(sending.current));
	}, []);

	const send = useCallback(
		(
			{ referenceId, comment, onSent }: IPendingComment,
			declared: IStoredVoter | null,
		) => {
			if (sending.current.has(referenceId)) {
				return;
			}
			sending.current.add(referenceId);
			showSending();

			const signed = commentFor(comment, declared);
			sizingLogService
				.addComment(teamId, referenceId, signed.comment, signed.voterKey)
				.then((answeredRow) => {
					// A comment never makes a Work Item ready; only a vote can.
					onAnswered({ ...answeredRow, madeReady: false });
					onSent();
				})
				.catch(onFailure)
				.finally(() => {
					sending.current.delete(referenceId);
					showSending();
				});
		},
		[commentFor, sizingLogService, teamId, onAnswered, onFailure, showSending],
	);

	const { submit, isAskingForName, submitUnderName, cancel } = useNameFirst(
		naming,
		send,
		teamId,
	);

	return {
		addComment: submit,
		commentsBeingSent,
		isAskingForName,
		commentUnderName: submitUnderName,
		cancelComment: cancel,
	};
};
