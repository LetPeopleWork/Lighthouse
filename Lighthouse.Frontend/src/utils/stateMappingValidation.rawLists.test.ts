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
	it("names each problem once, however often it occurs", () => {
		expect(
			validateStateMappings(
				[
					{ name: "A", states: ["X"] },
					{ name: "B", states: ["X"] },
					{ name: "C", states: ["X"] },
				],
				lists(["Analysis"]),
			),
		).toEqual(['Source state "X" is used in multiple mappings.']);
	});

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

	it("names the To Do list when the shadowing sits there", () => {
		expect(
			validateStateMappings(
				[inArbeit],
				lists(["Active"], ["In Arbeit", "In Progress"]),
			),
		).toEqual([
			'Mapping "In Arbeit" has the same name as the To Do state "In Arbeit". Choose another name.',
		]);
	});

	it("names the Done list when the shadowing sits there", () => {
		expect(
			validateStateMappings(
				[inArbeit],
				lists(["Active"], ["Backlog"], ["In Arbeit", "In Progress"]),
			),
		).toEqual([
			'Mapping "In Arbeit" has the same name as the Done state "In Arbeit". Choose another name.',
		]);
	});

	it("does not refuse a name that stands alone in To Do or Done in place of its mapping's states", () => {
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

	it("refuses a mapping whose listed states sit in different categories, naming the two states", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: ["Code Review", "Testing"] }],
				lists(["Testing"], ["Backlog", "Code Review"]),
			),
		).toEqual([
			'Mapping "Review" groups states from different categories: "Code Review" is in To Do and "Testing" is in Doing. Keep a mapping\'s states in one category.',
		]);
	});

	it("refuses a mapping whose only state is listed in two categories", () => {
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

// A list entry naming a mapping stands for the mapping's states, so a state can sit in a category through its
// mapping's name alone. The category rules read the lists that way, as the server does.
describe("validateStateMappings reading a mapping's name as its states", () => {
	it("refuses a mapping whose name stands in Doing while one of its states is listed in Done", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: ["Code Review", "Testing"] }],
				lists(["Review"], ["Backlog"], ["Done", "Testing"]),
			),
		).toEqual([
			'Mapping "Review" groups "Testing", which is in both Doing and Done. Keep a mapping\'s states in one category.',
		]);
	});

	it("reads a mapping named after one of its states as that state and the states not listed beside it", () => {
		expect(
			validateStateMappings(
				[{ name: "Active", states: ["Active", "Resolved"] }],
				lists(["Active"], ["Backlog"], ["Done", "Resolved"]),
			),
		).toEqual([
			'Mapping "Active" groups "Resolved", which is in both Doing and Done. Keep a mapping\'s states in one category.',
		]);
	});

	it("reads a mapping's name that another mapping groups as a plain state", () => {
		expect(
			validateStateMappings(
				[
					{ name: "Review", states: ["Code Review"] },
					{ name: "QA", states: ["Review", "Testing"] },
				],
				lists(["Review"], ["Backlog"], ["Done", "Testing"]),
			),
		).toContain(
			'Mapping "QA" groups states from different categories: "Review" is in Doing and "Testing" is in Done. Keep a mapping\'s states in one category.',
		);
	});

	it("names all three categories when a mapping's state is listed in each of them", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: ["Code Review"] }],
				lists(
					["Code Review"],
					["Backlog", "Code Review"],
					["Done", "Code Review"],
				),
			),
		).toEqual([
			'Mapping "Review" groups "Code Review", which is in To Do, Doing and Done. Keep a mapping\'s states in one category.',
		]);
	});

	it("only asks for a name for a nameless mapping whose states sit in different categories", () => {
		expect(
			validateStateMappings(
				[{ name: " ", states: ["Code Review", "Testing"] }],
				lists(["Testing"], ["Backlog", "Code Review"]),
			),
		).toEqual(["Mapping at position 1 has an empty name."]);
	});
});
