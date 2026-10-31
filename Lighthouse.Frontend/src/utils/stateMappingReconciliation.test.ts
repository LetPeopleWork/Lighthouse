import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import {
	reconcileDoingStates,
	wouldShadow,
} from "./stateMappingReconciliation";
import { validateStateMappings } from "./stateMappingValidation";

describe("reconcileDoingStates", () => {
	it("returns empty array when all inputs are empty", () => {
		expect(reconcileDoingStates([], [], [])).toEqual([]);
	});

	it("returns doing states unchanged when no mappings change", () => {
		const mapping: IStateMapping = {
			name: "Verification",
			states: ["QA", "Testing"],
		};
		const doingStates = ["Dev", "Review", "Verification"];

		const result = reconcileDoingStates([mapping], [mapping], doingStates);

		expect(result).toEqual(["Dev", "Review", "Verification"]);
	});

	describe("adding a mapping", () => {
		it("removes source states from Doing and appends mapping name", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "Review", "QA", "Testing"];

			const result = reconcileDoingStates([], [mapping], doingStates);

			expect(result).toEqual(["Dev", "Review", "Verification"]);
		});

		it("preserves order of unaffected Doing states before the appended mapping name", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "Review", "QA", "Testing"];

			const result = reconcileDoingStates([], [mapping], doingStates);

			expect(result.indexOf("Dev")).toBeLessThan(result.indexOf("Review"));
			expect(result.indexOf("Review")).toBeLessThan(
				result.indexOf("Verification"),
			);
		});

		it("leaves Doing alone when the mapping name is already a Doing state the mapping does not contain", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["QA"],
			};
			const doingStates = ["Dev", "Verification", "QA"];

			const result = reconcileDoingStates([], [mapping], doingStates);

			expect(result).toEqual(["Dev", "Verification", "QA"]);
		});

		it("ignores mappings with empty names", () => {
			const mapping: IStateMapping = { name: "", states: ["QA"] };
			const doingStates = ["Dev", "QA"];

			const result = reconcileDoingStates([], [mapping], doingStates);

			expect(result).toEqual(["Dev", "QA"]);
		});
	});

	describe("removing a mapping", () => {
		it("removes mapping name from Doing and restores source states", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "Review", "Verification"];

			const result = reconcileDoingStates([mapping], [], doingStates);

			expect(result).toEqual(["Dev", "Review", "QA", "Testing"]);
		});

		it("appends restored source states after remaining Doing states", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "Review", "Verification"];

			const result = reconcileDoingStates([mapping], [], doingStates);

			expect(result.indexOf("Dev")).toBeLessThan(result.indexOf("QA"));
			expect(result.indexOf("Dev")).toBeLessThan(result.indexOf("Testing"));
		});

		it("restores source states in their original order", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "Verification"];

			const result = reconcileDoingStates([mapping], [], doingStates);

			expect(result.indexOf("QA")).toBeLessThan(result.indexOf("Testing"));
		});
	});

	describe("editing a mapping", () => {
		it("reconciles when source states are extended", () => {
			const prev: IStateMapping = {
				name: "Verification",
				states: ["QA"],
			};
			const next: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "Review", "Testing", "Verification"];

			const result = reconcileDoingStates([prev], [next], doingStates);

			expect(result).toEqual(["Dev", "Review", "Verification"]);
		});

		it("reconciles when source states are reduced", () => {
			const prev: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const next: IStateMapping = {
				name: "Verification",
				states: ["QA"],
			};
			const doingStates = ["Dev", "Review", "Verification"];

			const result = reconcileDoingStates([prev], [next], doingStates);

			expect(result).toEqual(["Dev", "Review", "Testing", "Verification"]);
		});

		it("reconciles when a mapping is renamed", () => {
			const prev: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const next: IStateMapping = {
				name: "Quality Gates",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "Review", "Verification"];

			const result = reconcileDoingStates([prev], [next], doingStates);

			expect(result).toEqual(["Dev", "Review", "Quality Gates"]);
		});
	});

	describe("multiple mappings", () => {
		it("handles removing one of multiple mappings", () => {
			const mappings: IStateMapping[] = [
				{ name: "Track A", states: ["Dev", "Review"] },
				{ name: "Track B", states: ["QA", "Testing"] },
			];
			const doingStates = ["Track A", "Track B"];

			const result = reconcileDoingStates(mappings, [mappings[0]], doingStates);

			expect(result).toEqual(["Track A", "QA", "Testing"]);
		});

		it("handles adding multiple mappings at once", () => {
			const next: IStateMapping[] = [
				{ name: "Group A", states: ["Dev"] },
				{ name: "Group B", states: ["QA"] },
			];
			const doingStates = ["Dev", "Review", "QA", "Testing"];

			const result = reconcileDoingStates([], next, doingStates);

			expect(result).toEqual(["Review", "Testing", "Group A", "Group B"]);
		});
	});

	describe("a mapping named after a Doing state", () => {
		it("keeps Doing as it is while the mapping has no states, then groups the state once it is picked", () => {
			const doing = ["In Arbeit", "In Progress"];
			const named: IStateMapping = { name: "In Arbeit", states: [] };
			const withProgress: IStateMapping = {
				name: "In Arbeit",
				states: ["In Progress"],
			};
			const withBoth: IStateMapping = {
				name: "In Arbeit",
				states: ["In Progress", "In Arbeit"],
			};

			const afterNaming = reconcileDoingStates([], [named], doing);
			const afterFirstPick = reconcileDoingStates(
				[named],
				[withProgress],
				afterNaming,
			);
			const afterSecondPick = reconcileDoingStates(
				[withProgress],
				[withBoth],
				afterFirstPick,
			);

			expect(afterNaming).toEqual(["In Arbeit", "In Progress"]);
			expect(afterFirstPick).toEqual(["In Arbeit", "In Progress"]);
			expect(afterSecondPick).toEqual(["In Arbeit"]);
		});

		it("does not touch Doing while a name is typed letter by letter, and keeps an unrelated state that is a prefix of it", () => {
			const target = "Review DE/EN";
			let previous: IStateMapping[] = [{ name: "", states: [] }];
			const doing = ["Review", "Testing"];

			for (let length = 1; length <= target.length; length++) {
				const next = [{ name: target.slice(0, length), states: [] }];
				expect(reconcileDoingStates(previous, next, doing)).toEqual([
					"Review",
					"Testing",
				]);
				previous = next;
			}

			const withTesting: IStateMapping[] = [
				{ name: target, states: ["Testing"] },
			];
			expect(reconcileDoingStates(previous, withTesting, doing)).toEqual([
				"Review",
				"Review DE/EN",
			]);
		});

		it("groups the states again after they were picked first and the name is typed letter by letter", () => {
			const target = "In Arbeit";
			const states = ["In Arbeit", "In Progress"];
			let previous: IStateMapping[] = [{ name: "", states }];
			let doing = [...states];

			for (let length = 1; length <= target.length; length++) {
				const next = [{ name: target.slice(0, length), states }];
				doing = reconcileDoingStates(previous, next, doing);
				expect(doing).toEqual([target.slice(0, length).trim()]);
				previous = next;
			}

			expect(doing).toEqual(["In Arbeit"]);
		});

		it("adds the mapping name without surrounding spaces", () => {
			const mapping: IStateMapping = { name: " Verification ", states: ["QA"] };

			expect(reconcileDoingStates([], [mapping], ["Dev", "QA"])).toEqual([
				"Dev",
				"Verification",
			]);
		});

		it("does not put the states back when the mapping's name was removed from Doing by hand", () => {
			const mapping: IStateMapping = { name: "Verification", states: ["QA"] };

			expect(reconcileDoingStates([mapping], [], ["Dev"])).toEqual(["Dev"]);
		});

		it("leaves Doing exactly as it was when an empty mapping is added and deleted", () => {
			const doing = ["Dev", "Review"];
			const empty: IStateMapping = { name: "", states: [] };
			const named: IStateMapping = { name: "Dev", states: [] };

			expect(reconcileDoingStates([], [empty], doing)).toEqual(doing);
			expect(reconcileDoingStates([empty], [], doing)).toEqual(doing);
			expect(reconcileDoingStates([named], [], doing)).toEqual(doing);
		});

		it("leaves the name in Doing when a mapping is deleted after its state was put back by hand", () => {
			const mapping: IStateMapping = { name: "Verification", states: ["QA"] };

			const result = reconcileDoingStates(
				[mapping],
				[],
				["Verification", "QA"],
			);

			expect(result).toEqual(["Verification", "QA"]);
		});
	});

	describe("a mapping named after a state another mapping groups", () => {
		const group: IStateMapping = { name: "Group", states: ["In Arbeit"] };
		const unnamed: IStateMapping = { name: "In Arbeit", states: [] };
		const named: IStateMapping = { name: "In Arbeit", states: ["Review"] };
		const emptyLists = (doingStates: string[]) => ({
			toDoStates: [],
			doingStates,
			doneStates: [],
		});

		it("is not taken into Doing, and deleting the grouping mapping puts the state back and reports the clash", () => {
			const grouped = reconcileDoingStates(
				[],
				[group],
				["In Arbeit", "In Progress", "Review"],
			);
			const afterNaming = reconcileDoingStates(
				[group],
				[group, unnamed],
				grouped,
			);
			const afterStates = reconcileDoingStates(
				[group, unnamed],
				[group, named],
				afterNaming,
			);
			const afterDelete = reconcileDoingStates(
				[group, named],
				[named],
				afterStates,
			);

			expect(grouped).toEqual(["In Progress", "Review", "Group"]);
			expect(afterNaming).toEqual(["In Progress", "Review", "Group"]);
			expect(afterStates).toEqual(["In Progress", "Review", "Group"]);
			expect(
				validateStateMappings([group, named], emptyLists(afterStates)),
			).toEqual([
				'Mapping "In Arbeit" has the same name as a state grouped by mapping "Group". Choose another name.',
			]);
			expect(afterDelete).toEqual(["In Progress", "Review", "In Arbeit"]);
			expect(validateStateMappings([named], emptyLists(afterDelete))).toEqual([
				'Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.',
			]);
		});

		it("is taken into Doing normally once it is renamed", () => {
			const renamed: IStateMapping = { name: "Prüfung", states: ["Review"] };

			const result = reconcileDoingStates(
				[group, named],
				[group, renamed],
				["In Progress", "Review", "Group"],
			);

			expect(result).toEqual(["In Progress", "Group", "Prüfung"]);
		});

		it("takes the grouping mapping into Doing and leaves out the mapping named after the state it groups", () => {
			const result = reconcileDoingStates(
				[named],
				[named, group],
				["In Progress", "In Arbeit"],
			);

			expect(result).toEqual(["In Progress", "Group"]);
		});
	});

	describe("mappings that share a source state", () => {
		const emptyLists = (doingStates: string[]) => ({
			toDoStates: [],
			doingStates,
			doneStates: [],
		});
		const clash =
			'Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.';
		const duplicate = 'Source state "Review" is used in multiple mappings.';

		it("deleting a clashing mapping after another mapping took its source state keeps the raw state of the same name in Doing", () => {
			const clashing: IStateMapping = {
				name: "In Arbeit",
				states: ["Review"],
			};
			const rival: IStateMapping = { name: "Coding", states: ["Review"] };

			const afterClashing = reconcileDoingStates(
				[],
				[clashing],
				["In Progress", "In Arbeit", "Review"],
			);
			const afterRival = reconcileDoingStates(
				[clashing],
				[clashing, rival],
				afterClashing,
			);
			const afterDelete = reconcileDoingStates(
				[clashing, rival],
				[rival],
				afterRival,
			);

			expect(afterClashing).toEqual(["In Progress", "In Arbeit", "Review"]);
			expect(
				validateStateMappings([clashing], emptyLists(afterClashing)),
			).toEqual([clash]);
			expect(afterRival).toEqual(["In Progress", "In Arbeit", "Review"]);
			expect(
				validateStateMappings([clashing, rival], emptyLists(afterRival)),
			).toEqual([duplicate, clash]);
			expect(afterDelete).toEqual(["In Progress", "In Arbeit", "Coding"]);
			expect(validateStateMappings([rival], emptyLists(afterDelete))).toEqual(
				[],
			);
		});

		it("given two mappings sharing a state, which the editor cannot produce, keeps the earlier chip, leaves the later mapping out and reports both mappings", () => {
			const applied: IStateMapping = { name: "Coding", states: ["Review"] };
			const second: IStateMapping = { name: "Testing", states: ["Review"] };

			const afterApplied = reconcileDoingStates(
				[],
				[applied],
				["In Progress", "Review"],
			);
			const afterSecond = reconcileDoingStates(
				[applied],
				[applied, second],
				afterApplied,
			);
			const afterRemoval = reconcileDoingStates(
				[applied, second],
				[applied],
				afterSecond,
			);

			expect(afterApplied).toEqual(["In Progress", "Coding"]);
			expect(afterSecond).toEqual(["In Progress", "Coding"]);
			expect(
				validateStateMappings([applied, second], emptyLists(afterSecond)),
			).toEqual([
				duplicate,
				'Mapping "Coding" has the same name as the Doing state "Coding". Add "Coding" to this mapping to group it, or choose another name.',
			]);
			expect(afterRemoval).toEqual(["In Progress", "Coding"]);
			expect(
				validateStateMappings([applied], emptyLists(afterRemoval)),
			).toEqual([]);
		});
	});

	describe("wouldShadow", () => {
		it("is true when the name is a Doing state the mapping does not contain", () => {
			expect(
				wouldShadow({ name: " in arbeit ", states: ["In Progress"] }, [
					"In Arbeit",
				]),
			).toBe(true);
		});

		it("is false when the mapping contains the state it is named after", () => {
			expect(
				wouldShadow({ name: "In Arbeit", states: ["in arbeit"] }, [
					"In Arbeit",
				]),
			).toBe(false);
		});

		it("is false when the name is not in Doing", () => {
			expect(wouldShadow({ name: "Group", states: ["QA"] }, ["QA"])).toBe(
				false,
			);
		});
	});

	describe("case insensitivity", () => {
		it("matches mapping name case-insensitively when removing", () => {
			const prev: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			const doingStates = ["Dev", "VERIFICATION", "Review"];

			const result = reconcileDoingStates([prev], [], doingStates);

			expect(result).not.toContain("VERIFICATION");
			expect(result).toContain("QA");
			expect(result).toContain("Testing");
		});

		it("matches source state case-insensitively when adding a mapping", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["qa", "testing"],
			};
			const doingStates = ["Dev", "QA", "Testing"];

			const result = reconcileDoingStates([], [mapping], doingStates);

			expect(result).not.toContain("QA");
			expect(result).not.toContain("Testing");
			expect(result).toContain("Verification");
		});

		it("treats mappings with same name in different cases as the same mapping", () => {
			const prev: IStateMapping = { name: "Verification", states: ["QA"] };
			const next: IStateMapping = { name: "VERIFICATION", states: ["QA"] };
			const doingStates = ["Dev", "Verification"];

			const result = reconcileDoingStates([prev], [next], doingStates);

			expect(result).toEqual(["Dev", "Verification"]);
		});
	});
});
