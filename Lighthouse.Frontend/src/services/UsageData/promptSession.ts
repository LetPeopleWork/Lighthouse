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

const PROMPT_SLOT_KEY = "lighthouse:prompt-slot";

/** Who holds this session's slot, or null while it is still free. */
export const promptSlotHolder = (): PromptOwner | null => {
	try {
		return sessionStorage.getItem(PROMPT_SLOT_KEY) as PromptOwner | null;
	} catch {
		// A browser that will not hold the slot is treated as one where nobody holds it. That lets
		// one prompt through rather than none, which is the right way for a coordination rule to
		// fail: the alternative silences a consent dialog somebody is entitled to be shown.
		return null;
	}
};

/**
 * Takes this session's slot for `owner`, or reports that somebody else already has it.
 * Claiming twice for the same owner succeeds: a re-render must not lose a slot it already holds.
 */
export const claimPromptSlot = (owner: PromptOwner): boolean => {
	const holder = promptSlotHolder();

	if (holder !== null && holder !== owner) {
		return false;
	}

	try {
		sessionStorage.setItem(PROMPT_SLOT_KEY, owner);
	} catch {
		// The claim stands for this render even though nothing recorded it. A browser that cannot
		// remember who holds the slot cannot enforce the rule at all, and refusing to show anything
		// would be enforcing it in the direction that helps nobody.
	}

	return true;
};
