import { describe, expect, it } from "vitest";
import {
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
} from "./ragRules";

describe("ragRules", () => {
	describe("computeWipOverviewRag", () => {
		it("returns red when no system WIP limit is defined (0)", () => {
			const result = computeWipOverviewRag(5, 0);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define System WIP Limit.",
			});
		});

		it("returns red when no system WIP limit is defined (undefined)", () => {
			const result = computeWipOverviewRag(5, undefined);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define System WIP Limit.",
			});
		});

		it("returns red when WIP exceeds limit", () => {
			const result = computeWipOverviewRag(8, 5);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Close items to bring WIP down.",
			});
		});

		it("returns amber when WIP is below limit", () => {
			const result = computeWipOverviewRag(3, 5);
			expect(result).toEqual({
				ragStatus: "amber",
				tipText: "Start more items to operate at best capacity.",
			});
		});

		it("returns green when WIP matches limit", () => {
			const result = computeWipOverviewRag(5, 5);
			expect(result).toEqual({
				ragStatus: "green",
				tipText: "You match your System WIP Limit.",
			});
		});

		it("returns red with zero WIP and no limit", () => {
			const result = computeWipOverviewRag(0, 0);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define System WIP Limit.",
			});
		});
	});

	describe("computeBlockedOverviewRag", () => {
		it("returns red when no blocked config is defined", () => {
			const result = computeBlockedOverviewRag(0, false);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define blocked indicators in settings.",
			});
		});

		it("returns red when 2+ items are blocked", () => {
			const result = computeBlockedOverviewRag(2, true);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Focus on unblocking blocked work.",
			});
		});

		it("returns red when many items are blocked", () => {
			const result = computeBlockedOverviewRag(5, true);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Focus on unblocking blocked work.",
			});
		});

		it("returns amber when 1 item is blocked", () => {
			const result = computeBlockedOverviewRag(1, true);
			expect(result).toEqual({
				ragStatus: "amber",
				tipText: "Do not ignore blocked items.",
			});
		});

		it("returns green when no items are blocked", () => {
			const result = computeBlockedOverviewRag(0, true);
			expect(result).toEqual({
				ragStatus: "green",
				tipText: "No blockers.",
			});
		});
	});

	describe("computeFeaturesWorkedOnRag", () => {
		it("returns red when no feature WIP is defined (0)", () => {
			const result = computeFeaturesWorkedOnRag(3, 0);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define Feature WIP in settings.",
			});
		});

		it("returns red when no feature WIP is defined (undefined)", () => {
			const result = computeFeaturesWorkedOnRag(3, undefined);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define Feature WIP in settings.",
			});
		});

		it("returns red when feature count exceeds WIP", () => {
			const result = computeFeaturesWorkedOnRag(5, 3);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Focus your work to get features done more quickly.",
			});
		});

		it("returns amber when feature count is below WIP", () => {
			const result = computeFeaturesWorkedOnRag(1, 3);
			expect(result).toEqual({
				ragStatus: "amber",
				tipText: "Consider starting work for another feature.",
			});
		});

		it("returns green when feature count matches WIP", () => {
			const result = computeFeaturesWorkedOnRag(3, 3);
			expect(result).toEqual({
				ragStatus: "green",
				tipText: "Working at capacity.",
			});
		});
	});

	describe("computePredictabilityScoreRag", () => {
		it("returns undefined when score is null", () => {
			const result = computePredictabilityScoreRag(null);
			expect(result).toBeUndefined();
		});

		it("returns red when score is below 40%", () => {
			const result = computePredictabilityScoreRag(0.35);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Throughput is highly variable; forecasts will be unreliable.",
			});
		});

		it("returns red at exactly 0%", () => {
			const result = computePredictabilityScoreRag(0);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Throughput is highly variable; forecasts will be unreliable.",
			});
		});

		it("returns amber when score is between 40% and 60%", () => {
			const result = computePredictabilityScoreRag(0.5);
			expect(result).toEqual({
				ragStatus: "amber",
				tipText: "Moderate predictability; analyze bulk closings.",
			});
		});

		it("returns amber at exactly 40%", () => {
			const result = computePredictabilityScoreRag(0.4);
			expect(result).toEqual({
				ragStatus: "amber",
				tipText: "Moderate predictability; analyze bulk closings.",
			});
		});

		it("returns green when score is above 60%", () => {
			const result = computePredictabilityScoreRag(0.73);
			expect(result).toEqual({
				ragStatus: "green",
				tipText: "Process is reasonably stable; forecasts are trustworthy.",
			});
		});

		it("returns amber at exactly 60%", () => {
			const result = computePredictabilityScoreRag(0.6);
			expect(result).toEqual({
				ragStatus: "amber",
				tipText: "Moderate predictability; analyze bulk closings.",
			});
		});

		it("returns green above 60%", () => {
			const result = computePredictabilityScoreRag(0.61);
			expect(result).toEqual({
				ragStatus: "green",
				tipText: "Process is reasonably stable; forecasts are trustworthy.",
			});
		});

		it("returns green at 100%", () => {
			const result = computePredictabilityScoreRag(1.0);
			expect(result).toEqual({
				ragStatus: "green",
				tipText: "Process is reasonably stable; forecasts are trustworthy.",
			});
		});
	});

	describe("computeCycleTimePercentilesRag", () => {
		it("returns red when no SLE is defined (null)", () => {
			const result = computeCycleTimePercentilesRag(null, []);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define SLE in settings based on historical data.",
			});
		});

		it("returns green when at or above SLE", () => {
			// SLE: 85% within 10 days. percentiles show 85th = 9 days
			const sle = { percentile: 85, value: 10 };
			const percentiles = [
				{ percentile: 50, value: 5 },
				{ percentile: 70, value: 7 },
				{ percentile: 85, value: 9 },
				{ percentile: 95, value: 15 },
			];
			const result = computeCycleTimePercentilesRag(sle, percentiles);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when performance is 1-15% away from SLE", () => {
			// SLE: 85% within 10 days. 85th percentile = 11 (10% over)
			const sle = { percentile: 85, value: 10 };
			const percentiles = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 11 },
				{ percentile: 95, value: 18 },
			];
			const result = computeCycleTimePercentilesRag(sle, percentiles);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when performance is more than 15% away from SLE", () => {
			// SLE: 85% within 10 days. 85th percentile = 12 (20% over)
			const sle = { percentile: 85, value: 10 };
			const percentiles = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 12 },
				{ percentile: 95, value: 20 },
			];
			const result = computeCycleTimePercentilesRag(sle, percentiles);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when no matching percentile exists (no data)", () => {
			const sle = { percentile: 85, value: 10 };
			const result = computeCycleTimePercentilesRag(sle, []);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when actual equals SLE exactly", () => {
			const sle = { percentile: 85, value: 10 };
			const percentiles = [{ percentile: 85, value: 10 }];
			const result = computeCycleTimePercentilesRag(sle, percentiles);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber at exactly 1% over", () => {
			// SLE: 85% within 100 days. 85th = 101 (1% over)
			const sle = { percentile: 85, value: 100 };
			const percentiles = [{ percentile: 85, value: 101 }];
			const result = computeCycleTimePercentilesRag(sle, percentiles);
			expect(result.ragStatus).toBe("amber");
		});
	});

	describe("computeStartedVsClosedRag", () => {
		it("returns red when no system WIP limit is defined", () => {
			const result = computeStartedVsClosedRag(10, 8, undefined);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define System WIP Limit.",
			});
		});

		it("returns green when started and closed are equal", () => {
			const result = computeStartedVsClosedRag(10, 10, 5);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when difference is within 1 item", () => {
			const result = computeStartedVsClosedRag(10, 11, 5);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when starting much more than closing", () => {
			const result = computeStartedVsClosedRag(20, 10, 5);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when closing much more than starting", () => {
			const result = computeStartedVsClosedRag(10, 20, 5);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when both are zero", () => {
			const result = computeStartedVsClosedRag(0, 0, 5);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red for moderate starting-more gap (>10%)", () => {
			// started=12, closed=10, diff%=2/12=16.7%
			const result = computeStartedVsClosedRag(12, 10, 5);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computeTotalWorkItemAgeRag", () => {
		it("returns red when no WIP limit is defined", () => {
			const result = computeTotalWorkItemAgeRag(100, 5, undefined, 10);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define System WIP Limit and SLE.",
			});
		});

		it("returns red when no SLE is defined", () => {
			const result = computeTotalWorkItemAgeRag(100, 5, 5, undefined);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define System WIP Limit and SLE.",
			});
		});

		it("returns red when total age exceeds reference value", () => {
			// ref = WIP(5) * SLE(10) = 50. totalAge=60 > 50
			const result = computeTotalWorkItemAgeRag(60, 5, 5, 10);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when tomorrow projection exceeds reference", () => {
			// ref = 5*10=50. totalAge=46, tomorrow = 46+5=51 > 50
			const result = computeTotalWorkItemAgeRag(46, 5, 5, 10);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when total age and tomorrow are within range", () => {
			// ref = 5*10=50. totalAge=40, tomorrow=40+5=45 <= 50
			const result = computeTotalWorkItemAgeRag(40, 5, 5, 10);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when total age is exactly at reference", () => {
			// ref = 5*10=50. totalAge=50, tomorrow=50+5=55 but today is exactly at limit
			const result = computeTotalWorkItemAgeRag(50, 5, 5, 10);
			// totalAge = ref, but tomorrow > ref → amber
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when well below reference", () => {
			// ref =5*10=50. totalAge=20, tomorrow=20+5=25 <= 50
			const result = computeTotalWorkItemAgeRag(20, 5, 5, 10);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeThroughputRag", () => {
		it("returns green with consistent throughput (no zero periods)", () => {
			const values = [3, 5, 2, 4, 1];
			const result = computeThroughputRag(values, []);
			expect(result).toEqual({
				ragStatus: "green",
				tipText: "Stable, predictable delivery.",
			});
		});

		it("returns amber with one consecutive zero period", () => {
			const values = [3, 0, 2, 4, 1];
			const result = computeThroughputRag(values, []);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red with multiple consecutive zero periods", () => {
			const values = [3, 0, 0, 4, 1];
			const result = computeThroughputRag(values, []);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red with all zero periods", () => {
			const values = [0, 0, 0, 0];
			const result = computeThroughputRag(values, []);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green with empty data", () => {
			const result = computeThroughputRag([], []);
			expect(result.ragStatus).toBe("green");
		});

		it("excludes blackout days from zero-period detection", () => {
			// periods: [3, 0, 0, 4, 1], blackout at indices 1,2
			const values = [3, 0, 0, 4, 1];
			const result = computeThroughputRag(values, [1, 2]);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when one non-blackout zero remains", () => {
			// periods: [3, 0, 0, 0, 1], blackout at indices 1,2
			// non-blackout: [3, _, _, 0, 1] → one isolated zero
			const values = [3, 0, 0, 0, 1];
			const result = computeThroughputRag(values, [1, 2]);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when multiple consecutive non-blackout zeros", () => {
			// [3, 0, 0, 0, 0], blackout at [1]
			// non-blackout: [3, _, 0, 0, 0] → 3 consecutive
			const values = [3, 0, 0, 0, 0];
			const result = computeThroughputRag(values, [1]);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computeCycleTimeScatterplotRag", () => {
		it("returns red when no SLE is defined", () => {
			const result = computeCycleTimeScatterplotRag(null, []);
			expect(result).toEqual({
				ragStatus: "red",
				tipText: "Define a Service Level Expectation based on historical data.",
			});
		});

		it("returns green when within SLE", () => {
			// SLE: 85% within 10 days. Items: 9 under 10, 1 over
			// 10% above → within X(85%) tolerance
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [2, 3, 4, 5, 6, 7, 8, 9, 9, 11];
			// 1 out of 10 = 10% above, SLE says 15% allowed
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when between X% and (X+10)% above SLE days", () => {
			// SLE: 85% within 10 days → 15% allowed above
			// If 18% above → between 15% and 25% → amber
			const sle = { percentile: 85, value: 10 };
			// 18 of 100 items above 10 days
			const cycleTimes = Array(82).fill(5).concat(Array(18).fill(15));
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when (X+10)%+ items above SLE days", () => {
			// SLE: 85% within 10 days → 15% allowed, red at 25%+
			// 30% above
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = Array(70).fill(5).concat(Array(30).fill(15));
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green with empty cycle time data", () => {
			const sle = { percentile: 85, value: 10 };
			const result = computeCycleTimeScatterplotRag(sle, []);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when all items within SLE", () => {
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 5];
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes);
			expect(result.ragStatus).toBe("green");
		});
	});

	// -----------------------------------------------------------------------
	// M4 — Aging and Flow Stability Widgets
	// -----------------------------------------------------------------------

	describe("computeWorkItemAgeChartRag", () => {
		it("returns red when no SLE is set", () => {
			const result = computeWorkItemAgeChartRag(null, true, []);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("SLE");
		});

		it("returns red when no blocked config is set", () => {
			const sle = { percentile: 85, value: 14 };
			const result = computeWorkItemAgeChartRag(sle, false, []);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("blocked");
		});

		it("returns red when any item is above SLE", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 10, isBlocked: false },
				{ workItemAge: 15, isBlocked: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when item is within 15% of SLE", () => {
			const sle = { percentile: 85, value: 14 };
			// 15% of 14 = 2.1, so threshold = 14 - 2.1 = 11.9. Item at 12 is within 15%.
			const items = [
				{ workItemAge: 12, isBlocked: false },
				{ workItemAge: 5, isBlocked: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns amber when any item is blocked", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: true },
				{ workItemAge: 3, isBlocked: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when no blockers and all below SLE", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: false },
				{ workItemAge: 8, isBlocked: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green with empty items", () => {
			const sle = { percentile: 85, value: 14 };
			const result = computeWorkItemAgeChartRag(sle, true, []);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeWipOverTimeRag", () => {
		it("returns red when no system WIP is set", () => {
			const result = computeWipOverTimeRag([3, 4, 5], undefined);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("System WIP");
		});

		it("returns red when more days above WIP than at/below", () => {
			// WIP limit = 5, values: [6, 7, 6, 5, 4] → above=3, at=1, below=1 → above > at+below-above? above(3) > rest(2) → Red
			const result = computeWipOverTimeRag([6, 7, 6, 5, 4], 5);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when most days at the WIP limit (>50%)", () => {
			// WIP limit = 5, values: [5, 5, 5, 4, 6] → at=3 out of 5 = 60% → Green
			const result = computeWipOverTimeRag([5, 5, 5, 4, 6], 5);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when more days below than at/above", () => {
			// WIP limit = 5, values: [3, 2, 4, 5, 6] → above=1, at=1, below=3 → below > above+at → Amber
			const result = computeWipOverTimeRag([3, 2, 4, 5, 6], 5);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns amber for mixed distribution with no dominant pattern", () => {
			// WIP limit = 5, values: [4, 6, 5, 4] → above=1, at=1, below=2, atPercent=25% → mixed → Amber
			const result = computeWipOverTimeRag([4, 6, 5, 4], 5);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green with empty data", () => {
			const result = computeWipOverTimeRag([], 5);
			expect(result.ragStatus).toBe("green");
		});

		it("handles 50/50 split as not red", () => {
			// WIP limit = 5, values: [6, 4] → above=1, below=1 → 50/50 → not red
			const result = computeWipOverTimeRag([6, 4], 5);
			expect(result.ragStatus).not.toBe("red");
		});
	});

	describe("computeTotalWorkItemAgeOverTimeRag", () => {
		it("returns red when end is higher than start by >10%", () => {
			// start=100, end=115 → (115-100)/100 = 15% > 10% → Red
			const result = computeTotalWorkItemAgeOverTimeRag(100, 115);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when start is higher than end by >10%", () => {
			// start=100, end=85 → decrease of 15% → Amber
			const result = computeTotalWorkItemAgeOverTimeRag(100, 85);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when start and end are within 10% margin", () => {
			// start=100, end=105 → 5% change → Green
			const result = computeTotalWorkItemAgeOverTimeRag(100, 105);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when start and end are equal", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 100);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when both are zero", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(0, 0);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when start is zero and end is positive", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(0, 10);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computeSimplifiedCfdRag", () => {
		it("delegates to startedVsClosed logic", () => {
			// Same behavior: no WIP limit → Red
			const result = computeSimplifiedCfdRag(10, 8, undefined);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when balanced with WIP limit", () => {
			const result = computeSimplifiedCfdRag(10, 10, 5);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when started much more than closed", () => {
			const result = computeSimplifiedCfdRag(20, 10, 5);
			expect(result.ragStatus).toBe("red");
		});
	});

	// -----------------------------------------------------------------------
	// M5 — Portfolio and Correlation Widgets
	// -----------------------------------------------------------------------

	describe("computeWorkDistributionRag", () => {
		it("returns red when unlinked items >= 20%", () => {
			const result = computeWorkDistributionRag(20, 100, 3, 2);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("linked");
		});

		it("returns red when no feature WIP is set", () => {
			const result = computeWorkDistributionRag(0, 100, undefined, 5);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Feature WIP");
		});

		it("returns red when distribution rate exceeds feature WIP by >20%", () => {
			// featureWip=3, distributionRate=4 → (4-3)/3=33% → >20% → Red
			const result = computeWorkDistributionRag(5, 100, 3, 4);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("thin");
		});

		it("returns amber when distribution rate slightly above feature WIP (up to 20%)", () => {
			// featureWip=5, distributionRate=6 → (6-5)/5=20% → Amber
			const result = computeWorkDistributionRag(5, 100, 5, 6);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when distribution rate at or below feature WIP", () => {
			const result = computeWorkDistributionRag(5, 100, 5, 5);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when distribution rate is below feature WIP", () => {
			const result = computeWorkDistributionRag(5, 100, 5, 3);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green with zero total items", () => {
			const result = computeWorkDistributionRag(0, 0, 5, 0);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeFeatureSizeRag", () => {
		it("returns red when no feature size target is set", () => {
			const result = computeFeatureSizeRag(null, []);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Feature Size Target");
		});

		it("returns green when actual is at or below target", () => {
			const target = { percentile: 85, value: 10 };
			const percentiles = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 9 },
			];
			const result = computeFeatureSizeRag(target, percentiles);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when actual is 1-15% above target", () => {
			const target = { percentile: 85, value: 10 };
			// Actual 85th = 11 → (11-10)/10 = 10% → Amber
			const percentiles = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 11 },
			];
			const result = computeFeatureSizeRag(target, percentiles);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when actual is >15% above target", () => {
			const target = { percentile: 85, value: 10 };
			// Actual 85th = 12 → (12-10)/10 = 20% → Red
			const percentiles = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 12 },
			];
			const result = computeFeatureSizeRag(target, percentiles);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when no matching percentile found", () => {
			const target = { percentile: 85, value: 10 };
			const percentiles = [{ percentile: 50, value: 5 }];
			const result = computeFeatureSizeRag(target, percentiles);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeEstimationVsCycleTimeRag", () => {
		it("returns red when not configured", () => {
			const result = computeEstimationVsCycleTimeRag("NotConfigured", []);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("estimation");
		});

		it("returns green with empty data points", () => {
			const result = computeEstimationVsCycleTimeRag("Configured", []);
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
			const result = computeEstimationVsCycleTimeRag("Configured", data);
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
			const result = computeEstimationVsCycleTimeRag("Configured", data);
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
			const result = computeEstimationVsCycleTimeRag("Configured", data);
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
});
