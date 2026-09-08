import { useEffect } from "react";

/**
 * Runs `run` once the caller has stopped bumping `revision` for `delayMs`.
 *
 * Revision 0 means nothing has been requested yet, so the first render never
 * fires. Clearing the timer on cleanup is what keeps a component that unmounts
 * mid-debounce from setting state after it is gone.
 */
export function useDebouncedRevisionRun(
	revision: number,
	run: () => void,
	delayMs = 300,
) {
	useEffect(() => {
		if (revision === 0) {
			return;
		}

		const timer = setTimeout(run, delayMs);
		return () => clearTimeout(timer);
	}, [revision, run, delayMs]);
}
