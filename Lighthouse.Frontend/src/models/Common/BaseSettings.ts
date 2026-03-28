import type { IDataRetrievalSchema } from "./DataRetrievalSchema";
import type { IStateMapping } from "./StateMapping";

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
	stateMappings: IStateMapping[];
	doneItemsCutoffDays: number;
	processBehaviourChartBaselineStartDate: Date | null;
	processBehaviourChartBaselineEndDate: Date | null;
	estimationAdditionalFieldDefinitionId: number | null;
	estimationUnit: string | null;
	useNonNumericEstimation: boolean;
	estimationCategoryValues: string[];
	dataRetrievalSchema?: IDataRetrievalSchema | null;
}
