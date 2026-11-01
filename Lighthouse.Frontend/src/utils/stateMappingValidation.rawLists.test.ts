import { describe, expect, it } from "vitest";
import {
	mappingAutoSaveBlockers,
	validateStateMappings,
} from "./stateMappingValidation";

const inArbeit = { name: "In Arbeit", states: ["In Progress"] };

const lists = (doing: string[], toDo = ["Backlog"], done = ["Done"]) => ({
	toDoStates: toDo,
	doingStates: doing,
	doneStates: done,
});

const SHADOWS_A_DOING_STATE =
	'Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.';

describe("validateStateMappings over lists of raw states", () => {
	it("refuses a name that sits beside one of its mapping's states, with the sentence the server sends", () => {
		expect(
			validateStateMappings([inArbeit], lists(["In Arbeit", "In Progress"])),
		).toEqual([SHADOWS_A_DOING_STATE]);
	});

	it("holds back the autosave for that name with the same sentence", () => {
		expect(
			mappingAutoSaveBlockers([inArbeit], lists(["In Arbeit", "In Progress"])),
		).toEqual([SHADOWS_A_DOING_STATE]);
	});

	it("does not refuse a name that stands alone in place of its mapping's states", () => {
		expect(validateStateMappings([inArbeit], lists(["In Arbeit"]))).toEqual([]);
	});

	it.skip("names the To Do list when the shadowing sits there", () => {
		expect(
			validateStateMappings(
				[inArbeit],
				lists(["Active"], ["In Arbeit", "In Progress"]),
			),
		).toEqual([
			'Mapping "In Arbeit" has the same name as the To Do state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.',
		]);
	});

	it.skip("names the Done list when the shadowing sits there", () => {
		expect(
			validateStateMappings(
				[inArbeit],
				lists(["Active"], ["Backlog"], ["In Arbeit", "In Progress"]),
			),
		).toEqual([
			'Mapping "In Arbeit" has the same name as the Done state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.',
		]);
	});

	it.skip("does not refuse a name that stands alone in To Do or Done in place of its mapping's states", () => {
		expect(
			validateStateMappings([inArbeit], lists(["Active"], ["In Arbeit"])),
		).toEqual([]);
		expect(
			validateStateMappings(
				[inArbeit],
				lists(["Active"], ["Backlog"], ["In Arbeit"]),
			),
		).toEqual([]);
	});

	it("refuses a mapping that groups another mapping's name, with the sentence the server sends", () => {
		expect(
			validateStateMappings(
				[
					{ name: "A", states: ["X"] },
					{ name: "B", states: ["A"] },
				],
				lists(["X"]),
			),
		).toEqual([
			'Mapping "A" has the same name as a state grouped by mapping "B". Choose another name.',
		]);
	});

	it.skip("refuses a mapping whose listed states sit in different categories, naming the two states", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: ["Code Review", "Testing"] }],
				lists(["Testing"], ["Backlog", "Code Review"]),
			),
		).toEqual([
			'Mapping "Review" groups states from different categories: "Code Review" is in To Do and "Testing" is in Doing. Keep a mapping\'s states in one category.',
		]);
	});

	it.skip("refuses a mapping whose only state is listed in two categories", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: ["Testing"] }],
				lists(["Active"], ["Testing"], ["Testing"]),
			),
		).toEqual([
			'Mapping "Review" groups "Testing", which is in both To Do and Done. Keep a mapping\'s states in one category.',
		]);
	});

	it("says nothing about a mapping state that is in none of the lists", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: ["Code Review", "Testing"] }],
				lists(["Code Review"]),
			),
		).toEqual([]);
	});

	it("accepts a mapping named after one of its own states", () => {
		expect(
			validateStateMappings(
				[{ name: "Active", states: ["Active", "Resolved"] }],
				lists(["Active", "Resolved"]),
			),
		).toEqual([]);
	});

	it("reports two mappings over the same state, whatever the Doing list holds", () => {
		expect(
			validateStateMappings(
				[
					{ name: "A", states: ["X"] },
					{ name: "B", states: ["X"] },
				],
				lists(["Y", "X"]),
			),
		).toEqual(['Source state "X" is used in multiple mappings.']);
	});

	it("reports two mappings with the same name once", () => {
		expect(
			validateStateMappings(
				[
					{ name: "Review", states: ["Code Review"] },
					{ name: "review", states: ["Testing"] },
				],
				lists(["Code Review", "Testing"]),
			),
		).toEqual(['Duplicate mapping name: "review".']);
	});

	it("reports a mapping with no states, with the lists in view", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: [] }],
				lists(["Code Review"]),
			),
		).toEqual(['Mapping "Review" has no source states.']);
	});

	it("does not stumble on blank entries in the lists", () => {
		expect(() =>
			validateStateMappings(
				[{ name: "Review", states: ["Code Review"] }],
				lists(["", "  ", "Code Review"], ["Backlog", ""], ["Done", "\t"]),
			),
		).not.toThrow();
	});
});
