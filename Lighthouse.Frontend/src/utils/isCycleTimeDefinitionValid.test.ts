import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import {
	cycleTimeBoundaryIndex,
	isCycleTimeDefinitionValid,
	resolveWorkflowStates,
} from "./isCycleTimeDefinitionValid";

const NO_MAPPINGS: IStateMapping[] = [];
const VALIDATION_MAPPING: IStateMapping = {
	name: "Validation",
	states: ["Review"],
};

const workflow = (mappings: IStateMapping[] = NO_MAPPINGS) =>
	resolveWorkflowStates(
		["Backlog"],
		["Implementation", "Review"],
		["Done"],
		mappings,
	);

describe("isCycleTimeDefinitionValid (C#<->TS parity)", () => {
	it("is valid when both boundary states are present", () => {
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Implementation", endState: "Done" },
				workflow(),
				NO_MAPPINGS,
			),
		).toBe(true);
	});

	it("is invalid when the start boundary was removed", () => {
		const afterRemoval = resolveWorkflowStates(
			["Backlog"],
			["Review"],
			["Done"],
			NO_MAPPINGS,
		);
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Implementation", endState: "Done" },
				afterRemoval,
				NO_MAPPINGS,
			),
		).toBe(false);
	});

	it("is invalid when the end boundary was removed", () => {
		const afterRemoval = resolveWorkflowStates(
			["Backlog"],
			["Implementation", "Review"],
			[],
			NO_MAPPINGS,
		);
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Implementation", endState: "Done" },
				afterRemoval,
				NO_MAPPINGS,
			),
		).toBe(false);
	});

	it("resolves a State-Mapping name boundary to its raw states (valid)", () => {
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Backlog", endState: "Validation" },
				workflow([VALIDATION_MAPPING]),
				[VALIDATION_MAPPING],
			),
		).toBe(true);
	});

	it("is invalid when a mapping boundary's underlying state is gone", () => {
		const afterRemoval = resolveWorkflowStates(
			["Backlog"],
			["Implementation"],
			["Done"],
			[VALIDATION_MAPPING],
		);
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Backlog", endState: "Validation" },
				afterRemoval,
				[VALIDATION_MAPPING],
			),
		).toBe(false);
	});

	it("orders boundaries by their first workflow position", () => {
		const states = workflow();
		expect(
			cycleTimeBoundaryIndex("Implementation", states, NO_MAPPINGS),
		).toBeLessThan(cycleTimeBoundaryIndex("Done", states, NO_MAPPINGS));
	});

	it("matches a boundary ignoring surrounding whitespace and case", () => {
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "  implementation  ", endState: "DONE" },
				workflow(),
				NO_MAPPINGS,
			),
		).toBe(true);
	});

	it("is invalid when a boundary maps to an empty state set", () => {
		const emptyMapping: IStateMapping = { name: "Nothing", states: [] };
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Nothing", endState: "Done" },
				workflow([emptyMapping]),
				[emptyMapping],
			),
		).toBe(false);
	});

	it("is invalid when a boundary is only whitespace", () => {
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "   ", endState: "Done" },
				workflow(),
				NO_MAPPINGS,
			),
		).toBe(false);
	});

	it("is invalid when any underlying state of a multi-state mapping is absent", () => {
		const phaseMapping: IStateMapping = {
			name: "Phase",
			states: ["Review", "Ghost"],
		};
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Phase", endState: "Done" },
				workflow([phaseMapping]),
				[phaseMapping],
			),
		).toBe(false);
	});

	it("de-duplicates resolved workflow states case-insensitively", () => {
		const states = resolveWorkflowStates(
			["Backlog"],
			["Review"],
			["review"],
			NO_MAPPINGS,
		);
		expect(
			states.filter((state) => state.toLowerCase() === "review"),
		).toHaveLength(1);
	});

	it("resolves a mapping-name boundary ignoring whitespace and case", () => {
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Backlog", endState: "  validation  " },
				workflow([VALIDATION_MAPPING]),
				[VALIDATION_MAPPING],
			),
		).toBe(true);
	});

	it("is invalid when the end boundary maps to an empty state set", () => {
		const emptyMapping: IStateMapping = { name: "Nothing", states: [] };
		expect(
			isCycleTimeDefinitionValid(
				{ startState: "Backlog", endState: "Nothing" },
				workflow([emptyMapping]),
				[emptyMapping],
			),
		).toBe(false);
	});

	it("locates a boundary that matches any underlying state of a multi-state mapping", () => {
		const phaseMapping: IStateMapping = {
			name: "Phase",
			states: ["Ghost", "Review"],
		};
		const states = workflow([phaseMapping]);
		expect(
			cycleTimeBoundaryIndex("Phase", states, [phaseMapping]),
		).toBeGreaterThanOrEqual(0);
	});
});
