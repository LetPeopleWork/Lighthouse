import { describe, expect, it } from "vitest";
import {
	computeArrivalsRunChartRag,
	computeBlockedOverviewRag,
	computeCycleTimePercentilesRag,
	computeCycleTimeScatterplotRag,
	computeEstimationVsCycleTimeRag,
	computeFeatureSizeRag,
	computeFeaturesWorkedOnRag,
	computePbcRag,
	computePredictabilityScoreRag,
	computeSimplifiedCfdRag,
	computeStartedVsClosedRag,
	computeThroughputRag,
	computeTotalWorkItemAgeOverTimeRag,
	computeTotalWorkItemAgeRag,
	computeWipOverTimeRag,
	computeWipOverviewRag,
	computeWorkDistributionRag,
	computeWorkItemAgeChartRag,
	type RagTerms,
} from "./ragRules";

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
	describe("computeWipOverviewRag", () => {
		it("returns red when no system WIP limit is defined (0)", () => {
			const result = computeWipOverviewRag(5, 0, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit");
		});

		it("returns red when no system WIP limit is defined (undefined)", () => {
			const result = computeWipOverviewRag(5, undefined, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit");
		});

		it("returns red when WIP exceeds limit", () => {
			const result = computeWipOverviewRag(8, 5, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("exceeding the limit");
		});

		it("returns amber when WIP is below limit", () => {
			const result = computeWipOverviewRag(3, 5, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("Start more items");
		});

		it("returns green when WIP matches limit", () => {
			const result = computeWipOverviewRag(5, 5, terms);
			expect(result.ragStatus).toBe("green");
			expect(result.tipText).toContain("matches the System WIP Limit");
		});

		it("returns red with zero WIP and no limit", () => {
			const result = computeWipOverviewRag(0, 0, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit");
		});
	});

	describe("computeBlockedOverviewRag", () => {
		it("returns red when no blocked config is defined", () => {
			const result = computeBlockedOverviewRag(0, false, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define Blocked indicators");
		});

		it("returns red when 2+ items are blocked", () => {
			const result = computeBlockedOverviewRag(2, true, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Focus on unblocking");
		});

		it("returns red when many items are blocked", () => {
			const result = computeBlockedOverviewRag(5, true, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Focus on unblocking");
		});

		it("returns amber when 1 item is blocked", () => {
			const result = computeBlockedOverviewRag(1, true, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("Do not ignore");
		});

		it("returns green when no items are blocked", () => {
			const result = computeBlockedOverviewRag(0, true, terms);
			expect(result.ragStatus).toBe("green");
			expect(result.tipText).toContain("No Blocked");
		});
	});

	describe("computeFeaturesWorkedOnRag", () => {
		it("returns red when no feature WIP is defined (0)", () => {
			const result = computeFeaturesWorkedOnRag(3, 0, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define Feature WIP");
		});

		it("returns red when no feature WIP is defined (undefined)", () => {
			const result = computeFeaturesWorkedOnRag(3, undefined, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define Feature WIP");
		});

		it("returns red when feature count exceeds WIP", () => {
			const result = computeFeaturesWorkedOnRag(5, 3, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("exceeding the limit");
		});

		it("returns amber when feature count is below WIP", () => {
			const result = computeFeaturesWorkedOnRag(1, 3, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("Consider starting another");
		});

		it("returns green when feature count matches WIP", () => {
			const result = computeFeaturesWorkedOnRag(3, 3, terms);
			expect(result.ragStatus).toBe("green");
			expect(result.tipText).toContain("matching the Feature WIP limit");
		});
	});

	describe("computePredictabilityScoreRag", () => {
		it("returns undefined when score is null", () => {
			const result = computePredictabilityScoreRag(null, terms);
			expect(result).toBeUndefined();
		});

		it("returns red when score is below 40%", () => {
			const result = computePredictabilityScoreRag(0.35, terms);
			expect(result?.ragStatus).toBe("red");
			expect(result?.tipText).toContain("below 40%");
			expect(result?.tipText).toContain("highly variable");
		});

		it("returns red at exactly 0%", () => {
			const result = computePredictabilityScoreRag(0, terms);
			expect(result?.ragStatus).toBe("red");
			expect(result?.tipText).toContain("highly variable");
		});

		it("returns amber when score is between 40% and 60%", () => {
			const result = computePredictabilityScoreRag(0.5, terms);
			expect(result?.ragStatus).toBe("amber");
			expect(result?.tipText).toContain("40\u201360%");
		});

		it("returns amber at exactly 40%", () => {
			const result = computePredictabilityScoreRag(0.4, terms);
			expect(result?.ragStatus).toBe("amber");
			expect(result?.tipText).toContain("Analyze bulk closings");
		});

		it("returns green when score is above 60%", () => {
			const result = computePredictabilityScoreRag(0.73, terms);
			expect(result?.ragStatus).toBe("green");
			expect(result?.tipText).toContain("Forecasts are trustworthy");
		});

		it("returns amber at exactly 60%", () => {
			const result = computePredictabilityScoreRag(0.6, terms);
			expect(result?.ragStatus).toBe("amber");
		});

		it("returns green above 60%", () => {
			const result = computePredictabilityScoreRag(0.61, terms);
			expect(result?.ragStatus).toBe("green");
		});

		it("returns green at 100%", () => {
			const result = computePredictabilityScoreRag(1, terms);
			expect(result?.ragStatus).toBe("green");
			expect(result?.tipText).toContain("above 60%");
		});
	});

	describe("computeCycleTimePercentilesRag", () => {
		it("returns red when no SLE is defined (null)", () => {
			const result = computeCycleTimePercentilesRag(null, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define a SLE");
		});

		it("returns green when percentage meets SLE target", () => {
			// SLE: 85% within 10 days. 9 of 10 items <= 10 → 90% >= 85%
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 11];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when percentage is slightly below SLE target (within 20pp)", () => {
			// SLE: 85% within 10 days. 8 of 10 = 80%. Gap = 5pp < 20
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 5, 6, 7, 8, 11, 12];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when percentage is far below SLE target (>20pp)", () => {
			// SLE: 85% within 10 days. 4 of 10 = 40%. Gap = 45pp > 20
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 11, 12, 13, 14, 15, 16];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red when no cycle time data exists", () => {
			const sle = { percentile: 85, value: 10 };
			const result = computeCycleTimePercentilesRag(sle, [], terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when all cycle times are within SLE", () => {
			const sle = { percentile: 85, value: 10 };
			// 20 items, 17 ≤ 10, 3 > 10 → 85% >= 85%
			const cycleTimes = [...new Array(17).fill(5), 11, 12, 13];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when gap is exactly 1pp below target", () => {
			// SLE: 85% within 100. 84 of 100 within → 84%. Gap = 1pp < 20
			const sle = { percentile: 85, value: 100 };
			const cycleTimes = [
				...new Array(84).fill(50),
				...new Array(16).fill(150),
			];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("amber");
		});
	});

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

	describe("computeTotalWorkItemAgeRag", () => {
		it("returns red when no WIP limit is defined", () => {
			const result = computeTotalWorkItemAgeRag(100, 5, undefined, 10, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit and SLE");
		});

		it("returns red when no SLE is defined", () => {
			const result = computeTotalWorkItemAgeRag(100, 5, 5, undefined, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit and SLE");
		});

		it("returns red when total age exceeds reference value", () => {
			// ref = WIP(5) * SLE(10) = 50. totalAge=60 > 50
			const result = computeTotalWorkItemAgeRag(60, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when tomorrow projection exceeds reference", () => {
			// ref = 5*10=50. totalAge=46, tomorrow = 46+5=51 > 50
			const result = computeTotalWorkItemAgeRag(46, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when total age and tomorrow are within range", () => {
			// ref = 5*10=50. totalAge=40, tomorrow=40+5=45 <= 50
			const result = computeTotalWorkItemAgeRag(40, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when total age is exactly at reference", () => {
			// ref = 5*10=50. totalAge=50, tomorrow=50+5=55 but today is exactly at limit
			const result = computeTotalWorkItemAgeRag(50, 5, 5, 10, terms);
			// totalAge = ref, but tomorrow > ref → amber
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when well below reference", () => {
			// ref =5*10=50. totalAge=20, tomorrow=20+5=25 <= 50
			const result = computeTotalWorkItemAgeRag(20, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("green");
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

	describe("computeCycleTimeScatterplotRag", () => {
		it("returns red when no SLE is defined", () => {
			const result = computeCycleTimeScatterplotRag(null, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define a SLE");
		});

		it("returns green when within SLE", () => {
			// SLE: 85% within 10 days. Items: 9 under 10, 1 over
			// 10% above → within X(85%) tolerance
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [2, 3, 4, 5, 6, 7, 8, 9, 9, 11];
			// 1 out of 10 = 10% above, SLE says 15% allowed
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when between X% and (X+10)% above SLE days", () => {
			// SLE: 85% within 10 days → 15% allowed above
			// If 18% above → between 15% and 25% → amber
			const sle = { percentile: 85, value: 10 };
			// 18 of 100 items above 10 days
			const cycleTimes = [...new Array(82).fill(5), ...new Array(18).fill(15)];
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when (X+10)%+ items above SLE days", () => {
			// SLE: 85% within 10 days → 15% allowed, red at 25%+
			// 30% above
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [...new Array(70).fill(5), ...new Array(30).fill(15)];
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green with empty cycle time data", () => {
			const sle = { percentile: 85, value: 10 };
			const result = computeCycleTimeScatterplotRag(sle, [], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when all items within SLE", () => {
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 5];
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});
	});

	// -----------------------------------------------------------------------
	// M4 — Aging and Flow Stability Widgets
	// -----------------------------------------------------------------------

	describe("computeWorkItemAgeChartRag", () => {
		it("returns red when no SLE is set", () => {
			const result = computeWorkItemAgeChartRag(null, true, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("SLE");
		});

		it("returns red when no blocked config is set", () => {
			const sle = { percentile: 85, value: 14 };
			const result = computeWorkItemAgeChartRag(sle, false, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Blocked");
		});

		it("returns red when any item is above SLE", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 10, isBlocked: false },
				{ workItemAge: 15, isBlocked: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when some items are above SLE within allowed percentage", () => {
			const sle = { percentile: 85, value: 14 };
			// 1 of 10 above SLE → 10% <= allowedAbove(15%) → anyAbove=true → amber
			const items = [
				{ workItemAge: 15, isBlocked: false },
				...new Array(9).fill({ workItemAge: 5, isBlocked: false }),
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns amber when any item is blocked", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: true },
				{ workItemAge: 3, isBlocked: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when no blockers and all below SLE", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: false },
				{ workItemAge: 8, isBlocked: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green with empty items", () => {
			const sle = { percentile: 85, value: 14 };
			const result = computeWorkItemAgeChartRag(sle, true, [], terms);
			expect(result.ragStatus).toBe("green");
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

	describe("computeTotalWorkItemAgeOverTimeRag", () => {
		it("returns red when end is higher than start by >10%", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 115, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when start is higher than end by >10%", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 85, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when start and end are within 10% margin", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 105, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when start and end are equal", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 100, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when both are zero", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(0, 0, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when start is zero and end is positive", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(0, 10, terms);
			expect(result.ragStatus).toBe("red");
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

	// -----------------------------------------------------------------------
	// M5 — Portfolio and Correlation Widgets
	// -----------------------------------------------------------------------

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
		it("returns red when no feature size target is set", () => {
			const result = computeFeatureSizeRag(null, [], [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Feature Size Target");
		});

		it("returns green when no active items to evaluate", () => {
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const result = computeFeatureSizeRag(target, percentiles, [], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when no percentile data available", () => {
			const target = { percentile: 85, value: 90 };
			const result = computeFeatureSizeRag(target, [], [3, 5, 7], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when no matching percentile found", () => {
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 70, value: 5 }];
			const result = computeFeatureSizeRag(
				target,
				percentiles,
				[3, 5, 7],
				terms,
			);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when violations are within the allowed percentage", () => {
			// 85th percentile size = 7. Allowed above = 15%.
			// 1 of 10 above 7 = 10% → within 15% → green
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const sizes = [1, 2, 3, 4, 5, 6, 7, 7, 7, 9];
			const result = computeFeatureSizeRag(target, percentiles, sizes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when no items exceed the threshold", () => {
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const sizes = [1, 2, 3, 4, 5];
			const result = computeFeatureSizeRag(target, percentiles, sizes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when violations exceed allowed percentage but within red threshold", () => {
			// 85th percentile size = 7. Allowed above = 15%, red at > 25%.
			// 2 of 10 above 7 = 20% → between 15% and 25% → amber
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const sizes = [1, 2, 3, 4, 5, 6, 7, 7, 8, 9];
			const result = computeFeatureSizeRag(target, percentiles, sizes, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when violations exceed the red threshold", () => {
			// 85th percentile size = 7. Allowed above = 15%, red at 25%.
			// 3 of 10 above 7 = 30% → > 25% → red
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const sizes = [1, 2, 3, 4, 5, 8, 9, 10, 11, 12];
			const result = computeFeatureSizeRag(target, percentiles, sizes, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when violations are exactly at allowed percentage", () => {
			// 85th percentile size = 7. Allowed = 15%.
			// 15 of 100 above 7 = 15% → exactly at limit → green
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const sizes = [...new Array(85).fill(5), ...new Array(15).fill(10)];
			const result = computeFeatureSizeRag(target, percentiles, sizes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when violations are exactly at the red threshold", () => {
			// 85th percentile size = 7. Allowed = 15%, red at > 25%.
			// 25 of 100 above 7 = 25% → not > 25% → amber
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const sizes = [...new Array(75).fill(5), ...new Array(25).fill(10)];
			const result = computeFeatureSizeRag(target, percentiles, sizes, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("includes the threshold size and percentages in the tip text", () => {
			const target = { percentile: 85, value: 90 };
			const percentiles = [{ percentile: 85, value: 7 }];
			const sizes = [...new Array(75).fill(5), ...new Array(25).fill(10)];
			const result = computeFeatureSizeRag(target, percentiles, sizes, terms);
			expect(result.tipText).toContain("85th percentile");
			expect(result.tipText).toContain("7");
			expect(result.tipText).toContain("15%"); // allowedAbove
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

	// -----------------------------------------------------------------------
	// M6 — PBC Widget Family
	// -----------------------------------------------------------------------

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
});
