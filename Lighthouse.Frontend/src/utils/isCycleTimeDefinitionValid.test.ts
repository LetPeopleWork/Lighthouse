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
});
