import type { APIRequestContext } from "@playwright/test";

export async function createTeam(
	api: APIRequestContext,
	name: string,
	workTrackingSystemConnectionId: number,
	workItemQuery: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
	tags: string[],
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
			tags: tags,
			parentOverrideField: "",
			automaticallyAdjustFeatureWIP: false,
			useFixedDatesForThroughput: false,
			workTrackingSystemConnectionId: workTrackingSystemConnectionId,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: [],
		},
	});

	return response.json();
}

export async function updateTeam(api: APIRequestContext, teamId: number) {
	await api.post(`/api/Teams/${teamId}`);
}
