import { describe, expect, it } from "vitest";
import {
	type CategoryDefinition,
	type CategoryKey,
	getCategories,
	getDefaultCategoryKey,
	getTrendPolicy,
	getWidgetsForCategory,
} from "./categoryMetadata";

describe("categoryMetadata", () => {
	it("returns all four categories in order", () => {
		const categories = getCategories();
		expect(categories).toHaveLength(4);
		expect(categories.map((c: CategoryDefinition) => c.key)).toEqual([
			"flow-overview",
			"flow-metrics",
			"predictability",
			"portfolio",
		]);
	});

	it("every category has a displayName, icon, and hoverText", () => {
		for (const cat of getCategories()) {
			expect(cat.displayName).toBeTruthy();
			expect(cat.icon).toBeTruthy();
			expect(cat.hoverText).toBeTruthy();
		}
	});

	it("default category is flow-overview", () => {
		expect(getDefaultCategoryKey()).toBe("flow-overview");
	});

	it("every placement has a valid size", () => {
		const validSizes = ["small", "medium", "large", "xlarge"];
		for (const cat of getCategories()) {
			for (const w of getWidgetsForCategory(cat.key, "team")) {
				expect(validSizes).toContain(w.size);
			}
		}
	});

	it("category display names match the story taxonomy", () => {
		const categories = getCategories();
		expect(categories.map((c) => c.displayName)).toEqual([
			"Flow Overview",
			"Flow Metrics",
			"Predictability",
			"Portfolio & Features",
		]);
	});

	describe("Story 4804 — category ownership invariants", () => {
		it("each widget appears in exactly one category for team owner", () => {
			const allCategories = getCategories();
			const widgetOccurrences = new Map<string, CategoryKey[]>();

			for (const cat of allCategories) {
				for (const w of getWidgetsForCategory(cat.key, "team")) {
					const existing = widgetOccurrences.get(w.widgetKey) ?? [];
					widgetOccurrences.set(w.widgetKey, [...existing, cat.key]);
				}
			}

			for (const [widgetKey, cats] of widgetOccurrences) {
				expect(
					cats,
					`${widgetKey} appears in [${cats.join(", ")}]`,
				).toHaveLength(1);
			}
		});

		it("each widget appears in exactly one category for portfolio owner", () => {
			const allCategories = getCategories();
			const widgetOccurrences = new Map<string, CategoryKey[]>();

			for (const cat of allCategories) {
				for (const w of getWidgetsForCategory(cat.key, "portfolio")) {
					const existing = widgetOccurrences.get(w.widgetKey) ?? [];
					widgetOccurrences.set(w.widgetKey, [...existing, cat.key]);
				}
			}

			for (const [widgetKey, cats] of widgetOccurrences) {
				expect(
					cats,
					`${widgetKey} appears in [${cats.join(", ")}]`,
				).toHaveLength(1);
			}
		});

		it("all PBC widgets appear only in predictability", () => {
			const pbcWidgetKeys = [
				"throughputPbc",
				"arrivalsPbc",
				"wipPbc",
				"totalWorkItemAgePbc",
				"cycleTimePbc",
				"featureSizePbc",
			];
			const nonPredictabilityCategories = getCategories().filter(
				(c) => c.key !== "predictability",
			);

			for (const cat of nonPredictabilityCategories) {
				const teamWidgets = getWidgetsForCategory(cat.key, "team").map(
					(w) => w.widgetKey,
				);
				const portfolioWidgets = getWidgetsForCategory(
					cat.key,
					"portfolio",
				).map((w) => w.widgetKey);
				for (const pbcKey of pbcWidgetKeys) {
					expect(
						teamWidgets,
						`${pbcKey} should not be in ${cat.key} (team)`,
					).not.toContain(pbcKey);
					expect(
						portfolioWidgets,
						`${pbcKey} should not be in ${cat.key} (portfolio)`,
					).not.toContain(pbcKey);
				}
			}
		});

		it("startedVsFinished is not present in any category", () => {
			for (const cat of getCategories()) {
				const teamWidgets = getWidgetsForCategory(cat.key, "team").map(
					(w) => w.widgetKey,
				);
				const portfolioWidgets = getWidgetsForCategory(
					cat.key,
					"portfolio",
				).map((w) => w.widgetKey);
				expect(teamWidgets).not.toContain("startedVsFinished");
				expect(portfolioWidgets).not.toContain("startedVsFinished");
			}
		});

		it("dedicated throughput info widget exists in flow-overview", () => {
			const widgets = getWidgetsForCategory("flow-overview", "team");
			expect(widgets.map((w) => w.widgetKey)).toContain("totalThroughput");
		});

		it("dedicated arrivals info widget exists in flow-overview", () => {
			const widgets = getWidgetsForCategory("flow-overview", "team");
			expect(widgets.map((w) => w.widgetKey)).toContain("totalArrivals");
		});

		it("dedicated feature size percentiles info widget exists in flow-overview for portfolio", () => {
			const widgets = getWidgetsForCategory("flow-overview", "portfolio");
			expect(widgets.map((w) => w.widgetKey)).toContain(
				"featureSizePercentiles",
			);
		});

		it("feature size percentiles info widget is portfolio-only", () => {
			const widgets = getWidgetsForCategory("flow-overview", "team");
			expect(widgets.map((w) => w.widgetKey)).not.toContain(
				"featureSizePercentiles",
			);
		});
	});

	describe("Story 4804 — per-category widget composition", () => {
		it("flow-overview contains expected info widgets in order for team", () => {
			const widgets = getWidgetsForCategory("flow-overview", "team");
			expect(widgets.map((w) => w.widgetKey)).toEqual([
				"wipOverview",
				"blockedOverview",
				"featuresWorkedOnOverview",
				"totalWorkItemAge",
				"predictabilityScore",
				"percentiles",
				"totalThroughput",
				"totalArrivals",
			]);
		});

		it("flow-overview contains expected info widgets for portfolio", () => {
			const widgets = getWidgetsForCategory("flow-overview", "portfolio");
			expect(widgets.map((w) => w.widgetKey)).toEqual([
				"wipOverview",
				"blockedOverview",
				"totalWorkItemAge",
				"predictabilityScore",
				"percentiles",
				"totalThroughput",
				"totalArrivals",
				"featureSizePercentiles",
			]);
		});

		it("flow-overview excludes team-only widgets for portfolio", () => {
			const widgets = getWidgetsForCategory("flow-overview", "portfolio");
			expect(widgets.map((w) => w.widgetKey)).not.toContain(
				"featuresWorkedOnOverview",
			);
		});

		it("flow-metrics contains expected widgets in order for team", () => {
			const widgets = getWidgetsForCategory("flow-metrics", "team");
			expect(widgets.map((w) => w.widgetKey)).toEqual([
				"cycleScatter",
				"aging",
				"throughput",
				"stacked",
				"wipOverTime",
				"totalWorkItemAgeOverTime",
			]);
		});

		it("predictability contains expected widgets in order for team", () => {
			const widgets = getWidgetsForCategory("predictability", "team");
			expect(widgets.map((w) => w.widgetKey)).toEqual([
				"predictabilityScoreDetails",
				"arrivals",
				"throughputPbc",
				"arrivalsPbc",
				"wipPbc",
				"totalWorkItemAgePbc",
				"cycleTimePbc",
			]);
		});

		it("predictability includes featureSizePbc for portfolio", () => {
			const widgets = getWidgetsForCategory("predictability", "portfolio");
			expect(widgets.map((w) => w.widgetKey)).toContain("featureSizePbc");
		});

		it("portfolio contains expected widgets in order for team", () => {
			const widgets = getWidgetsForCategory("portfolio", "team");
			expect(widgets.map((w) => w.widgetKey)).toEqual([
				"workDistribution",
				"estimationVsCycleTime",
			]);
		});

		it("portfolio includes feature size for portfolio owner", () => {
			const widgets = getWidgetsForCategory("portfolio", "portfolio");
			expect(widgets.map((w) => w.widgetKey)).toContain("featureSize");
		});
	});

	describe("Story 4804 — trend policy", () => {
		it("snapshot-compare widgets have snapshot trend policy", () => {
			const snapshotWidgets = [
				"wipOverview",
				"featuresWorkedOnOverview",
				"totalWorkItemAge",
			];
			for (const widgetKey of snapshotWidgets) {
				expect(
					getTrendPolicy(widgetKey),
					`${widgetKey} should be snapshot-compare`,
				).toBe("snapshot-compare");
			}
		});

		it("previous-period widgets have previous-period trend policy", () => {
			const previousPeriodWidgets = [
				"predictabilityScore",
				"totalThroughput",
				"totalArrivals",
				"percentiles",
				"featureSizePercentiles",
			];
			for (const widgetKey of previousPeriodWidgets) {
				expect(
					getTrendPolicy(widgetKey),
					`${widgetKey} should be previous-period`,
				).toBe("previous-period");
			}
		});

		it("no-trend widgets have none trend policy", () => {
			const noTrendWidgets = [
				"blockedOverview",
				"predictabilityScoreDetails",
				"throughputPbc",
				"arrivalsPbc",
				"wipPbc",
				"totalWorkItemAgePbc",
				"cycleTimePbc",
				"featureSizePbc",
				"stacked",
				"cycleScatter",
				"wipOverTime",
				"throughput",
				"arrivals",
				"totalWorkItemAgeOverTime",
				"workDistribution",
				"featureSize",
				"aging",
				"estimationVsCycleTime",
			];
			for (const widgetKey of noTrendWidgets) {
				expect(getTrendPolicy(widgetKey), `${widgetKey} should be none`).toBe(
					"none",
				);
			}
		});

		it("returns none for unknown widget keys", () => {
			expect(getTrendPolicy("nonexistent-widget")).toBe("none");
		});
	});
});
