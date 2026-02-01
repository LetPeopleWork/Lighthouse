import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IDeliveryRuleCondition } from "../../../models/DeliveryRules";
import { DeliveryRuleBuilder } from "./DeliveryRuleBuilder";

const mockFields = [
	{ fieldKey: "feature.type", displayName: "Type", isMultiValue: false },
	{ fieldKey: "feature.state", displayName: "State", isMultiValue: false },
	{ fieldKey: "feature.name", displayName: "Name", isMultiValue: false },
];

const mockOperators = ["equals", "notEquals", "contains"];

describe("DeliveryRuleBuilder", () => {
	it("renders empty state with info message", () => {
		render(
			<DeliveryRuleBuilder
				rules={[]}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
			/>,
		);

		expect(screen.getByText(/add at least one rule/i)).toBeInTheDocument();
		expect(screen.getByTestId("add-rule-button")).toBeInTheDocument();
	});

	it("renders rules", () => {
		const rules: IDeliveryRuleCondition[] = [
			{ fieldKey: "feature.type", operator: "equals", value: "Epic" },
		];

		render(
			<DeliveryRuleBuilder
				rules={rules}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
			/>,
		);

		expect(screen.getByTestId("rule-field-select-0")).toBeInTheDocument();
		expect(screen.getByTestId("rule-operator-select-0")).toBeInTheDocument();
		expect(screen.getByTestId("rule-value-input-0")).toBeInTheDocument();
	});

	it("calls onChange when adding a rule", () => {
		const onChange = vi.fn();

		render(
			<DeliveryRuleBuilder
				rules={[]}
				onChange={onChange}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
			/>,
		);

		fireEvent.click(screen.getByTestId("add-rule-button"));

		expect(onChange).toHaveBeenCalledWith([
			{ fieldKey: "feature.type", operator: "equals", value: "" },
		]);
	});

	it("calls onChange when deleting a rule", () => {
		const onChange = vi.fn();
		const rules: IDeliveryRuleCondition[] = [
			{ fieldKey: "feature.type", operator: "equals", value: "Epic" },
			{ fieldKey: "feature.state", operator: "equals", value: "New" },
		];

		render(
			<DeliveryRuleBuilder
				rules={rules}
				onChange={onChange}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
			/>,
		);

		fireEvent.click(screen.getByTestId("rule-delete-0"));

		expect(onChange).toHaveBeenCalledWith([
			{ fieldKey: "feature.state", operator: "equals", value: "New" },
		]);
	});

	it("disables add button when max rules reached", () => {
		const rules: IDeliveryRuleCondition[] = [
			{ fieldKey: "feature.type", operator: "equals", value: "Epic" },
			{ fieldKey: "feature.state", operator: "equals", value: "New" },
		];

		render(
			<DeliveryRuleBuilder
				rules={rules}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={2}
				maxValueLength={500}
			/>,
		);

		expect(screen.getByTestId("add-rule-button")).toBeDisabled();
		expect(screen.getByText(/maximum 2 rules allowed/i)).toBeInTheDocument();
	});

	it("shows warning for incomplete rules", () => {
		const rules: IDeliveryRuleCondition[] = [
			{ fieldKey: "feature.type", operator: "equals", value: "" },
		];

		render(
			<DeliveryRuleBuilder
				rules={rules}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
			/>,
		);

		expect(
			screen.getByText(/please complete all rule fields/i),
		).toBeInTheDocument();
	});

	it("shows AND between multiple rules", () => {
		const rules: IDeliveryRuleCondition[] = [
			{ fieldKey: "feature.type", operator: "equals", value: "Epic" },
			{ fieldKey: "feature.state", operator: "equals", value: "New" },
		];

		render(
			<DeliveryRuleBuilder
				rules={rules}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
			/>,
		);

		// Only one AND should show (between first and second rule, not after last)
		expect(screen.getByText("AND")).toBeInTheDocument();
	});

	it("disables controls when disabled prop is true", () => {
		const rules: IDeliveryRuleCondition[] = [
			{ fieldKey: "feature.type", operator: "equals", value: "Epic" },
		];

		render(
			<DeliveryRuleBuilder
				rules={rules}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
				disabled={true}
			/>,
		);

		expect(screen.getByTestId("add-rule-button")).toBeDisabled();
		expect(screen.getByTestId("rule-delete-0")).toBeDisabled();
	});
});
