import { createContext, useContext, useLayoutEffect } from "react";
import {
	getFetchRequirementsForWidget,
	type MetricsFetchKey,
} from "./categoryMetadata";

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

const notAskedFor: FetchKeyState = { status: "ready", hasData: true };

export function combineWidgetStatuses(
	statuses: readonly WidgetStatus[],
): WidgetStatus {
	if (statuses.includes("error")) return "error";
	if (statuses.includes("loading")) return "loading";
	return "ready";
}

function inputsOf(widgetKey: string): readonly MetricsFetchKey[] {
	return getFetchRequirementsForWidget(widgetKey) ?? [];
}

// A key with no entry was never asked for, so it holds nothing back.
function stateOf(
	key: MetricsFetchKey,
	keyStates: FetchKeyStates,
): FetchKeyState {
	return keyStates[key] ?? notAskedFor;
}

// While a stepped window waits to be committed, everything the page fetched answers a window the
// reader is leaving, a failure included, so the chart reads as loading whatever its inputs say.
export function widgetStatusFor(
	widgetKey: string,
	keyStates: FetchKeyStates,
	isCommitPending: boolean,
): WidgetStatus {
	if (isCommitPending) return "loading";
	return combineWidgetStatuses(
		inputsOf(widgetKey).map((key) => stateOf(key, keyStates).status),
	);
}

export function widgetHasData(
	widgetKey: string,
	keyStates: FetchKeyStates,
): boolean {
	return inputsOf(widgetKey).every((key) => stateOf(key, keyStates).hasData);
}

function isBehind(query: QueryProgress): boolean {
	return query.isPending || query.isPlaceholderData;
}

function showsAnAnswer(query: QueryProgress): boolean {
	return !query.isPending && !query.isError;
}

function statusOfOne(query: QueryProgress): WidgetStatus {
	if (query.isError) return "error";
	if (isBehind(query)) return "loading";
	return "ready";
}

function statusOf(queries: readonly QueryProgress[]): WidgetStatus {
	return combineWidgetStatuses(queries.map(statusOfOne));
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

/** How a chart that fetches for itself tells its frame where its own request stands. */
type ReportWidgetStatus = (status: WidgetStatus | undefined) => void;

export const WidgetStatusReporterContext =
	createContext<ReportWidgetStatus | null>(null);

// A layout effect lands before the browser paints, so a frame is never painted ready for a chart
// whose request has not started yet. Outside a frame there is nobody to tell.
export function useReportWidgetStatus(status: WidgetStatus): void {
	const report = useContext(WidgetStatusReporterContext);

	useLayoutEffect(() => {
		if (!report) return;
		report(status);
		return () => report(undefined);
	}, [report, status]);
}
