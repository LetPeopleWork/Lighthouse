import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import { reconcileDoingStates } from "./stateMappingReconciliation";

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

			expect(result).not.toContain("QA");
			expect(result).not.toContain("Testing");
			expect(result).toContain("Verification");
			expect(result).toContain("Dev");
			expect(result).toContain("Review");
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

		it("does not duplicate mapping name when it is already in Doing states", () => {
			const mapping: IStateMapping = {
				name: "Verification",
				states: ["QA"],
			};
			// Edge case: mapping name already present before explicit reconciliation
			const doingStates = ["Dev", "Verification", "QA"];

			const result = reconcileDoingStates([], [mapping], doingStates);

			const count = result.filter(
				(s) => s.toLowerCase() === "verification",
			).length;
			expect(count).toBe(1);
			expect(result).not.toContain("QA");
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

			expect(result).not.toContain("Verification");
			expect(result).toContain("QA");
			expect(result).toContain("Testing");
			expect(result).toContain("Dev");
			expect(result).toContain("Review");
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
			// After creating prev mapping: QA absorbed, Testing still in Doing
			const doingStates = ["Dev", "Review", "Testing", "Verification"];

			const result = reconcileDoingStates([prev], [next], doingStates);

			expect(result).toContain("Verification");
			expect(result).not.toContain("Testing");
			expect(result).not.toContain("QA");
			expect(result).toContain("Dev");
			expect(result).toContain("Review");
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

			expect(result).toContain("Verification");
			expect(result).toContain("Testing");
			expect(result).not.toContain("QA");
			expect(result).toContain("Dev");
			expect(result).toContain("Review");
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

			expect(result).not.toContain("Verification");
			expect(result).toContain("Quality Gates");
			expect(result).not.toContain("QA");
			expect(result).not.toContain("Testing");
			expect(result).toContain("Dev");
			expect(result).toContain("Review");
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

			expect(result).toContain("Track A");
			expect(result).not.toContain("Track B");
			expect(result).toContain("QA");
			expect(result).toContain("Testing");
		});

		it("handles adding multiple mappings at once", () => {
			const next: IStateMapping[] = [
				{ name: "Group A", states: ["Dev"] },
				{ name: "Group B", states: ["QA"] },
			];
			const doingStates = ["Dev", "Review", "QA", "Testing"];

			const result = reconcileDoingStates([], next, doingStates);

			expect(result).toContain("Group A");
			expect(result).toContain("Group B");
			expect(result).not.toContain("Dev");
			expect(result).not.toContain("QA");
			expect(result).toContain("Review");
			expect(result).toContain("Testing");
		});
	});

	describe("case insensitivity", () => {
		it("matches mapping name case-insensitively when removing", () => {
			const prev: IStateMapping = {
				name: "Verification",
				states: ["QA", "Testing"],
			};
			// Case variation in Doing list
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

			// Same states, same name (different case) → treated as unchanged
			const result = reconcileDoingStates([prev], [next], doingStates);

			// Mapping name stayed (possibly updated casing), no spurious changes
			const hasVerification = result.some(
				(s) => s.toLowerCase() === "verification",
			);
			expect(hasVerification).toBe(true);
			expect(result).not.toContain("QA");
		});
	});
});
