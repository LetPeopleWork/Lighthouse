import { ApiError } from "../../../../services/Api/ApiError";

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

/** Where the fonts the page shows stand: still being asked for, all found, or one could not be loaded. */
export type PrintFontsState = "pending" | "loaded" | "failed";

/** Everything the print page's state depends on, and nothing else. */
export interface PrintPageInputs {
	/** When the report answer in hand arrived, or null when there is none. */
	readonly fetchedAt: number | null;
	/** When the print page was opened; an answer from before it is held but never printed. */
	readonly mountedAt: number;
	readonly reportError: unknown;
	readonly ownerName: string | null;
	readonly ownerError: unknown;
	readonly termsLoading: boolean;
	readonly termsError: unknown;
	readonly fonts: PrintFontsState;
}

/** Whether the report answer in hand arrived after the page opened; only such an answer is ever printed. */
export const isAFreshAnswer = (
	fetchedAt: number | null,
	mountedAt: number,
): boolean => fetchedAt !== null && fetchedAt >= mountedAt;

/** A report that no longer exists, or that a newer version made, stays that way however often it is asked for. */
const REFUSALS_THAT_DO_NOT_CHANGE = new Set<string | number>([404, 422]);

const isAnError = (error: unknown): boolean =>
	error !== null && error !== undefined;

const isFinal = (reportError: unknown): boolean =>
	reportError instanceof ApiError &&
	REFUSALS_THAT_DO_NOT_CHANGE.has(reportError.code);

export const printPageStateOf = (inputs: PrintPageInputs): PrintPageState => {
	if (isFinal(inputs.reportError)) return "failedFinal";
	if (isAnError(inputs.reportError)) return "failedRetryable";
	if (
		isAnError(inputs.ownerError) ||
		isAnError(inputs.termsError) ||
		inputs.fonts === "failed"
	)
		return "failedPart";

	const everythingIsIn =
		isAFreshAnswer(inputs.fetchedAt, inputs.mountedAt) &&
		inputs.ownerName !== null &&
		!inputs.termsLoading &&
		inputs.fonts === "loaded";
	return everythingIsIn ? "ready" : "preparing";
};
