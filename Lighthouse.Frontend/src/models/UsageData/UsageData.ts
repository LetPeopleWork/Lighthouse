/** The two answers a person can give. Withdrawal is a separate act, not a third button. */
export type UsageDataDecisionValue = "granted" | "declined";

/**
 * What the server says about this instance and this browser.
 *
 * `decision` is null for a browser that has not answered - and also for one presenting a token this
 * instance never minted. The two are indistinguishable on purpose: an endpoint that told them apart
 * would let anyone find out whether a given token is real.
 */
export interface IUsageDataState {
	sending: boolean;
	decision: string | null;
	willAskAgain: boolean;
}

export const USAGE_DATA_DOCS_URL =
	"https://docs.lighthouse.letpeople.work/settings/usagedata.html";

/**
 * The one promise the dialog makes about content, rather than a list of what is sent.
 *
 * What *is* sent lives on the linked page instead: it grows as the feature does, and a list inside
 * a dialog goes stale silently while still looking authoritative. This list is the opposite - it is
 * a boundary, so it changes rarely and a reader deciding in the moment needs it in front of them.
 *
 * The dialog's own tests deliberately keep a separate copy. A test that imported this would only be
 * asserting that the code equals itself.
 */
export const USAGE_DATA_NEVER_SENT = [
	"work item titles",
	"queries",
	"names",
	"URLs",
	"email addresses",
	"or any free text you have typed",
] as const;
