import { act } from "@testing-library/react";

/**
 * Runs `step` inside React's `act` and waits until the promises it started have settled
 * and their state updates are rendered. `act` only waits like that when it is handed a
 * promise, which is why the step's result is followed by a resolved one.
 */
export function actAndSettle(step: () => void): Promise<void> {
	return act(() => {
		step();
		return Promise.resolve();
	});
}
