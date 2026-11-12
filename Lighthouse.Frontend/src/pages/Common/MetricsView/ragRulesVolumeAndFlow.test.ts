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

describe("what the volume and flow rules say, and where they change", () => {
	const defineWipLimit = "Define System WIP Limit in settings.";

	describe("a system WIP limit that is not a positive number", () => {
		it("is asked for again, whatever the work looks like", () => {
			const red = { ragStatus: "red", tipText: defineWipLimit };

			expect(computeStartedVsClosedRag(10, 8, -2, terms)).toEqual(red);
			expect(computeWipOverTimeRag([1, 2], -2, terms)).toEqual(red);
			expect(computeArrivalsRunChartRag([1, 2], [], 10, 10, -2, terms)).toEqual(
				red,
			);
		});
	});

	describe("computeStartedVsClosedRag", () => {
		it("is green and says so when both are zero", () => {
			expect(computeStartedVsClosedRag(0, 0, 5, terms)).toEqual({
				ragStatus: "green",
				tipText: "Started and closed are both 0. Flow is balanced.",
			});
		});

		it("does not call it balanced when only one of them is zero", () => {
			expect(computeStartedVsClosedRag(5, 0, 5, terms).ragStatus).toBe("red");
			expect(computeStartedVsClosedRag(0, 5, 5, terms).ragStatus).toBe("amber");
		});

		it("is balanced within one item", () => {
			expect(computeStartedVsClosedRag(10, 11, 5, terms)).toEqual({
				ragStatus: "green",
				tipText: "Started (10) and closed (11) are balanced.",
			});
		});

		it("is green at exactly 5% apart", () => {
			expect(computeStartedVsClosedRag(38, 40, 5, terms)).toEqual({
				ragStatus: "green",
				tipText: "Started (38) and closed (40) are within 5% of each other.",
			});
		});

		it("is red when started is well ahead, and says by how much of the larger", () => {
			expect(computeStartedVsClosedRag(20, 10, 5, terms)).toEqual({
				ragStatus: "red",
				tipText:
					"Started (20) exceeds closed (10) by 50.0%. Focus on finishing in-progress Work Items.",
			});
		});

		it("is amber when closed is well ahead", () => {
			expect(computeStartedVsClosedRag(10, 20, 5, terms)).toEqual({
				ragStatus: "amber",
				tipText:
					"Closed (20) significantly exceeds started (10). The process may be starving.",
			});
		});
	});

	describe("computeThroughputRag", () => {
		it("is stable with nothing to look at", () => {
			expect(computeThroughputRag([], [], terms)).toEqual({
				ragStatus: "green",
				tipText: "Stable, predictable Throughput.",
			});
		});

		it("counts every run of three zero days, the last one included", () => {
			expect(computeThroughputRag([0, 0, 0, 0], [], terms)).toEqual({
				ragStatus: "red",
				tipText:
					"2 runs of 3+ consecutive zero-Throughput days detected. Check for blockers or excessive WIP.",
			});
			expect(computeThroughputRag([0, 0, 0], [], terms)).toEqual({
				ragStatus: "amber",
				tipText:
					"1 run of 3 consecutive zero-Throughput days detected. Inspect the PBC to understand what happened.",
			});
		});

		it("is stable when no three days in a row are all zero", () => {
			expect(computeThroughputRag([0, 1, 0, 1, 0, 1], [], terms)).toEqual({
				ragStatus: "green",
				tipText:
					"No extended zero-Throughput runs detected. Throughput is stable.",
			});
		});
	});

	describe("computeWipOverTimeRag", () => {
		it("says there is no data to judge", () => {
			expect(computeWipOverTimeRag([], 5, terms)).toEqual({
				ragStatus: "green",
				tipText: "No WIP data available. Operating within boundaries.",
			});
		});

		it("is green when more than half of the days sit at the limit", () => {
			expect(computeWipOverTimeRag([5, 5, 5, 6], 5, terms)).toEqual({
				ragStatus: "green",
				tipText: "75.0% of days at the WIP limit of 5. Flow is consistent.",
			});
		});

		it("is not green at exactly half of the days", () => {
			expect(computeWipOverTimeRag([5, 6], 5, terms)).toEqual({
				ragStatus: "amber",
				tipText:
					"WIP distribution is uneven across the limit of 5. Aim for consistency.",
			});
		});

		it("is red when most days are above the limit", () => {
			expect(computeWipOverTimeRag([6, 6, 6, 5], 5, terms)).toEqual({
				ragStatus: "red",
				tipText:
					"WIP exceeded the limit of 5 on 3 of 4 days. Reduce work in progress.",
			});
		});

		it("is amber when most days are below the limit", () => {
			expect(computeWipOverTimeRag([1, 1, 1, 5], 5, terms)).toEqual({
				ragStatus: "amber",
				tipText:
					"WIP was below the limit of 5 on 3 of 4 days. Consider starting more work.",
			});
		});

		it("is uneven when the days below only match the days above and at the limit", () => {
			expect(computeWipOverTimeRag([1, 1, 6, 5], 5, terms).tipText).toBe(
				"WIP distribution is uneven across the limit of 5. Aim for consistency.",
			);
		});
	});

	describe("computeWorkDistributionRag", () => {
		it("says there is nothing to evaluate", () => {
			expect(computeWorkDistributionRag(0, 0, 5, 3, terms)).toEqual({
				ragStatus: "green",
				tipText: "No Work Items to evaluate. Distribution is healthy.",
			});
		});

		it("is red at exactly a fifth of the items not linked to a Feature", () => {
			expect(computeWorkDistributionRag(2, 10, 5, 3, terms)).toEqual({
				ragStatus: "red",
				tipText:
					"20.0% of Work Items (2 of 10) are not linked to a Feature. Link them to improve visibility.",
			});
		});

		it("asks for the Feature WIP unless it is a positive number", () => {
			for (const featureWip of [undefined, 0, -2]) {
				expect(computeWorkDistributionRag(1, 10, featureWip, 3, terms)).toEqual(
					{
						ragStatus: "red",
						tipText: "Define Feature WIP in settings.",
					},
				);
			}
		});

		it("says how far the work is spread against the Feature WIP in each band", () => {
			expect(computeWorkDistributionRag(0, 10, 5, 7, terms)).toEqual({
				ragStatus: "red",
				tipText:
					"Work is spread across 7.0 Features, exceeding the Feature WIP of 5 by more than 20%. Focus on fewer initiatives.",
			});
			expect(computeWorkDistributionRag(0, 10, 5, 5.5, terms)).toEqual({
				ragStatus: "amber",
				tipText:
					"Work is spread across 5.5 Features, slightly above the Feature WIP of 5. Consider reducing scope.",
			});
			expect(computeWorkDistributionRag(0, 10, 5, 5, terms)).toEqual({
				ragStatus: "green",
				tipText:
					"Work is spread across 5.0 Features, within the Feature WIP of 5.",
			});
		});
	});

	describe("computeFeatureSizeRag", () => {
		const percentiles = [
			{ percentile: 70, value: 6 },
			{ percentile: 85, value: 10 },
		];

		it("says there are no active Features", () => {
			expect(computeFeatureSizeRag(percentiles, [], terms)).toEqual({
				ragStatus: "green",
				tipText: "No active Features to evaluate.",
			});
		});

		it("is red for each Feature past the 85th percentile size, and counts them", () => {
			expect(computeFeatureSizeRag(percentiles, [11, 12, 3], terms)).toEqual({
				ragStatus: "red",
				tipText:
					"2 Feature(s) exceed the 85th percentile size of 10 items. These are bigger than most other Features and still in progress — look to split or close as soon as possible.",
			});
		});

		it("is amber for each Feature past the 70th percentile size but not the 85th, and counts them", () => {
			expect(computeFeatureSizeRag(percentiles, [7, 8, 3], terms)).toEqual({
				ragStatus: "amber",
				tipText:
					"2 Feature(s) exceed the 70th percentile size of 6 items. They are approaching the upper limit — if they grow further, try to split and deliver in smaller slices.",
			});
			expect(computeFeatureSizeRag(percentiles, [10, 6], terms).ragStatus).toBe(
				"amber",
			);
		});

		it("is green when no Feature is bigger than the 70th percentile size", () => {
			expect(computeFeatureSizeRag(percentiles, [6, 3], terms)).toEqual({
				ragStatus: "green",
				tipText: "All active Features are within the expected size range.",
			});
		});
	});

	describe("computeEstimationVsCycleTimeRag", () => {
		const points = (cycleTimes: number[]) =>
			cycleTimes.map((cycleTime, index) => ({
				estimate: index + 1,
				cycleTime,
			}));

		it("has too little to judge below two data points, and judges two", () => {
			expect(
				computeEstimationVsCycleTimeRag("Ready", points([5]), terms),
			).toEqual({
				ragStatus: "green",
				tipText: "Not enough data points to compute correlation yet.",
			});
			expect(
				computeEstimationVsCycleTimeRag("Ready", points([5, 9]), terms),
			).toEqual({
				ragStatus: "green",
				tipText:
					"Spearman correlation is 1.00 (≥ 0.6). Estimates correlate well with actual Cycle Time.",
			});
		});

		it("is green at a correlation of exactly 0.6", () => {
			expect(
				computeEstimationVsCycleTimeRag(
					"Ready",
					points([30, 20, 10, 40, 50]),
					terms,
				),
			).toEqual({
				ragStatus: "green",
				tipText:
					"Spearman correlation is 0.60 (≥ 0.6). Estimates correlate well with actual Cycle Time.",
			});
		});

		it("is amber for a weak correlation", () => {
			expect(
				computeEstimationVsCycleTimeRag(
					"Ready",
					points([20, 40, 10, 30, 50]),
					terms,
				),
			).toEqual({
				ragStatus: "amber",
				tipText:
					"Spearman correlation is 0.50 (0.3–0.6). Weak correlation between estimates and Cycle Time. Review your estimation approach.",
			});
		});

		it("is red for no correlation, and reads a reversed order as negative", () => {
			expect(
				computeEstimationVsCycleTimeRag("Ready", points([30, 20, 10]), terms),
			).toEqual({
				ragStatus: "red",
				tipText:
					"Spearman correlation is -1.00 (< 0.3). No meaningful correlation between estimates and Cycle Time.",
			});
		});
	});

	describe("computeLoadBalanceMatrixRag", () => {
		const noBaseline =
			"Configure a baseline period to enable load balance analysis.";

		it("asks for a baseline when any part of it is missing", () => {
			for (const [available, wip, age] of [
				[false, 5, 10],
				[true, null, 10],
				[true, 5, null],
			] as const) {
				expect(
					computeLoadBalanceMatrixRag(available, 5, 10, wip, age, terms),
				).toEqual({ ragStatus: "red", tipText: noBaseline });
			}
		});

		it("says which side of the baseline the age and the WIP are on", () => {
			expect(computeLoadBalanceMatrixRag(true, 5, 12, 5, 10, terms)).toEqual({
				ragStatus: "red",
				tipText:
					"Work Item Age is above baseline (12 > 10). Close ongoing work before starting new items.",
			});
			expect(computeLoadBalanceMatrixRag(true, 6, 10, 5, 10, terms)).toEqual({
				ragStatus: "green",
				tipText:
					"WIP is above baseline (6 > 5) while Work Item Age is at or below baseline (10 <= 10).",
			});
			expect(computeLoadBalanceMatrixRag(true, 5, 10, 5, 10, terms)).toEqual({
				ragStatus: "amber",
				tipText:
					"WIP is at or below baseline (5 <= 5) while Work Item Age is at or below baseline. Consider starting more work.",
			});
		});
	});

	describe("computeArrivalsRunChartRag", () => {
		it("is amber and says why when arrivals are balanced but come in batches, the last run included", () => {
			expect(
				computeArrivalsRunChartRag([0, 0, 0, 0], [], 5, 5, 5, terms),
			).toEqual({
				ragStatus: "amber",
				tipText:
					"Arrivals are balanced overall, but 2 runs of 3+ consecutive zero-arrival days suggest work is starting in batches rather than continuously.",
			});
		});

		it("does not count days that only touch zeros, or a run that blackout days break", () => {
			expect(
				computeArrivalsRunChartRag([0, 5, 0, 5, 0, 5], [], 5, 5, 5, terms),
			).toEqual({
				ragStatus: "green",
				tipText: "Started (5) and closed (5) are balanced.",
			});
			expect(
				computeArrivalsRunChartRag([0, 0, 0, 0], [1], 5, 5, 5, terms).ragStatus,
			).toBe("green");
		});

		it("keeps the starving message when the balance is already amber", () => {
			expect(
				computeArrivalsRunChartRag([0, 0, 0, 0], [], 10, 20, 5, terms),
			).toEqual({
				ragStatus: "amber",
				tipText:
					"Closed (20) significantly exceeds started (10). The process may be starving.",
			});
		});
	});

	describe("computePbcRag", () => {
		const noBaseline = "Configure a baseline period to enable PBC analysis.";

		it("asks for a baseline when it is not configured, or its status says it cannot be used", () => {
			const unusable = [
				{ baselineConfigured: false, status: "Ready" },
				{ baselineConfigured: true, status: "BaselineMissing" },
				{ baselineConfigured: true, status: "BaselineInvalid" },
				{ baselineConfigured: true, status: "InsufficientData" },
			];

			for (const { baselineConfigured, status } of unusable) {
				expect(
					computePbcRag({ baselineConfigured, status, dataPoints: [] }),
				).toEqual({ ragStatus: "red", tipText: noBaseline });
			}
		});

		it("says which kind of signal it found, or that there is none", () => {
			const run = (specialCauses: string[]) =>
				computePbcRag({
					baselineConfigured: true,
					status: "Ready",
					dataPoints: [{ specialCauses }],
				});

			expect(run(["LargeChange"])).toEqual({
				ragStatus: "red",
				tipText:
					"Signal detected: Large-magnitude change in process behaviour. Investigate recent changes.",
			});
			expect(run(["ModerateChange"])).toEqual({
				ragStatus: "amber",
				tipText:
					"Signal detected: Moderate change in process behaviour. Investigate before it escalates.",
			});
			expect(run([])).toEqual({
				ragStatus: "green",
				tipText:
					"Process behaviour is within expected limits. No signals detected.",
			});
		});
	});
});
