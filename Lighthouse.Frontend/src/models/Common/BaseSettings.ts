export interface IBaseSettings {
	id: number;
	name: string;
	workItemQuery: string;
	workItemTypes: string[];
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	tags: string[];
	workTrackingSystemConnectionId: number;
	serviceLevelExpectationProbability: number;
	serviceLevelExpectationRange: number;
	systemWipLimit: number;
}
