/**
 * Share of the machine's cores a local test run may use when nobody says otherwise, so that
 * parallel sessions on one machine do not each take every core.
 */
export const LOCAL_WORKER_FRACTION = 0.5;

export const TEST_WORKERS_VARIABLE = "LIGHTHOUSE_TEST_WORKERS";

export type TestWorkersEnvironment = Readonly<
	Record<string, string | undefined>
>;

const explicitCount = /^[1-9]\d*$/;

/**
 * The worker count for a test run, or `undefined` to leave it to the test runner's own default.
 */
export function resolveTestWorkers(
	env: TestWorkersEnvironment,
	availableCores: number,
): number | undefined {
	const requested = env[TEST_WORKERS_VARIABLE];

	if (requested !== undefined && requested !== "") {
		if (!explicitCount.test(requested)) {
			throw new Error(
				`${TEST_WORKERS_VARIABLE} must be a whole number of at least 1, but was "${requested}"`,
			);
		}
		return Number(requested);
	}

	if (env.CI?.toLowerCase() === "true") {
		return undefined;
	}

	return Math.max(1, Math.floor(availableCores * LOCAL_WORKER_FRACTION));
}
