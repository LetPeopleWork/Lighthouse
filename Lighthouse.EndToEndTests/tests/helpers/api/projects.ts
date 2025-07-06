import type { APIRequestContext } from "@playwright/test";

export async function createProject(
	api: APIRequestContext,
	projectName: string,
	involvedTeams: { id: number; name: string }[],
	workTrackingSystemConnectionId: number,
	workItemQuery: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
	tags: string[],
): Promise<{ id: number; name: string }> {
	const involvedTeamsData = involvedTeams.map((team) => ({
		id: team.id,
		name: team.name,
		featureWip: 1,
		lastUpdated: new Date().toISOString(),
		throughput: [],
		tags: [],
	}));

	const response = await api.post("/api/projects", {
		data: {
			id: 0,
			name: projectName,
			workItemTypes: workItemTypes,
			milestones: [],
			toDoStates: states.toDo,
			doingStates: states.doing,
			doneStates: states.done,
			tags: tags,
			overrideRealChildCountStates: [],
			workItemQuery: workItemQuery,
			unparentedItemsQuery: "",
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			historicalFeaturesWorkItemQuery: "",
			defaultAmountOfWorkItemsPerFeature: 10,
			sizeEstimateField: "",
			owningTeam: null,
			featureOwnerField: "",
			involvedTeams: involvedTeamsData,
			workTrackingSystemConnectionId: workTrackingSystemConnectionId,
			serviceLevelExpectationProbability: 80,
			serviceLevelExpectationRange: 25,
			systemWIPLImit: 2,
			parentOverrideField: "",
			blockedStates: [],
			blockedTags: [],
		},
	});
	return response.json();
}
