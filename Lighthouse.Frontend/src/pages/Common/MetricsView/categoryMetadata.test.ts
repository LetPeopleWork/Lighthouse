import { describe, expect, it } from "vitest";
import {
	type CategoryDefinition,
	getCategories,
	getDefaultCategoryKey,
	getWidgetsForCategory,
} from "./categoryMetadata";

describe("categoryMetadata", () => {
	it("returns all five categories", () => {
		const categories = getCategories();
		expect(categories).toHaveLength(5);
		expect(categories.map((c: CategoryDefinition) => c.key)).toEqual([
			"flow-health",
			"aging-stability",
			"predictability",
			"portfolio",
			"overview",
		]);
	});

	it("default category is flow-health", () => {
		expect(getDefaultCategoryKey()).toBe("flow-health");
	});

	it("flow-health contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("flow-health", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"itemsInProgress",
			"startedVsFinished",
			"percentiles",
			"totalWorkItemAge",
			"throughput",
			"cycleScatter",
			"workDistribution",
			"wipOverTime",
		]);
	});

	it("excludes portfolio-only widgets for team owner", () => {
		const widgets = getWidgetsForCategory("predictability", "team");
		expect(widgets.map((w) => w.widgetKey)).not.toContain("featureSizePbc");
	});

	it("includes portfolio-only widgets for portfolio owner", () => {
		const widgets = getWidgetsForCategory("predictability", "portfolio");
		expect(widgets.map((w) => w.widgetKey)).toContain("featureSizePbc");
	});

	it("widgets appear in multiple categories", () => {
		const flow = getWidgetsForCategory("flow-health", "team");
		const aging = getWidgetsForCategory("aging-stability", "team");
		expect(flow.map((w) => w.widgetKey)).toContain("wipOverTime");
		expect(aging.map((w) => w.widgetKey)).toContain("wipOverTime");
	});

	it("every placement has a valid size", () => {
		const validSizes = ["small", "medium", "large", "xlarge"];
		for (const cat of getCategories()) {
			for (const w of getWidgetsForCategory(cat.key, "team")) {
				expect(validSizes).toContain(w.size);
			}
		}
	});
});
