import type { APIRequestContext } from "@playwright/test";
import {
	isRowOf,
	type RefreshLogRow,
	type RoundVerdict,
	verdictOf,
} from "./apiVersionMap";

const pollEveryMilliseconds = 1_000;
const defaultTimeoutMilliseconds = 30 * 60_000;

// A year: the periodic refresh still ticks every minute, but finds nothing old enough to refresh, so no
// background work lands in the middle of a reading unless a skipped round asks for it.
const neverByItself = 525_600;

type RefreshSettings = {
	interval: number;
	refreshAfter: number;
	startDelay: number;
};

type UpdateTask = { updateType: string | number; id: number };

export type RoundReading = {
	portfolioId: number;
	verdict: RoundVerdict;
	startedAt: Date;
	finishedAt: Date;
	refreshLogRow: RefreshLogRow;
};

async function readJson<T>(api: APIRequestContext, url: string): Promise<T> {
	const response = await api.get(url);
	if (!response.ok()) {
		throw new Error(
			`GET ${url} answered ${response.status()}: ${await response.text()}`,
		);
	}
	return (await response.json()) as T;
}

async function putJson(
	api: APIRequestContext,
	url: string,
	data: unknown,
): Promise<void> {
	const response = await api.put(url, { data });
	if (!response.ok()) {
		throw new Error(
			`PUT ${url} answered ${response.status()}: ${await response.text()}`,
		);
	}
}

async function waitFor<T>(
	what: string,
	timeoutMilliseconds: number,
	probe: () => Promise<T | undefined>,
): Promise<T> {
	const deadline = Date.now() + timeoutMilliseconds;
	while (Date.now() < deadline) {
		const answer = await probe();
		if (answer !== undefined) {
			return answer;
		}
		await new Promise((resolve) => setTimeout(resolve, pollEveryMilliseconds));
	}
	throw new Error(
		`Gave up after ${timeoutMilliseconds} ms waiting for ${what}`,
	);
}

export function readRefreshLog(
	api: APIRequestContext,
): Promise<RefreshLogRow[]> {
	return readJson<RefreshLogRow[]>(api, "/api/latest/systeminfo/refreshlog");
}

async function lastRefreshLogId(api: APIRequestContext): Promise<number> {
	const rows = await readRefreshLog(api);
	return rows.reduce((highest, row) => Math.max(highest, row.id), 0);
}

function readTasks(api: APIRequestContext): Promise<UpdateTask[]> {
	return readJson<UpdateTask[]>(api, "/api/latest/update/tasks");
}

function isTask(
	task: UpdateTask,
	updateType: string,
	updateTypeNumber: number,
	id: number,
): boolean {
	return (
		(task.updateType === updateType || task.updateType === updateTypeNumber) &&
		task.id === id
	);
}

/** Turns the periodic refreshes down to a one-minute tick that finds nothing to do. */
export async function quietTheInstance(api: APIRequestContext): Promise<void> {
	const quiet: RefreshSettings = {
		interval: 1,
		refreshAfter: neverByItself,
		startDelay: 1,
	};
	await putJson(api, "/api/latest/appsettings/FeatureRefresh", quiet);
	await putJson(api, "/api/latest/appsettings/TeamRefresh", quiet);
}

/** Waits until Task Manager has shown nothing running or queued for three polls in a row. */
export async function waitUntilNothingIsQueued(
	api: APIRequestContext,
	timeoutMilliseconds = defaultTimeoutMilliseconds,
): Promise<void> {
	let idlePolls = 0;
	await waitFor("the update queue to drain", timeoutMilliseconds, async () => {
		const status = await readJson<{ hasActiveUpdates: boolean }>(
			api,
			"/api/latest/update/status",
		);
		idlePolls = status.hasActiveUpdates ? 0 : idlePolls + 1;
		return idlePolls >= 3 ? true : undefined;
	});
}

async function waitForTheRound(
	api: APIRequestContext,
	portfolioId: number,
	afterRowId: number,
	startedAt: Date,
	timeoutMilliseconds: number,
): Promise<RoundReading> {
	const row = await waitFor(
		`the forecast round of Portfolio ${portfolioId}`,
		timeoutMilliseconds,
		async () => {
			const tasks = await readTasks(api);
			if (tasks.some((task) => isTask(task, "Forecasts", 2, portfolioId))) {
				return undefined;
			}
			const rows = await readRefreshLog(api);
			return rows.find(
				(candidate) =>
					candidate.id > afterRowId &&
					isRowOf(candidate, "Forecast", portfolioId),
			);
		},
	);

	return {
		portfolioId,
		verdict: verdictOf(row),
		startedAt,
		finishedAt: new Date(),
		refreshLogRow: row,
	};
}

/** "Update forecast": forgets what the last forecast was computed from, so the round computes. */
export async function startComputedRound(
	api: APIRequestContext,
	portfolioId: number,
	timeoutMilliseconds = defaultTimeoutMilliseconds,
): Promise<RoundReading> {
	const afterRowId = await lastRefreshLogId(api);
	const startedAt = new Date();
	const response = await api.post(`/api/latest/forecast/update/${portfolioId}`);
	if (!response.ok()) {
		throw new Error(
			`Update forecast for Portfolio ${portfolioId} answered ${response.status()}`,
		);
	}
	return waitForTheRound(
		api,
		portfolioId,
		afterRowId,
		startedAt,
		timeoutMilliseconds,
	);
}

/**
 * An update that changes nothing. Every button that refreshes on request also forgets the forecast's inputs,
 * so the only update that keeps them is the periodic one: this lets its next one-minute tick find every
 * Portfolio due, and turns it back down as soon as the Portfolio's own refresh is under way. The other
 * Portfolios refresh in the same tick, as they do in the hourly update this stands in for.
 * Needs quietTheInstance to have run before the backend last started: the periodic refresh reads its interval
 * only after each wait, so a backend started earlier keeps its old hour-long wait until restarted.
 */
export async function startSkippedRound(
	api: APIRequestContext,
	portfolioId: number,
	timeoutMilliseconds = defaultTimeoutMilliseconds,
): Promise<RoundReading> {
	const afterRowId = await lastRefreshLogId(api);
	const settings = await readJson<RefreshSettings>(
		api,
		"/api/latest/appsettings/FeatureRefresh",
	);
	const startedAt = new Date();

	await putJson(api, "/api/latest/appsettings/FeatureRefresh", {
		...settings,
		refreshAfter: 0,
	});
	try {
		await waitFor(
			`the periodic refresh to pick up Portfolio ${portfolioId}`,
			timeoutMilliseconds,
			async () => {
				const tasks = await readTasks(api);
				if (tasks.some((task) => isTask(task, "Features", 1, portfolioId))) {
					return true;
				}
				const rows = await readRefreshLog(api);
				return rows.some(
					(row) =>
						row.id > afterRowId && isRowOf(row, "Portfolio", portfolioId),
				)
					? true
					: undefined;
			},
		);
	} finally {
		await putJson(api, "/api/latest/appsettings/FeatureRefresh", settings);
	}

	return waitForTheRound(
		api,
		portfolioId,
		afterRowId,
		startedAt,
		timeoutMilliseconds,
	);
}
