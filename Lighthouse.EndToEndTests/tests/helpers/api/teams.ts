import type { APIRequestContext } from "@playwright/test";

export async function createTeam(
	api: APIRequestContext,
	name: string,
	workTrackingSystemConnectionId: number,
	dataRetrievalValue: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
	tags: string[],
): Promise<{ id: number; name: string }> {
	const sixWeeksAgo = new Date(Date.now() - 6 * 7 * 24 * 60 * 60 * 1000);
	const fourWeeksAgo = new Date(Date.now() - 4 * 7 * 24 * 60 * 60 * 1000);

	const response = await api.post("/api/Teams", {
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
			tags: tags,
			parentOverrideField: "",
			automaticallyAdjustFeatureWIP: false,
			useFixedDatesForThroughput: false,
			workTrackingSystemConnectionId: workTrackingSystemConnectionId,
			serviceLevelExpectationProbability: 70,
			serviceLevelExpectationRange: 7,
			systemWIPLimit: 6,
			blockedStates: [],
			blockedTags: ["Blocked"],
			doneItemsCutoffDays: 180,
			processBehaviourChartBaselineStartDate: sixWeeksAgo,
			processBehaviourChartBaselineEndDate: fourWeeksAgo,
		},
	});

	return response.json();
}

export async function updateTeam(api: APIRequestContext, teamId: number) {
	await api.post(`/api/Teams/${teamId}`);
}
