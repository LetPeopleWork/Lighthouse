import { describe, expect, it } from "vitest";
import {
	mappingAutoSaveBlockers,
	validateStateMappings,
} from "./stateMappingValidation";

const review = { name: "Review", states: ["Code Review", "Testing"] };
const active = { name: "Active", states: ["Active", "Resolved"] };

// The settings object the pages pass in carries its wait states next to the
// three lists, so the rule reads them from there.
const settingsWaitingIn = (...waitStates: string[]) => ({
	toDoStates: ["Backlog"],
	doingStates: ["Analysis", "Code Review", "Testing", "Active", "Resolved"],
	doneStates: ["Done"],
	waitStates,
});

const TESTING_IS_PART_OF_REVIEW =
	'Wait state "Testing" is part of the mapping "Review". Use "Review" as the wait state instead.';

describe("validateStateMappings for wait states inside a mapping", () => {
	it("refuses a wait state that names one state inside a mapping, asking for the mapping's name", () => {
		expect(
			validateStateMappings([review], settingsWaitingIn("Testing")),
		).toEqual([TESTING_IS_PART_OF_REVIEW]);
	});

	it("holds back the autosave for that wait state with the same sentence", () => {
		expect(
			mappingAutoSaveBlockers([review], settingsWaitingIn("Testing")),
		).toEqual([TESTING_IS_PART_OF_REVIEW]);
	});

	it("names the wait state trimmed, as it was written, when its case and padding differ", () => {
		expect(
			validateStateMappings(
				[{ name: " Review ", states: ["Code Review", "Testing"] }],
				settingsWaitingIn(" testing "),
			),
		).toEqual([
			'Wait state "testing" is part of the mapping "Review". Use "Review" as the wait state instead.',
		]);
	});

	it("refuses the other state of a mapping named after one of its states", () => {
		expect(
			validateStateMappings([active], settingsWaitingIn("Resolved")),
		).toEqual([
			'Wait state "Resolved" is part of the mapping "Active". Use "Active" as the wait state instead.',
		]);
	});

	it("accepts a wait state equal to a mapping named after one of its states, since it is the mapping's name", () => {
		expect(
			validateStateMappings([active], settingsWaitingIn("active")),
		).toEqual([]);
	});

	it("accepts a wait state that names the mapping", () => {
		expect(
			validateStateMappings([review], settingsWaitingIn("Review")),
		).toEqual([]);
	});

	it("accepts a wait state outside every mapping", () => {
		expect(
			validateStateMappings([review], settingsWaitingIn("Analysis")),
		).toEqual([]);
	});

	it("does not hold back the autosave for a mapping still being filled in", () => {
		expect(
			mappingAutoSaveBlockers(
				[{ name: "", states: ["Testing"] }],
				settingsWaitingIn("Testing"),
			),
		).toEqual([]);
	});

	it("does not hold back the autosave for a stored wait state inside an unchanged mapping", () => {
		const stored = {
			stateMappings: [{ name: "Review", states: ["Code Review", "Testing"] }],
			waitStates: ["Testing"],
		};

		expect(
			mappingAutoSaveBlockers([review], settingsWaitingIn("Testing"), stored),
		).toEqual([]);
	});
});
