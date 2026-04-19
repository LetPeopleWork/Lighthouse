import { describe, expect, it } from "vitest";
import {
	getWidgetInfo,
	type WidgetInfoEntry,
	widgetInfoMetadata,
} from "./widgetInfoMetadata";

const DOCS_BASE = "https://docs.lighthouse.letpeople.work/metrics/widgets.html";

describe("widgetInfoMetadata", () => {
	it("provides an entry for every widget that buildWidgetNodes produces", () => {
		const expectedWidgetKeys = [
			"wipOverview",
			"blockedOverview",
			"featuresWorkedOnOverview",
			"predictabilityScore",
			"predictabilityScoreDetails",
			"percentiles",
			"totalWorkItemAge",
			"throughput",
			"cycleScatter",
			"workDistribution",
			"aging",
			"wipOverTime",
			"totalWorkItemAgeOverTime",
			"stacked",
			"estimationVsCycleTime",
			"featureSize",
			"throughputPbc",
			"wipPbc",
			"totalWorkItemAgePbc",
			"cycleTimePbc",
			"featureSizePbc",
			"arrivals",
			"arrivalsPbc",
			"totalThroughput",
			"totalArrivals",
			"featureSizePercentiles",
		];

		for (const key of expectedWidgetKeys) {
			expect(widgetInfoMetadata[key]).toBeDefined();
		}
	});

	it("every entry has a non-empty description and a valid learnMoreUrl", () => {
		for (const [, entry] of Object.entries(widgetInfoMetadata) as [
			string,
			WidgetInfoEntry,
		][]) {
			expect(entry.description.length).toBeGreaterThan(0);
			expect(entry.learnMoreUrl).toContain(DOCS_BASE);
			expect(entry.learnMoreUrl).toMatch(/#.+/);
			expect(entry.statusGuidance.sustain.length).toBeGreaterThan(0);
			expect(entry.statusGuidance.observe.length).toBeGreaterThan(0);
			expect(entry.statusGuidance.act.length).toBeGreaterThan(0);
		}
	});

	it("getWidgetInfo returns the entry for a known widget key", () => {
		const info = getWidgetInfo("throughput");
		expect(info).toBeDefined();
		expect(info?.description).toBeTruthy();
		expect(info?.learnMoreUrl).toContain(DOCS_BASE);
	});

	it("getWidgetInfo returns undefined for an unknown widget key", () => {
		expect(getWidgetInfo("nonexistent")).toBeUndefined();
	});

	it("no two widgets share the same description", () => {
		const descriptions = Object.values(widgetInfoMetadata).map(
			(e: WidgetInfoEntry) => e.description,
		);
		const unique = new Set(descriptions);
		expect(unique.size).toBe(descriptions.length);
	});
});
