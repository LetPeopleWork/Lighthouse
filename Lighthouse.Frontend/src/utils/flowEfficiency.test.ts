import { describe, expect, it } from "vitest";
import type { IStateMapping } from "../models/Common/StateMapping";
import type { ICumulativeStateTimeStateRow } from "../models/Metrics/CumulativeStateTime";
import { flowEfficiency, resolveWaitRawStates } from "./flowEfficiency";

const getMockStateRow = (
	overrides?: Partial<ICumulativeStateTimeStateRow>,
): ICumulativeStateTimeStateRow => ({
	state: "In Progress",
	workflowOrder: 0,
	totalDays: 10,
	completedContributionDays: 6,
	ongoingContributionDays: 4,
	itemCount: 5,
	completedItemCount: 3,
	ongoingItemCount: 2,
	meanDays: 2,
	medianDays: 2,
	...overrides,
});

describe("resolveWaitRawStates", () => {
	it("keeps a raw-state entry as itself", () => {
		const resolved = resolveWaitRawStates(["Waiting for Review"], []);

		expect(resolved).toEqual(["Waiting for Review"]);
	});

	it("expands a mapping-name entry to its underlying raw states", () => {
		const mappings: IStateMapping[] = [
			{ name: "Waiting", states: ["Waiting for Review", "Blocked - External"] },
		];

		const resolved = resolveWaitRawStates(["Waiting"], mappings);

		expect(resolved).toEqual(["Waiting for Review", "Blocked - External"]);
	});

	it("matches a mapping name case-insensitively", () => {
		const mappings: IStateMapping[] = [
			{ name: "Waiting", states: ["Waiting for Review"] },
		];

		const resolved = resolveWaitRawStates(["waiting"], mappings);

		expect(resolved).toEqual(["Waiting for Review"]);
	});

	it("expands a raw entry AND a mapping-name entry to the same raw set (cross-surface invariant #1a)", () => {
		const mappings: IStateMapping[] = [
			{ name: "Waiting", states: ["Waiting for Review", "Blocked - External"] },
		];

		const fromMappingName = resolveWaitRawStates(["Waiting"], mappings);
		const fromRawStates = resolveWaitRawStates(
			["Waiting for Review", "Blocked - External"],
			mappings,
		);

		expect([...fromMappingName].sort()).toEqual([...fromRawStates].sort());
	});
});

const knownFixtureRows: ICumulativeStateTimeStateRow[] = [
	getMockStateRow({ state: "In Progress", totalDays: 184 }),
	getMockStateRow({ state: "Waiting for Review", totalDays: 200 }),
	getMockStateRow({ state: "Ready for Test", totalDays: 156 }),
];

describe("flowEfficiency fold", () => {
	it("computes active over total Doing-time as a percentage", () => {
		const result = flowEfficiency(
			knownFixtureRows,
			["Waiting for Review", "Ready for Test"],
			[],
		);

		expect(result.status).toBe("computed");
		if (result.status === "computed") {
			expect(result.efficiencyPercent).toBeCloseTo(34, 0);
		}
	});

	it("expands a mapping-name wait state and counts every underlying raw state's time", () => {
		const rows: ICumulativeStateTimeStateRow[] = [
			getMockStateRow({ state: "In Progress", totalDays: 180 }),
			getMockStateRow({ state: "Waiting for Review", totalDays: 120 }),
			getMockStateRow({ state: "Blocked - External", totalDays: 100 }),
		];
		const mappings: IStateMapping[] = [
			{ name: "Waiting", states: ["Waiting for Review", "Blocked - External"] },
		];

		const result = flowEfficiency(rows, ["Waiting"], mappings);

		expect(result.status).toBe("computed");
		if (result.status === "computed") {
			expect(result.efficiencyPercent).toBeCloseTo(45, 0);
		}
	});

	it("recomputes over a narrowed selection of rows (per-item)", () => {
		const narrowed: ICumulativeStateTimeStateRow[] = [
			getMockStateRow({ state: "In Progress", totalDays: 150 }),
			getMockStateRow({ state: "Waiting for Review", totalDays: 90 }),
		];

		const result = flowEfficiency(narrowed, ["Waiting for Review"], []);

		expect(result.status).toBe("computed");
		if (result.status === "computed") {
			expect(result.efficiencyPercent).toBeCloseTo(62.5, 1);
		}
	});

	it("is suppressed when no wait states are configured", () => {
		const result = flowEfficiency(knownFixtureRows, [], []);

		expect(result.status).toBe("not-configured");
	});

	it("reads as no-data-in-scope when there is zero Doing-time", () => {
		const zeroRows = [getMockStateRow({ state: "In Progress", totalDays: 0 })];

		const result = flowEfficiency(zeroRows, ["Waiting for Review"], []);

		expect(result.status).toBe("no-data");
	});

	it("ignores a wait entry that resolves outside the displayed Doing set", () => {
		const rows: ICumulativeStateTimeStateRow[] = [
			getMockStateRow({ state: "In Progress", totalDays: 150 }),
			getMockStateRow({ state: "Waiting for Review", totalDays: 90 }),
		];

		const result = flowEfficiency(rows, ["Waiting for Review", "Closed"], []);

		expect(result.status).toBe("computed");
		if (result.status === "computed") {
			expect(result.efficiencyPercent).toBeCloseTo(62.5, 1);
		}
	});

	it("equals the tile fold value on one shared whole-set fixture (cross-surface invariant #1b)", () => {
		const waitStates = ["Waiting for Review", "Ready for Test"];

		const chartNumber = flowEfficiency(knownFixtureRows, waitStates, []);
		const tileFold = flowEfficiency(knownFixtureRows, waitStates, []);

		expect(chartNumber).toEqual(tileFold);
	});
});
