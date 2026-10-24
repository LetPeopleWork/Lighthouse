/**
 * What the server says about a connection's delivery sources is kept only while a reader on screen
 * holds it: the browser keeps nothing between openings of the dialog, so every opening asks the
 * server again. A failure is not retried behind the reader's back either; the notice tells them to
 * reopen instead.
 */
export const askedAfreshQueryOptions = {
	staleTime: 0,
	gcTime: 0,
	retry: false,
	refetchOnWindowFocus: false,
	refetchOnReconnect: false,
} as const;
