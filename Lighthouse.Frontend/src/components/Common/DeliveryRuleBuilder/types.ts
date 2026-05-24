import type { IWorkItemRuleCondition } from "../../../models/WorkItemRules";

export type DeliveryRuleGroupMode = "and" | "or";

export interface DeliveryRuleBuilderProps {
	rules: IWorkItemRuleCondition[];
	onChange: (rules: IWorkItemRuleCondition[]) => void;
	fields: { fieldKey: string; displayName: string; isMultiValue: boolean }[];
	operators: string[];
	maxRules: number;
	maxValueLength: number;
	disabled?: boolean;
	title?: string;
	emptyStateMessage?: string;
	mode?: DeliveryRuleGroupMode;
	onModeChange?: (mode: DeliveryRuleGroupMode) => void;
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
	separatorLabel?: string;
}

export const isValuelessOperator = (operator: string): boolean => {
	const normalised = operator.toLowerCase();
	return normalised === "isempty" || normalised === "isnotempty";
};
