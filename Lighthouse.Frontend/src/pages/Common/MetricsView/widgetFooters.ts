import type { ICumulativeStateTimeResponse } from "../../../models/Metrics/CumulativeStateTime";
import type { IFlowEfficiencyInfo } from "../../../models/Metrics/FlowEfficiencyInfo";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import { computeBlockedMaxAgeRag } from "./blockedMaxAgeRag";
import {
	computeBlockedOverviewRag,
	computeFeaturesWorkedOnRag,
	computeFlowEfficiencyRag,
	computePredictabilityScoreRag,
	computeStaleOverviewRag,
	computeWipOverviewRag,
} from "./ragRulesOverview";
import {
	computeCumulativeStateTimeRag,
	computeCycleTimePercentilesRag,
	computeCycleTimeScatterplotRag,
	computeSleRiskRag,
	computeTotalWorkItemAgeOverTimeRag,
	computeTotalWorkItemAgeRag,
	computeWorkItemAgeChartRag,
	computeWorkItemAgePercentilesRag,
} from "./ragRulesTimeAndAge";
import {
	computeArrivalsRunChartRag,
	computeEstimationVsCycleTimeRag,
	computeFeatureSizeRag,
	computeLoadBalanceMatrixRag,
	computePbcRag,
	computeSimplifiedCfdRag,
	computeStartedVsClosedRag,
	computeThroughputRag,
	computeWipOverTimeRag,
	computeWorkDistributionRag,
} from "./ragRulesVolumeAndFlow";
import type { RagTerms } from "./ragTypes";

type RagFooter = {
	readonly ragStatus: "red" | "amber" | "green" | "none";
	readonly tipText: string;
};

const MS_PER_DAY = 1000 * 60 * 60 * 24;

/**
 * Maximum, across the blocked items, of (now - blockedSince) in whole days. Items with no
 * established blockedSince baseline are excluded. Returns null when no blocked item has a baseline.
 */
export function computeMaxBlockedAgeDays(
	blockedItems: readonly IWorkItem[],
	now: number,
): number | null {
	let max: number | null = null;
	for (const item of blockedItems) {
		if (!item.blockedSince) continue;
		const since = new Date(item.blockedSince).getTime();
		if (Number.isNaN(since)) continue;
		// biome-ignore lint/plugin/no-day-count-from-milliseconds: how long an item has been blocked is an elapsed time in whole 24-hour periods, not a count of calendar days
		const days = Math.floor((now - since) / MS_PER_DAY);
		if (max === null || days > max) {
			max = days;
		}
	}
	return max;
}

type RagInputs = {
	readonly wipCount: number;
	readonly systemWipLimit: number | undefined;
	readonly blockedCount: number;
	readonly maxBlockedAgeDays: number | null;
	readonly blockedStalenessThresholdDays: number;
	readonly hasBlockedConfig: boolean;
	readonly staleCount: number;
	readonly hasStaleConfig: boolean;
	readonly featuresInProgress: IWorkItem[] | undefined;
	readonly featureWip: number | undefined;
	readonly predictabilityScore: number | null;
	readonly sle: IPercentileValue | null;
	readonly percentileValues: IPercentileValue[];
	readonly startedTotal: number;
	readonly closedTotal: number;
	readonly totalWorkItemAge: number | null;
	readonly currentWip: number;
	/** How much of `currentWip` the risk answers put at seventy percent or worse. */
	readonly atRiskCount: number;
	readonly sleDays: number | undefined;
	readonly throughputValues: ReadonlyArray<number>;
	readonly blackoutDayIndices: ReadonlyArray<number>;
	readonly cycleTimes: ReadonlyArray<number>;
	readonly agingItems: ReadonlyArray<{
		workItemAge: number;
		isBlocked: boolean;
		isStale: boolean;
	}>;
	readonly wipOverTimeValues: ReadonlyArray<number>;
	readonly totalAgeStart: number;
	readonly totalAgeEnd: number;
	readonly unlinkedCount: number;
	readonly workDistTotalCount: number;
	readonly distributionRate: number;
	readonly sizePercentileValues: IPercentileValue[];
	readonly featureSizes: ReadonlyArray<number>;
	readonly estimationStatus: string;
	readonly estimationDataPoints: ReadonlyArray<{
		estimate: number;
		cycleTime: number;
	}>;
	readonly throughputPbcData: ProcessBehaviourChartData | null;
	readonly wipPbcData: ProcessBehaviourChartData | null;
	readonly totalWorkItemAgePbcData: ProcessBehaviourChartData | null;
	readonly cycleTimePbcData: ProcessBehaviourChartData | null;
	readonly featureSizePbcData: ProcessBehaviourChartData | null;
	readonly arrivalsPbcData: ProcessBehaviourChartData | null;
	readonly arrivalsValues: ReadonlyArray<number>;
	readonly arrivalsBlackoutDayIndices: ReadonlyArray<number>;
	readonly loadBalanceBaselineAvailable: boolean;
	readonly loadBalanceAverageWip: number | null;
	readonly loadBalanceAverageTotalWorkItemAge: number | null;
	readonly cumulativeStateTime: ICumulativeStateTimeResponse | null;
	readonly flowEfficiencyInfo: IFlowEfficiencyInfo | null;
	readonly percentilesScopeDefinitionId: number | null;
	readonly terms: RagTerms;
};

// A status is only honest once wait states are configured AND there is work in scope; in the two
// degraded states the widget shows an explanatory body instead, and emitting a footer here would
// paint a misleading colour on top of it.
function computeFlowEfficiencyFooter(
	info: IFlowEfficiencyInfo | null,
	terms: RagTerms,
): RagFooter | undefined {
	if (info === null) {
		return undefined;
	}

	// Unconfigured is a red state, mirroring the no-SLE branch of computeCycleTimePercentilesRag:
	// the user has not told us what to measure, so we ask for the setting rather than going quiet.
	// Kept here rather than inside computeFlowEfficiencyRag so that rule's bands stay untouched.
	if (!info.isConfigured) {
		return {
			ragStatus: "red",
			tipText: "Define wait states in settings to measure flow efficiency.",
		};
	}

	// No data in scope is a different problem — nothing to measure, as opposed to nothing
	// configured — so it stays colourless rather than borrowing a status it has not earned.
	if (!info.hasDataInScope) {
		return undefined;
	}

	return computeFlowEfficiencyRag(info.efficiencyPercent, terms);
}

export function buildWidgetFooters(
	inputs: RagInputs,
): Record<string, RagFooter | undefined> {
	return {
		wipOverview: computeWipOverviewRag(
			inputs.wipCount,
			inputs.systemWipLimit,
			inputs.terms,
		),
		blockedOverview: computeBlockedOverviewRag(
			inputs.blockedCount,
			inputs.hasBlockedConfig,
			inputs.terms,
		),
		sleRisk: computeSleRiskRag(
			inputs.atRiskCount,
			inputs.currentWip,
			inputs.sle,
			inputs.terms,
		),
		blockedCountHistory: computeBlockedMaxAgeRag(
			inputs.maxBlockedAgeDays,
			inputs.blockedStalenessThresholdDays,
			inputs.terms,
		),
		staleOverview: computeStaleOverviewRag(
			inputs.staleCount,
			inputs.hasStaleConfig,
			inputs.terms,
		),
		featuresWorkedOnOverview: inputs.featuresInProgress
			? computeFeaturesWorkedOnRag(
					inputs.featuresInProgress.length,
					inputs.featureWip,
					inputs.terms,
				)
			: undefined,
		predictabilityScore: computePredictabilityScoreRag(
			inputs.predictabilityScore,
			inputs.terms,
		),
		predictabilityScoreDetails: computePredictabilityScoreRag(
			inputs.predictabilityScore,
			inputs.terms,
		),
		percentiles:
			inputs.percentilesScopeDefinitionId === null
				? computeCycleTimePercentilesRag(
						inputs.sle,
						inputs.cycleTimes,
						inputs.terms,
					)
				: {
						ragStatus: "none",
						tipText: `${inputs.terms.sle} applies to the Default ${inputs.terms.cycleTime}. Named ${inputs.terms.cycleTime}s have no ${inputs.terms.sle} target.`,
					},
		totalWorkItemAge:
			inputs.totalWorkItemAge === null
				? undefined
				: computeTotalWorkItemAgeRag(
						inputs.totalWorkItemAge,
						inputs.currentWip,
						inputs.systemWipLimit,
						inputs.sleDays,
						inputs.terms,
					),
		throughput: computeThroughputRag(
			inputs.throughputValues,
			inputs.blackoutDayIndices,
			inputs.terms,
		),
		cycleScatter: computeCycleTimeScatterplotRag(
			inputs.sle,
			inputs.cycleTimes,
			inputs.terms,
		),
		// Measured over the same in-progress items the aging chart reads, so the widget and the chart
		// can never disagree about what is in flight. The judgement is made on how many items sit past
		// the SLE's day count, not on where a percentile falls — an SLE is a promise about days, and
		// counting how many items break it is the only reading a team can act on.
		workItemAgePercentiles: computeWorkItemAgePercentilesRag(
			inputs.sle,
			inputs.agingItems.map((item) => item.workItemAge),
			inputs.terms,
		),
		aging: computeWorkItemAgeChartRag(
			inputs.sle,
			inputs.hasBlockedConfig,
			inputs.agingItems,
			inputs.terms,
		),
		wipOverTime: computeWipOverTimeRag(
			inputs.wipOverTimeValues,
			inputs.systemWipLimit,
			inputs.terms,
		),
		totalWorkItemAgeOverTime: computeTotalWorkItemAgeOverTimeRag(
			inputs.totalAgeStart,
			inputs.totalAgeEnd,
			inputs.terms,
		),
		stacked: computeSimplifiedCfdRag(
			inputs.startedTotal,
			inputs.closedTotal,
			inputs.systemWipLimit,
			inputs.terms,
		),
		workDistribution: computeWorkDistributionRag(
			inputs.unlinkedCount,
			inputs.workDistTotalCount,
			inputs.featureWip,
			inputs.distributionRate,
			inputs.terms,
		),
		featureSize: computeFeatureSizeRag(
			inputs.sizePercentileValues,
			inputs.featureSizes,
			inputs.terms,
		),
		estimationVsCycleTime: computeEstimationVsCycleTimeRag(
			inputs.estimationStatus,
			inputs.estimationDataPoints,
			inputs.terms,
		),
		throughputPbc: inputs.throughputPbcData
			? computePbcRag(inputs.throughputPbcData)
			: undefined,
		wipPbc: inputs.wipPbcData ? computePbcRag(inputs.wipPbcData) : undefined,
		totalWorkItemAgePbc: inputs.totalWorkItemAgePbcData
			? computePbcRag(inputs.totalWorkItemAgePbcData)
			: undefined,
		cycleTimePbc: inputs.cycleTimePbcData
			? computePbcRag(inputs.cycleTimePbcData)
			: undefined,
		featureSizePbc: inputs.featureSizePbcData
			? computePbcRag(inputs.featureSizePbcData)
			: undefined,
		totalThroughput: computeStartedVsClosedRag(
			inputs.startedTotal,
			inputs.closedTotal,
			inputs.systemWipLimit,
			inputs.terms,
		),
		arrivals: computeArrivalsRunChartRag(
			inputs.arrivalsValues,
			inputs.arrivalsBlackoutDayIndices,
			inputs.startedTotal,
			inputs.closedTotal,
			inputs.systemWipLimit,
			inputs.terms,
		),
		totalArrivals: computeStartedVsClosedRag(
			inputs.startedTotal,
			inputs.closedTotal,
			inputs.systemWipLimit,
			inputs.terms,
		),
		arrivalsPbc: inputs.arrivalsPbcData
			? computePbcRag(inputs.arrivalsPbcData)
			: undefined,
		featureSizePercentiles: computeFeatureSizeRag(
			inputs.sizePercentileValues,
			inputs.featureSizes,
			inputs.terms,
		),
		loadBalanceMatrix: computeLoadBalanceMatrixRag(
			inputs.loadBalanceBaselineAvailable,
			inputs.currentWip,
			inputs.totalWorkItemAge ?? 0,
			inputs.loadBalanceAverageWip,
			inputs.loadBalanceAverageTotalWorkItemAge,
			inputs.terms,
		),
		stateTimeCumulative: computeCumulativeStateTimeRag(
			inputs.cumulativeStateTime?.states ?? [],
			inputs.terms,
		),
		flowEfficiency: computeFlowEfficiencyFooter(
			inputs.flowEfficiencyInfo,
			inputs.terms,
		),
	};
}
