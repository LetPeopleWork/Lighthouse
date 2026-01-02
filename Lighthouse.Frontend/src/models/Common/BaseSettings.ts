export interface IBaseSettings {
	id: number;
	name: string;
	dataRetrievalValue: string;
	workItemTypes: string[];
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	tags: string[];
	workTrackingSystemConnectionId: number;
	serviceLevelExpectationProbability: number;
	serviceLevelExpectationRange: number;
	systemWIPLimit: number;
	parentOverrideField: string;
	blockedStates: string[];
	blockedTags: string[];
	doneItemsCutoffDays: number;
}
