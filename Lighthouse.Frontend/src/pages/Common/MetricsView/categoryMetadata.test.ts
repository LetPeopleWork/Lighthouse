import { describe, expect, it } from "vitest";
import {
	type CategoryDefinition,
	type CategoryKey,
	getCategories,
	getDefaultCategoryKey,
	getFetchKeysForCategories,
	getFetchRequirementsForWidget,
	getMetricsFetchKeys,
	getTrendPolicy,
	getWidgetsForCategory,
	type MetricsFetchKey,
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
				"staleOverview",
				"featuresWorkedOnOverview",
				"totalWorkItemAge",
				"flowEfficiency",
				"predictabilityScore",
				"percentiles",
				"workItemAgePercentiles",
				"totalThroughput",
				"totalArrivals",
			]);
		});

		it("flow-overview contains expected info widgets for portfolio", () => {
			const widgets = getWidgetsForCategory("flow-overview", "portfolio");
			expect(widgets.map((w) => w.widgetKey)).toEqual([
				"wipOverview",
				"blockedOverview",
				"staleOverview",
				"totalWorkItemAge",
				"flowEfficiency",
				"predictabilityScore",
				"percentiles",
				"workItemAgePercentiles",
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
				"wipOverTime",
				"totalWorkItemAgeOverTime",
				"arrivals",
				"stacked",
				"loadBalanceMatrix",
				"stateTimeCumulative",
				"blockedCountHistory",
			]);
		});

		it("stateTimeCumulative appears in flow-metrics for both team and portfolio owners", () => {
			const teamWidgets = getWidgetsForCategory("flow-metrics", "team").map(
				(w) => w.widgetKey,
			);
			const portfolioWidgets = getWidgetsForCategory(
				"flow-metrics",
				"portfolio",
			).map((w) => w.widgetKey);
			expect(teamWidgets).toContain("stateTimeCumulative");
			expect(portfolioWidgets).toContain("stateTimeCumulative");
		});

		it("predictability contains expected widgets in order for team", () => {
			const widgets = getWidgetsForCategory("predictability", "team");
			expect(widgets.map((w) => w.widgetKey)).toEqual([
				"predictabilityScoreDetails",
				"percentilesOverTime",
				"pbcOverTime",
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
				"blockedOverview",
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
				"staleOverview",
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
				"loadBalanceMatrix",
				"workDistribution",
				"featureSize",
				"aging",
				"estimationVsCycleTime",
				"stateTimeCumulative",
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

	describe("Bug #5571 — widget fetch requirements", () => {
		const ownerTypes = ["team", "portfolio"] as const;

		// The only widgets allowed to declare nothing: they own their fetch lifecycle inside
		// themselves (lazy, on mount, per-selection cache) and read nothing from useMetricsData.
		const selfFetchingWidgets = ["percentilesOverTime", "pbcOverTime"];

		function everyPlacement(): { widgetKey: string; where: string }[] {
			const placements: { widgetKey: string; where: string }[] = [];
			for (const cat of getCategories()) {
				for (const ownerType of ownerTypes) {
					for (const w of getWidgetsForCategory(cat.key, ownerType)) {
						placements.push({
							widgetKey: w.widgetKey,
							where: `${cat.key}/${ownerType}`,
						});
					}
				}
			}
			return placements;
		}

		it("gives every widget a declared data requirement, so none can ship eagerly", () => {
			for (const { widgetKey, where } of everyPlacement()) {
				expect(
					getFetchRequirementsForWidget(widgetKey),
					`widget "${widgetKey}" (${where}) has no widgetFetchRequirements entry — it would render without data`,
				).toBeDefined();
			}
		});

		it("only self-fetching widgets are allowed to require nothing", () => {
			for (const { widgetKey, where } of everyPlacement()) {
				if (selfFetchingWidgets.includes(widgetKey)) {
					continue;
				}
				expect(
					getFetchRequirementsForWidget(widgetKey) ?? [],
					`widget "${widgetKey}" (${where}) declares an empty requirement set`,
				).not.toHaveLength(0);
			}
		});

		it("flow-overview does not pull data only other categories can render", () => {
			const keys = getFetchKeysForCategories(["flow-overview"], "team");
			const forbidden: MetricsFetchKey[] = [
				"pbcCore",
				"pbcCharts",
				"blackoutPeriods",
				"wipOverTime",
				"ageInStatePercentiles",
				"cumulativeStateTime",
				"estimationVsCycleTime",
				"featureSizeData",
				"featureSizePbc",
				"featureSizeEstimation",
				"featureSizePercentilesInfo",
			];
			for (const key of forbidden) {
				expect(
					keys.has(key),
					`${key} has no flow-overview consumer and must not be fetched for it`,
				).toBe(false);
			}
		});

		it("flow-overview pulls everything its bodies, RAG chips and trends read", () => {
			const keys = getFetchKeysForCategories(["flow-overview"], "team");
			const required: MetricsFetchKey[] = [
				"inProgressItems",
				"wipOverviewInfo",
				"blockedItems",
				"blockedCountHistory",
				"featuresWorkedOnInfo",
				"totalWorkItemAge",
				"totalWorkItemAgeInfo",
				"flowEfficiency",
				"predictability",
				"predictabilityScoreInfo",
				"cycleTimeData",
				"cycleTimePercentiles",
				"cycleTimePercentilesInfo",
				"workItemAgePercentiles",
				"throughput",
				"throughputInfo",
				"arrivals",
				"arrivalsInfo",
			];
			for (const key of required) {
				expect(keys.has(key), `flow-overview needs ${key}`).toBe(true);
			}
		});

		it("flow-overview pulls feature sizes only for a portfolio owner", () => {
			expect(
				getFetchKeysForCategories(["flow-overview"], "portfolio").has(
					"featureSizeData",
				),
			).toBe(true);
			expect(
				getFetchKeysForCategories(["flow-overview"], "team").has(
					"featureSizeData",
				),
			).toBe(false);
		});

		it("unions the requirements of every category it is given", () => {
			const union = getFetchKeysForCategories(
				["flow-overview", "flow-metrics"],
				"team",
			);
			expect(union.has("wipOverviewInfo")).toBe(true);
			expect(union.has("ageInStatePercentiles")).toBe(true);
		});

		it("keeps the PBC drill-through lookup sources on predictability (R3)", () => {
			const teamKeys = getFetchKeysForCategories(["predictability"], "team");
			const lookupSources: MetricsFetchKey[] = [
				"throughput",
				"wipOverTime",
				"cycleTimeData",
				"inProgressItems",
			];
			for (const key of lookupSources) {
				expect(
					teamKeys.has(key),
					`predictability needs ${key}: every PBC node receives workItemLookup, which is built from it`,
				).toBe(true);
			}
			expect(
				getFetchKeysForCategories(["predictability"], "portfolio").has(
					"featureSizeData",
				),
			).toBe(true);
		});

		it("leaves no fetch key unreachable — nothing is fetched that no widget consumes", () => {
			const reachable = new Set<MetricsFetchKey>();
			for (const cat of getCategories()) {
				for (const ownerType of ownerTypes) {
					for (const key of getFetchKeysForCategories([cat.key], ownerType)) {
						reachable.add(key);
					}
				}
			}
			for (const key of getMetricsFetchKeys()) {
				expect(
					reachable.has(key),
					`${key} is declared but no widget in any category requires it`,
				).toBe(true);
			}
		});

		it("returns undefined for unknown widget keys", () => {
			expect(
				getFetchRequirementsForWidget("nonexistent-widget"),
			).toBeUndefined();
		});

		it("returns an empty set for no categories", () => {
			const keys = getFetchKeysForCategories([] as CategoryKey[], "team");
			expect(keys.size).toBe(0);
		});
	});
});
