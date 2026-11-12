import type { ReactNode } from "react";
import {
	combineWidgetStatuses,
	type FetchKeyStates,
	type WidgetStatus,
	widgetHasData,
	widgetStatusFor,
} from "./widgetStatus";

// The aging chart lays its columns out from the Doing states. Drawn before they arrive, it would
// be an empty chart that reads as "nothing in progress".
export function columnsStatusOf(
	widgetKey: string,
	doingStatesStatus: WidgetStatus,
): WidgetStatus {
	return widgetKey === "aging" ? doingStatesStatus : "ready";
}

export function frameStateOf(
	widgetKey: string,
	fetchStates: FetchKeyStates,
	isCommitPending: boolean,
	columnsStatus: WidgetStatus,
) {
	return {
		status: combineWidgetStatuses([
			widgetStatusFor(widgetKey, fetchStates, isCommitPending),
			columnsStatus,
		]),
		hasContentToDim:
			columnsStatus === "ready" && widgetHasData(widgetKey, fetchStates),
	};
}

// Estimation vs. Cycle Time appears only once its data says estimation is set up. A frame put up
// for it early could be taken away again.
const placedOnlyOnceTheirDataSaysSo = new Set(["estimationVsCycleTime"]);

// Every chart the category places takes its frame from the first render, so the layout never
// reshuffles as the data arrives. The one above keeps a frame only once it has something to
// show, or to say that its data could not be loaded.
export function isPlaced(
	widgetKey: string,
	node: ReactNode,
	fetchStates: FetchKeyStates,
): boolean {
	return (
		node != null ||
		!placedOnlyOnceTheirDataSaysSo.has(widgetKey) ||
		widgetStatusFor(widgetKey, fetchStates, false) === "error"
	);
}

// Until a chart's data has arrived once, its frame shows the lone spinner rather than a chart
// drawn from empty defaults.
export function bodyOf(
	widgetKey: string,
	node: ReactNode,
	fetchStates: FetchKeyStates,
	columnsStatus: WidgetStatus,
): ReactNode {
	return columnsStatus === "ready" && widgetHasData(widgetKey, fetchStates)
		? node
		: null;
}
