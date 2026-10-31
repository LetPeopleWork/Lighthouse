import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import { validateStateMappings } from "./stateMappingValidation";

describe("validateStateMappings", () => {
	it("returns no errors for empty mappings", () => {
		const result = validateStateMappings([], []);
		expect(result).toEqual([]);
	});

	it("returns no errors for valid mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress", "In Review"] },
			{ name: "Closed", states: ["Done", "Resolved"] },
		];
		const result = validateStateMappings(mappings, []);
		expect(result).toEqual([]);
	});

	it("returns error for empty mapping name", () => {
		const mappings: IStateMapping[] = [{ name: "", states: ["In Progress"] }];
		const result = validateStateMappings(mappings, []);
		expect(result).toContain("Mapping at position 1 has an empty name.");
	});

	it("returns error for mapping with no source states", () => {
		const mappings: IStateMapping[] = [{ name: "Active", states: [] }];
		const result = validateStateMappings(mappings, []);
		expect(result).toContain('Mapping "Active" has no source states.');
	});

	it("returns error for duplicate mapping names (case-insensitive)", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress"] },
			{ name: "active", states: ["Working"] },
		];
		const result = validateStateMappings(mappings, []);
		expect(result).toContain('Duplicate mapping name: "active".');
	});

	it("returns error for overlapping source states across mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress"] },
			{ name: "Working", states: ["In Progress", "Development"] },
		];
		const result = validateStateMappings(mappings, []);
		expect(result).toContain(
			'Source state "In Progress" is used in multiple mappings.',
		);
	});

	it("reports a mapping named after a Doing state it does not contain", () => {
		const mappings: IStateMapping[] = [
			{ name: "In Arbeit", states: ["In Progress"] },
		];
		const result = validateStateMappings(mappings, [
			"In Arbeit",
			"In Progress",
		]);
		expect(result).toEqual([
			'Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.',
		]);
	});

	it("allows a mapping named after a Doing state it contains", () => {
		const mappings: IStateMapping[] = [
			{ name: "In Arbeit", states: ["In Arbeit", "In Progress"] },
		];
		const result = validateStateMappings(mappings, ["In Arbeit"]);
		expect(result).toEqual([]);
	});

	it("allows a mapping already taken into the Doing list under its own name", () => {
		const mappings: IStateMapping[] = [
			{ name: "Implementation", states: ["Active", "Resolved"] },
		];
		const result = validateStateMappings(mappings, ["Implementation"]);
		expect(result).toEqual([]);
	});

	it("reports no name clash for a mapping that has no states yet", () => {
		const mappings: IStateMapping[] = [{ name: "In Arbeit", states: [] }];
		const result = validateStateMappings(mappings, ["In Arbeit"]);
		expect(result).toEqual(['Mapping "In Arbeit" has no source states.']);
	});

	it("returns multiple errors for multiple issues", () => {
		const mappings: IStateMapping[] = [
			{ name: "", states: [] },
			{ name: "Active", states: ["In Progress"] },
			{ name: "Active", states: ["Working"] },
		];
		const result = validateStateMappings(mappings, []);
		expect(result.length).toBeGreaterThanOrEqual(3);
	});
});
