import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import {
	Alert,
	Box,
	Button,
	FormControl,
	IconButton,
	InputLabel,
	MenuItem,
	Select,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useRef } from "react";
import type { IDeliveryRuleCondition } from "../../../models/DeliveryRules";
import type { DeliveryRuleBuilderProps, RuleRowProps } from "./types";

const getOperatorLabel = (operator: string): string => {
	switch (operator) {
		case "equals":
			return "Equals";
		case "notequals":
			return "Not Equals";
		case "contains":
			return "Contains";
		default:
			return operator;
	}
};

const RuleRow: React.FC<RuleRowProps> = ({
	rule,
	index,
	fields,
	operators,
	maxValueLength,
	onChange,
	onDelete,
	disabled,
	isLast,
}) => {
	const handleFieldChange = (fieldKey: string) => {
		onChange(index, { ...rule, fieldKey });
	};

	const handleOperatorChange = (operator: string) => {
		onChange(index, { ...rule, operator });
	};

	const handleValueChange = (value: string) => {
		if (value.length <= maxValueLength) {
			onChange(index, { ...rule, value });
		}
	};

	return (
		<Box
			sx={{ display: "flex", gap: 1, alignItems: "center", mb: 1 }}
			data-testid="rule-row"
		>
			<FormControl size="small" sx={{ minWidth: 180 }} disabled={disabled}>
				<InputLabel id={`field-label-${index}`}>Field</InputLabel>
				<Select
					labelId={`field-label-${index}`}
					value={rule.fieldKey}
					onChange={(e) => handleFieldChange(e.target.value)}
					label="Field"
					data-testid={`rule-field-select-${index}`}
				>
					{fields.map((field) => (
						<MenuItem key={field.fieldKey} value={field.fieldKey}>
							{field.displayName}
						</MenuItem>
					))}
				</Select>
			</FormControl>

			<FormControl size="small" sx={{ minWidth: 120 }} disabled={disabled}>
				<InputLabel id={`operator-label-${index}`}>Operator</InputLabel>
				<Select
					labelId={`operator-label-${index}`}
					value={rule.operator}
					onChange={(e) => handleOperatorChange(e.target.value)}
					label="Operator"
					data-testid={`rule-operator-select-${index}`}
				>
					{operators.map((op) => (
						<MenuItem key={op} value={op}>
							{getOperatorLabel(op)}
						</MenuItem>
					))}
				</Select>
			</FormControl>

			<TextField
				size="small"
				label="Value"
				value={rule.value}
				onChange={(e) => handleValueChange(e.target.value)}
				disabled={disabled}
				sx={{ flex: 1 }}
				slotProps={{ htmlInput: { maxLength: maxValueLength } }}
				data-testid={`rule-value-input-${index}`}
			/>

			<IconButton
				onClick={() => onDelete(index)}
				disabled={disabled}
				color="error"
				size="small"
				aria-label="Remove rule"
				data-testid={`rule-delete-${index}`}
			>
				<DeleteIcon />
			</IconButton>

			{!isLast && (
				<Typography
					variant="body2"
					sx={{ color: "text.secondary", fontWeight: 500, ml: 1, mr: 1 }}
				>
					AND
				</Typography>
			)}
		</Box>
	);
};

export const DeliveryRuleBuilder: React.FC<DeliveryRuleBuilderProps> = ({
	rules,
	onChange,
	fields,
	operators,
	maxRules,
	maxValueLength,
	disabled,
}) => {
	const safeFields = fields || [];
	const safeOperators = operators || [];
	const ruleIdsRef = useRef<Map<number, string>>(new Map());
	const nextIdRef = useRef(0);

	// Generate stable IDs for each rule based on their position
	const getRuleId = (index: number): string => {
		if (!ruleIdsRef.current.has(index)) {
			ruleIdsRef.current.set(index, `rule-${nextIdRef.current++}`);
		}
		return ruleIdsRef.current.get(index) || `rule-${index}`;
	};

	const handleAddRule = () => {
		if (rules.length < maxRules && safeFields.length > 0) {
			const newRule: IDeliveryRuleCondition = {
				fieldKey: safeFields[0].fieldKey,
				operator: safeOperators[0] || "equals",
				value: "",
			};
			onChange([...rules, newRule]);
		}
	};

	const handleUpdateRule = (
		index: number,
		updatedRule: IDeliveryRuleCondition,
	) => {
		const newRules = [...rules];
		newRules[index] = updatedRule;
		onChange(newRules);
	};

	const handleDeleteRule = (index: number) => {
		// Clear IDs for this and subsequent rules so they get regenerated
		for (let i = index; i < rules.length; i++) {
			ruleIdsRef.current.delete(i);
		}
		onChange(rules.filter((_, i) => i !== index));
	};

	const hasEmptyRules = rules.some(
		(rule) =>
			!rule.fieldKey.trim() || !rule.operator.trim() || !rule.value.trim(),
	);

	return (
		<Box data-testid="delivery-rule-builder">
			<Typography variant="subtitle1" sx={{ mb: 2 }}>
				Define Rules (all conditions must match)
			</Typography>

			{safeFields.length === 0 && (
				<Alert severity="error" sx={{ mb: 2 }}>
					No fields available. Please ensure custom fields are configured before
					creating rules.
				</Alert>
			)}

			{rules.length === 0 && safeFields.length > 0 && (
				<Alert severity="info" sx={{ mb: 2 }}>
					Add at least one rule to define which features to include.
				</Alert>
			)}

			{rules.map((rule, index) => (
				<RuleRow
					key={getRuleId(index)}
					rule={rule}
					index={index}
					fields={safeFields}
					operators={safeOperators}
					maxValueLength={maxValueLength}
					onChange={handleUpdateRule}
					onDelete={handleDeleteRule}
					disabled={disabled}
					isLast={index === rules.length - 1}
				/>
			))}

			{hasEmptyRules && rules.length > 0 && (
				<Alert severity="warning" sx={{ mb: 2 }}>
					Please complete all rule fields before saving.
				</Alert>
			)}

			<Button
				startIcon={<AddIcon />}
				onClick={handleAddRule}
				disabled={
					disabled || rules.length >= maxRules || safeFields.length === 0
				}
				variant="outlined"
				size="small"
				sx={{ mt: 1 }}
				data-testid="add-rule-button"
			>
				Add Rule
			</Button>

			{rules.length >= maxRules && (
				<Typography
					variant="caption"
					color="text.secondary"
					sx={{ ml: 2, display: "inline" }}
				>
					Maximum {maxRules} rules allowed
				</Typography>
			)}
		</Box>
	);
};
