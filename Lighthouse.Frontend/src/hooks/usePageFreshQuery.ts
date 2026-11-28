import { type QueryKey, useQuery } from "@tanstack/react-query";
import { useCallback, useContext, useState } from "react";
import { fireAndForget } from "../utils/fireAndForget";
import type { PageFreshAnswer } from "./PageFreshAnswer";
import { PageFreshFailure } from "./PageFreshFailure";
import type { PageFreshQuery } from "./PageFreshQuery";
import type { PageFreshRead } from "./PageFreshRead";
import { PageMovesContext } from "./PageMovesContext";
import { pageFreshQueryKey } from "./pageFreshQueryKey";
import { recordAskOnPage } from "./pageOfTheLatestAsk";

const pageOfTheLatestSettledAsk = <T>(
	query: PageFreshQuery<T>,
): number | undefined =>
	query.state.status === "error"
		? query.state.error?.askedOnPage
		: query.state.data?.askedOnPage;

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
				recordAskOnPage(asked, page);
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
