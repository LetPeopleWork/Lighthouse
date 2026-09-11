/**
 * RED scaffold — Epic 5733 slice 01. DELIVER replaces this body.
 *
 * The throw interpolates its arguments because noUnusedParameters is on: a stub that ignores
 * its props does not compile, and underscore-prefixing them would force a rename in DELIVER.
 */

/**
 * What the instance is doing right now, as far as this browser can tell.
 *
 * `unknown` exists because the state endpoint can fail or be unreachable, and the indicator has to
 * render something. It renders as not-sending: the opposite of `useRbac`, which fails open on
 * purpose. A privacy indicator that guesses "sending" when it does not know would be alarming and
 * wrong; one that guesses "not sending" when it does not know is only wrong.
 */
export type UsageDataSendingState = "sending" | "not-sending" | "unknown";

export interface UsageDataIndicatorProps {
	state: UsageDataSendingState;
	onOpenDecision: () => void;
}

export const UsageDataIndicator = ({
	state,
	onOpenDecision,
}: UsageDataIndicatorProps): React.ReactElement => {
	throw new Error(
		`UsageDataIndicator(state=${state}, onOpenDecision=${typeof onOpenDecision}) is not implemented`,
	);
};

export default UsageDataIndicator;
