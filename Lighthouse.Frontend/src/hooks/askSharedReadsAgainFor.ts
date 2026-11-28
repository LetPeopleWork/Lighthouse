import type { Query, QueryClient } from "@tanstack/react-query";
import { PAGE_FRESH_READS } from "./pageFreshQueryKey";
import { pageOfTheLatestAskFor } from "./pageOfTheLatestAsk";

/** Asks every shared read again whose latest ask belongs to a page before this one; a request still on its way
 * from such a page is cancelled, so its answer never lands as the new page's. */
export const askSharedReadsAgainFor = (
	queryClient: QueryClient,
	page: number,
): Promise<void> => {
	const askedBefore = {
		queryKey: PAGE_FRESH_READS,
		predicate: (query: Query) => pageOfTheLatestAskFor(query) < page,
	};
	// Invalidating alone would join a first ask still on its way instead of replacing it.
	return queryClient
		.cancelQueries(askedBefore)
		.then(() => queryClient.invalidateQueries(askedBefore));
};
