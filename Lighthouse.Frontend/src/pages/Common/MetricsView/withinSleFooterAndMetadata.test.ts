import { describe, expect, it } from "vitest";
import type { IWithinSleInfo } from "../../../models/Metrics/WithinSleInfo";
import {
	getFetchRequirementsForWidget,
	getTrendPolicy,
} from "./categoryMetadata";
import { computeWithinSleRag } from "./ragRulesTimeAndAge";
import type { RagTerms } from "./ragTypes";
import { buildWidgetFooters } from "./widgetFooters";

const terms: RagTerms = {
	workItem: "Item",
	workItems: "Items",
	feature: "Feature",
	features: "Features",
	cycleTime: "Cycle Time",
	throughput: "Throughput",
	wip: "WIP",
	workItemAge: "Work Item Age",
	blocked: "Blocked",
	sle: "Target Time",
};

const noAnswerYet: Parameters<typeof buildWidgetFooters>[0] = {
	wipCount: 0,
	systemWipLimit: undefined,
	blockedCount: 0,
	maxBlockedAgeDays: null,
	blockedStalenessThresholdDays: 0,
	hasBlockedConfig: false,
	staleCount: 0,
	hasStaleConfig: false,
	featuresInProgress: undefined,
	featureWip: undefined,
	predictabilityScore: null,
	sle: null,
	percentileValues: [],
	startedTotal: 0,
	closedTotal: 0,
	totalWorkItemAge: null,
	currentWip: 0,
	atRiskCount: 0,
	sleDays: undefined,
	throughputValues: [],
	blackoutDayIndices: [],
	cycleTimes: [],
	agingItems: [],
	wipOverTimeValues: [],
	totalAgeStart: 0,
	totalAgeEnd: 0,
	unlinkedCount: 0,
	workDistTotalCount: 0,
	distributionRate: 0,
	sizePercentileValues: [],
	featureSizes: [],
	estimationStatus: "NotConfigured",
	estimationDataPoints: [],
	throughputPbcData: null,
	wipPbcData: null,
	totalWorkItemAgePbcData: null,
	cycleTimePbcData: null,
	featureSizePbcData: null,
	arrivalsPbcData: null,
	arrivalsValues: [],
	arrivalsBlackoutDayIndices: [],
	loadBalanceBaselineAvailable: false,
	loadBalanceAverageWip: null,
	loadBalanceAverageTotalWorkItemAge: null,
	cumulativeStateTime: null,
	flowEfficiencyInfo: null,
	percentilesScopeDefinitionId: null,
	withinSleInfo: null,
	terms,
};

describe("Within SLE footer before its answer arrives", () => {
	it("stays neutral and explains the widget in the reader's own words", () => {
		expect(buildWidgetFooters(noAnswerYet).withinSle).toEqual({
			ragStatus: "none",
			tipText:
				"The share of closed Items that finished within the Target Time, next to the target.",
		});
	});
});

describe("Within SLE tip once the answer is in", () => {
	it("names the minimum when too few closed to judge", () => {
		const tooFew = {
			verdict: "TooFew",
			closedCount: 2,
			withinCount: 1,
			minimumClosed: 6,
		} as IWithinSleInfo;

		expect(computeWithinSleRag(tooFew, terms).tipText).toBe(
			"Too few closed Items to judge; at least 6 are needed.",
		);
	});

	it("states the share, the SLE days and the target when measured", () => {
		const measured = {
			verdict: "Measured",
			closedCount: 10,
			withinCount: 8,
			sharePercent: 80,
			slePercentile: 70,
			sleDays: 12,
		} as IWithinSleInfo;

		expect(computeWithinSleRag(measured, terms).tipText).toBe(
			"80% of closed Items finished within 12 days or less, against a target of 70%.",
		);
	});
});

describe("Within SLE Over Time metadata", () => {
	it("draws no trend arrow and fetches nothing through the shared metrics data", () => {
		expect(getTrendPolicy("withinSleOverTime")).toBe("none");
		expect(getFetchRequirementsForWidget("withinSleOverTime")).toEqual([]);
	});
});
