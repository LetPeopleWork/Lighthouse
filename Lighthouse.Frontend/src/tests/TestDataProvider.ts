import type { IPortfolioSettings } from "../models/Portfolio/PortfolioSettings";
import type { ITeamSettings } from "../models/Team/TeamSettings";
import type { IWorkItem } from "../models/WorkItem";

export function generateWorkItemMapForRunChart(rawDataInput: number[]) {
	const workItemsPerUnitOfTime: { [key: number]: IWorkItem[] } = {};

	let counter = 0;
	for (let day = 0; day < rawDataInput.length; day++) {
		const itemCount = rawDataInput[day];
		workItemsPerUnitOfTime[day] = [];

		// For each day, generate the required number of work items
		for (let i = 0; i < itemCount; i++) {
			const workItem = generateWorkItem(counter++);
			workItemsPerUnitOfTime[day].push(workItem);
		}
	}

	return workItemsPerUnitOfTime;
}

export function createMockTeamSettings(): ITeamSettings {
	return {
		id: 1,
		name: "Team A",
		throughputHistory: 30,
		useFixedDatesForThroughput: false,
		throughputHistoryStartDate: new Date(),
		throughputHistoryEndDate: new Date(),
		featureWIP: 1,
		dataRetrievalValue: "Query",
		workItemTypes: ["Bug", "Story"],
		workTrackingSystemConnectionId: 1,
		parentOverrideAdditionalFieldDefinitionId: null,
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Done"],
		tags: [],
		automaticallyAdjustFeatureWIP: true,
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		blockedStates: [],
		blockedTags: [],
		doneItemsCutoffDays: 180,
		processBehaviourChartBaselineStartDate: null,
		processBehaviourChartBaselineEndDate: null,
	};
}

export function createMockProjectSettings(): IPortfolioSettings {
	return {
		id: 1,
		name: "Project A",
		workItemTypes: ["Epic"],
		dataRetrievalValue: "Query",
		usePercentileToCalculateDefaultAmountOfWorkItems: false,
		defaultAmountOfWorkItemsPerFeature: 15,
		defaultWorkItemPercentile: 85,
		percentileHistoryInDays: 90,
		workTrackingSystemConnectionId: 1,
		sizeEstimateAdditionalFieldDefinitionId: null,
		featureOwnerAdditionalFieldDefinitionId: null,
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Done"],
		tags: [],
		overrideRealChildCountStates: [""],
		involvedTeams: [],
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		parentOverrideAdditionalFieldDefinitionId: null,
		blockedStates: [],
		blockedTags: [],
		doneItemsCutoffDays: 365,
		processBehaviourChartBaselineStartDate: null,
		processBehaviourChartBaselineEndDate: null,
	};
}

export function generateWorkItem(id: number): IWorkItem {
	// Random date between now and 30 days ago
	const getRandomDate = (maxDaysAgo: number) => {
		const today = new Date();
		const daysAgo = Math.floor(Math.random() * (maxDaysAgo + 1));
		const date = new Date(today);
		date.setDate(today.getDate() - daysAgo);
		return date;
	};

	// Generate random work item
	const startedDate = getRandomDate(30);
	// Closed date must be after start date (or same day)
	const daysAfterStart = Math.floor(
		Math.random() * (30 - (30 - startedDate.getDate()) + 1),
	);

	const closedDate = new Date(startedDate);
	closedDate.setDate(startedDate.getDate() + daysAfterStart);

	const potentialStates = [
		"In Progress",
		"In Discovery",
		"Ready for Testing",
		"In Q&A Testing",
		"Peer Review",
		"Testing Completed",
		"Ready for Release",
	];
	const state =
		potentialStates[Math.floor(Math.random() * potentialStates.length)];

	return {
		name: `Work Item that has a very long name so I can test whether the text wrapping works so I'm just adding more text and see whenever it's getting too big. I wonder what people think, don't they know titles should be short - put all that other stuff in the description...anyway, is this wrapped? - ${id}`,
		id: id,
		referenceId: `WI-${id}`,
		url: `https://example.com/work-items/${id}`,
		state: state,
		stateCategory: "Doing",
		type: "Feature",
		workItemAge: Math.floor(Math.random() * (19 - 3 + 1)) + 3,
		startedDate,
		closedDate,
		cycleTime: daysAfterStart + 1,
		parentWorkItemReference: "",
		isBlocked: Math.random() < 0.5,
	};
}
