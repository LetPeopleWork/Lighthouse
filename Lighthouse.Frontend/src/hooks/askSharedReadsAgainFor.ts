import type { Query, QueryClient } from "@tanstack/react-query";
import { PAGE_FRESH_READS } from "./pageFreshQueryKey";
import { pageOfTheLatestAskFor } from "./pageOfTheLatestAsk";
import { LICENSE_STATUS_KEY } from "./useLicenseRestrictions";

const isSharedRead = (query: Query): boolean =>
	query.queryKey[0] === PAGE_FRESH_READS[0] ||
	query.queryKey[0] === LICENSE_STATUS_KEY[0];

/** Asks every shared read again whose latest ask belongs to a page before this one (the page-fresh reads and the
 * license status); a request still on its way from such a page is cancelled, so its answer never lands as the
 * new page's. */
export const askSharedReadsAgainFor = (
	queryClient: QueryClient,
	page: number,
): Promise<void> => {
	const askedBefore = {
		predicate: (query: Query) =>
			isSharedRead(query) && pageOfTheLatestAskFor(query) < page,
	};
	// Invalidating alone would join a first ask still on its way instead of replacing it.
	return queryClient
		.cancelQueries(askedBefore)
		.then(() => queryClient.invalidateQueries(askedBefore));
};
