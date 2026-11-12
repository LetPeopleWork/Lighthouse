import { describe, expect, it } from "vitest";
import {
	computeArrivalsRunChartRag,
	computeEstimationVsCycleTimeRag,
	computeFeatureSizeRag,
	computeLoadBalanceMatrixRag,
	computePbcRag,
	computeSimplifiedCfdRag,
	computeStartedVsClosedRag,
	computeThroughputRag,
	computeWipOverTimeRag,
	computeWorkDistributionRag,
} from "./ragRulesVolumeAndFlow";
import type { RagTerms } from "./ragTypes";

const terms: RagTerms = {
	workItem: "Work Item",
	workItems: "Work Items",
	feature: "Feature",
	features: "Features",
	cycleTime: "Cycle Time",
	throughput: "Throughput",
	wip: "WIP",
	workItemAge: "Work Item Age",
	blocked: "Blocked",
	sle: "SLE",
};

describe("ragRules", () => {
	describe("computeStartedVsClosedRag", () => {
		it("returns red when no system WIP limit is defined", () => {
			const result = computeStartedVsClosedRag(10, 8, undefined, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit");
		});

		it("returns green when started and closed are equal", () => {
			const result = computeStartedVsClosedRag(10, 10, 5, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when difference is within 1 item", () => {
			const result = computeStartedVsClosedRag(10, 11, 5, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when starting much more than closing", () => {
			const result = computeStartedVsClosedRag(20, 10, 5, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when closing much more than starting", () => {
			const result = computeStartedVsClosedRag(10, 20, 5, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when both are zero", () => {
			const result = computeStartedVsClosedRag(0, 0, 5, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red for moderate starting-more gap (>10%)", () => {
			// started=12, closed=10, diff%=2/12=16.7%
			const result = computeStartedVsClosedRag(12, 10, 5, terms);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computeThroughputRag", () => {
		it("returns green with consistent throughput (no zero periods)", () => {
			const values = [3, 5, 2, 4, 1];
			const result = computeThroughputRag(values, [], terms);
			expect(result.ragStatus).toBe("green");
			expect(result.tipText).toContain("Throughput is stable");
		});

		it("returns amber with one run of 3 consecutive zeros", () => {
			const values = [3, 0, 0, 0, 4, 1];
			const result = computeThroughputRag(values, [], terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red with multiple runs of 3 consecutive zeros", () => {
			const values = [0, 0, 0, 0, 4, 1];
			const result = computeThroughputRag(values, [], terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red with all zero periods", () => {
			const values = [0, 0, 0, 0];
			const result = computeThroughputRag(values, [], terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green with empty data", () => {
			const result = computeThroughputRag([], [], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("excludes blackout days from zero-period detection", () => {
			// periods: [3, 0, 0, 4, 1], blackout at indices 1,2
			const values = [3, 0, 0, 4, 1];
			const result = computeThroughputRag(values, [1, 2], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when one non-blackout zero run remains", () => {
			// [3, 0, 0, 0, 0, 0, 1], blackout at [1,2]
			// Window [3,4,5]: all non-blackout && zero → zeroRuns=1 → amber
			const values = [3, 0, 0, 0, 0, 0, 1];
			const result = computeThroughputRag(values, [1, 2], terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when multiple consecutive non-blackout zero runs", () => {
			// [3, 0, 0, 0, 0, 0, 0], blackout at [1]
			// Window [2,3,4]: allZero → run1, [3,4,5]: allZero → run2, [4,5,6]: allZero → run3
			const values = [3, 0, 0, 0, 0, 0, 0];
			const result = computeThroughputRag(values, [1], terms);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computeWipOverTimeRag", () => {
		it("returns red when no system WIP is set", () => {
			const result = computeWipOverTimeRag([3, 4, 5], undefined, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("System WIP Limit");
		});

		it("returns red when more days above WIP than at/below", () => {
			const result = computeWipOverTimeRag([6, 7, 6, 5, 4], 5, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when most days at the WIP limit (>50%)", () => {
			const result = computeWipOverTimeRag([5, 5, 5, 4, 6], 5, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when more days below than at/above", () => {
			const result = computeWipOverTimeRag([3, 2, 4, 5, 6], 5, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns amber for mixed distribution with no dominant pattern", () => {
			const result = computeWipOverTimeRag([4, 6, 5, 4], 5, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green with empty data", () => {
			const result = computeWipOverTimeRag([], 5, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("handles 50/50 split as not red", () => {
			const result = computeWipOverTimeRag([6, 4], 5, terms);
			expect(result.ragStatus).not.toBe("red");
		});
	});

	describe("computeSimplifiedCfdRag", () => {
		it("delegates to startedVsClosed logic", () => {
			// Same behavior: no WIP limit → Red
			const result = computeSimplifiedCfdRag(10, 8, undefined, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when balanced with WIP limit", () => {
			const result = computeSimplifiedCfdRag(10, 10, 5, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when started much more than closed", () => {
			const result = computeSimplifiedCfdRag(20, 10, 5, terms);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computeWorkDistributionRag", () => {
		it("returns red when unlinked items >= 20%", () => {
			const result = computeWorkDistributionRag(20, 100, 3, 2, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("linked");
		});

		it("returns red when no feature WIP is set", () => {
			const result = computeWorkDistributionRag(0, 100, undefined, 5, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Feature WIP");
		});

		it("returns red when distribution rate exceeds feature WIP by >20%", () => {
			// featureWip=3, distributionRate=4 → (4-3)/3=33% → >20% → Red
			const result = computeWorkDistributionRag(5, 100, 3, 4, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Focus on fewer");
		});

		it("returns amber when distribution rate slightly above feature WIP (up to 20%)", () => {
			// featureWip=5, distributionRate=6 → (6-5)/5=20% → Amber
			const result = computeWorkDistributionRag(5, 100, 5, 6, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when distribution rate at or below feature WIP", () => {
			const result = computeWorkDistributionRag(5, 100, 5, 5, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when distribution rate is below feature WIP", () => {
			const result = computeWorkDistributionRag(5, 100, 5, 3, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green with zero total items", () => {
			const result = computeWorkDistributionRag(0, 0, 5, 0, terms);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeFeatureSizeRag", () => {
		const p70 = 5;
		const p85 = 10;
		const percentiles = [
			{ percentile: 70, value: p70 },
			{ percentile: 85, value: p85 },
		];

		it("returns green when no active items to evaluate", () => {
			const result = computeFeatureSizeRag(percentiles, [], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when sizePercentileValues is empty", () => {
			const result = computeFeatureSizeRag([], [3, 8, 12], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when neither 70th nor 85th percentile is in the array", () => {
			const result = computeFeatureSizeRag(
				[{ percentile: 50, value: 4 }],
				[3, 8, 12],
				terms,
			);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when only 70th percentile is present and all sizes are within it", () => {
			const result = computeFeatureSizeRag(
				[{ percentile: 70, value: p70 }],
				[1, 2, 3],
				terms,
			);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when any item exceeds the 85th percentile value", () => {
			const result = computeFeatureSizeRag(percentiles, [3, 5, 11], terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red when multiple items exceed the 85th percentile value", () => {
			const result = computeFeatureSizeRag(percentiles, [11, 12, 15], terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when an item exceeds 70th but not 85th", () => {
			const result = computeFeatureSizeRag(percentiles, [2, 4, 7], terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns amber when exactly at 70th boundary (equal)", () => {
			// p70 = 5, using size = 6 (> 5)
			const result = computeFeatureSizeRag(percentiles, [1, 6], terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when all sizes are within the 70th percentile value", () => {
			const result = computeFeatureSizeRag(percentiles, [1, 3, 5], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("prioritises red over amber when some items exceed 85th and others exceed only 70th", () => {
			const result = computeFeatureSizeRag(percentiles, [7, 11], terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red when only 85th percentile is present and item exceeds it", () => {
			const result = computeFeatureSizeRag(
				[{ percentile: 85, value: p85 }],
				[11],
				terms,
			);
			expect(result.ragStatus).toBe("red");
		});

		it("includes the 85th percentile value in red tip text", () => {
			const result = computeFeatureSizeRag(percentiles, [11], terms);
			expect(result.tipText).toContain(String(p85));
		});

		it("includes the 70th percentile value in amber tip text", () => {
			const result = computeFeatureSizeRag(percentiles, [7], terms);
			expect(result.tipText).toContain(String(p70));
		});
	});

	describe("computeEstimationVsCycleTimeRag", () => {
		it("returns red when not configured", () => {
			const result = computeEstimationVsCycleTimeRag(
				"NotConfigured",
				[],
				terms,
			);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("estimation");
		});

		it("returns green with empty data points", () => {
			const result = computeEstimationVsCycleTimeRag("Ready", [], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when clear positive correlation", () => {
			// Increasing estimates with increasing cycle times
			const data = [
				{ estimate: 1, cycleTime: 5 },
				{ estimate: 3, cycleTime: 10 },
				{ estimate: 5, cycleTime: 15 },
				{ estimate: 8, cycleTime: 25 },
			];
			const result = computeEstimationVsCycleTimeRag("Ready", data, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when no correlation (high estimates with low cycle time)", () => {
			// No meaningful pattern
			const data = [
				{ estimate: 10, cycleTime: 2 },
				{ estimate: 1, cycleTime: 20 },
				{ estimate: 8, cycleTime: 1 },
				{ estimate: 2, cycleTime: 25 },
			];
			const result = computeEstimationVsCycleTimeRag("Ready", data, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when weak correlation", () => {
			// Some positive trend but noisy
			const data = [
				{ estimate: 1, cycleTime: 5 },
				{ estimate: 3, cycleTime: 8 },
				{ estimate: 5, cycleTime: 3 },
				{ estimate: 8, cycleTime: 20 },
			];
			const result = computeEstimationVsCycleTimeRag("Ready", data, terms);
			expect(result.ragStatus).toBe("amber");
		});
	});

	describe("computeLoadBalanceMatrixRag", () => {
		it("returns red with baseline setup guidance when baseline is missing", () => {
			const result = computeLoadBalanceMatrixRag(
				false,
				6,
				42,
				null,
				null,
				terms,
			);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("baseline");
		});

		it("returns green when WIP is above baseline and total age is at or below baseline", () => {
			const result = computeLoadBalanceMatrixRag(true, 8, 40, 5, 40, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when WIP is at or below baseline and total age is at or below baseline", () => {
			const result = computeLoadBalanceMatrixRag(true, 4, 40, 5, 40, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("Consider starting more work");
		});

		it("returns red when total age is above baseline regardless of WIP", () => {
			const result = computeLoadBalanceMatrixRag(true, 9, 41, 5, 40, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain(
				"Close ongoing work before starting new items",
			);
		});
	});

	describe("computeArrivalsRunChartRag", () => {
		it("returns red when no system WIP limit is defined", () => {
			const result = computeArrivalsRunChartRag([], [], 5, 5, undefined, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit");
		});

		it("returns red when arrivals materially exceed departures", () => {
			const result = computeArrivalsRunChartRag(
				[3, 2, 1],
				[],
				20,
				10,
				5,
				terms,
			);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("exceeds closed");
		});

		it("returns green when arrivals and departures are balanced with no batching", () => {
			const result = computeArrivalsRunChartRag(
				[1, 1, 1, 1, 1],
				[],
				5,
				5,
				5,
				terms,
			);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when balance is green but batching is detected", () => {
			// 2+ runs of 3 consecutive zero-arrival days
			const values = [5, 0, 0, 0, 1, 0, 0, 0, 1];
			const result = computeArrivalsRunChartRag(values, [], 5, 5, 5, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("batches");
		});

		it("excludes blackout days from batching detection", () => {
			// Same zero runs but blackout covers the zero days
			const values = [5, 0, 0, 0, 1, 0, 0, 0, 1];
			const blackouts = [1, 2, 3, 5, 6, 7];
			const result = computeArrivalsRunChartRag(
				values,
				blackouts,
				5,
				5,
				5,
				terms,
			);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber from balance when closed exceeds started", () => {
			const result = computeArrivalsRunChartRag([1, 1], [], 5, 20, 5, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("starving");
		});

		it("does not upgrade balance red to amber for batching", () => {
			// Balance is already red, batching should not override
			const values = [0, 0, 0, 0, 0, 0, 0];
			const result = computeArrivalsRunChartRag(values, [], 20, 5, 5, terms);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computePbcRag", () => {
		it("returns red when no baseline configured", () => {
			const data = {
				status: "BaselineMissing" as const,
				baselineConfigured: false,
				dataPoints: [],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("baseline");
		});

		it("returns red when status is BaselineInvalid", () => {
			const data = {
				status: "BaselineInvalid" as const,
				baselineConfigured: true,
				dataPoints: [],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red when any LargeChange signal present", () => {
			const data = {
				status: "Ready" as const,
				baselineConfigured: true,
				dataPoints: [
					{ specialCauses: ["None" as const] },
					{ specialCauses: ["LargeChange" as const] },
				],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Signal");
		});

		it("returns amber when ModerateChange but no LargeChange", () => {
			const data = {
				status: "Ready" as const,
				baselineConfigured: true,
				dataPoints: [
					{ specialCauses: ["ModerateChange" as const] },
					{ specialCauses: ["None" as const] },
				],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when no LargeChange and no ModerateChange", () => {
			const data = {
				status: "Ready" as const,
				baselineConfigured: true,
				dataPoints: [
					{ specialCauses: ["None" as const] },
					{ specialCauses: ["SmallShift" as const] },
				],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green with empty data points and ready status", () => {
			const data = {
				status: "Ready" as const,
				baselineConfigured: true,
				dataPoints: [],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when InsufficientData status", () => {
			const data = {
				status: "InsufficientData" as const,
				baselineConfigured: true,
				dataPoints: [],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red when LargeChange and ModerateChange both present", () => {
			const data = {
				status: "Ready" as const,
				baselineConfigured: true,
				dataPoints: [
					{
						specialCauses: ["LargeChange" as const, "ModerateChange" as const],
					},
				],
			};
			const result = computePbcRag(data);
			expect(result.ragStatus).toBe("red");
		});
	});
});
