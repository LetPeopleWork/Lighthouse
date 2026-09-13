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
