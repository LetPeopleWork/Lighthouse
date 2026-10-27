/**
 * Marks a promise as deliberately not awaited, so the caller continues straight away.
 * A rejection is not handled here: it stays exactly as unhandled as it was before the call.
 */
export const fireAndForget = (_pending: unknown): void => {
	// Nothing to do: the call itself is the marker.
};
