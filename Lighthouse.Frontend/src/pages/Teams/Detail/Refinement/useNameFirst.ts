import { useCallback, useState } from "react";
import type { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";

type NamingIdentity = Pick<
	ReturnType<typeof useVoterIdentity>,
	"voter" | "asksForName" | "declareName"
>;

/**
 * A vote or a comment from somebody without sign-in who has not named themselves yet waits while they are
 * asked for a name, and goes out under that name once they give it. Cancelling the prompt drops it, and so
 * does moving to another Team: what waits was meant for the Team it was asked on.
 */
export const useNameFirst = <TWaiting>(
	{ voter, asksForName, declareName }: NamingIdentity,
	send: (waiting: TWaiting, declared: IStoredVoter | null) => void,
	teamId: number,
) => {
	const [held, setHeld] = useState<{
		chosen: TWaiting;
		teamId: number;
	} | null>(null);
	const waiting = held?.teamId === teamId ? held.chosen : null;

	const submit = useCallback(
		(chosen: TWaiting) => {
			if (asksForName) {
				setHeld({ chosen, teamId });
				return;
			}

			send(chosen, voter);
		},
		[asksForName, send, voter, teamId],
	);

	const submitUnderName = (name: string) => {
		if (waiting !== null) {
			send(waiting, declareName(name));
		}
		setHeld(null);
	};

	return {
		submit,
		isAskingForName: waiting !== null,
		submitUnderName,
		cancel: () => setHeld(null),
	};
};
