import { useCallback, useState } from "react";
import type {
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

	const ballotFor = useCallback(
		(answer: SizingAnswer, declared: IStoredVoter | null): IVoterBallot => {
			if (isAccount || declared === null) {
				return { vote: { answer, channel: "Web" }, voterKey: null };
			}

			return {
				vote: { answer, channel: "Web", voterName: declared.name },
				voterKey: declared.key,
			};
		},
		[isAccount],
	);

	return {
		voter,
		asksForName: !isAccount && voter === null,
		changeableName: isAccount ? null : (voter?.name ?? null),
		declareName,
		ballotFor,
	};
};
