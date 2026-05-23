import type { IWorkItemRuleCondition } from "../../../models/WorkItemRules";

export interface DeliveryRuleBuilderProps {
	rules: IWorkItemRuleCondition[];
	onChange: (rules: IWorkItemRuleCondition[]) => void;
	fields: { fieldKey: string; displayName: string; isMultiValue: boolean }[];
	operators: string[];
	maxRules: number;
	maxValueLength: number;
	disabled?: boolean;
}

export interface RuleRowProps {
	rule: IWorkItemRuleCondition;
	index: number;
	fields: { fieldKey: string; displayName: string; isMultiValue: boolean }[];
	operators: string[];
	maxValueLength: number;
	onChange: (index: number, rule: IWorkItemRuleCondition) => void;
	onDelete: (index: number) => void;
	disabled?: boolean;
	isLast: boolean;
}
