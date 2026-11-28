import type { Query } from "@tanstack/react-query";

// The page each shared read was last asked on, also while that ask is still on its way. A component that appears
// on a new page asks before the page-move refresh runs; this lets the refresh leave that ask alone instead of
// cancelling it and asking a second time. Kept per query object, so it goes with the cache it belongs to.
const pageOfTheLatestAsk = new WeakMap<Query, number>();

export const recordAskOnPage = (query: Query, page: number): void => {
	pageOfTheLatestAsk.set(query, page);
};

/** The page the query was last asked on, or -1 when it was never asked. */
export const pageOfTheLatestAskFor = (query: Query): number =>
	pageOfTheLatestAsk.get(query) ?? -1;
