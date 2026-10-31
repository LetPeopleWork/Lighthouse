import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import { type IStateLists, validateStateMappings } from "./stateMappingValidation";

const lists = (
	doingStates: string[] = [],
	toDoStates: string[] = [],
	doneStates: string[] = [],
): IStateLists => ({ toDoStates, doingStates, doneStates });

describe("validateStateMappings", () => {
	it("returns no errors for empty mappings", () => {
		const result = validateStateMappings([], lists());
		expect(result).toEqual([]);
	});

	it("returns no errors for valid mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress", "In Review"] },
			{ name: "Closed", states: ["Done", "Resolved"] },
		];
		const result = validateStateMappings(mappings, lists());
		expect(result).toEqual([]);
	});

	it("returns error for empty mapping name", () => {
		const mappings: IStateMapping[] = [{ name: "", states: ["In Progress"] }];
		const result = validateStateMappings(mappings, lists());
		expect(result).toContain("Mapping at position 1 has an empty name.");
	});

	it("returns error for mapping with no source states", () => {
		const mappings: IStateMapping[] = [{ name: "Active", states: [] }];
		const result = validateStateMappings(mappings, lists());
		expect(result).toContain('Mapping "Active" has no source states.');
	});

	it("returns error for duplicate mapping names (case-insensitive)", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress"] },
			{ name: "active", states: ["Working"] },
		];
		const result = validateStateMappings(mappings, lists());
		expect(result).toContain('Duplicate mapping name: "active".');
	});

	it("returns error for overlapping source states across mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["In Progress"] },
			{ name: "Working", states: ["In Progress", "Development"] },
		];
		const result = validateStateMappings(mappings, lists());
		expect(result).toContain(
			'Source state "In Progress" is used in multiple mappings.',
		);
	});

	it("reports a mapping named after a Doing state it does not contain", () => {
		const mappings: IStateMapping[] = [
			{ name: "In Arbeit", states: ["In Progress"] },
		];
		const result = validateStateMappings(mappings, lists(["In Arbeit", "In Progress"]));
		expect(result).toEqual([
			'Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.',
		]);
	});

	it("allows a mapping named after a Doing state it contains", () => {
		const mappings: IStateMapping[] = [
			{ name: "In Arbeit", states: ["In Arbeit", "In Progress"] },
		];
		const result = validateStateMappings(mappings, lists(["In Arbeit"]));
		expect(result).toEqual([]);
	});

	it("allows a mapping already taken into the Doing list under its own name", () => {
		const mappings: IStateMapping[] = [
			{ name: "Implementation", states: ["Active", "Resolved"] },
		];
		const result = validateStateMappings(mappings, lists(["Implementation"]));
		expect(result).toEqual([]);
	});

	it("reports no name clash for a mapping that has no states yet", () => {
		const mappings: IStateMapping[] = [{ name: "In Arbeit", states: [] }];
		const result = validateStateMappings(mappings, lists(["In Arbeit"]));
		expect(result).toEqual(['Mapping "In Arbeit" has no source states.']);
	});

	it("reports a mapping named after a state another mapping groups, and not the mapping that groups it", () => {
		const mappings: IStateMapping[] = [
			{ name: "Group", states: ["In Arbeit"] },
			{ name: "In Arbeit", states: ["Review"] },
		];
		const result = validateStateMappings(
			mappings,
			lists(["In Progress", "Group", "In Arbeit"]),
		);
		expect(result).toEqual([
			'Mapping "In Arbeit" has the same name as a state grouped by mapping "Group". Choose another name.',
		]);
	});

	it("reports a mapping named after a To Do state", () => {
		const mappings: IStateMapping[] = [
			{ name: " in arbeit ", states: ["In Progress"] },
		];
		const result = validateStateMappings(
			mappings,
			lists(["Group"], ["In Arbeit"]),
		);
		expect(result).toEqual([
			'Mapping "in arbeit" has the same name as the To Do state "In Arbeit". Choose another name.',
		]);
	});

	it("reports a mapping named after a Done state", () => {
		const mappings: IStateMapping[] = [
			{ name: "In Arbeit", states: ["In Progress"] },
		];
		const result = validateStateMappings(
			mappings,
			lists(["Group"], [], ["In Arbeit"]),
		);
		expect(result).toEqual([
			'Mapping "In Arbeit" has the same name as the Done state "In Arbeit". Choose another name.',
		]);
	});

	it("reports no To Do or Done clash for a mapping that has no states yet", () => {
		const mappings: IStateMapping[] = [{ name: "In Arbeit", states: [] }];
		const result = validateStateMappings(
			mappings,
			lists([], ["In Arbeit"], ["In Arbeit"]),
		);
		expect(result).toEqual(['Mapping "In Arbeit" has no source states.']);
	});

	it("returns multiple errors for multiple issues", () => {
		const mappings: IStateMapping[] = [
			{ name: "", states: [] },
			{ name: "Active", states: ["In Progress"] },
			{ name: "Active", states: ["Working"] },
		];
		const result = validateStateMappings(mappings, lists());
		expect(result.length).toBeGreaterThanOrEqual(3);
	});
});
