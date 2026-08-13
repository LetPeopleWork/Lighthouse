import { describe, expect, it } from "vitest";
import {
	parseRuleSet,
	RULE_SET_SCHEMA_VERSION,
	ruleSetSchema,
	serializeRuleSet,
} from "./WorkItemRules";

describe("rule set wire format", () => {
	it("accepts a well-formed rule set", () => {
		const result = ruleSetSchema.safeParse({
			version: 1,
			mode: "or",
			conditions: [{ fieldKey: "state", operator: "equals", value: "Blocked" }],
		});

		expect(result.success).toBe(true);
	});

	it("rejects a malformed rule set at the boundary", () => {
		const result = ruleSetSchema.safeParse({
			version: "not-a-number",
			mode: "sometimes",
			conditions: "nope",
		});

		expect(result.success).toBe(false);
	});

	it("returns null when parsing malformed JSON at the boundary", () => {
		expect(parseRuleSet('{"conditions": ')).toBeNull();
		expect(parseRuleSet('{"mode":"maybe"}')).toBeNull();
		expect(parseRuleSet(null)).toBeNull();
		expect(parseRuleSet(undefined)).toBeNull();
		expect(parseRuleSet("")).toBeNull();
		expect(parseRuleSet("   ")).toBeNull();
	});

	it("parses a well-formed rule set JSON string", () => {
		const result = parseRuleSet(
			JSON.stringify({
				version: RULE_SET_SCHEMA_VERSION,
				mode: "or",
				conditions: [
					{ fieldKey: "state", operator: "equals", value: "Blocked" },
				],
			}),
		);

		expect(result).not.toBeNull();
		expect(result?.mode).toBe("or");
		expect(result?.conditions).toHaveLength(1);
		expect(result?.conditions[0].fieldKey).toBe("state");
	});

	it("serializes a rule set to JSON", () => {
		const json = serializeRuleSet({
			version: RULE_SET_SCHEMA_VERSION,
			mode: "or",
			conditions: [{ fieldKey: "state", operator: "equals", value: "Blocked" }],
		});

		expect(json).toBe(
			JSON.stringify({
				version: RULE_SET_SCHEMA_VERSION,
				mode: "or",
				conditions: [
					{ fieldKey: "state", operator: "equals", value: "Blocked" },
				],
			}),
		);
	});

	it("round-trips what it serialized", () => {
		const ruleSet = {
			version: RULE_SET_SCHEMA_VERSION,
			mode: "and" as const,
			conditions: [
				{
					fieldKey: "workitem.tags",
					operator: "contains",
					value: "Impediment",
				},
			],
		};

		expect(parseRuleSet(serializeRuleSet(ruleSet))).toEqual(ruleSet);
	});
});
