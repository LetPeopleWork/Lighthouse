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

/**
 * The consent copy, in one place.
 *
 * Three things have to say the same thing: this dialog, the settings page, and the payload the
 * backend actually sends. They are checked against each other at build time, and the check needs
 * something to point at - a list typed out at the place it is rendered has nowhere to be compared
 * from, and drifts the first time one of the three is edited alone.
 *
 * The dialog's own tests deliberately keep a separate copy. A test that imported these would only
 * be asserting that the code equals itself.
 */
export const USAGE_DATA_COLLECTOR_NAME = "PostHog";

export const USAGE_DATA_RESIDENCY = "Frankfurt, Germany";

export const USAGE_DATA_DOCS_URL =
	"https://docs.lighthouse.letpeople.work/settings/usagedata.html";

/** Exactly what leaves the instance. There is no sixth field. */
export const USAGE_DATA_SENT_FIELDS = [
	"A random identifier for this instance",
	"Lighthouse version",
	"How Lighthouse is deployed",
	"Whether the licence is Community or Premium",
	"When the data was sent",
] as const;

/** Said out loud rather than left to be inferred from the list above. */
export const USAGE_DATA_NEVER_SENT = [
	"work item titles",
	"queries",
	"names",
	"URLs",
	"email addresses",
	"free text",
] as const;
