import type { APIRequestContext } from "@playwright/test";

export async function createTeam(
	api: APIRequestContext,
	name: string,
	workTrackingSystemConnectionId: number,
	workItemQuery: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
): Promise<{ id: number; name: string }> {
	const response = await api.post("/api/Teams", {
		data: {
			id: 0,
			name: name,
			throughputHistory: 5,
			featureWIP: 1,
			workItemQuery: workItemQuery,
			workItemTypes: workItemTypes,
			toDoStates: states.toDo,
			doingStates: states.doing,
			doneStates: states.done,
			tags: [],
			relationCustomField: "",
			automaticallyAdjustFeatureWIP: false,
			useFixedDatesForThroughput: false,
			workTrackingSystemConnectionId: workTrackingSystemConnectionId,
		},
	});

	return response.json();
}

export async function updateTeam(api: APIRequestContext, teamId: number) {
	await api.post(`/api/Teams/${teamId}`);
}

export async function deleteTeam(
	api: APIRequestContext,
	teamId: number,
): Promise<void> {
	await api.delete(`/api/Teams/${teamId}`);
}
