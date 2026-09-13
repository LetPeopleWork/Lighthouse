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

	// The marker is read only for a browser that has never answered, and that restriction is the
	// load-bearing part. A browser that declined carries a marker from the day it was asked; months
	// later the server says it is due again, and a marker left over from the first ask must not be
	// what silences the second.
	if (input.decision === null) {
		return { shouldAsk: askedLongEnoughAgo(input) };
	}

	return { shouldAsk: true };
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
	if (input.lastAskedAt === null) {
		return true;
	}

	const askedAt = Date.parse(input.lastAskedAt);

	if (Number.isNaN(askedAt)) {
		// Something wrote a value nothing can read. Treating it as "never asked" would put the
		// dialog back in front of somebody on every visit, so it counts as a recent ask instead -
		// the failure that costs a browser one cycle rather than every one.
		return false;
	}

	const quietFor = input.reAskAfterDays * MILLISECONDS_PER_DAY;

	return (input.now ?? new Date()).getTime() - askedAt >= quietFor;
};
