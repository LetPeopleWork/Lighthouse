import type { UseQueryResult } from "@tanstack/react-query";
import type { WorkItemsDialogStatus } from "./WorkItemsDialog";

const NO_ROWS: never[] = [];

export interface WorkItemsDialogRows<T> {
	readonly status: WorkItemsDialogStatus;
	readonly items: T[];
}

/**
 * What a dialog shows for the query that fetches its rows. While the query is still fetching, the
 * dialog counts as loading even if an earlier answer is in hand, so it never passes off an old list
 * as the current one.
 */
export function dialogRowsOf<T>(
	query: UseQueryResult<T[]>,
): WorkItemsDialogRows<T> {
	if (query.isError) {
		return { status: "error", items: NO_ROWS };
	}
	if (query.isSuccess && !query.isFetching) {
		return { status: "ready", items: query.data };
	}
	return { status: "loading", items: NO_ROWS };
}
