export interface ITeamSettings {
	id: number;
	name: string;
	throughputHistory: number;
	useFixedDatesForThroughput: boolean;
	throughputHistoryStartDate: Date;
	throughputHistoryEndDate: Date;
	featureWIP: number;
	workItemQuery: string;
	workItemTypes: string[];
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	workTrackingSystemConnectionId: number;
	relationCustomField: string;
	automaticallyAdjustFeatureWIP: boolean;
}
