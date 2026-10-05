import { useCallback, useState } from "react";
import type {
	ISizingComment,
	ISizingVote,
	SizingAnswer,
	VoterIdentity,
} from "../models/Refinement/Refinement";
import {
	type IStoredVoter,
	readStoredVoter,
	rememberVoter,
} from "../services/Refinement/voterStore";

export interface IVoterBallot {
	vote: ISizingVote;
	voterKey: string | null;
}

export interface IVoterComment {
	comment: ISizingComment;
	voterKey: string | null;
}

interface IVoterSignature {
	voterName?: string;
	voterKey: string | null;
}

/**
 * Who a vote from this browser is cast as. A signed-in voter is their account and needs nothing from the
 * browser; without sign-in the voter names themselves once, and the browser keeps that name and its key.
 */
export const useVoterIdentity = (voterIdentity: VoterIdentity | undefined) => {
	const [voter, setVoter] = useState<IStoredVoter | null>(readStoredVoter);
	const isAccount = voterIdentity === "Account";

	const declareName = useCallback(
		(name: string): IStoredVoter => {
			const declared = rememberVoter(name.trim(), voter?.key ?? null);
			setVoter(declared);
			return declared;
		},
		[voter],
	);

	const signatureFor = useCallback(
		(declared: IStoredVoter | null): IVoterSignature =>
			isAccount || declared === null
				? { voterKey: null }
				: { voterName: declared.name, voterKey: declared.key },
		[isAccount],
	);

	const ballotFor = useCallback(
		(answer: SizingAnswer, declared: IStoredVoter | null): IVoterBallot => {
			const { voterKey, ...name } = signatureFor(declared);
			return { vote: { answer, channel: "Web", ...name }, voterKey };
		},
		[signatureFor],
	);

	const commentFor = useCallback(
		(comment: string, declared: IStoredVoter | null): IVoterComment => {
			const { voterKey, ...name } = signatureFor(declared);
			return { comment: { comment, channel: "Web", ...name }, voterKey };
		},
		[signatureFor],
	);

	return {
		voter,
		asksForName: !isAccount && voter === null,
		changeableName: isAccount ? null : (voter?.name ?? null),
		/** The key reads go out with; an account is known to the server without it. */
		readerKey: isAccount ? null : (voter?.key ?? null),
		declareName,
		ballotFor,
		commentFor,
	};
};
