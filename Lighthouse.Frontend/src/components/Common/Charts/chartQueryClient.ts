import { QueryClient } from "@tanstack/react-query";

// Each fetcher a view hands over stands for one owner and date range, so a new fetcher is a new question.
const fetcherIds = new WeakMap<object, number>();
let lastFetcherId = 0;
export const fetcherIdOf = (fetcher: object | undefined): number | null => {
	if (!fetcher) return null;
	let id = fetcherIds.get(fetcher);
	if (id === undefined) {
		lastFetcherId += 1;
		id = lastFetcherId;
		fetcherIds.set(fetcher, id);
	}
	return id;
};

// A chart without a fetcher asks nothing, and may be drawn outside a query provider.
export const withoutAProvider = new QueryClient();
