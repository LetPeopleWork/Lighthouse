/**
 * Whether this browser should be shown the usage data dialog without having asked for it.
 *
 * RED scaffold (DISTILL). DELIVER replaces the body.
 *
 * Almost every part of this question is answered on the server, because the facts it rests on -
 * how long this instance has been installed, whether an administrator has switched the asking off,
 * what this browser previously answered and how long ago - are either not the browser's to know or
 * not the browser's to be trusted with. What is left here is the one thing the server cannot see:
 * a browser that was shown the dialog and closed it without answering leaves no record anywhere
 * else, because it has no consent row to record anything against.
 */

export interface AskEligibilityInput {
	/** The server's derived answer: install age reached, not switched off, and this browser is due. */
	mayAsk: boolean;
	/** What this browser previously answered, or null when it has never answered. */
	decision: string | null;
	/** When this browser was last shown the dialog unprompted, or null if it never was. */
	lastAskedAt: string | null;
	/** Whether another prompt has already taken this session's one slot. */
	promptSlotTaken: boolean;
}

export interface AskDecision {
	shouldAsk: boolean;
}

export const evaluateAskEligibility = (
	input: AskEligibilityInput,
): AskDecision => {
	// Outranks being due. Consent asked alongside an unrelated request is not freely given, so a
	// browser the server says is due still waits for the next session.
	if (input.promptSlotTaken) {
		return { shouldAsk: false };
	}

	if (!input.mayAsk) {
		return { shouldAsk: false };
	}

	// The marker is read only for a browser that has never answered, and that restriction is the
	// load-bearing part. A browser that declined carries a marker from the day it was asked; three
	// months later the server says it is due again, and a marker left over from the first ask must
	// not be what silences the second.
	if (input.decision === null) {
		return { shouldAsk: input.lastAskedAt === null };
	}

	return { shouldAsk: true };
};
