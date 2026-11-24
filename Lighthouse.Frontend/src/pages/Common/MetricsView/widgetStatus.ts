import type { MetricsFetchKey } from "../../../hooks/metricsFetchKeys";
import type { QueryProgress } from "../../../hooks/useMetricsQuery";
import {
	combineWidgetStatuses,
	type WidgetStatus,
} from "../../../hooks/useReportWidgetStatus";
import { getFetchRequirementsForWidget } from "./categoryMetadata";

export {
	type ChartReport,
	combineWidgetStatuses,
	useReportWidgetStatus,
	type WidgetStatus,
	WidgetStatusReporterContext,
} from "../../../hooks/useReportWidgetStatus";

export type FetchKeyState = {
	readonly status: WidgetStatus;
	/** True once the key has answered some window, so there is an older chart to dim. */
	readonly hasData: boolean;
};

export type FetchKeyStates = Partial<Record<MetricsFetchKey, FetchKeyState>>;

const notAskedFor: FetchKeyState = { status: "ready", hasData: true };

/** Where a view's one query stands; a refetch reads as loading even after an earlier failure. */
export function queryStatusOf(
	isFetching: boolean,
	isError: boolean,
): WidgetStatus {
	if (isFetching) return "loading";
	return isError ? "error" : "ready";
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

// A failed request can still hold the answer its chart last drew. Keeping that chart mounted
// behind the could-not-load note is what lets it come back with the settings the reader made in it.
function holdsAnAnswer(query: QueryProgress): boolean {
	return query.data !== undefined;
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
	return { status: statusOf(queries), hasData: queries.every(holdsAnAnswer) };
}

/** A chart that fetches its own series is ready once one has come back, whatever its length. */
export function seriesStatusOf(
	series: readonly unknown[] | null,
	failed: boolean,
): WidgetStatus {
	if (series !== null) return "ready";
	return failed ? "error" : "loading";
}
