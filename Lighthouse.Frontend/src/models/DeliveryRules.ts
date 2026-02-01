/**
 * Selection mode for deliveries
 */
export enum DeliverySelectionMode {
	Manual = 0,
	RuleBased = 1,
}

/**
 * A single rule condition for matching features
 */
export interface IDeliveryRuleCondition {
	fieldKey: string;
	operator: string;
	value: string;
}

/**
 * Field definition from the rule schema
 */
export interface IDeliveryRuleFieldDefinition {
	fieldKey: string;
	displayName: string;
	isMultiValue: boolean;
}

/**
 * Rule schema returned from the backend
 */
export interface IDeliveryRuleSchema {
	fields: IDeliveryRuleFieldDefinition[];
	operators: string[];
	maxRules: number;
	maxValueLength: number;
}

/**
 * Request to validate delivery rules
 */
export interface IValidateDeliveryRulesRequest {
	portfolioId: number;
	rules: IDeliveryRuleCondition[];
}

/**
 * Helper class for working with rule conditions
 */
export class DeliveryRuleCondition implements IDeliveryRuleCondition {
	fieldKey: string;
	operator: string;
	value: string;

	constructor(fieldKey = "", operator = "equals", value = "") {
		this.fieldKey = fieldKey;
		this.operator = operator;
		this.value = value;
	}

	isValid(): boolean {
		return (
			this.fieldKey.trim().length > 0 &&
			this.operator.trim().length > 0 &&
			this.value.trim().length > 0
		);
	}

	static fromBackend(data: IDeliveryRuleCondition): DeliveryRuleCondition {
		return new DeliveryRuleCondition(data.fieldKey, data.operator, data.value);
	}
}
