import { describe, expect, it } from "vitest";
import {
	type EvaluableWorkItem,
	evaluateCondition,
	formatConditions,
	matchesAllConditions,
} from "./evaluateCondition";

const baseItem: EvaluableWorkItem = {
	type: "Epic",
	state: "Active",
	name: "Authentication Module",
	referenceId: "AB-12",
	parentReferenceId: "AB-1",
	tags: ["Priority", "Q1"],
	additionalFieldValues: { 42: "alpha", 7: "beta" },
};

describe("evaluateCondition — scalar field operator parity with C# RuleEvaluator", () => {
	it.each([
		["workitem.type", "equals", "Epic", true],
		["workitem.type", "equals", "EPIC", true],
		["workitem.type", "equals", "Story", false],
		["workitem.state", "equals", "Active", true],
		["workitem.state", "equals", "Done", false],
		["workitem.name", "equals", "Authentication Module", true],
		["workitem.referenceid", "equals", "AB-12", true],
		["workitem.parentreferenceid", "equals", "AB-1", true],
		["workitem.type", "notEquals", "Story", true],
		["workitem.type", "notEquals", "epic", false],
		["workitem.name", "contains", "Auth", true],
		["workitem.name", "contains", "AUTH", true],
		["workitem.name", "contains", "Payment", false],
		["workitem.referenceid", "contains", "B-1", true],
	] as const)("%s %s %s -> %s", (fieldKey, operator, value, expected) => {
		const condition = { fieldKey, operator, value };
		expect(evaluateCondition(baseItem, condition)).toBe(expected);
	});
});

describe("evaluateCondition — tags field operator parity", () => {
	it.each([
		["equals", "Priority", true],
		["equals", "PRIORITY", true],
		["equals", "Backlog", false],
		["notEquals", "Backlog", true],
		["notEquals", "Priority", false],
		["contains", "Pri", true],
		["contains", "PRI", true],
		["contains", "Xyz", false],
	] as const)("tags %s %s -> %s", (operator, value, expected) => {
		const condition = { fieldKey: "workitem.tags", operator, value };
		expect(evaluateCondition(baseItem, condition)).toBe(expected);
	});
});

describe("evaluateCondition — additionalField lookup", () => {
	it("resolves additionalField.{id} via additionalFieldValues map", () => {
		const condition = {
			fieldKey: "additionalField.42",
			operator: "equals",
			value: "alpha",
		};
		expect(evaluateCondition(baseItem, condition)).toBe(true);
	});

	it("returns false for an additionalField id that is not in the item", () => {
		const condition = {
			fieldKey: "additionalField.999",
			operator: "equals",
			value: "anything",
		};
		expect(evaluateCondition(baseItem, condition)).toBe(false);
	});

	it("returns false for a malformed additionalField key", () => {
		const condition = {
			fieldKey: "additionalField.not-a-number",
			operator: "equals",
			value: "alpha",
		};
		expect(evaluateCondition(baseItem, condition)).toBe(false);
	});
});

describe("evaluateCondition — defensive defaults", () => {
	it("returns false for an unknown field key", () => {
		const condition = {
			fieldKey: "workitem.unknown",
			operator: "equals",
			value: "Epic",
		};
		expect(evaluateCondition(baseItem, condition)).toBe(false);
	});

	it("returns false for an unknown operator", () => {
		const condition = {
			fieldKey: "workitem.type",
			operator: "greaterThan",
			value: "Epic",
		};
		expect(evaluateCondition(baseItem, condition)).toBe(false);
	});
});

describe("matchesAllConditions", () => {
	it("returns true when every condition matches the item", () => {
		const conditions = [
			{ fieldKey: "workitem.type", operator: "equals", value: "Epic" },
			{ fieldKey: "workitem.state", operator: "equals", value: "Active" },
			{ fieldKey: "workitem.tags", operator: "contains", value: "Pri" },
		];
		expect(matchesAllConditions(baseItem, conditions)).toBe(true);
	});

	it("returns false when at least one condition fails", () => {
		const conditions = [
			{ fieldKey: "workitem.type", operator: "equals", value: "Epic" },
			{ fieldKey: "workitem.state", operator: "equals", value: "Done" },
		];
		expect(matchesAllConditions(baseItem, conditions)).toBe(false);
	});

	it("returns true for an empty conditions list (vacuous truth; mirrors C# conditions.All)", () => {
		expect(matchesAllConditions(baseItem, [])).toBe(true);
	});
});

describe("evaluateCondition — does not mutate input", () => {
	it("leaves the work item tags array untouched after evaluation", () => {
		const tagsBefore = [...baseItem.tags];
		evaluateCondition(baseItem, {
			fieldKey: "workitem.tags",
			operator: "contains",
			value: "Pri",
		});
		expect(baseItem.tags).toEqual(tagsBefore);
	});
});

describe("formatConditions — human-readable excludedSummary for FilteredThroughputChip tooltip", () => {
	it("formats a single equals condition with no quotes around the value", () => {
		expect(
			formatConditions([
				{ fieldKey: "workitem.type", operator: "equals", value: "Bug" },
			]),
		).toBe("Type = Bug");
	});

	it("formats a contains condition with quotes around the value", () => {
		expect(
			formatConditions([
				{
					fieldKey: "workitem.tags",
					operator: "contains",
					value: "maintenance",
				},
			]),
		).toBe('Tags contains "maintenance"');
	});

	it("joins multiple conditions with '; '", () => {
		expect(
			formatConditions([
				{ fieldKey: "workitem.type", operator: "equals", value: "Bug" },
				{
					fieldKey: "workitem.tags",
					operator: "contains",
					value: "maintenance",
				},
			]),
		).toBe('Type = Bug; Tags contains "maintenance"');
	});

	it("renders notEquals as '!='", () => {
		expect(
			formatConditions([
				{ fieldKey: "workitem.state", operator: "notequals", value: "Closed" },
			]),
		).toBe("State != Closed");
	});

	it("renders additionalField.{id} as 'Custom field {id}'", () => {
		expect(
			formatConditions([
				{ fieldKey: "additionalField.42", operator: "equals", value: "alpha" },
			]),
		).toBe("Custom field 42 = alpha");
	});

	it("returns empty string for empty conditions array", () => {
		expect(formatConditions([])).toBe("");
	});

	it("is case-insensitive on the fieldKey lookup", () => {
		expect(
			formatConditions([
				{ fieldKey: "WorkItem.Type", operator: "equals", value: "Bug" },
			]),
		).toBe("Type = Bug");
	});
});
