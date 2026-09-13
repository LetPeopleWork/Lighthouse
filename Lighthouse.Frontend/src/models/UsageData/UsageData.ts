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
	/**
	 * Whether to put the question to this browser now, unprompted.
	 *
	 * Worked out entirely on the server, from how long the instance has been installed, whether an
	 * administrator has switched the asking off, and what this browser last answered and when. None
	 * of those are readable here - the install timestamp needs authentication and this endpoint has
	 * none - and a privacy gate settled against a clock the browser owns would not be a gate.
	 *
	 * The browser adds exactly one thing the server cannot know: that it was shown the dialog once
	 * and closed it without answering, which leaves no consent row to have recorded it against.
	 */
	mayAsk: boolean;
	/**
	 * How long a browser that was shown the dialog and did not answer is left alone.
	 *
	 * The same number for every caller, straight from the instance's configuration, so it says
	 * nothing about this browser or the licence. It has to travel because the two halves of that
	 * cadence live apart: only the browser knows it closed the dialog, and only the server knows how
	 * long that should count for.
	 */
	reAskAfterDays: number;
}

/**
 * Which page was opened, said as a choice from this list rather than as an address.
 *
 * The Team and Portfolio detail pages are the two whose address holds both a customer's identifier
 * and the name of the view somebody opened. The identifier is theirs and must never leave the
 * browser; the view is one of the few things worth knowing. Naming the pair as a single choice keeps
 * the second without there ever being a value that could carry the first.
 *
 * The values are the names rather than numbers because the server answers and accepts these as
 * text - a numbered mirror would compare false against every response.
 */
export const UsageDataRouteKey = {
	TeamDetail_Features: "TeamDetail_Features",
	TeamDetail_Forecasts: "TeamDetail_Forecasts",
	TeamDetail_Metrics: "TeamDetail_Metrics",
	TeamDetail_Settings: "TeamDetail_Settings",
	TeamDetail_Access: "TeamDetail_Access",
	PortfolioDetail_Features: "PortfolioDetail_Features",
	PortfolioDetail_Metrics: "PortfolioDetail_Metrics",
	PortfolioDetail_Deliveries: "PortfolioDetail_Deliveries",
	PortfolioDetail_Settings: "PortfolioDetail_Settings",
	PortfolioDetail_Access: "PortfolioDetail_Access",
} as const;

export type UsageDataRouteKey =
	(typeof UsageDataRouteKey)[keyof typeof UsageDataRouteKey];

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
