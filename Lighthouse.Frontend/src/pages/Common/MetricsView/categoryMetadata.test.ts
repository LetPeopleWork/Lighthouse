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
			"wipOverview",
			"startedVsFinished",
			"percentiles",
			"totalWorkItemAge",
			"throughput",
			"cycleScatter",
			"workDistribution",
			"wipOverTime",
		]);
	});

	it("aging-stability contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("aging-stability", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"wipOverview",
			"totalWorkItemAge",
			"aging",
			"wipOverTime",
			"totalWorkItemAgeOverTime",
			"stacked",
		]);
	});

	it("overview contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("overview", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"wipOverview",
			"blockedOverview",
			"featuresWorkedOnOverview",
			"totalWorkItemAge",
			"predictabilityScore",
			"startedVsFinished",
			"percentiles",
		]);
	});

	it("overview excludes team-only widgets for portfolio", () => {
		const widgets = getWidgetsForCategory("overview", "portfolio");
		expect(widgets.map((w) => w.widgetKey)).not.toContain(
			"featuresWorkedOnOverview",
		);
	});

	it("predictability contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("predictability", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"predictabilityScore",
			"percentiles",
			"throughputPbc",
			"wipPbc",
			"totalWorkItemAgePbc",
			"cycleTimePbc",
		]);
	});

	it("portfolio contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("portfolio", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"featuresWorkedOnOverview",
			"predictabilityScore",
			"workDistribution",
			"estimationVsCycleTime",
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

	it("excludes team-only widgets for portfolio owner", () => {
		const widgets = getWidgetsForCategory("portfolio", "portfolio");
		expect(widgets.map((w) => w.widgetKey)).not.toContain(
			"featuresWorkedOnOverview",
		);
	});

	it("includes team-only widgets for team owner", () => {
		const widgets = getWidgetsForCategory("portfolio", "team");
		expect(widgets.map((w) => w.widgetKey)).toContain(
			"featuresWorkedOnOverview",
		);
	});

	it("widgets appear in multiple categories", () => {
		const flow = getWidgetsForCategory("flow-health", "team");
		const aging = getWidgetsForCategory("aging-stability", "team");
		expect(flow.map((w) => w.widgetKey)).toContain("wipOverTime");
		expect(aging.map((w) => w.widgetKey)).toContain("wipOverTime");
	});

	it("wipOverview appears in flow-health, aging-stability, and overview", () => {
		const flow = getWidgetsForCategory("flow-health", "team");
		const aging = getWidgetsForCategory("aging-stability", "team");
		const overview = getWidgetsForCategory("overview", "team");
		expect(flow.map((w) => w.widgetKey)).toContain("wipOverview");
		expect(aging.map((w) => w.widgetKey)).toContain("wipOverview");
		expect(overview.map((w) => w.widgetKey)).toContain("wipOverview");
	});

	it("predictabilityScore appears in overview, predictability, and portfolio", () => {
		const overview = getWidgetsForCategory("overview", "team");
		const pred = getWidgetsForCategory("predictability", "team");
		const port = getWidgetsForCategory("portfolio", "team");
		expect(overview.map((w) => w.widgetKey)).toContain("predictabilityScore");
		expect(pred.map((w) => w.widgetKey)).toContain("predictabilityScore");
		expect(port.map((w) => w.widgetKey)).toContain("predictabilityScore");
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
