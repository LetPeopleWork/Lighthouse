import type { APIRequestContext } from "@playwright/test";

export async function createTeam(
	api: APIRequestContext,
	name: string,
	workTrackingSystemConnectionId: number,
	dataRetrievalValue: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
): Promise<{ id: number; name: string }> {
	const sixWeeksAgo = new Date(Date.now() - 6 * 7 * 24 * 60 * 60 * 1000);
	const fourWeeksAgo = new Date(Date.now() - 4 * 7 * 24 * 60 * 60 * 1000);

	const response = await api.post("/api/latest/Teams", {
		data: {
			id: 0,
			name: name,
			throughputHistory: 5,
			featureWIP: 1,
			dataRetrievalValue: dataRetrievalValue,
			workItemTypes: workItemTypes,
			toDoStates: states.toDo,
			doingStates: states.doing,
			doneStates: states.done,
			parentOverrideField: "",
			automaticallyAdjustFeatureWIP: false,
			useFixedDatesForThroughput: false,
			workTrackingSystemConnectionId: workTrackingSystemConnectionId,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			stalenessThresholdDays: 0,
			blockedStalenessThresholdDays: 0,
			doneItemsCutoffDays: 180,
			processBehaviourChartBaselineStartDate: sixWeeksAgo,
			processBehaviourChartBaselineEndDate: fourWeeksAgo,
		},
	});

	return response.json();
}

export async function updateTeam(api: APIRequestContext, teamId: number) {
	await api.post(`/api/latest/teams/${teamId}`);
}

/** Has the named Team read its Estimation from its connection's default. */
export async function followTheConnectionsEstimation(
	api: APIRequestContext,
	teamName: string,
): Promise<void> {
	const teams = (await (await api.get("/api/latest/teams")).json()) as {
		id: number;
		name: string;
	}[];
	const team = teams.find((t) => t.name === teamName);
	if (!team) {
		throw new Error(`No Team named ${teamName}`);
	}

	const settings = await (
		await api.get(`/api/latest/teams/${team.id}/settings`)
	).json();
	const response = await api.put(`/api/latest/teams/${team.id}`, {
		data: { ...settings, estimationSource: "ConnectionDefault" },
	});
	if (!response.ok()) {
		throw new Error(
			`Could not have ${teamName} follow the Estimation default: ${response.status()} ${await response.text()}`,
		);
	}
}

/** Has the named Team keep only its last `days` of finished work, so a long report window no longer fits it. */
export async function keepHistoryFor(
	api: APIRequestContext,
	teamName: string,
	days: number,
): Promise<void> {
	const teams = (await (await api.get("/api/latest/teams")).json()) as {
		id: number;
		name: string;
	}[];
	const team = teams.find((t) => t.name === teamName);
	if (!team) {
		throw new Error(`No Team named ${teamName}`);
	}

	const settings = await (
		await api.get(`/api/latest/teams/${team.id}/settings`)
	).json();
	const response = await api.put(`/api/latest/teams/${team.id}`, {
		data: { ...settings, doneItemsCutoffDays: days },
	});
	if (!response.ok()) {
		throw new Error(
			`Could not have ${teamName} keep ${days} days: ${response.status()} ${await response.text()}`,
		);
	}
}

export async function deleteTeam(
	api: APIRequestContext,
	teamId: number,
): Promise<void> {
	const response = await api.delete(`/api/latest/teams/${teamId}`);
	if (!response.ok()) {
		throw new Error(`Could not delete Team ${teamId}: ${response.status()}`);
	}
}
