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
	throw new Error(
		`evaluateAskEligibility is not implemented — RED scaffold. Asked with mayAsk=${input.mayAsk}, decision=${input.decision}, lastAskedAt=${input.lastAskedAt}, promptSlotTaken=${input.promptSlotTaken}.`,
	);
};
