import { describe, expect, it } from "vitest";
import {
	computeBlockedOverviewRag,
	computeCycleTimePercentilesRag,
	computeCycleTimeScatterplotRag,
	computeFeaturesWorkedOnRag,
	computePredictabilityScoreRag,
	computeStartedVsClosedRag,
	computeThroughputRag,
	computeTotalWorkItemAgeRag,
	computeWipOverviewRag,
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
});
