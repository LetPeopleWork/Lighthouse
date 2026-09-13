/**
 * One session, one unsolicited prompt.
 *
 * RED scaffold (DISTILL). DELIVER replaces the bodies.
 *
 * Two prompts now arrive uninvited - the survey nudge and the usage data dialog - and both are
 * relevant to the same people on overlapping clocks, so meeting each other is the normal case
 * rather than an edge one. Showing both at once is not merely untidy: consent asked alongside an
 * unrelated request is not freely given, so the two must never share a session.
 *
 * The slot is claimed rather than negotiated, because neither prompt can see the other's
 * eligibility - the survey nudge works it out in the browser from facts only a signed-in caller
 * can read, and usage data is told the answer by the server. First claim wins. On a genuine tie
 * the survey nudge takes it, because it claims while rendering and usage data claims from an
 * effect, which React runs afterwards.
 */

export type PromptOwner = "survey-nudge" | "usage-data";

/**
 * Takes this session's slot for `owner`, or reports that somebody else already has it.
 * Claiming twice for the same owner succeeds: a re-render must not lose a slot it already holds.
 */
export const claimPromptSlot = (owner: PromptOwner): boolean => {
	throw new Error(
		`claimPromptSlot is not implemented — RED scaffold. Asked on behalf of ${owner}.`,
	);
};

/** Who holds this session's slot, or null while it is still free. */
export const promptSlotHolder = (): PromptOwner | null => {
	throw new Error("promptSlotHolder is not implemented — RED scaffold.");
};
