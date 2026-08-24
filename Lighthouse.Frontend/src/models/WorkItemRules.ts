import { z } from "zod";

/**
 * How a Delivery's Features are chosen. The numbers are the storage format: the server keeps this
 * as a bare number, so the two lists have to agree member for member. Adding one here means adding
 * it to DeliverySelectionMode.cs in the backend, and vice versa - nothing checks at runtime, and a
 * mismatch reads every saved Delivery as a kind it never was.
 */
export enum DeliverySelectionMode {
	Manual = 0,
	RuleBased = 1,
	SourceBound = 2,
}

/**
 * A single rule condition for matching work items
 */
export interface IWorkItemRuleCondition {
	fieldKey: string;
	operator: string;
	value: string;
}

/**
 * Field definition from the rule schema
 */
export interface IWorkItemRuleFieldDefinition {
	fieldKey: string;
	displayName: string;
	isMultiValue: boolean;
}

/**
 * Rule schema returned from the backend
 */
export interface IWorkItemRuleSchema {
	fields: IWorkItemRuleFieldDefinition[];
	operators: string[];
	maxRules: number;
	maxValueLength: number;
}

export const ruleConditionSchema = z.object({
	fieldKey: z.string(),
	operator: z.string(),
	value: z.string(),
});

export const RULE_SET_SCHEMA_VERSION = 1;

/**
 * The stored shape of every rule set the settings screens read and write — blocked rules
 * and the forecast filter alike. Parsing through the schema keeps a malformed rule set
 * from reaching the rule builder; the backend stores all of them the same way.
 *
 * Version and mode are filled in when absent, because rule sets stored before the match
 * mode existed carry neither and still describe a working set of rules. An invalid mode
 * is a different matter and rejects the whole set.
 */
export const ruleSetSchema = z.object({
	version: z.number().default(RULE_SET_SCHEMA_VERSION),
	mode: z.enum(["and", "or"]).default("and"),
	conditions: z.array(ruleConditionSchema),
});

export type IWorkItemRuleSet = z.infer<typeof ruleSetSchema>;

/**
 * Returns null for absent, empty, non-JSON or schema-invalid input, so a malformed rule
 * set never leaks past this point.
 */
export function parseRuleSet(
	json: string | null | undefined,
): IWorkItemRuleSet | null {
	if (!json) {
		return null;
	}

	let candidate: unknown;
	try {
		candidate = JSON.parse(json);
	} catch {
		return null;
	}

	const result = ruleSetSchema.safeParse(candidate);
	return result.success ? result.data : null;
}

export function serializeRuleSet(ruleSet: IWorkItemRuleSet): string {
	return JSON.stringify(ruleSet);
}

/**
 * Request to validate delivery rules
 */
export interface IValidateDeliveryRulesRequest {
	portfolioId: number;
	rules: IWorkItemRuleCondition[];
}

/**
 * Helper class for working with rule conditions
 */
export class WorkItemRuleCondition implements IWorkItemRuleCondition {
	fieldKey: string;
	operator: string;
	value: string;

	constructor(fieldKey = "", operator = "equals", value = "") {
		this.fieldKey = fieldKey;
		this.operator = operator;
		this.value = value;
	}

	static fromBackend(data: IWorkItemRuleCondition): WorkItemRuleCondition {
		return new WorkItemRuleCondition(data.fieldKey, data.operator, data.value);
	}
}
