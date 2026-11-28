import {
	type Query,
	type QueryClient,
	type QueryKey,
	useQuery,
} from "@tanstack/react-query";
import { createContext, useCallback, useContext, useState } from "react";
import { fireAndForget } from "../utils/fireAndForget";

/** Every shared page read lives under this key, so a page move can ask them all again at once. */
export const PAGE_FRESH_READS = ["page-fresh"] as const;

/** The number of the page the user is on: 0 for the first page, one more after each move. */
export const PageMovesContext = createContext<number>(0);

/** An answer together with the page it was asked on, so a component can tell whether it is its own. */
export interface PageFreshAnswer<T> {
	answer: T;
	askedOnPage: number;
}

/** A failed ask, together with the page it was asked on, so a later page knows to ask again. */
export class PageFreshFailure extends Error {
	constructor(
		readonly askedOnPage: number,
		readonly reason: unknown,
	) {
		super(`The shared read asked on page ${askedOnPage} failed.`);
		this.name = "PageFreshFailure";
	}
}

export interface PageFreshRead<T> {
	/** The latest good answer asked on or after the page the component appeared on; undefined while there is none. */
	answer: T | undefined;
	/** True while the component has no answer of its own and an ask is on its way. */
	isLoading: boolean;
	/** True when the component has no answer of its own, nothing is on its way and the latest ask failed. */
	isFailed: boolean;
	/** Asks again once, for every component that shares the read. */
	askAgain: () => void;
}

export const pageFreshQueryKey = (key: QueryKey): QueryKey => [
	...PAGE_FRESH_READS,
	...key,
];

// The page each shared read was last asked on, also while that ask is still on its way. A component that appears
// on a new page asks before the page-move refresh runs; this lets the refresh leave that ask alone instead of
// cancelling it and asking a second time. Kept per query object, so it goes with the cache it belongs to.
const pageOfTheLatestAsk = new WeakMap<Query, number>();

type PageFreshQuery<T> = Query<
	PageFreshAnswer<T>,
	PageFreshFailure,
	PageFreshAnswer<T>,
	QueryKey
>;

const pageOfTheLatestSettledAsk = <T>(
	query: PageFreshQuery<T>,
): number | undefined =>
	query.state.status === "error"
		? query.state.error?.askedOnPage
		: query.state.data?.askedOnPage;

/** Asks every shared read again whose latest ask belongs to a page before this one; a request still on its way
 * from such a page is cancelled, so its answer never lands as the new page's. */
export const askSharedReadsAgainFor = (
	queryClient: QueryClient,
	page: number,
): Promise<void> => {
	const askedBefore = {
		queryKey: PAGE_FRESH_READS,
		predicate: (query: Query) => (pageOfTheLatestAsk.get(query) ?? -1) < page,
	};
	// Invalidating alone would join a first ask still on its way instead of replacing it.
	return queryClient
		.cancelQueries(askedBefore)
		.then(() => queryClient.invalidateQueries(askedBefore));
};

/**
 * A read every component on a page shares: asked once per page, reused by components that appear later on the
 * same page, and asked again after each page move. A component that appears asks only when nothing is on its way
 * and the latest settled ask belongs to an earlier page, so a page without readers in between does not leave the
 * next reader waiting.
 */
export function usePageFreshQuery<T>(
	key: QueryKey,
	ask: () => Promise<T>,
): PageFreshRead<T> {
	const page = useContext(PageMovesContext);
	// React keeps a component of the same type at the same place across a route change; it keeps the page it
	// first appeared on, so the answer it already shows stays its own.
	const [mountPage] = useState(page);

	const asksOnMount = useCallback(
		(query: PageFreshQuery<T>): boolean => {
			if (query.state.fetchStatus === "fetching") {
				return false;
			}
			const settledOn = pageOfTheLatestSettledAsk(query);
			return settledOn === undefined || settledOn < page;
		},
		[page],
	);

	const query = useQuery<
		PageFreshAnswer<T>,
		PageFreshFailure,
		PageFreshAnswer<T>,
		QueryKey
	>({
		queryKey: pageFreshQueryKey(key),
		queryFn: async ({ client, queryKey }) => {
			const asked = client.getQueryCache().find({ queryKey, exact: true });
			if (asked) {
				pageOfTheLatestAsk.set(asked, page);
			}
			try {
				return { answer: await ask(), askedOnPage: page };
			} catch (error) {
				throw new PageFreshFailure(page, error);
			}
		},
		staleTime: Number.POSITIVE_INFINITY,
		retry: false,
		refetchOnMount: (q) => (asksOnMount(q) ? "always" : false),
		retryOnMount: asksOnMount,
	});

	// The cache tells its readers about a new ask a moment later; the component that asked shows it at once.
	const [askingAgain, setAskingAgain] = useState(false);
	const { refetch } = query;
	const askAgain = useCallback(() => {
		setAskingAgain(true);
		fireAndForget(refetch().finally(() => setAskingAgain(false)));
	}, [refetch]);

	const isReady =
		query.data !== undefined && query.data.askedOnPage >= mountPage;
	const isFailed =
		!isReady &&
		!askingAgain &&
		query.fetchStatus !== "fetching" &&
		query.status === "error";

	return {
		answer: isReady ? query.data?.answer : undefined,
		isLoading: !isReady && !isFailed,
		isFailed,
		askAgain,
	};
}
