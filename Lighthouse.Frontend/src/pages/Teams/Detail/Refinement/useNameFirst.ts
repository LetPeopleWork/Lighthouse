import { useCallback, useState } from "react";
import type { useVoterIdentity } from "../../../../hooks/useVoterIdentity";
import type { IStoredVoter } from "../../../../services/Refinement/voterStore";

type NamingIdentity = Pick<
	ReturnType<typeof useVoterIdentity>,
	"voter" | "asksForName" | "declareName"
>;

/**
 * A vote or a comment from somebody without sign-in who has not named themselves yet waits while they are
 * asked for a name, and goes out under that name once they give it. Cancelling the prompt drops it.
 */
export const useNameFirst = <TWaiting>(
	{ voter, asksForName, declareName }: NamingIdentity,
	send: (waiting: TWaiting, declared: IStoredVoter | null) => void,
) => {
	const [waiting, setWaiting] = useState<TWaiting | null>(null);

	const submit = useCallback(
		(chosen: TWaiting) => {
			if (asksForName) {
				setWaiting(chosen);
				return;
			}

			send(chosen, voter);
		},
		[asksForName, send, voter],
	);

	const submitUnderName = (name: string) => {
		if (waiting !== null) {
			send(waiting, declareName(name));
		}
		setWaiting(null);
	};

	return {
		submit,
		isAskingForName: waiting !== null,
		submitUnderName,
		cancel: () => setWaiting(null),
	};
};
