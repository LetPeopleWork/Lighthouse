export const __SCAFFOLD__ = true;

/**
 * Share of the machine's cores a local test run may use when nobody says otherwise, so that
 * parallel sessions on one machine do not each take every core.
 */
export const LOCAL_WORKER_FRACTION = 0.5;

export const TEST_WORKERS_VARIABLE = "LIGHTHOUSE_TEST_WORKERS";

export type TestWorkersEnvironment = Readonly<
	Record<string, string | undefined>
>;

/**
 * The worker count for a test run, or `undefined` to leave it to the test runner's own default.
 */
export function resolveTestWorkers(
	_env: TestWorkersEnvironment,
	_availableCores: number,
): number | undefined {
	throw new Error("Not yet implemented -- RED scaffold");
}
