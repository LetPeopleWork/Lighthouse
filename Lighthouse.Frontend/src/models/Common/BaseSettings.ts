import { z } from "zod";
import type { ICycleTimeDefinition } from "../Metrics/NamedCycleTime";
import type { IDataRetrievalSchema } from "./DataRetrievalSchema";
import type { IStateMapping } from "./StateMapping";

/**
 * Zod schema for the blocked rule set — the changed settings contract (Epic 5074).
 * This validates the parsed BlockedRuleSetJson at the trust boundary so a malformed
 * rule set is rejected before it reaches the DeliveryRuleBuilder. The rule set mirrors
 * the backend WorkItemRuleSet idiom already used by forecast filters and deliveries.
 */
export const blockedRuleConditionSchema = z.object({
	fieldKey: z.string(),
	operator: z.string(),
	value: z.string(),
});

export const blockedRuleSetSchema = z.object({
	version: z.number(),
	mode: z.enum(["and", "or"]),
	conditions: z.array(blockedRuleConditionSchema),
});

export type BlockedRuleSet = z.infer<typeof blockedRuleSetSchema>;

export const BLOCKED_RULE_SET_SCHEMA_VERSION = 1;

/**
 * Parses BlockedRuleSetJson at the trust boundary. Returns null for absent, empty,
 * non-JSON, or schema-invalid input — malformed rule sets never leak past this point.
 */
export function parseBlockedRuleSet(
	json: string | null | undefined,
): BlockedRuleSet | null {
	if (!json || json.trim() === "") {
		return null;
	}
	let candidate: unknown;
	try {
		candidate = JSON.parse(json);
	} catch {
		return null;
	}
	const result = blockedRuleSetSchema.safeParse(candidate);
	return result.success ? result.data : null;
}

export function serializeBlockedRuleSet(ruleSet: BlockedRuleSet): string {
	return JSON.stringify(ruleSet);
}

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
	blockedStates: string[];
	blockedTags: string[];
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
