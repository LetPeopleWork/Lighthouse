import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import {
	type IStateLists,
	type IStoredStateSettings,
	mappingAutoSaveBlockers,
	storedStateSettingsOf,
} from "./stateMappingValidation";

// An owner saved before these rules existed may already break them. Like the
// server, the page only judges what an edit adds or changes, so an unrelated
// edit on such an owner still saves.

const storedAs = (
	stateMappings: IStateMapping[],
	lists: IStateLists,
): IStoredStateSettings => ({
	stateMappings,
	waitStates: [],
	toDoStates: lists.toDoStates,
	doingStates: lists.doingStates,
	doneStates: lists.doneStates,
});

// The unrelated edit: one more To Do state than the server holds.
const withNewToDoState = (lists: IStateLists): IStateLists => ({
	...lists,
	toDoStates: [...lists.toDoStates, "Ideas"],
});

describe("mappingAutoSaveBlockers on a stored owner that already breaks a rule", () => {
	const dev: IStateMapping = { name: "Dev", states: ["Build", "Test"] };
	const shadowingLists: IStateLists = {
		toDoStates: ["Backlog"],
		doingStates: ["Dev", "Build"],
		doneStates: ["Done"],
	};
	const DEV_SHADOWS_A_STATE =
		'Mapping "Dev" has the same name as the Doing state "Dev". Add "Dev" to this mapping to group it, or choose another name.';

	it("does not hold back an unrelated edit for a stored name that hides a listed state", () => {
		expect(
			mappingAutoSaveBlockers(
				[dev],
				withNewToDoState(shadowingLists),
				storedAs([dev], shadowingLists),
			),
		).toEqual([]);
	});

	it("still refuses that name when the mapping is new", () => {
		expect(
			mappingAutoSaveBlockers(
				[dev],
				withNewToDoState(shadowingLists),
				storedAs([], shadowingLists),
			),
		).toEqual([DEV_SHADOWS_A_STATE]);
	});

	it("still refuses that name when the edit lists it in Doing for the first time", () => {
		expect(
			mappingAutoSaveBlockers(
				[dev],
				shadowingLists,
				storedAs([dev], { ...shadowingLists, doingStates: ["Build"] }),
			),
		).toEqual([DEV_SHADOWS_A_STATE]);
	});

	const groupsB: IStateMapping = { name: "A", states: ["B", "C"] };
	const namedB: IStateMapping = { name: "B", states: ["X", "Y"] };
	const chainLists: IStateLists = {
		toDoStates: ["Backlog"],
		doingStates: ["B"],
		doneStates: ["Done"],
	};
	const B_IS_GROUPED_BY_A =
		'Mapping "B" has the same name as a state grouped by mapping "A". Choose another name.';

	it("does not hold back an unrelated edit for a stored mapping named after a state another mapping groups", () => {
		expect(
			mappingAutoSaveBlockers(
				[groupsB, namedB],
				withNewToDoState(chainLists),
				storedAs([groupsB, namedB], chainLists),
			),
		).toEqual([]);
	});

	it("still refuses that chain when one of its mappings is new", () => {
		expect(
			mappingAutoSaveBlockers(
				[groupsB, namedB],
				withNewToDoState(chainLists),
				storedAs([groupsB], chainLists),
			),
		).toEqual([B_IS_GROUPED_BY_A]);
	});

	const split: IStateMapping = { name: "Flow", states: ["Active", "Closed"] };
	const splitLists: IStateLists = {
		toDoStates: ["Backlog"],
		doingStates: ["Active"],
		doneStates: ["Closed"],
	};
	const FLOW_SPANS_TWO_CATEGORIES =
		'Mapping "Flow" groups states from different categories: "Active" is in Doing and "Closed" is in Done. Keep a mapping\'s states in one category.';

	it("does not hold back an unrelated edit for a stored mapping whose states sit in two categories", () => {
		expect(
			mappingAutoSaveBlockers(
				[split],
				withNewToDoState(splitLists),
				storedAs([split], splitLists),
			),
		).toEqual([]);
	});

	it("still refuses that mapping when the edit changes it", () => {
		expect(
			mappingAutoSaveBlockers(
				[split],
				withNewToDoState(splitLists),
				storedAs([{ name: "Flow", states: ["Active"] }], splitLists),
			),
		).toEqual([FLOW_SPANS_TWO_CATEGORIES]);
	});

	it("judges everything when nothing is stored yet", () => {
		expect(
			mappingAutoSaveBlockers([split], withNewToDoState(splitLists)),
		).toEqual([FLOW_SPANS_TWO_CATEGORIES]);
	});
});

describe("storedStateSettingsOf", () => {
	it("has nothing stored before the settings have loaded", () => {
		expect(storedStateSettingsOf(null)).toBeUndefined();
	});

	it("keeps the mappings, the wait states and the three lists the server holds", () => {
		const mappings = [{ name: "Review", states: ["Code Review"] }];

		expect(
			storedStateSettingsOf({
				stateMappings: mappings,
				waitStates: ["Blocked"],
				toDoStates: ["Backlog"],
				doingStates: ["Code Review"],
				doneStates: ["Done"],
			}),
		).toEqual({
			stateMappings: mappings,
			waitStates: ["Blocked"],
			toDoStates: ["Backlog"],
			doingStates: ["Code Review"],
			doneStates: ["Done"],
		});
	});

	it("reads anything the settings leave out as empty", () => {
		expect(storedStateSettingsOf({})).toEqual({
			stateMappings: [],
			waitStates: [],
			toDoStates: [],
			doingStates: [],
			doneStates: [],
		});
	});
});
