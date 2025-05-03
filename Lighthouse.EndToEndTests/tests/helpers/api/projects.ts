import type { APIRequestContext } from "@playwright/test";

export async function createProject(
	api: APIRequestContext,
	projectName: string,
	involvedTeams: { id: number; name: string }[],
	workTrackingSystemConnectionId: number,
	workItemQuery: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
): Promise<{ id: number; name: string }> {
	const involvedTeamsData = involvedTeams.map((team) => ({
		id: team.id,
		name: team.name,
		featureWip: 1,
		lastUpdated: new Date().toISOString(),
		throughput: [],
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
			tags: [],
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
		},
	});
	return response.json();
}

export async function deleteProject(
	api: APIRequestContext,
	projectId: number,
): Promise<void> {
	await api.delete(`/api/Projects/${projectId}`);
}
