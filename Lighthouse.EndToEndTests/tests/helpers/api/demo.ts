import type { APIRequestContext } from "@playwright/test";

const POLL_INTERVAL_MS = 1000;
const DEFAULT_TIMEOUT_MS = 3 * 60 * 1000;

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

export async function waitForBackgroundUpdates(
	request: APIRequestContext,
	timeoutMs: number = DEFAULT_TIMEOUT_MS,
): Promise<void> {
	// Give UpdateController a moment to register the queued work before
	// polling — otherwise the very first status check can return idle
	// before the work has been enqueued.
	await new Promise((resolve) => setTimeout(resolve, 1500));

	const deadline = Date.now() + timeoutMs;

	while (true) {
		const response = await request.get("/api/latest/update/status");
		if (!response.ok()) {
			throw new Error(`Failed to fetch update status: ${response.status}`);
		}

		const body = (await response.json()) as {
			hasActiveUpdates: boolean;
			activeCount: number;
		};

		if (!body.hasActiveUpdates && body.activeCount === 0) {
			return;
		}

		if (Date.now() > deadline) {
			throw new Error("Timed out waiting for background updates to complete");
		}

		await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS));
	}
}
