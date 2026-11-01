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

describe("validateStateMappings over lists of raw states", () => {
	it.skip("refuses a list that still has the mapping's state next to the mapping's name, and says to rename the mapping", () => {
		const errors = validateStateMappings(
			[inArbeit],
			lists(["In Arbeit", "In Progress"]),
		);

		expect(errors).toEqual([expect.stringContaining("Rename the mapping")]);
		expect(errors[0]).toContain("In Arbeit");
	});

	it.skip("still refuses the name once the mapping's state has been taken out of the list by hand", () => {
		const errors = validateStateMappings([inArbeit], lists(["In Arbeit"]));

		expect(errors).toEqual([expect.stringContaining("Rename the mapping")]);
	});

	it.skip("holds back the autosave for a name that shadows a state", () => {
		expect(
			mappingAutoSaveBlockers([inArbeit], lists(["In Arbeit"])),
		).not.toEqual([]);
	});

	it.skip("refuses a name that shadows a state in To Do or Done as well", () => {
		expect(
			validateStateMappings([inArbeit], lists(["Active"], ["In Arbeit"])),
		).not.toEqual([]);
		expect(
			validateStateMappings(
				[inArbeit],
				lists(["Active"], ["Backlog"], ["In Arbeit"]),
			),
		).not.toEqual([]);
	});

	it.skip("refuses a mapping that groups another mapping's name, naming both", () => {
		const errors = validateStateMappings(
			[
				{ name: "A", states: ["X"] },
				{ name: "B", states: ["A"] },
			],
			lists(["X"]),
		);

		expect(errors).toEqual([expect.stringMatching(/"A".*"B"|"B".*"A"/)]);
		expect(errors[0]).toContain("another mapping");
	});

	it.skip("refuses a mapping whose listed states sit in different categories", () => {
		expect(
			validateStateMappings(
				[{ name: "Review", states: ["Code Review", "Testing"] }],
				lists(["Testing"], ["Backlog", "Code Review"]),
			),
		).not.toEqual([]);
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
		const errors = validateStateMappings(
			[
				{ name: "A", states: ["X"] },
				{ name: "B", states: ["X"] },
			],
			lists(["Y", "X"]),
		);

		expect(errors).toEqual([expect.stringContaining("multiple mappings")]);
	});
});
