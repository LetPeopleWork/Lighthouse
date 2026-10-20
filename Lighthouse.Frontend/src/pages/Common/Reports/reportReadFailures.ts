import { ApiError } from "../../../services/Api/ApiError";

/** A report that no longer exists, or that a newer version made, stays that way however often it is asked for. */
const REFUSALS_THAT_DO_NOT_CHANGE = new Set<string | number>([404, 422]);

/** Whether a failed report read is one that asking again will not change. */
export const isAFinalReadRefusal = (error: unknown): boolean =>
	error instanceof ApiError && REFUSALS_THAT_DO_NOT_CHANGE.has(error.code);

/** Retries a report read as the app usually does, except when asking again would only delay saying it failed for good. */
export const retriesUnlessFinal =
	(usualRetry: unknown) =>
	(failureCount: number, error: Error): boolean => {
		if (isAFinalReadRefusal(error)) return false;
		return typeof usualRetry === "number"
			? failureCount < usualRetry
			: usualRetry === true;
	};
