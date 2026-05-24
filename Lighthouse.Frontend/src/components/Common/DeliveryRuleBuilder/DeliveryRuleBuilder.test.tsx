import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IWorkItemRuleCondition } from "../../../models/WorkItemRules";
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
		const rules: IWorkItemRuleCondition[] = [
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
		const rules: IWorkItemRuleCondition[] = [
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
		const rules: IWorkItemRuleCondition[] = [
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
		const rules: IWorkItemRuleCondition[] = [
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
		const rules: IWorkItemRuleCondition[] = [
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
		const rules: IWorkItemRuleCondition[] = [
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

	it("renders default title when no title prop is provided", () => {
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

		expect(
			screen.getByText("Define Rules (all conditions must match)"),
		).toBeInTheDocument();
	});

	it("renders custom title when title prop is provided", () => {
		render(
			<DeliveryRuleBuilder
				rules={[]}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
				title="Exclude items where…"
			/>,
		);

		expect(screen.getByText("Exclude items where…")).toBeInTheDocument();
		expect(
			screen.queryByText("Define Rules (all conditions must match)"),
		).not.toBeInTheDocument();
	});

	it("renders default empty-state message when no emptyStateMessage prop is provided", () => {
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

		expect(
			screen.getByText(
				"Add at least one rule to define which features to include.",
			),
		).toBeInTheDocument();
	});

	it("renders custom empty-state message when emptyStateMessage prop is provided", () => {
		render(
			<DeliveryRuleBuilder
				rules={[]}
				onChange={vi.fn()}
				fields={mockFields}
				operators={mockOperators}
				maxRules={20}
				maxValueLength={500}
				emptyStateMessage="Add at least one rule to exclude items from throughput."
			/>,
		);

		expect(
			screen.getByText(
				"Add at least one rule to exclude items from throughput.",
			),
		).toBeInTheDocument();
		expect(
			screen.queryByText(
				"Add at least one rule to define which features to include.",
			),
		).not.toBeInTheDocument();
	});

	describe("Empty-value operators", () => {
		const operatorsWithEmpty = [
			...mockOperators,
			"notContains",
			"isEmpty",
			"isNotEmpty",
		];

		it("hides the Value input when the operator is isEmpty", () => {
			const rules: IWorkItemRuleCondition[] = [
				{ fieldKey: "feature.type", operator: "isEmpty", value: "" },
			];

			render(
				<DeliveryRuleBuilder
					rules={rules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={operatorsWithEmpty}
					maxRules={20}
					maxValueLength={500}
				/>,
			);

			expect(screen.getByTestId("rule-operator-select-0")).toBeInTheDocument();
			expect(
				screen.queryByTestId("rule-value-input-0"),
			).not.toBeInTheDocument();
		});

		it("hides the Value input when the operator is isNotEmpty", () => {
			const rules: IWorkItemRuleCondition[] = [
				{ fieldKey: "feature.type", operator: "isNotEmpty", value: "" },
			];

			render(
				<DeliveryRuleBuilder
					rules={rules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={operatorsWithEmpty}
					maxRules={20}
					maxValueLength={500}
				/>,
			);

			expect(
				screen.queryByTestId("rule-value-input-0"),
			).not.toBeInTheDocument();
		});

		it("does not require a value to be filled for isEmpty rules", () => {
			const rules: IWorkItemRuleCondition[] = [
				{ fieldKey: "feature.type", operator: "isEmpty", value: "" },
			];

			render(
				<DeliveryRuleBuilder
					rules={rules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={operatorsWithEmpty}
					maxRules={20}
					maxValueLength={500}
				/>,
			);

			expect(
				screen.queryByText(/please complete all rule fields/i),
			).not.toBeInTheDocument();
		});

		it("still surfaces the missing-value warning for contains operator", () => {
			const rules: IWorkItemRuleCondition[] = [
				{ fieldKey: "feature.type", operator: "contains", value: "" },
			];

			render(
				<DeliveryRuleBuilder
					rules={rules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={operatorsWithEmpty}
					maxRules={20}
					maxValueLength={500}
				/>,
			);

			expect(
				screen.getByText(/please complete all rule fields/i),
			).toBeInTheDocument();
		});

		it("blanks out the value on the persisted rule when switching to isEmpty", () => {
			const onChange = vi.fn();
			const rules: IWorkItemRuleCondition[] = [
				{ fieldKey: "feature.type", operator: "equals", value: "Bug" },
			];

			render(
				<DeliveryRuleBuilder
					rules={rules}
					onChange={onChange}
					fields={mockFields}
					operators={operatorsWithEmpty}
					maxRules={20}
					maxValueLength={500}
				/>,
			);

			const operatorSelect = screen
				.getByTestId("rule-operator-select-0")
				.querySelector("input");
			expect(operatorSelect).not.toBeNull();

			fireEvent.change(operatorSelect as HTMLInputElement, {
				target: { value: "isEmpty" },
			});

			expect(onChange).toHaveBeenCalledWith([
				{ fieldKey: "feature.type", operator: "isEmpty", value: "" },
			]);
		});
	});

	describe("Group mode (AND/OR) toggle", () => {
		const twoRules: IWorkItemRuleCondition[] = [
			{ fieldKey: "feature.type", operator: "equals", value: "Bug" },
			{ fieldKey: "feature.state", operator: "equals", value: "Done" },
		];

		it("does not render the mode toggle when onModeChange is not supplied", () => {
			render(
				<DeliveryRuleBuilder
					rules={twoRules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={mockOperators}
					maxRules={20}
					maxValueLength={500}
				/>,
			);

			expect(
				screen.queryByTestId("rule-group-mode-toggle"),
			).not.toBeInTheDocument();
		});

		it("does not render the mode toggle when there are fewer than two rules", () => {
			const singleRule: IWorkItemRuleCondition[] = [
				{ fieldKey: "feature.type", operator: "equals", value: "Bug" },
			];

			render(
				<DeliveryRuleBuilder
					rules={singleRule}
					onChange={vi.fn()}
					fields={mockFields}
					operators={mockOperators}
					maxRules={20}
					maxValueLength={500}
					onModeChange={vi.fn()}
				/>,
			);

			expect(
				screen.queryByTestId("rule-group-mode-toggle"),
			).not.toBeInTheDocument();
		});

		it("renders the mode toggle when two or more rules and onModeChange is supplied", () => {
			render(
				<DeliveryRuleBuilder
					rules={twoRules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={mockOperators}
					maxRules={20}
					maxValueLength={500}
					onModeChange={vi.fn()}
				/>,
			);

			expect(screen.getByTestId("rule-group-mode-toggle")).toBeInTheDocument();
		});

		it("emits onModeChange when the user flips to OR", () => {
			const onModeChange = vi.fn();

			render(
				<DeliveryRuleBuilder
					rules={twoRules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={mockOperators}
					maxRules={20}
					maxValueLength={500}
					mode="and"
					onModeChange={onModeChange}
				/>,
			);

			fireEvent.click(
				screen.getByRole("button", { name: /match any rule \(or\)/i }),
			);

			expect(onModeChange).toHaveBeenCalledExactlyOnceWith("or");
		});

		it("renders the separator label as AND by default", () => {
			render(
				<DeliveryRuleBuilder
					rules={twoRules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={mockOperators}
					maxRules={20}
					maxValueLength={500}
				/>,
			);

			expect(screen.getByText("AND")).toBeInTheDocument();
			expect(screen.queryByText("OR")).not.toBeInTheDocument();
		});

		it("renders the separator label as OR when mode='or'", () => {
			render(
				<DeliveryRuleBuilder
					rules={twoRules}
					onChange={vi.fn()}
					fields={mockFields}
					operators={mockOperators}
					maxRules={20}
					maxValueLength={500}
					mode="or"
					onModeChange={vi.fn()}
				/>,
			);

			expect(screen.getByText("OR")).toBeInTheDocument();
		});
	});
});
