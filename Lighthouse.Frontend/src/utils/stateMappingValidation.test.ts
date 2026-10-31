import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import {
	type IStateLists,
	mappingAutoSaveBlockers,
	validateStateMappings,
} from "./stateMappingValidation";

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
		const result = validateStateMappings(
			mappings,
			lists(["In Arbeit", "In Progress"]),
		);
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
		expect(result).toEqual([
			"Mapping at position 1 has an empty name.",
			'Mapping "at position 1" has no source states.',
			'Duplicate mapping name: "Active".',
		]);
	});

	it("treats a name of only spaces as empty and labels the mapping by its position", () => {
		const mappings: IStateMapping[] = [{ name: "   ", states: [] }];
		const result = validateStateMappings(mappings, lists());
		expect(result).toEqual([
			"Mapping at position 1 has an empty name.",
			'Mapping "at position 1" has no source states.',
		]);
	});

	it("labels a mapping without states by its name without surrounding spaces", () => {
		const mappings: IStateMapping[] = [{ name: " Active ", states: [] }];
		const result = validateStateMappings(mappings, lists());
		expect(result).toEqual(['Mapping "Active" has no source states.']);
	});

	it("reports each name of only spaces as empty without calling them duplicates", () => {
		const mappings: IStateMapping[] = [
			{ name: "", states: ["QA"] },
			{ name: " ", states: ["Testing"] },
		];
		const result = validateStateMappings(mappings, lists());
		expect(result).toEqual([
			"Mapping at position 1 has an empty name.",
			"Mapping at position 2 has an empty name.",
		]);
	});

	it("names a duplicate mapping without surrounding spaces", () => {
		const mappings: IStateMapping[] = [
			{ name: "Active", states: ["QA"] },
			{ name: "  active ", states: ["Testing"] },
		];
		const result = validateStateMappings(mappings, lists());
		expect(result).toEqual(['Duplicate mapping name: "active".']);
	});

	it("names the mapping and the Doing state without surrounding spaces", () => {
		const mappings: IStateMapping[] = [
			{ name: " In Arbeit ", states: ["In Progress"] },
		];
		const result = validateStateMappings(
			mappings,
			lists(["In Arbeit", "In Progress"]),
		);
		expect(result).toEqual([
			'Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.',
		]);
	});

	it("names both mappings without surrounding spaces when a name is a state another mapping groups", () => {
		const mappings: IStateMapping[] = [
			{ name: " Active ", states: ["QA"] },
			{ name: " Group ", states: ["active"] },
		];
		const result = validateStateMappings(mappings, lists());
		expect(result).toEqual([
			'Mapping "Active" has the same name as a state grouped by mapping "Group". Choose another name.',
		]);
	});

	it("reports no clash with a To Do state for a mapping whose name is only spaces", () => {
		const mappings: IStateMapping[] = [{ name: " ", states: ["QA"] }];
		const result = validateStateMappings(mappings, lists([], [""]));
		expect(result).toEqual(["Mapping at position 1 has an empty name."]);
	});
});

describe("mappingAutoSaveBlockers", () => {
	it("names a source state used in two complete mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "A", states: ["X"] },
			{ name: "B", states: ["X"] },
		];
		expect(mappingAutoSaveBlockers(mappings, lists())).toEqual([
			'Source state "X" is used in multiple mappings.',
		]);
	});

	it("names a mapping name used twice by complete mappings", () => {
		const mappings: IStateMapping[] = [
			{ name: "QA", states: ["X"] },
			{ name: "QA", states: ["Y"] },
		];
		expect(mappingAutoSaveBlockers(mappings, lists())).toEqual([
			'Duplicate mapping name: "QA".',
		]);
	});

	it("still reports a mapping named after a Doing state it does not group", () => {
		const mappings: IStateMapping[] = [
			{ name: "In Arbeit", states: ["In Progress"] },
		];
		expect(
			mappingAutoSaveBlockers(mappings, lists(["In Arbeit", "In Progress"])),
		).toEqual([
			'Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.',
		]);
	});

	it.each([
		["an empty row", [{ name: "", states: [] }]],
		[
			"a name without states next to a complete mapping of that name",
			[
				{ name: "QA", states: [] },
				{ name: "QA", states: ["X"] },
			],
		],
		[
			"a nameless row next to a complete mapping sharing its state",
			[
				{ name: "", states: ["X"] },
				{ name: "A", states: ["X"] },
			],
		],
	])("does not block for %s, because it is not saved", (_, mappings) => {
		expect(mappingAutoSaveBlockers(mappings, lists())).toEqual([]);
	});

	it("leaves field errors to the editor", () => {
		const mappings: IStateMapping[] = [{ name: "", states: [] }];
		expect(mappingAutoSaveBlockers(mappings, lists())).toEqual([]);
		expect(validateStateMappings(mappings, lists())).not.toEqual([]);
	});
});
