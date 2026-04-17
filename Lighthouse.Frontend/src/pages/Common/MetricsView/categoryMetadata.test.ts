import { describe, expect, it } from "vitest";
import {
	type CategoryDefinition,
	getCategories,
	getDefaultCategoryKey,
	getWidgetsForCategory,
} from "./categoryMetadata";

describe("categoryMetadata", () => {
	it("returns all six categories in order", () => {
		const categories = getCategories();
		expect(categories).toHaveLength(6);
		expect(categories.map((c: CategoryDefinition) => c.key)).toEqual([
			"flow-overview",
			"cycle-time",
			"throughput",
			"wip-aging",
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

	it("flow-overview contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("flow-overview", "team");
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

	it("flow-overview excludes team-only widgets for portfolio", () => {
		const widgets = getWidgetsForCategory("flow-overview", "portfolio");
		expect(widgets.map((w) => w.widgetKey)).not.toContain(
			"featuresWorkedOnOverview",
		);
	});

	it("cycle-time contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("cycle-time", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"percentiles",
			"totalWorkItemAge",
			"cycleScatter",
			"aging",
			"cycleTimePbc",
		]);
	});

	it("throughput contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("throughput", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"startedVsFinished",
			"predictabilityScore",
			"throughput",
			"stacked",
			"throughputPbc",
		]);
	});

	it("wip-aging contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("wip-aging", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"wipOverview",
			"totalWorkItemAge",
			"aging",
			"wipOverTime",
			"totalWorkItemAgeOverTime",
			"wipPbc",
			"totalWorkItemAgePbc",
		]);
	});

	it("predictability contains expected widgets in order for team", () => {
		const widgets = getWidgetsForCategory("predictability", "team");
		expect(widgets.map((w) => w.widgetKey)).toEqual([
			"cycleScatter",
			"predictabilityScoreDetails",
			"arrivals",
			"throughputPbc",
			"arrivalsPbc",
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
		const cycleTime = getWidgetsForCategory("cycle-time", "team");
		const wipAging = getWidgetsForCategory("wip-aging", "team");
		expect(cycleTime.map((w) => w.widgetKey)).toContain("aging");
		expect(wipAging.map((w) => w.widgetKey)).toContain("aging");
	});

	it("wipOverview appears in flow-overview and wip-aging", () => {
		const overview = getWidgetsForCategory("flow-overview", "team");
		const wipAging = getWidgetsForCategory("wip-aging", "team");
		expect(overview.map((w) => w.widgetKey)).toContain("wipOverview");
		expect(wipAging.map((w) => w.widgetKey)).toContain("wipOverview");
	});

	it("predictabilityScore appears in flow-overview, throughput, and portfolio", () => {
		const overview = getWidgetsForCategory("flow-overview", "team");
		const tp = getWidgetsForCategory("throughput", "team");
		const port = getWidgetsForCategory("portfolio", "team");
		expect(overview.map((w) => w.widgetKey)).toContain("predictabilityScore");
		expect(tp.map((w) => w.widgetKey)).toContain("predictabilityScore");
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

	it("category display names match the story taxonomy", () => {
		const categories = getCategories();
		expect(categories.map((c) => c.displayName)).toEqual([
			"Flow Overview",
			"Cycle Time",
			"Throughput",
			"WIP & Aging",
			"Predictability",
			"Portfolio & Features",
		]);
	});
});
