/**
 * A failure a test causes on purpose to see the page recover from it.
 *
 * A click handler that fails rejects a promise nobody awaits: in the app that rejection is what the
 * global error handler reports, so the component lets it escape. Vitest fails the whole run on any
 * unhandled rejection, so `vitest.config.ts` lets this one error, and only this one, pass.
 */
export const EXPECTED_TEST_FAILURE = "ExpectedTestFailure";

export class ExpectedTestFailure extends Error {
	constructor(message = "The test made this fail on purpose") {
		super(message);
		this.name = EXPECTED_TEST_FAILURE;
	}
}
