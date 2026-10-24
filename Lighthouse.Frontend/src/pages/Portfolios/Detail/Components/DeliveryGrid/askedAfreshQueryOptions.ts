/**
 * What the server says about a connection's delivery sources is kept only while a reader on screen
 * holds it, so the next opening asks again: a reader who gave a Release a date in the tracker and
 * reopens expects to see that date. A failure is not retried behind the reader's back either; the
 * notice tells them to reopen instead.
 */
export const askedAfreshQueryOptions = {
	staleTime: 0,
	gcTime: 0,
	retry: false,
	refetchOnWindowFocus: false,
	refetchOnReconnect: false,
} as const;
