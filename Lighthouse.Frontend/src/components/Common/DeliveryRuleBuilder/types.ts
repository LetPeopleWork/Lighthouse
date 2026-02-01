import type { IDeliveryRuleCondition } from "../../../models/DeliveryRules";

export interface DeliveryRuleBuilderProps {
	rules: IDeliveryRuleCondition[];
	onChange: (rules: IDeliveryRuleCondition[]) => void;
	fields: { fieldKey: string; displayName: string; isMultiValue: boolean }[];
	operators: string[];
	maxRules: number;
	maxValueLength: number;
	disabled?: boolean;
}

export interface RuleRowProps {
	rule: IDeliveryRuleCondition;
	index: number;
	fields: { fieldKey: string; displayName: string; isMultiValue: boolean }[];
	operators: string[];
	maxValueLength: number;
	onChange: (index: number, rule: IDeliveryRuleCondition) => void;
	onDelete: (index: number) => void;
	disabled?: boolean;
	isLast: boolean;
}
