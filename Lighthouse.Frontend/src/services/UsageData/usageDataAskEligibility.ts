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
	/** How long a browser that was asked and did not answer is left alone. The server's number. */
	reAskAfterDays: number;
	/** Whether another prompt has already taken this session's one slot. */
	promptSlotTaken: boolean;
	now?: Date;
}

export interface AskDecision {
	shouldAsk: boolean;
}

const MILLISECONDS_PER_DAY = 24 * 60 * 60 * 1000;

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

	// Read for every browser, not only one that has never answered.
	//
	// It used to be consulted only when there was no recorded decision, on the reasoning that a
	// browser which had answered carried a marker from its first ask and that stale marker must not
	// silence a later one. That cannot happen: the marker is rewritten every time the question is
	// put, so it always holds the most recent ask, and it is measured against the same window the
	// server uses.
	//
	// What the restriction did instead was remove this browser's only local defence. Telling the
	// server it has been asked is one fire-and-forget request; when it does not land - the tab
	// closed, the machine slept, an anonymous rate limit shared across everyone behind a proxy - the
	// server goes on saying "due" and the browser had been instructed to ignore the note it had just
	// written to itself. The dialog then returned on every page load until a request happened to
	// succeed.
	return { shouldAsk: askedLongEnoughAgo(input) };
};

/**
 * Whether a browser that was shown the dialog and walked away has waited out its quiet period.
 *
 * The same period a refusal gets, and deliberately so. Closing the dialog is not a decision - the
 * reader did not refuse, they declined to engage - so silencing it harder than an explicit "no"
 * would have the weaker signal producing the stronger effect, and would quietly shrink the
 * population the whole feature exists to measure.
 */
const askedLongEnoughAgo = (input: AskEligibilityInput): boolean => {
	const askedAt = Date.parse(input.lastAskedAt ?? "");
	const now = (input.now ?? new Date()).getTime();

	// Three ways to have no usable record of when this browser was last asked: never asked at all,
	// a value nothing can read, and one stamped ahead of now by a clock that was wrong - a restored
	// snapshot, a flat battery, a boot before the network settled. They were separate branches and
	// are one, because they mean the same thing and want the same answer.
	//
	// That answer is to ask. The tempting reading for the latter two is "recently, so leave it
	// alone", and it is a trap: nothing rewrites the marker except an ask, so a browser silenced on
	// an unusable value stays silenced for as long as the value survives - for ever, in the
	// unreadable case. Asking once re-stamps the marker from a clock that works.
	//
	// Written as a branch rather than left to the arithmetic below, which would reach the same
	// silence by accident: NaN and a negative elapsed time are each never greater than the window.
	if (Number.isNaN(askedAt) || askedAt > now) {
		return true;
	}

	return now - askedAt >= input.reAskAfterDays * MILLISECONDS_PER_DAY;
};
