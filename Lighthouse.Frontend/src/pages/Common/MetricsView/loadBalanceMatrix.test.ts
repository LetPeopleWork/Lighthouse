import { describe, expect, it } from "vitest";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import {
	deriveLoadBalanceMatrixData,
	isLoadBalanceBaselineAvailable,
} from "./loadBalanceMatrix";

function createPbc(average: number): ProcessBehaviourChartData {
	return {
		status: "Ready",
		statusReason: "Ready",
		xAxisKind: "Date",
		average,
		upperNaturalProcessLimit: average + 3,
		lowerNaturalProcessLimit: Math.max(0, average - 3),
		baselineConfigured: true,
		dataPoints: [],
	};
}

describe("loadBalanceMatrix", () => {
	describe("isLoadBalanceBaselineAvailable", () => {
		it("returns true only when both PBC baselines are ready and configured", () => {
			expect(isLoadBalanceBaselineAvailable(createPbc(5), createPbc(40))).toBe(
				true,
			);

			expect(
				isLoadBalanceBaselineAvailable(
					{ ...createPbc(5), status: "BaselineMissing" },
					createPbc(40),
				),
			).toBe(false);

			expect(
				isLoadBalanceBaselineAvailable(createPbc(5), {
					...createPbc(40),
					baselineConfigured: false,
				}),
			).toBe(false);
		});
	});

	describe("deriveLoadBalanceMatrixData", () => {
		it("builds six points from selected end date with stable WIP and projected total age", () => {
			const endDate = new Date("2026-04-20T00:00:00.000Z");
			const result = deriveLoadBalanceMatrixData({
				endDate,
				currentWip: 3,
				currentTotalWorkItemAge: 10,
				wipPbcData: createPbc(5),
				totalWorkItemAgePbcData: createPbc(40),
			});

			expect(result.points).toHaveLength(6);
			expect(result.points[0].dayOffset).toBe(0);
			expect(result.points[0].date.toISOString()).toBe(endDate.toISOString());
			expect(result.points[0].wip).toBe(3);
			expect(result.points[0].totalWorkItemAge).toBe(10);

			expect(result.points[5].dayOffset).toBe(5);
			expect(result.points[5].wip).toBe(3);
			expect(result.points[5].totalWorkItemAge).toBe(25);
			expect(result.points[5].opacity).toBeLessThan(result.points[0].opacity);

			expect(result.averageWip).toBe(5);
			expect(result.averageTotalWorkItemAge).toBe(40);
			expect(result.baselineAvailable).toBe(true);
		});

		it("handles missing baseline and still returns six projection points", () => {
			const endDate = new Date("2026-04-20T00:00:00.000Z");
			const result = deriveLoadBalanceMatrixData({
				endDate,
				currentWip: 2,
				currentTotalWorkItemAge: 8,
				wipPbcData: { ...createPbc(0), status: "BaselineMissing" },
				totalWorkItemAgePbcData: { ...createPbc(0), status: "BaselineMissing" },
			});

			expect(result.baselineAvailable).toBe(false);
			expect(result.averageWip).toBeNull();
			expect(result.averageTotalWorkItemAge).toBeNull();
			expect(result.points).toHaveLength(6);
			expect(result.points[0].date.toISOString()).toBe(endDate.toISOString());
		});
	});
});
