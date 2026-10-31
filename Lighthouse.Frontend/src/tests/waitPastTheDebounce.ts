import { DEBOUNCE_MS } from "../hooks/useModifySettings";

const MARGIN_MS = 200;

/** Waits in real time until an autosave debounce would have fired, had one been pending. */
export const waitPastTheDebounce = () =>
	new Promise((resolve) => setTimeout(resolve, DEBOUNCE_MS + MARGIN_MS));
