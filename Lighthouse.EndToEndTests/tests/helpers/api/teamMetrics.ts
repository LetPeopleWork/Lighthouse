import type { APIRequestContext } from "@playwright/test";

/**
 * WHY-NEW-FILE: tests/helpers/api/teamMetrics.ts
 *   CLOSEST-EXISTING: tests/helpers/api/teams.ts
 *   EXTENSION-COST: teams.ts is covered by a read/edit deny rule in the environment
 *     this step ran in and could not be modified at all.
 *   PARALLEL-RATIONALE: tooling constraint, not a design boundary — these are team
 *     helpers and belong in teams.ts; the split is mechanical and reversible.
 */

function toLocalDateParam(date: Date): string {
	const month = `${date.getMonth() + 1}`.padStart(2, "0");
	const day = `${date.getDate()}`.padStart(2, "0");
	return `${date.getFullYear()}-${month}-${day}`;
}

export async function findTeamIdByName(
	api: APIRequestContext,
	name: string,
): Promise<number> {
	const response = await api.get("/api/latest/teams");
	if (!response.ok()) {
		throw new Error(`Failed to list teams: ${response.status()}`);
	}

	const teams = (await response.json()) as { id: number; name: string }[];
	const team = teams.find((candidate) => candidate.name === name);

	if (!team) {
		throw new Error(`Team "${name}" not found in the seeded data`);
	}

	return team.id;
}

/**
 * The in-progress population as of a day, read from the very endpoint the
 * dashboard reads (`metrics/wip`). BaseMetricsView feeds exactly these ages into
 * the Work Item Age Percentiles RAG rule, so a spec can derive an SLE that puts
 * the widget in a chosen band without hard-coding what the demo seeder produced.
 */
export async function readInProgressWorkItemAges(
	api: APIRequestContext,
	teamId: number,
	asOfDate: Date,
): Promise<number[]> {
	const response = await api.get(
		`/api/latest/teams/${teamId}/metrics/wip?asOfDate=${toLocalDateParam(asOfDate)}`,
	);
	if (!response.ok()) {
		throw new Error(
			`Failed to read in-progress items for team ${teamId}: ${response.status()}`,
		);
	}

	const items = (await response.json()) as { workItemAge: number }[];
	return items.map((item) => item.workItemAge);
}

/**
 * Sets the team's SLE by round-tripping its settings, so every unrelated setting
 * (and the concurrency token) is preserved. The dashboard treats an SLE as absent
 * unless BOTH the probability and the range are above zero, which is how the
 * seeded demo team starts out.
 */
export async function configureServiceLevelExpectation(
	api: APIRequestContext,
	teamId: number,
	probability: number,
	rangeInDays: number,
): Promise<void> {
	const current = await api.get(`/api/latest/teams/${teamId}/settings`);
	if (!current.ok()) {
		throw new Error(
			`Failed to read settings for team ${teamId}: ${current.status()}`,
		);
	}

	const settings = (await current.json()) as Record<string, unknown>;
	settings.serviceLevelExpectationProbability = probability;
	settings.serviceLevelExpectationRange = rangeInDays;

	const updated = await api.put(`/api/latest/teams/${teamId}`, {
		data: settings,
	});
	if (!updated.ok()) {
		throw new Error(
			`Failed to update settings for team ${teamId}: ${updated.status()}`,
		);
	}
}
