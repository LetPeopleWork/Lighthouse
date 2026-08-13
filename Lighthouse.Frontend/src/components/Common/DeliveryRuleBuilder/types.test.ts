import { describe, expect, it } from "vitest";
import { isRuleConditionComplete, isValuelessOperator } from "./types";

describe("isValuelessOperator", () => {
	it.each(["isEmpty", "isempty", "isNotEmpty", "ISNOTEMPTY"])(
		"treats %s as needing no value",
		(operator) => {
			expect(isValuelessOperator(operator)).toBe(true);
		},
	);

	it.each(["equals", "notEquals", "contains", "notContains", ""])(
		"treats %s as needing a value",
		(operator) => {
			expect(isValuelessOperator(operator)).toBe(false);
		},
	);
});

describe("isRuleConditionComplete", () => {
	it("accepts a rule with a field, an operator and a value", () => {
		expect(
			isRuleConditionComplete({
				fieldKey: "workitem.state",
				operator: "equals",
				value: "Blocked",
			}),
		).toBe(true);
	});

	it("accepts a valueless operator with no value", () => {
		expect(
			isRuleConditionComplete({
				fieldKey: "workitem.tags",
				operator: "isEmpty",
				value: "",
			}),
		).toBe(true);
	});

	it("rejects a rule with no field", () => {
		expect(
			isRuleConditionComplete({
				fieldKey: "",
				operator: "equals",
				value: "Blocked",
			}),
		).toBe(false);
	});

	it("rejects a rule with no operator", () => {
		expect(
			isRuleConditionComplete({
				fieldKey: "workitem.state",
				operator: "",
				value: "Blocked",
			}),
		).toBe(false);
	});

	it("rejects a rule whose value was left empty", () => {
		expect(
			isRuleConditionComplete({
				fieldKey: "workitem.state",
				operator: "equals",
				value: "",
			}),
		).toBe(false);
	});

	it.each([
		{ fieldKey: "   ", operator: "equals", value: "Blocked" },
		{ fieldKey: "workitem.state", operator: "   ", value: "Blocked" },
		{ fieldKey: "workitem.state", operator: "equals", value: "   " },
	])("rejects whitespace standing in for a real entry (%o)", (condition) => {
		expect(isRuleConditionComplete(condition)).toBe(false);
	});
});
