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

	it("returns error when mapping name collides with direct state", () => {
		const mappings: IStateMapping[] = [
			{ name: "In Progress", states: ["Working"] },
		];
		const directStates = ["New", "In Progress", "Done"];
		const result = validateStateMappings(mappings, directStates);
		expect(result).toContain(
			'Mapping name "In Progress" conflicts with a directly configured state.',
		);
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
