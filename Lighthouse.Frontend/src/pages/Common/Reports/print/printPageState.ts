import { isAFinalReadRefusal } from "../reportReadFailures";

/**
 * What the print page shows, decided from what it has: still preparing; the report could not be read, with a way to
 * try again; the report was read but part of the page was not, with a way to try again; a failure nothing can mend;
 * or ready to print.
 */
export type PrintPageState =
	| "preparing"
	| "failedRetryable"
	| "failedPart"
	| "failedFinal"
	| "ready";

/** Where something the page asks the browser to load stands: still being asked for, in, or it could not be loaded. */
export type PrintLoadState = "pending" | "loaded" | "failed";

/** Everything the print page's state depends on, and nothing else. */
export interface PrintPageInputs {
	/** When the report answer in hand arrived, or null when there is none. */
	readonly fetchedAt: number | null;
	/** When the print page was opened; an answer from before it is held but never printed. */
	readonly mountedAt: number;
	readonly reportError: unknown;
	/** Whether the report is being read at this moment, as Retry does; a failure that read may mend is not shown meanwhile. */
	readonly reportFetching?: boolean;
	readonly ownerName: string | null;
	readonly ownerError: unknown;
	readonly termsLoading: boolean;
	readonly termsError: unknown;
	readonly fonts: PrintLoadState;
	/** The Lighthouse mark in the footer, a picture the browser fetches. */
	readonly mark: PrintLoadState;
	/** Whether the time a hidden frame allows has run out; the print page opened by its address has no such limit. */
	readonly timedOut?: boolean;
}

/** Whether the report answer in hand arrived after the page opened; only such an answer is ever printed. */
export const isAFreshAnswer = (
	fetchedAt: number | null,
	mountedAt: number,
): boolean => fetchedAt !== null && fetchedAt >= mountedAt;

const isAnError = (error: unknown): boolean =>
	error !== null && error !== undefined;

/** What the report read alone decides, or nothing when it has not failed. */
const reportReadStateOf = (
	inputs: PrintPageInputs,
): PrintPageState | undefined => {
	if (isAFinalReadRefusal(inputs.reportError)) return "failedFinal";
	if (!isAnError(inputs.reportError)) return undefined;
	const stillReadingInTime =
		inputs.reportFetching === true && inputs.timedOut !== true;
	return stillReadingInTime ? "preparing" : "failedRetryable";
};

/** A page still preparing when time runs out failed on the report when no fresh answer came, else on a part of it. */
const whenTimeRanOut = (inputs: PrintPageInputs): PrintPageState =>
	isAFreshAnswer(inputs.fetchedAt, inputs.mountedAt)
		? "failedPart"
		: "failedRetryable";

export const printPageStateOf = (inputs: PrintPageInputs): PrintPageState => {
	const decidedByTheReport = reportReadStateOf(inputs);
	if (decidedByTheReport !== undefined) return decidedByTheReport;
	if (
		isAnError(inputs.ownerError) ||
		isAnError(inputs.termsError) ||
		inputs.fonts === "failed" ||
		inputs.mark === "failed"
	)
		return "failedPart";

	const everythingIsIn =
		isAFreshAnswer(inputs.fetchedAt, inputs.mountedAt) &&
		inputs.ownerName !== null &&
		!inputs.termsLoading &&
		inputs.fonts === "loaded" &&
		inputs.mark === "loaded";
	if (everythingIsIn) return "ready";
	return inputs.timedOut === true ? whenTimeRanOut(inputs) : "preparing";
};
