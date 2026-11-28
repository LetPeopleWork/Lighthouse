import type {
	Query,
	QueryFunctionContext,
	QueryKey,
} from "@tanstack/react-query";

// The page each shared read was last asked on, also while that ask is still on its way. A component that appears
// on a new page asks before the page-move refresh runs; this lets the refresh leave that ask alone instead of
// cancelling it and asking a second time. Kept per query object, so it goes with the cache it belongs to.
const pageOfTheLatestAsk = new WeakMap<Query, number>();

export const recordAskOnPage = (query: Query, page: number): void => {
	pageOfTheLatestAsk.set(query, page);
};

/** Records the page on the query a query function is about to ask for; call it first thing in the function. */
export const recordAskOnPageFor = (
	{ client, queryKey }: QueryFunctionContext<QueryKey>,
	page: number,
): void => {
	const asked = client.getQueryCache().find({ queryKey, exact: true });
	if (asked) {
		recordAskOnPage(asked, page);
	}
};

/** The page the query was last asked on, or -1 when it was never asked. */
export const pageOfTheLatestAskFor = (query: Query): number =>
	pageOfTheLatestAsk.get(query) ?? -1;
