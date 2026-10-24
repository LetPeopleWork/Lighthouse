import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";

/** What a body of the dialog needs to know of one of its queries to say what it is doing. */
export interface DialogQueryState {
	data: unknown;
	isError: boolean;
	isFetching: boolean;
}

/**
 * What a body of the dialog shows for one of its queries. A query that has not started, because the licence, the
 * sources or the open flag are not in yet, has neither data nor an error and reads as loading, never as ready: the
 * reader is waiting for it, whatever the reason it has not been sent.
 */
export function dialogQueryStatusOf(query: DialogQueryState): WidgetStatus {
	if (query.isFetching) {
		return "loading";
	}
	if (query.isError) {
		return "error";
	}
	if (query.data === undefined) {
		return "loading";
	}
	return "ready";
}

/** One of the dialog's own answers, as far as the form needs to know it. */
export interface DialogAnswer<T> {
	status: WidgetStatus;
	data: T | undefined;
}

export function dialogAnswerOf<T>(
	query: DialogQueryState & { data: T | undefined },
): DialogAnswer<T> {
	const status = dialogQueryStatusOf(query);
	return { status, data: status === "ready" ? query.data : undefined };
}
