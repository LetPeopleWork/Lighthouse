import { z } from "zod";
import type { ICycleTimeDefinition } from "../Metrics/NamedCycleTime";
import type { IDataRetrievalSchema } from "./DataRetrievalSchema";
import type { IStateMapping } from "./StateMapping";

/**
 * Zod schema for blockedStalenessThresholdDays — rolling-adoption rule.
 * Validates the new field at the trust boundary; existing fields are not schema-validated.
 */
export const blockedStalenessThresholdSchema = z.number().int().min(0).max(365);

export function parseBlockedStalenessThreshold(value: unknown): number {
	const result = blockedStalenessThresholdSchema.safeParse(value);
	return result.success ? result.data : 0;
}

export interface IBaseSettings {
	id: number;
	name: string;
	dataRetrievalValue: string;
	workItemTypes: string[];
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	workTrackingSystemConnectionId: number;
	serviceLevelExpectationProbability: number;
	serviceLevelExpectationRange: number;
	systemWIPLimit: number;
	parentOverrideAdditionalFieldDefinitionId: number | null;
	blockedRuleSetJson?: string | null;
	stateMappings: IStateMapping[];
	cycleTimeDefinitions?: ICycleTimeDefinition[];
	waitStates?: string[];
	doneItemsCutoffDays: number;
	stalenessThresholdDays: number;
	blockedStalenessThresholdDays: number;
	processBehaviourChartBaselineStartDate: Date | null;
	processBehaviourChartBaselineEndDate: Date | null;
	estimationAdditionalFieldDefinitionId: number | null;
	estimationUnit: string | null;
	useNonNumericEstimation: boolean;
	estimationCategoryValues: string[];
	dataRetrievalSchema?: IDataRetrievalSchema | null;
	concurrencyToken?: string;
}
