import type { APIRequestContext } from "@playwright/test";

export async function createPortfolio(
	api: APIRequestContext,
	portfolioName: string,
	workTrackingSystemConnectionId: number,
	dataRetrievalValue: string,
	workItemTypes: string[],
	states: { toDo: string[]; doing: string[]; done: string[] },
): Promise<{ id: number; name: string }> {
	const response = await api.post("/api/latest/portfolios", {
		data: {
			id: 0,
			name: portfolioName,
			workItemTypes: workItemTypes,
			milestones: [],
			toDoStates: states.toDo,
			doingStates: states.doing,
			doneStates: states.done,
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
			stalenessThresholdDays: 0,
			parentOverrideField: "",
			blockedStalenessThresholdDays: 0,
			doneItemsCutoffDays: 365,
		},
	});
	return response.json();
}

export async function updatePortfolio(
	api: APIRequestContext,
	portfolioId: number,
) {
	await api.post(`/api/latest/portfolios/refresh/${portfolioId}`);
}

/** Has the named Portfolio read its Feature Owner Field from its connection's default. */
export async function followTheConnectionsFeatureOwnerField(
	api: APIRequestContext,
	portfolioName: string,
): Promise<void> {
	const portfolios = (await (
		await api.get("/api/latest/portfolios")
	).json()) as { id: number; name: string }[];
	const portfolio = portfolios.find((p) => p.name === portfolioName);
	if (!portfolio) {
		throw new Error(`No Portfolio named ${portfolioName}`);
	}

	const settings = await (
		await api.get(`/api/latest/portfolios/${portfolio.id}/settings`)
	).json();
	const response = await api.put(`/api/latest/portfolios/${portfolio.id}`, {
		data: { ...settings, featureOwnerSource: "ConnectionDefault" },
	});
	if (!response.ok()) {
		throw new Error(
			`Could not have ${portfolioName} follow the Feature Owner Field default: ${response.status()} ${await response.text()}`,
		);
	}
}
