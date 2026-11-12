import { existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import type { RagTerms } from "./ragTypes";

type Family = typeof import("./ragRulesOverview");

// Loaded without a static import so a missing family module fails the rows below, not the collection.
const familyPath = "./ragRulesOverview";
const family = (await import(/* @vite-ignore */ familyPath).catch(
	() => ({}),
)) as Family;
const {
	computeBlockedOverviewRag,
	computeFeaturesWorkedOnRag,
	computeFlowEfficiencyRag,
	computePredictabilityScoreRag,
	computeStaleOverviewRag,
	computeWipOverviewRag,
} = family;

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

	describe("computeStaleOverviewRag", () => {
		it("returns red when staleness is not configured", () => {
			const result = computeStaleOverviewRag(0, false, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define a staleness threshold");
		});

		it("returns red when 2+ items are stale", () => {
			const result = computeStaleOverviewRag(2, true, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Focus on unsticking");
		});

		it("returns red when many items are stale", () => {
			const result = computeStaleOverviewRag(5, true, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Focus on unsticking");
		});

		it("returns amber when 1 item is stale", () => {
			const result = computeStaleOverviewRag(1, true, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("Do not ignore");
		});

		it("returns green when no items are stale", () => {
			const result = computeStaleOverviewRag(0, true, terms);
			expect(result.ragStatus).toBe("green");
			expect(result.tipText).toContain("No stale");
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

	describe("computeFlowEfficiencyRag", () => {
		it.each([
			{
				scenario: "efficiency is below 40%",
				efficiencyPercent: 39,
				expected: "red",
			},
			{
				scenario: "efficiency is between 40% and 60%",
				efficiencyPercent: 50,
				expected: "amber",
			},
			{
				scenario: "efficiency is at or above 60%",
				efficiencyPercent: 72,
				expected: "green",
			},
		])(
			"returns $expected when $scenario",
			({ efficiencyPercent, expected }) => {
				const result = computeFlowEfficiencyRag(efficiencyPercent, terms);

				expect(result.ragStatus).toBe(expected);
			},
		);

		it("treats exactly 40% as the amber lower boundary", () => {
			const result = computeFlowEfficiencyRag(40, terms);

			expect(result.ragStatus).toBe("amber");
		});

		it("treats exactly 60% as the green lower boundary", () => {
			const result = computeFlowEfficiencyRag(60, terms);

			expect(result.ragStatus).toBe("green");
		});

		it("names the efficiency percentage in the red tip", () => {
			const result = computeFlowEfficiencyRag(25, terms);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("25");
		});

		it("names the 40-60 band and the rounded value in the amber tip", () => {
			const result = computeFlowEfficiencyRag(50, terms);

			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("50");
			expect(result.tipText).toContain("40");
			expect(result.tipText).toContain("60");
		});

		it("names the 60% target and the rounded value in the green tip", () => {
			const result = computeFlowEfficiencyRag(72, terms);

			expect(result.ragStatus).toBe("green");
			expect(result.tipText).toContain("72");
			expect(result.tipText).toContain("60");
		});
	});
});

const here = dirname(fileURLToPath(import.meta.url));

/** The rule functions a family module exports, or null when the module does not exist. */
const exportedRules = async (module: string): Promise<string[] | null> => {
	try {
		const loaded = await import(/* @vite-ignore */ `./${module}`);

		return Object.keys(loaded)
			.filter((name) => typeof loaded[name] === "function")
			.sort();
	} catch {
		return null;
	}
};

const MISSING =
	"The family module does not exist yet; create it and move its rules there unchanged, so the metric status rules are split by family.";

describe("the status rules are split into three rule families", () => {
	it("keeps the overview rules in the overview family", async () => {
		expect(await exportedRules("ragRulesOverview"), MISSING).toEqual([
			"computeBlockedOverviewRag",
			"computeFeaturesWorkedOnRag",
			"computeFlowEfficiencyRag",
			"computePredictabilityScoreRag",
			"computeStaleOverviewRag",
			"computeWipOverviewRag",
		]);
	});

	it("keeps the time and age rules in the time-and-age family", async () => {
		expect(await exportedRules("ragRulesTimeAndAge"), MISSING).toEqual([
			"computeCumulativeStateTimeRag",
			"computeCycleTimePercentilesRag",
			"computeCycleTimeScatterplotRag",
			"computeSleRiskRag",
			"computeTotalWorkItemAgeOverTimeRag",
			"computeTotalWorkItemAgeRag",
			"computeWorkItemAgeChartRag",
			"computeWorkItemAgePercentilesRag",
		]);
	});

	it("keeps the volume and flow rules in the volume-and-flow family", async () => {
		expect(await exportedRules("ragRulesVolumeAndFlow"), MISSING).toEqual([
			"computeArrivalsRunChartRag",
			"computeEstimationVsCycleTimeRag",
			"computeFeatureSizeRag",
			"computeLoadBalanceMatrixRag",
			"computePbcRag",
			"computeSimplifiedCfdRag",
			"computeStartedVsClosedRag",
			"computeThroughputRag",
			"computeWipOverTimeRag",
			"computeWorkDistributionRag",
		]);
	});

	it("leaves no catch-all rules file behind", () => {
		expect(
			existsSync(join(here, "ragRules.ts")),
			"ragRules.ts still exists. Once its rules have moved to the families, delete it; no file re-exports them.",
		).toBe(false);
	});
});
