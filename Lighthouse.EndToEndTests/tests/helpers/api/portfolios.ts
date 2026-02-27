import type { APIRequestContext } from "@playwright/test";

export async function createPortfolio(
	api: APIRequestContext,
	portfolioName: string,
	workTrackingSystemConnectionId: number,
	dataRetrievalValue: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
	tags: string[],
): Promise<{ id: number; name: string }> {
	const response = await api.post("/api/portfolios", {
		data: {
			id: 0,
			name: portfolioName,
			workItemTypes: workItemTypes,
			milestones: [],
			toDoStates: states.toDo,
			doingStates: states.doing,
			doneStates: states.done,
			tags: tags,
			overrideRealChildCountStates: [],
			dataRetrievalValue: dataRetrievalValue,
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultWorkItemPercentile: 85,
			percentileHistoryInDays: 90,
			defaultAmountOfWorkItemsPerFeature: 10,
			sizeEstimateField: "",
			owningTeam: null,
			featureOwnerField: "",
			workTrackingSystemConnectionId: workTrackingSystemConnectionId,
			serviceLevelExpectationProbability: 80,
			serviceLevelExpectationRange: 25,
			systemWIPLImit: 2,
			parentOverrideField: "",
			blockedStates: [],
			blockedTags: [],
			doneItemsCutoffDays: 365,
		},
	});
	return response.json();
}

export async function updatePortfolio(
	api: APIRequestContext,
	portfolioId: number,
) {
	await api.post(`/api/portfolios/refresh/${portfolioId}`);
}
