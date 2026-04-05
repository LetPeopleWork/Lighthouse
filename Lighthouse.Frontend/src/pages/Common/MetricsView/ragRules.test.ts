import { describe, expect, it } from "vitest";
import {
	computeBlockedOverviewRag,
	computeFeaturesWorkedOnRag,
	computePredictabilityScoreRag,
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
});
