import type { MetricsFetchKey } from "./categoryMetadata";

// RED scaffold: the signatures the specifications import, with no behaviour yet. Nothing in the
// app calls these until the loading state is built, so the dashboard behaves exactly as before.
export const __SCAFFOLD__ = true;

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

export function fetchKeyStateOf(
	_queries: readonly QueryProgress[],
	_applicable: boolean,
): FetchKeyState {
	throw new Error(NOT_YET_IMPLEMENTED);
}

export function useReportWidgetStatus(_status: WidgetStatus): void {
	throw new Error(NOT_YET_IMPLEMENTED);
}
