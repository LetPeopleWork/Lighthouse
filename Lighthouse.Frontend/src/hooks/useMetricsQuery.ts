import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useRef } from "react";
import { parseLocalDate } from "../utils/date/localDate";
import type { MetricsOwnerRequest } from "./metricsOwnerKey";

/**
 * What a widget needs to know about one request: is it still on its way, did it fail, and which
 * answer its chart has meanwhile, if any.
 */
export type QueryProgress = {
	readonly isPending: boolean;
	readonly isError: boolean;
	readonly isPlaceholderData: boolean;
	readonly data: unknown;
};

/**
 * Every metrics request is asked fresh for the window on screen and forgotten once nothing shows
 * it: a dashboard answer is only true for the moment it was asked, so caching it, retrying it or
 * refetching it behind the reader's back would show numbers the reader did not ask for.
 */
export const metricsQueryOptions = {
	staleTime: 0,
	gcTime: 0,
	retry: false,
	refetchOnWindowFocus: false,
	refetchOnReconnect: false,
} as const;

export function dayOf(localDay: string): Date {
	const day = parseLocalDate(localDay);
	if (day === null) {
		throw new TypeError(`${localDay} is not a calendar day.`);
	}
	return day;
}

export type MetricsQuery<V> = {
	readonly progress: QueryProgress;
	/** The answer for the selected window or, while it has none, the previous window's answer. */
	readonly answer: V | null;
};

// The key holds everything the request sends, so an answer for a window the reader has already
// left lands under that window's key and is never read.
export function useMetricsQuery<R extends MetricsOwnerRequest, V>(
	fetchName: string,
	request: R,
	ask: (request: R) => Promise<V | undefined> | V | undefined,
	enabled: boolean,
	failureMessage?: string,
): MetricsQuery<V> {
	const query = useQuery({
		...metricsQueryOptions,
		queryKey: ["metrics", fetchName, request] as const,
		queryFn: async ({ queryKey }) => {
			try {
				// An empty answer is an answer: undefined would read as still on its way.
				return (await ask(queryKey[2])) ?? null;
			} catch (error) {
				if (failureMessage) console.error(failureMessage, error);
				throw error;
			}
		},
		enabled,
		placeholderData: keepPreviousData,
	});

	// A failure keeps the answer the chart last drew for this owner, so the chart stays mounted out of
	// sight behind the could-not-load note and comes back with whatever the reader set inside it.
	const lastDrawn = useRef<LastDrawn<typeof query.data>>(undefined);
	const owner = ownerOf(request);
	if (query.isSuccess && !query.isPlaceholderData) {
		lastDrawn.current = { owner, answer: query.data };
	}
	const data =
		query.isError && lastDrawn.current?.owner === owner
			? lastDrawn.current.answer
			: query.data;

	return {
		progress: {
			isPending: query.isPending,
			isError: query.isError,
			isPlaceholderData: query.isPlaceholderData,
			data,
		},
		answer: data ?? null,
	};
}

type LastDrawn<V> = { readonly owner: string; readonly answer: V } | undefined;

function ownerOf(request: MetricsOwnerRequest): string {
	return `${request.ownerType}:${request.ownerId}`;
}

export function listOf<V>(query: MetricsQuery<V[]>): V[] {
	return query.answer ?? [];
}
