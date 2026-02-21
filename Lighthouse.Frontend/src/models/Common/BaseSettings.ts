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
	parentOverrideAdditionalFieldDefinitionId: number | null;
	blockedStates: string[];
	blockedTags: string[];
	doneItemsCutoffDays: number;
	processBehaviourChartBaselineStartDate: Date | null;
	processBehaviourChartBaselineEndDate: Date | null;
	estimationAdditionalFieldDefinitionId: number | null;
}
