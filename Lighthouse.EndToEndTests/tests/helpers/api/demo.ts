import type { APIRequestContext } from "@playwright/test";

const POLL_INTERVAL_MS = 1000;
const DEFAULT_TIMEOUT_MS = 3 * 60 * 1000;

// How long to wait for the load's work to show up in the queue at all, and how
// often to look. Fast polling keeps the normal path cheaper than the fixed 1.5s
// sleep this replaced; the ceiling only matters when work never appears.
const UPDATE_APPEARANCE_TIMEOUT_MS = 30 * 1000;
const APPEARANCE_POLL_INTERVAL_MS = 100;

export async function loadDemoScenario(
	request: APIRequestContext,
	scenarioId: number,
): Promise<void> {
	const response = await request.post(
		`/api/latest/demo/scenarios/${scenarioId}/load`,
	);
	if (!response.ok()) {
		throw new Error(
			`Failed to load demo scenario ${scenarioId}: ${response.status}`,
		);
	}
}

async function readActiveUpdateCount(
	request: APIRequestContext,
): Promise<number> {
	const response = await request.get("/api/latest/update/status");
	if (!response.ok()) {
		throw new Error(`Failed to fetch update status: ${response.status}`);
	}

	const body = (await response.json()) as {
		hasActiveUpdates: boolean;
		activeCount: number;
	};

	return body.hasActiveUpdates
		? Math.max(body.activeCount, 1)
		: body.activeCount;
}

function sleep(ms: number): Promise<void> {
	return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Waits for the work a demo-scenario load kicked off to finish.
 *
 * `/update/status` counts only entries sitting in Queued or InProgress, and that
 * collection is EMPTY until the load has enqueued anything — so "not enqueued
 * yet" and "all finished" are the same response. This used to be papered over
 * with a fixed 1.5s sleep before the first poll, which is a bet that enqueueing
 * always beats the clock. On a cold Postgres it does not: the first poll saw an
 * idle queue, this returned immediately, and the spec then read a page whose
 * data had not been written yet. That is the `verifypostgres` flake on the first
 * demo-driven spec of the run — 71s failing, 10s passing on the retry once the
 * data had landed (CI runs 30250405183 and 30258220986, both on the run's first
 * metrics spec because files execute alphabetically).
 *
 * So: wait for the work to APPEAR before waiting for it to drain. The appearance
 * wait is bounded — a scenario whose work genuinely completes between the POST
 * and the first poll would otherwise hang here — but polling fast means the
 * normal path leaves this loop on its first or second try and costs less than
 * the fixed sleep it replaces.
 */
export async function waitForBackgroundUpdates(
	request: APIRequestContext,
	timeoutMs: number = DEFAULT_TIMEOUT_MS,
): Promise<void> {
	const deadline = Date.now() + timeoutMs;

	const appearanceDeadline = Math.min(
		Date.now() + UPDATE_APPEARANCE_TIMEOUT_MS,
		deadline,
	);
	while (Date.now() < appearanceDeadline) {
		if ((await readActiveUpdateCount(request)) > 0) {
			break;
		}
		await sleep(APPEARANCE_POLL_INTERVAL_MS);
	}

	while ((await readActiveUpdateCount(request)) > 0) {
		if (Date.now() > deadline) {
			throw new Error("Timed out waiting for background updates to complete");
		}
		await sleep(POLL_INTERVAL_MS);
	}
}
