import type { MetricsFetchKey } from "./categoryMetadata";

const NOT_YET_IMPLEMENTED = "Not yet implemented -- RED scaffold";

export type WidgetStatus = "loading" | "error" | "ready";

export type FetchKeyState = {
	readonly status: WidgetStatus;
	/** True once the key has answered some window, so there is an older chart to dim. */
	readonly hasData: boolean;
};

export type FetchKeyStates = Partial<Record<MetricsFetchKey, FetchKeyState>>;

/** What a widget needs to know about one request: is it still on its way, and did it fail. */
export type QueryProgress = {
	readonly isPending: boolean;
	readonly isError: boolean;
	readonly isPlaceholderData: boolean;
};

export function combineWidgetStatuses(
	_statuses: readonly WidgetStatus[],
): WidgetStatus {
	throw new Error(NOT_YET_IMPLEMENTED);
}

export function widgetStatusFor(
	_widgetKey: string,
	_keyStates: FetchKeyStates,
	_isCommitPending: boolean,
): WidgetStatus {
	throw new Error(NOT_YET_IMPLEMENTED);
}

export function widgetHasData(
	_widgetKey: string,
	_keyStates: FetchKeyStates,
): boolean {
	throw new Error(NOT_YET_IMPLEMENTED);
}

const notAskedFor: FetchKeyState = { status: "ready", hasData: true };

function isBehind(query: QueryProgress): boolean {
	return query.isPending || query.isPlaceholderData;
}

function showsAnAnswer(query: QueryProgress): boolean {
	return !query.isPending && !query.isError;
}

function statusOf(queries: readonly QueryProgress[]): WidgetStatus {
	if (queries.some((query) => query.isError)) return "error";
	if (queries.some(isBehind)) return "loading";
	return "ready";
}

// A query on placeholder data is still loading: it shows the previous window's answer while the
// selected window has none yet, and that answer is the older picture a chart dims.
export function fetchKeyStateOf(
	queries: readonly QueryProgress[],
	applicable: boolean,
): FetchKeyState {
	if (!applicable) return notAskedFor;
	return { status: statusOf(queries), hasData: queries.every(showsAnAnswer) };
}

export function useReportWidgetStatus(_status: WidgetStatus): void {
	throw new Error(NOT_YET_IMPLEMENTED);
}
