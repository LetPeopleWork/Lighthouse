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
			"staleOverview",
			"featuresWorkedOnOverview",
			"predictabilityScore",
			"predictabilityScoreDetails",
			"percentiles",
			"workItemAgePercentiles",
			"totalWorkItemAge",
			"throughput",
			"cycleScatter",
			"workDistribution",
			"aging",
			"loadBalanceMatrix",
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
			// statusGuidance is optional: widgets without a RAG status indicator
			// (e.g. Work Item Age Percentiles) omit it. When present it must be complete.
			if (entry.statusGuidance) {
				expect(entry.statusGuidance.sustain.length).toBeGreaterThan(0);
				expect(entry.statusGuidance.observe.length).toBeGreaterThan(0);
				expect(entry.statusGuidance.act.length).toBeGreaterThan(0);
			}
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

	it("stateTimeCumulative has a description, a cumulative-time-per-state learn-more URL, and sustain/observe/act guidance", () => {
		const info = getWidgetInfo("stateTimeCumulative");
		expect(info).toBeDefined();
		expect(info?.description.length).toBeGreaterThan(0);
		expect(info?.learnMoreUrl).toBe(`${DOCS_BASE}#cumulative-time-per-state`);
		expect(info?.statusGuidance?.sustain).toBe(
			"No single state holds 40% or more of the total time.",
		);
		expect(info?.statusGuidance?.observe).toBe(
			"One state holds between 40% and 60% of the total time.",
		);
		expect(info?.statusGuidance?.act).toBe(
			"One state holds more than 60% of the total time, or no time is in scope. Investigate the bottleneck or widen the filter.",
		);
	});

	it("no two widgets share the same description", () => {
		const descriptions = Object.values(widgetInfoMetadata).map(
			(e: WidgetInfoEntry) => e.description,
		);
		const unique = new Set(descriptions);
		expect(unique.size).toBe(descriptions.length);
	});
});
