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

/**
 * The claim, held in memory as well as in storage.
 *
 * Storage is where it survives a reload; this is where it survives a browser that refuses storage
 * at all. Both prompts run in the same page, so this is enough to keep them apart even when nothing
 * can be written down - and keeping them apart is the point, because a consent request put beside
 * an unrelated one is not freely given.
 *
 * Treating a storage failure as "nobody holds it" was the earlier behaviour and did the opposite of
 * what it claimed: it answered "free" to both askers, so a private window got the survey popup and
 * the consent dialog at the same time.
 */
let heldInThisPage: PromptOwner | null = null;

/** Who holds this session's slot, or null while it is still free. */
export const promptSlotHolder = (): PromptOwner | null => {
	try {
		// Storage answers whenever it can, and the copy in memory is kept level with it rather than
		// sitting alongside as a second source of truth - so clearing storage really does free the
		// slot, and a claim made while storage was refusing does not outlive it.
		heldInThisPage = sessionStorage.getItem(
			PROMPT_SLOT_KEY,
		) as PromptOwner | null;

		return heldInThisPage;
	} catch {
		return heldInThisPage;
	}
};

/**
 * Whether somebody other than `owner` is already showing an unsolicited prompt this session.
 *
 * Both prompts have to ask this, and asking it as "is the slot taken, and not by me" in each of
 * them is one rule written twice - the kind that drifts when only one of the two is edited.
 */
export const slotIsHeldByAnother = (owner: PromptOwner): boolean => {
	const holder = promptSlotHolder();

	return holder !== null && holder !== owner;
};

/**
 * Takes this session's slot for `owner`, or reports that somebody else already has it.
 * Claiming twice for the same owner succeeds: a re-render must not lose a slot it already holds.
 */
export const claimPromptSlot = (owner: PromptOwner): boolean => {
	if (slotIsHeldByAnother(owner)) {
		return false;
	}

	heldInThisPage = owner;

	try {
		sessionStorage.setItem(PROMPT_SLOT_KEY, owner);
	} catch {
		// Only persistence is lost. The claim above already holds for this page, which is where the
		// other prompt will ask about it.
	}

	return true;
};
