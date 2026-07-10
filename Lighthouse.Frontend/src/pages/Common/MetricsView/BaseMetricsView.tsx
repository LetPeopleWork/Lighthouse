import { Grid } from "@mui/material";
import {
	type ReactNode,
	useCallback,
	useEffect,
	useMemo,
	useRef,
	useState,
} from "react";
import { useSearchParams } from "react-router-dom";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import BlockedItemsOverTimeChart from "../../../components/Common/Charts/BlockedItemsOverTimeChart";
import CumulativeStateTimeChart from "../../../components/Common/Charts/CumulativeStateTimeChart";
import CumulativeStateTimeItemPicker from "../../../components/Common/Charts/CumulativeStateTimeItemPicker";
import CumulativeStateTimeScopeControl from "../../../components/Common/Charts/CumulativeStateTimeScopeControl";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import EstimationVsCycleTimeChart from "../../../components/Common/Charts/EstimationVsCycleTimeChart";
import FeatureSizeScatterPlotChart from "../../../components/Common/Charts/FeatureSizeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import LoadBalanceMatrixChart from "../../../components/Common/Charts/LoadBalanceMatrixChart";
import ProcessBehaviourChart, {
	ProcessBehaviourChartType,
} from "../../../components/Common/Charts/ProcessBehaviourChart";
import StackedAreaChart from "../../../components/Common/Charts/StackedAreaChart";
import type { EvaluatorCondition } from "../../../components/Common/Charts/ThroughputChart/evaluateCondition";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import TotalWorkItemAgeRunChart from "../../../components/Common/Charts/TotalWorkItemAgeRunChart";
import TotalWorkItemAgeWidget from "../../../components/Common/Charts/TotalWorkItemAgeWidget";
import WorkDistributionChart from "../../../components/Common/Charts/WorkDistributionChart";
import WorkItemAgePercentiles from "../../../components/Common/Charts/WorkItemAgePercentiles";
import WorkItemAgingChart from "../../../components/Common/Charts/WorkItemAgingChart";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useMetricsData } from "../../../hooks/useMetricsData";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { IFeature } from "../../../models/Feature";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { ICumulativeStateTimeResponse } from "../../../models/Metrics/CumulativeStateTime";
import type { ICumulativeStateTimeCandidateRow } from "../../../models/Metrics/CumulativeStateTimeCandidates";
import type { ICumulativeStateTimeItemRow } from "../../../models/Metrics/CumulativeStateTimeItems";
import type { IEstimationVsCycleTimeResponse } from "../../../models/Metrics/EstimationVsCycleTimeData";
import type { IFeatureSizeEstimationResponse } from "../../../models/Metrics/FeatureSizeEstimationData";
import type {
	IArrivalsInfo,
	IFeatureSizePercentilesInfo,
	IThroughputInfo,
} from "../../../models/Metrics/InfoWidgetData";
import type {
	ICycleTimeDefinition,
	INamedCycleTimeDefinition,
} from "../../../models/Metrics/NamedCycleTime";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IPerStatePercentileValues } from "../../../models/PerStatePercentileValues";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useTerminology } from "../../../services/TerminologyContext";
import { deriveStaleness } from "../../../utils/staleness/deriveStaleness";
import { appColors } from "../../../utils/theme/colors";
import BlockedOverviewWidget from "./BlockedOverviewWidget";
import { computeBlockedMaxAgeRag } from "./blockedMaxAgeRag";
import { computeBlockedTrend } from "./blockedTrend";
import { getWidgetsForCategory } from "./categoryMetadata";
import type { DashboardItem } from "./Dashboard";
import Dashboard from "./Dashboard";
import DashboardHeader from "./DashboardHeader";
import FeatureSizePercentilesWidget from "./FeatureSizePercentilesWidget";
import FeaturesWorkedOnWidget from "./FeaturesWorkedOnWidget";
import FlowEfficiencyOverviewWidget from "./FlowEfficiencyOverviewWidget";
import {
	deriveLoadBalanceMatrixData,
	type LoadBalanceMatrixData,
} from "./loadBalanceMatrix";
import PredictabilityScoreDetailsWidget from "./PredictabilityScoreDetailsWidget";
import PredictabilityScoreOverviewWidget from "./PredictabilityScoreOverviewWidget";
import {
	computeArrivalsRunChartRag,
	computeBlockedOverviewRag,
	computeCumulativeStateTimeRag,
	computeCycleTimePercentilesRag,
	computeCycleTimeScatterplotRag,
	computeEstimationVsCycleTimeRag,
	computeFeatureSizeRag,
	computeFeaturesWorkedOnRag,
	computeLoadBalanceMatrixRag,
	computePbcRag,
	computePredictabilityScoreRag,
	computeSimplifiedCfdRag,
	computeStaleOverviewRag,
	computeStartedVsClosedRag,
	computeThroughputRag,
	computeTotalWorkItemAgeOverTimeRag,
	computeTotalWorkItemAgeRag,
	computeWipOverTimeRag,
	computeWipOverviewRag,
	computeWorkDistributionRag,
	computeWorkItemAgeChartRag,
	type RagTerms,
} from "./ragRules";
import StaleOverviewWidget from "./StaleOverviewWidget";
import ThroughputRunChartCard from "./ThroughputRunChartCard";
import TotalArrivalsWidget from "./TotalArrivalsWidget";
import TotalThroughputWidget from "./TotalThroughputWidget";
import type { TrendPayload } from "./trendTypes";
import { useCategorySelection } from "./useCategorySelection";
import { useShowTips } from "./useShowTips";
import type { ViewDataPayload } from "./WidgetShell";
import WidgetShell from "./WidgetShell";
import WipOverviewWidget from "./WipOverviewWidget";
import { getWidgetInfo } from "./widgetInfoMetadata";

export interface BaseMetricsViewProps<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
> {
	entity: E;
	metricsService: IMetricsService<T>;
	title: string;
	defaultDateRange?: number;
	featuresInProgress?: IWorkItem[];
	featureWip?: number;
	hasBlockedConfig?: boolean;
	doingStates: string[];
	hasForecastFilter?: boolean;
	forecastFilterConditions?: readonly EvaluatorCondition[];
	stalenessThresholdDays?: number;
	blockedStalenessThresholdDays?: number;
	waitStates?: string[];
	stateMappings?: IStateMapping[];
	cycleTimeDefinitions?: ICycleTimeDefinition[];
}

function formatDate(date: Date): string {
	return date.toISOString().split("T")[0];
}

function parseDate(dateString: string): Date | null {
	const date = new Date(dateString);
	return Number.isNaN(date.getTime()) ? null : date;
}

function getDefaultStartDate(defaultDateRange: number): Date {
	const date = new Date();
	date.setDate(date.getDate() - defaultDateRange);
	return date;
}

function extractWorkItems(
	workItemsPerUnitOfTime?: Record<string, IWorkItem[]>,
): IWorkItem[] {
	if (!workItemsPerUnitOfTime) return [];
	return Object.values(workItemsPerUnitOfTime).flat();
}

function addItemsWithoutDuplicates(
	lookup: Map<number, IWorkItem>,
	items: IWorkItem[],
): void {
	for (const item of items) {
		if (!lookup.has(item.id)) {
			lookup.set(item.id, item);
		}
	}
}

function buildWorkItemLookup(
	throughputData: RunChartData | null,
	wipOverTimeData: RunChartData | null,
	cycleTimeData: IWorkItem[],
	inProgressItems: IWorkItem[],
	allFeaturesForSizeChart: IFeature[],
): Map<number, IWorkItem> {
	const lookup = new Map<number, IWorkItem>();

	for (const item of extractWorkItems(throughputData?.workItemsPerUnitOfTime)) {
		lookup.set(item.id, item);
	}

	addItemsWithoutDuplicates(
		lookup,
		extractWorkItems(wipOverTimeData?.workItemsPerUnitOfTime),
	);
	addItemsWithoutDuplicates(lookup, cycleTimeData);
	addItemsWithoutDuplicates(lookup, inProgressItems);

	for (const item of allFeaturesForSizeChart) {
		lookup.set(item.id, item as unknown as IWorkItem);
	}

	return lookup;
}

type RagFooter = {
	readonly ragStatus: "red" | "amber" | "green" | "none";
	readonly tipText: string;
};

const MS_PER_DAY = 1000 * 60 * 60 * 24;

/**
 * Maximum, across the blocked items, of (now - blockedSince) in whole days. Items with no
 * established blockedSince baseline are excluded. Returns null when no blocked item has a baseline.
 */
function computeMaxBlockedAgeDays(
	blockedItems: readonly IWorkItem[],
	now: number,
): number | null {
	let max: number | null = null;
	for (const item of blockedItems) {
		if (!item.blockedSince) continue;
		const since = new Date(item.blockedSince).getTime();
		if (Number.isNaN(since)) continue;
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
	readonly terms: RagTerms;
};

function buildWidgetFooters(
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
		percentiles: computeCycleTimePercentilesRag(
			inputs.sle,
			inputs.cycleTimes,
			inputs.terms,
		),
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
	};
}

type ViewDataInputs = {
	readonly title: string;
	readonly inProgressItems: IWorkItem[];
	readonly blockedItems: IWorkItem[];
	readonly staleItems: IWorkItem[];
	readonly featuresInProgress: IWorkItem[] | undefined;
	readonly cycleTimeData: IWorkItem[];
	readonly throughputData: RunChartData | null;
	readonly wipOverTimeData: RunChartData | null;
	readonly allFeaturesForSizeChart: IFeature[];
	readonly serviceLevelExpectation: IPercentileValue | null;
	readonly estimationVsCycleTimeData: IEstimationVsCycleTimeResponse | null;
	readonly arrivalsData: RunChartData | null;
	readonly workItemLookup: Map<number, IWorkItem>;
	readonly stalenessThresholdDays: number | undefined;
	readonly blockedStalenessThresholdDays: number | undefined;
	readonly terms: {
		workItems: string;
		features: string;
		cycleTime: string;
		workItemAge: string;
		blocked: string;
	};
};

function buildViewData(
	inputs: ViewDataInputs,
): Record<string, ViewDataPayload | undefined> {
	const { terms } = inputs;

	const cycleTimeHighlight = {
		title: terms.cycleTime,
		description: "days",
		valueGetter: (item: IWorkItem) => item.cycleTime,
	};
	const ageHighlight = {
		title: terms.workItemAge,
		description: "days",
		valueGetter: (item: IWorkItem) => item.workItemAge,
	};
	const ageCycleHighlight = {
		title: `${terms.workItemAge}/${terms.cycleTime}`,
		description: "days",
		valueGetter: (item: IWorkItem) =>
			item.cycleTime > 0 ? item.cycleTime : item.workItemAge,
	};

	const throughputItems = extractWorkItems(
		inputs.throughputData?.workItemsPerUnitOfTime,
	);
	const wipOverTimeItems = extractWorkItems(
		inputs.wipOverTimeData?.workItemsPerUnitOfTime,
	);

	const startedVsFinishedItems = (() => {
		const items: IWorkItem[] = [];
		if (inputs.arrivalsData) {
			const startedWorkItems = extractWorkItems(
				inputs.arrivalsData.workItemsPerUnitOfTime,
			);
			const notClosedStartedItems = startedWorkItems.filter(
				(item) => item.closedDate === null,
			);
			items.push(...notClosedStartedItems);
		}
		if (inputs.throughputData) {
			items.push(...throughputItems);
		}
		return items;
	})();

	const workDistributionItems = [
		...inputs.cycleTimeData,
		...inputs.inProgressItems,
	];

	const estimationItems =
		inputs.estimationVsCycleTimeData?.dataPoints
			?.flatMap((dp) =>
				dp.workItemIds.map((id) => inputs.workItemLookup.get(id)),
			)
			.filter((item): item is IWorkItem => item !== undefined) ?? [];

	return {
		wipOverview: {
			title: `${inputs.title} in Progress`,
			items: inputs.inProgressItems,
			highlightColumn: ageHighlight,
			timeInStateColumn: {
				stalenessThresholdDays: inputs.stalenessThresholdDays,
				blockedStalenessThresholdDays: inputs.blockedStalenessThresholdDays,
			},
		},
		blockedOverview: {
			title: `${terms.blocked} ${terms.workItems}`,
			items: inputs.blockedItems,
			highlightColumn: ageHighlight,
		},
		staleOverview: {
			title: `Stale ${terms.workItems}`,
			items: inputs.staleItems,
			highlightColumn: ageHighlight,
			timeInStateColumn: {
				stalenessThresholdDays: inputs.stalenessThresholdDays,
				blockedStalenessThresholdDays: inputs.blockedStalenessThresholdDays,
			},
		},
		featuresWorkedOnOverview: inputs.featuresInProgress
			? {
					title: `${terms.features} being Worked On`,
					items: inputs.featuresInProgress,
					highlightColumn: ageHighlight,
				}
			: undefined,
		percentiles: {
			title: `Closed ${terms.workItems}`,
			items: inputs.cycleTimeData,
			highlightColumn: cycleTimeHighlight,
			sle: inputs.serviceLevelExpectation?.value,
		},
		totalWorkItemAge: {
			title: `${inputs.title} in Progress`,
			items: inputs.inProgressItems,
			highlightColumn: ageHighlight,
		},
		workItemAgePercentiles: {
			title: `${inputs.title} in Progress`,
			items: inputs.inProgressItems,
			highlightColumn: ageHighlight,
		},
		throughput: {
			title: `${inputs.title} Completed`,
			items: throughputItems,
			highlightColumn: cycleTimeHighlight,
		},
		cycleScatter: {
			title: `Closed ${terms.workItems}`,
			items: inputs.cycleTimeData,
			highlightColumn: cycleTimeHighlight,
			sle: inputs.serviceLevelExpectation?.value,
		},
		workDistribution: {
			title: `${terms.workItems} Distribution`,
			items: workDistributionItems,
			highlightColumn: ageCycleHighlight,
		},
		aging: {
			title: `${terms.workItems} in Progress`,
			items: inputs.inProgressItems,
			highlightColumn: ageHighlight,
		},
		wipOverTime: {
			title: `${inputs.title} In Progress`,
			items: wipOverTimeItems,
			highlightColumn: ageCycleHighlight,
		},
		totalWorkItemAgeOverTime: {
			title: `${inputs.title} Contributing to Total Age`,
			items: wipOverTimeItems,
			highlightColumn: ageHighlight,
		},
		stacked: {
			title: `Started and Closed ${terms.workItems}`,
			items: startedVsFinishedItems,
			highlightColumn: ageCycleHighlight,
		},
		estimationVsCycleTime: {
			title: `${terms.workItems} with Estimates`,
			items: estimationItems,
			highlightColumn: cycleTimeHighlight,
		},
		featureSize: {
			title: `${terms.features}`,
			items: inputs.allFeaturesForSizeChart as unknown as IWorkItem[],
			highlightColumn: ageCycleHighlight,
		},
		throughputPbc: {
			title: `${inputs.title} Completed`,
			items: throughputItems,
			highlightColumn: cycleTimeHighlight,
		},
		wipPbc: {
			title: `${inputs.title} In Progress`,
			items: wipOverTimeItems,
			highlightColumn: ageCycleHighlight,
		},
		totalWorkItemAgePbc: {
			title: `${inputs.title} Contributing to Total Age`,
			items: wipOverTimeItems,
			highlightColumn: ageHighlight,
		},
		cycleTimePbc: {
			title: `Closed ${terms.workItems}`,
			items: inputs.cycleTimeData,
			highlightColumn: cycleTimeHighlight,
			sle: inputs.serviceLevelExpectation?.value,
		},
		featureSizePbc: {
			title: `${terms.features}`,
			items: inputs.allFeaturesForSizeChart as unknown as IWorkItem[],
			highlightColumn: ageCycleHighlight,
		},
		arrivals: {
			title: `${inputs.title} Started`,
			items: extractWorkItems(inputs.arrivalsData?.workItemsPerUnitOfTime),
			highlightColumn: ageCycleHighlight,
		},
		arrivalsPbc: {
			title: `${inputs.title} Started`,
			items: extractWorkItems(inputs.arrivalsData?.workItemsPerUnitOfTime),
			highlightColumn: ageCycleHighlight,
		},
	};
}

function buildPbcNode(
	data: ProcessBehaviourChartData | null,
	titleSuffix: string,
	type: ProcessBehaviourChartType,
	workItemLookup: Map<number, IWorkItem>,
	filterToggle?: ReactNode,
): ReactNode | null {
	if (!data) return null;
	return (
		<ProcessBehaviourChart
			data={data}
			title={titleSuffix}
			workItemLookup={workItemLookup}
			type={type}
			filterToggle={filterToggle}
		/>
	);
}

type PbcNodesCtx = {
	throughputPbcData: ProcessBehaviourChartData | null;
	wipPbcData: ProcessBehaviourChartData | null;
	totalWorkItemAgePbcData: ProcessBehaviourChartData | null;
	cycleTimePbcData: ProcessBehaviourChartData | null;
	featureSizePbcData: ProcessBehaviourChartData | null;
	arrivalsPbcData: ProcessBehaviourChartData | null;
	workItemLookup: Map<number, IWorkItem>;
	throughputTerm: string;
	workItemAgeTerm: string;
	workInProgressTerm: string;
	featureTerm: string;
	getTerm: (key: string) => string;
	isPremium: boolean;
	hasForecastFilter: boolean;
	forecastFilterConditions: readonly EvaluatorCondition[];
	refetchThroughputPbc: (view?: "raw" | "filtered") => Promise<void>;
};

function buildPbcNodes(ctx: PbcNodesCtx): Record<string, ReactNode | null> {
	const throughputPbcFilterToggle = (
		<ThroughputChartFilterToggle
			isPremium={ctx.isPremium}
			hasFilter={ctx.hasForecastFilter}
			onChange={(filtered) => {
				void ctx.refetchThroughputPbc(filtered ? "filtered" : "raw");
			}}
		/>
	);

	const pbcConfigs = [
		{
			id: "throughputPbc",
			data: ctx.throughputPbcData,
			titleSuffix: ctx.throughputTerm,
			type: ProcessBehaviourChartType.Throughput,
			filterToggle: throughputPbcFilterToggle as ReactNode | undefined,
		},
		{
			id: "wipPbc",
			data: ctx.wipPbcData,
			titleSuffix: ctx.workInProgressTerm,
			type: ProcessBehaviourChartType.WorkInProgress,
			filterToggle: undefined,
		},
		{
			id: "totalWorkItemAgePbc",
			data: ctx.totalWorkItemAgePbcData,
			titleSuffix: `Total ${ctx.workItemAgeTerm}`,
			type: ProcessBehaviourChartType.TotalWorkItemAge,
			filterToggle: undefined,
		},
		{
			id: "cycleTimePbc",
			data: ctx.cycleTimePbcData,
			titleSuffix: ctx.getTerm(TERMINOLOGY_KEYS.CYCLE_TIME),
			type: ProcessBehaviourChartType.CycleTime,
			filterToggle: undefined,
		},
		{
			id: "featureSizePbc",
			data: ctx.featureSizePbcData,
			titleSuffix: `${ctx.featureTerm} Size`,
			type: ProcessBehaviourChartType.FeatureSize,
			filterToggle: undefined,
		},
		{
			id: "arrivalsPbc",
			data: ctx.arrivalsPbcData,
			titleSuffix: "Arrivals",
			type: ProcessBehaviourChartType.Throughput,
			filterToggle: undefined,
		},
	];

	const result: Record<string, ReactNode | null> = {};
	for (const config of pbcConfigs) {
		result[config.id] = buildPbcNode(
			config.data,
			config.titleSuffix,
			config.type,
			ctx.workItemLookup,
			config.filterToggle,
		);
	}
	return result;
}

function buildWidgetNodes(ctx: {
	entity: IFeatureOwner;
	title: string;
	ownerType: "team" | "portfolio";
	startDate: Date;
	endDate: Date;
	inProgressItems: IWorkItem[];
	blockedItems: IWorkItem[];
	staleItems: IWorkItem[];
	blockedTerm: string;
	featuresInProgress: IWorkItem[] | undefined;
	featureWip: number | undefined;
	featureTerm: string;
	featuresTerm: string;
	predictabilityData: IForecastPredictabilityScore | null;
	percentileValues: IPercentileValue[];
	workItemAgePercentilesValues: IPercentileValue[];
	perStatePercentileValues: IPerStatePercentileValues[];
	serviceLevelExpectation: IPercentileValue | null;
	cycleTimeData: IWorkItem[];
	namedCycleTimeDefinitions: INamedCycleTimeDefinition[];
	onFetchNamedCycleTimePercentiles: (
		definitionId: number,
	) => Promise<IPercentileValue[]>;
	throughputData: RunChartData | null;
	wipOverTimeData: RunChartData | null;
	allFeaturesForSizeChart: IFeature[];
	sizePercentileValues: IPercentileValue[];
	featureSizeEstimationData: IFeatureSizeEstimationResponse | null;
	estimationVsCycleTimeData: IEstimationVsCycleTimeResponse | null;
	blackoutPeriods: IBlackoutPeriod[];
	doingStates: string[];
	workItemLookup: Map<number, IWorkItem>;
	metricsService: IMetricsService<IWorkItem | IFeature>;
	throughputTerm: string;
	workItemAgeTerm: string;
	workInProgressTerm: string;
	getTerm: (key: string) => string;
	totalWorkItemAge: number | null;
	throughputPbcData: ProcessBehaviourChartData | null;
	wipPbcData: ProcessBehaviourChartData | null;
	totalWorkItemAgePbcData: ProcessBehaviourChartData | null;
	cycleTimePbcData: ProcessBehaviourChartData | null;
	featureSizePbcData: ProcessBehaviourChartData | null;
	arrivalsData: RunChartData | null;
	arrivalsPbcData: ProcessBehaviourChartData | null;
	throughputInfo: IThroughputInfo | null;
	arrivalsInfo: IArrivalsInfo | null;
	featureSizePercentilesInfo: IFeatureSizePercentilesInfo | null;
	loadBalanceData: LoadBalanceMatrixData;
	isPremium: boolean;
	hasForecastFilter: boolean;
	forecastFilterConditions: readonly EvaluatorCondition[];
	stalenessThresholdDays: number | undefined;
	blockedStalenessThresholdDays?: number;
	cumulativeStateTime: ICumulativeStateTimeResponse | null;
	displayedCumulativeStateTime: ICumulativeStateTimeResponse | null;
	cumulativeScopeDefinitionId: number | null;
	onCumulativeScopeChange: (definitionId: number | null) => void;
	cumulativeStateTimeCandidates: ICumulativeStateTimeCandidateRow[];
	cumulativeStateTimeCandidatesLoaded: boolean;
	cumulativeStateTimeSelectedItemIds: number[];
	onCumulativeStateTimeSelectionChange: (itemIds: number[]) => void;
	onCumulativeStateTimePickerOpen: () => void;
	onCumulativeStateTimeBarClick: (stateName: string) => void;
	waitStates: string[];
	stateMappings: IStateMapping[];
	refetchThroughputPbc: (view?: "raw" | "filtered") => Promise<void>;
	blockedCountHistory: BlockedCountSnapshot[] | null;
}): Record<string, ReactNode | null> {
	const nodes: Record<string, ReactNode | null> = {
		wipOverview: (
			<WipOverviewWidget
				wipCount={ctx.inProgressItems.length}
				systemWipLimit={
					ctx.entity.systemWIPLimit > 0 ? ctx.entity.systemWIPLimit : undefined
				}
				title={`${ctx.title} in Progress`}
			/>
		),
		blockedOverview: (
			<BlockedOverviewWidget
				blockedCount={ctx.blockedItems.length}
				title={ctx.blockedTerm}
			/>
		),
		staleOverview: <StaleOverviewWidget staleCount={ctx.staleItems.length} />,
		flowEfficiency: (
			<FlowEfficiencyOverviewWidget
				entityId={ctx.entity.id}
				metricsService={ctx.metricsService}
				ownerType={ctx.ownerType}
				startDate={ctx.startDate}
				endDate={ctx.endDate}
			/>
		),
		featuresWorkedOnOverview: ctx.featuresInProgress ? (
			<FeaturesWorkedOnWidget
				featureCount={ctx.featuresInProgress.length}
				featureWip={ctx.featureWip}
				title={`${ctx.featuresTerm} being Worked On`}
			/>
		) : null,
		predictabilityScore: (
			<PredictabilityScoreOverviewWidget
				score={ctx.predictabilityData?.predictabilityScore ?? null}
			/>
		),
		predictabilityScoreDetails: (
			<PredictabilityScoreDetailsWidget
				predictabilityData={ctx.predictabilityData}
				entityId={ctx.entity.id}
				metricsService={ctx.metricsService}
				startDate={ctx.startDate}
				endDate={ctx.endDate}
				isPremium={ctx.isPremium}
				hasForecastFilter={ctx.hasForecastFilter}
			/>
		),
		percentiles: (
			<CycleTimePercentiles percentileValues={ctx.percentileValues} />
		),
		workItemAgePercentiles: (
			<WorkItemAgePercentiles
				percentileValues={ctx.workItemAgePercentilesValues}
			/>
		),
		totalWorkItemAge: (
			<TotalWorkItemAgeWidget
				entityId={ctx.entity.id}
				metricsService={ctx.metricsService}
				asOfDate={ctx.endDate}
			/>
		),
		throughput: ctx.throughputData ? (
			<ThroughputRunChartCard
				entityId={ctx.entity.id}
				metricsService={ctx.metricsService}
				startDate={ctx.startDate}
				endDate={ctx.endDate}
				rawData={ctx.throughputData}
				title={`${ctx.title} Completed`}
				isPremium={ctx.isPremium}
				hasForecastFilter={ctx.hasForecastFilter}
			/>
		) : null,
		cycleScatter: (
			<CycleTimeScatterPlotChart
				cycleTimeDataPoints={ctx.cycleTimeData}
				percentileValues={ctx.percentileValues}
				serviceLevelExpectation={ctx.serviceLevelExpectation}
				blackoutPeriods={ctx.blackoutPeriods}
				namedCycleTimeDefinitions={ctx.namedCycleTimeDefinitions}
				onFetchNamedCycleTimePercentiles={ctx.onFetchNamedCycleTimePercentiles}
			/>
		),
		workDistribution: (
			<WorkDistributionChart
				workItems={
					[...ctx.cycleTimeData, ...ctx.inProgressItems] as IWorkItem[]
				}
				title="Work Distribution"
			/>
		),
		aging: (
			<WorkItemAgingChart
				inProgressItems={ctx.inProgressItems}
				percentileValues={ctx.percentileValues}
				serviceLevelExpectation={ctx.serviceLevelExpectation}
				doingStates={ctx.doingStates}
				stalenessThresholdDays={ctx.stalenessThresholdDays}
				blockedStalenessThresholdDays={ctx.blockedStalenessThresholdDays}
				perStatePercentileValues={ctx.perStatePercentileValues}
				workItemAgePercentileValues={ctx.workItemAgePercentilesValues}
			/>
		),
		loadBalanceMatrix: <LoadBalanceMatrixChart data={ctx.loadBalanceData} />,
		wipOverTime: ctx.wipOverTimeData ? (
			<LineRunChart
				title={`${ctx.title} In Progress Over Time`}
				startDate={ctx.startDate}
				chartData={ctx.wipOverTimeData}
				displayTotal={false}
				wipLimit={ctx.entity.systemWIPLimit}
			/>
		) : null,
		totalWorkItemAgeOverTime: ctx.wipOverTimeData ? (
			<TotalWorkItemAgeRunChart
				title={`${ctx.title} Total Work Item Age Over Time`}
				startDate={ctx.startDate}
				wipOverTimeData={ctx.wipOverTimeData}
			/>
		) : null,
		stacked:
			ctx.throughputData && ctx.arrivalsData ? (
				<StackedAreaChart
					title="Simplified Cumulative Flow Diagram"
					startDate={ctx.startDate}
					areas={[
						{
							index: 1,
							title: "Doing",
							area: ctx.arrivalsData,
							color: appColors.primary.light,
							startOffset: ctx.wipOverTimeData?.getValueOnDay(0) ?? 0,
						},
						{
							index: 2,
							title: "Done",
							area: ctx.throughputData,
							color: appColors.secondary.light,
						},
					]}
				/>
			) : null,
		estimationVsCycleTime:
			ctx.estimationVsCycleTimeData?.status !== "NotConfigured" &&
			ctx.estimationVsCycleTimeData ? (
				<EstimationVsCycleTimeChart
					data={ctx.estimationVsCycleTimeData}
					workItemLookup={ctx.workItemLookup}
				/>
			) : null,
		featureSize:
			ctx.allFeaturesForSizeChart.length > 0 ? (
				<FeatureSizeScatterPlotChart
					sizeDataPoints={ctx.allFeaturesForSizeChart}
					sizePercentileValues={ctx.sizePercentileValues}
					estimationData={ctx.featureSizeEstimationData ?? undefined}
				/>
			) : null,
		arrivals: ctx.arrivalsData ? (
			<BarRunChart
				title={`${ctx.title} Started`}
				startDate={ctx.startDate}
				chartData={ctx.arrivalsData}
				displayTotal={true}
			/>
		) : null,
		blockedCountHistory: (
			<BlockedItemsOverTimeChart
				snapshots={ctx.blockedCountHistory}
				metricsService={ctx.metricsService}
				ownerId={ctx.entity.id}
				title={`${ctx.blockedTerm} Over Time`}
			/>
		),
		totalThroughput: ctx.throughputInfo ? (
			<TotalThroughputWidget data={ctx.throughputInfo} />
		) : null,
		totalArrivals: ctx.arrivalsInfo ? (
			<TotalArrivalsWidget data={ctx.arrivalsInfo} />
		) : null,
		featureSizePercentiles: ctx.featureSizePercentilesInfo ? (
			<FeatureSizePercentilesWidget data={ctx.featureSizePercentilesInfo} />
		) : null,
		stateTimeCumulative: ctx.displayedCumulativeStateTime ? (
			<CumulativeStateTimeChart
				data={ctx.displayedCumulativeStateTime}
				onBarClick={ctx.onCumulativeStateTimeBarClick}
				waitStates={ctx.waitStates}
				stateMappings={ctx.stateMappings}
				completionFilterEnabled={
					ctx.cumulativeStateTimeSelectedItemIds.length === 0
				}
				scopeSlot={
					<CumulativeStateTimeScopeControl
						namedCycleTimeDefinitions={ctx.namedCycleTimeDefinitions}
						scopeDefinitionId={ctx.cumulativeScopeDefinitionId}
						onScopeChange={ctx.onCumulativeScopeChange}
					/>
				}
				pickerSlot={
					<CumulativeStateTimeItemPicker
						candidates={ctx.cumulativeStateTimeCandidates}
						candidatesLoaded={ctx.cumulativeStateTimeCandidatesLoaded}
						selectedItemIds={ctx.cumulativeStateTimeSelectedItemIds}
						onSelectionChange={ctx.onCumulativeStateTimeSelectionChange}
						onOpen={ctx.onCumulativeStateTimePickerOpen}
					/>
				}
			/>
		) : null,
	};

	Object.assign(nodes, buildPbcNodes(ctx));

	return nodes;
}

export const BaseMetricsView = <
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
>({
	entity,
	metricsService,
	title,
	defaultDateRange = 30,
	featuresInProgress,
	featureWip,
	hasBlockedConfig = false,
	doingStates,
	hasForecastFilter = false,
	forecastFilterConditions = [],
	stalenessThresholdDays,
	blockedStalenessThresholdDays,
	waitStates = [],
	stateMappings = [],
	cycleTimeDefinitions = [],
}: BaseMetricsViewProps<T, E>) => {
	const [searchParams, setSearchParams] = useSearchParams();
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? false;

	const [startDate, setStartDate] = useState<Date>(() => {
		const parsed = parseDate(searchParams.get("startDate") ?? "");
		return parsed ?? getDefaultStartDate(defaultDateRange);
	});

	const [endDate, setEndDate] = useState<Date>(() => {
		const parsed = parseDate(searchParams.get("endDate") ?? "");
		return parsed ?? new Date();
	});

	const updateDateParams = (start: Date, end: Date) => {
		const newParams = new URLSearchParams(searchParams);
		newParams.set("startDate", formatDate(start));
		newParams.set("endDate", formatDate(end));
		setSearchParams(newParams, { replace: true });
	};

	const handleStartDateChange = (date: Date | null) => {
		if (!date) return;
		setStartDate(date);
		updateDateParams(date, endDate);
	};

	const handleEndDateChange = (date: Date | null) => {
		if (!date) return;
		setEndDate(date);
		updateDateParams(startDate, date);
	};

	const {
		blackoutPeriods,
		throughputData,
		wipOverTimeData,
		inProgressItems,
		cycleTimeData,
		percentileValues,
		workItemAgePercentilesValues,
		perStatePercentileValues,
		sizePercentileValues,
		allFeaturesForSizeChart,
		predictabilityData,
		throughputPbcData,
		wipPbcData,
		totalWorkItemAgePbcData,
		cycleTimePbcData,
		featureSizePbcData,
		estimationVsCycleTimeData,
		featureSizeEstimationData,
		serviceLevelExpectation,
		totalWorkItemAge,
		arrivalsData,
		arrivalsPbcData,
		throughputInfo,
		arrivalsInfo,
		featureSizePercentilesInfo,
		wipOverviewInfo,
		featuresWorkedOnInfo,
		totalWorkItemAgeInfo,
		predictabilityScoreInfo,
		cycleTimePercentilesInfo,
		cumulativeStateTime,
		blockedCountHistory,
		refetchThroughputPbc,
	} = useMetricsData(entity, metricsService, startDate, endDate);

	const namedCycleTimeDefinitions: INamedCycleTimeDefinition[] = useMemo(
		() =>
			isPremium
				? cycleTimeDefinitions.map((definition) => ({
						id: definition.id,
						name: definition.name,
						isValid: definition.isValid,
					}))
				: [],
		[isPremium, cycleTimeDefinitions],
	);

	const onFetchNamedCycleTimePercentiles = useCallback(
		(definitionId: number) =>
			metricsService.getCycleTimePercentiles(
				entity.id,
				startDate,
				endDate,
				definitionId,
			),
		[metricsService, entity.id, startDate, endDate],
	);

	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const workInProgressTerm = getTerm(TERMINOLOGY_KEYS.WORK_IN_PROGRESS);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const ownerType: "team" | "portfolio" =
		"getFeaturesInProgress" in metricsService ? "team" : "portfolio";

	const [drillDownState, setDrillDownState] = useState<string | null>(null);
	const [drillDownItems, setDrillDownItems] = useState<
		ICumulativeStateTimeItemRow[]
	>([]);
	const [drillDownOpen, setDrillDownOpen] = useState(false);

	const [selectedItemIds, setSelectedItemIds] = useState<number[]>([]);
	const [cumulativeCandidates, setCumulativeCandidates] = useState<
		ICumulativeStateTimeCandidateRow[]
	>([]);
	const [cumulativeCandidatesLoaded, setCumulativeCandidatesLoaded] =
		useState(false);
	const [narrowedCumulativeStateTime, setNarrowedCumulativeStateTime] =
		useState<ICumulativeStateTimeResponse | null>(null);
	const [cumulativeScopeDefinitionId, setCumulativeScopeDefinitionId] =
		useState<number | null>(null);
	const [scopedCumulativeStateTime, setScopedCumulativeStateTime] =
		useState<ICumulativeStateTimeResponse | null>(null);
	const candidatesRequestedRef = useRef(false);

	useEffect(() => {
		candidatesRequestedRef.current = false;
		setSelectedItemIds([]);
		setNarrowedCumulativeStateTime(null);
		setCumulativeCandidates([]);
		setCumulativeCandidatesLoaded(false);
	}, []);

	const fetchCumulativeStateTimeForScope = useCallback(
		(itemIds?: number[]) =>
			metricsService.getCumulativeStateTimeForTeam(
				entity.id,
				startDate,
				endDate,
				itemIds,
			),
		[metricsService, entity.id, startDate, endDate],
	);

	const handleCumulativeScopeChange = useCallback(
		(definitionId: number | null) => {
			setCumulativeScopeDefinitionId(definitionId);
			if (definitionId === null) {
				setScopedCumulativeStateTime(null);
				return;
			}
			void metricsService
				.getCumulativeStateTimeForTeam(
					entity.id,
					startDate,
					endDate,
					undefined,
					definitionId,
				)
				.then(setScopedCumulativeStateTime);
		},
		[metricsService, entity.id, startDate, endDate],
	);

	// biome-ignore lint/correctness/useExhaustiveDependencies: entity/window are reset triggers, not values read in the body — a scope chosen for one team/window must not leak into the next.
	useEffect(() => {
		setCumulativeScopeDefinitionId(null);
		setScopedCumulativeStateTime(null);
	}, [entity.id, startDate, endDate]);

	const handleCumulativeStateTimePickerOpen = useCallback(() => {
		if (candidatesRequestedRef.current) {
			return;
		}
		candidatesRequestedRef.current = true;
		const request =
			ownerType === "team"
				? metricsService.getCumulativeStateTimeCandidatesForTeam(
						entity.id,
						startDate,
						endDate,
					)
				: metricsService.getCumulativeStateTimeCandidatesForPortfolio(
						entity.id,
						startDate,
						endDate,
					);
		void request.then((response) => {
			setCumulativeCandidates(response.items);
			setCumulativeCandidatesLoaded(true);
		});
	}, [ownerType, metricsService, entity.id, startDate, endDate]);

	const handleCumulativeStateTimeSelectionChange = useCallback(
		(itemIds: number[]) => {
			setSelectedItemIds(itemIds);
			if (itemIds.length === 0) {
				setNarrowedCumulativeStateTime(null);
				return;
			}
			void fetchCumulativeStateTimeForScope(itemIds).then(
				setNarrowedCumulativeStateTime,
			);
		},
		[fetchCumulativeStateTimeForScope],
	);

	const handleCumulativeStateTimeBarClick = async (stateName: string) => {
		const activeItemIds =
			selectedItemIds.length > 0 ? selectedItemIds : undefined;
		const response =
			ownerType === "team"
				? await metricsService.getCumulativeStateTimeItemsForTeam(
						entity.id,
						stateName,
						startDate,
						endDate,
						activeItemIds,
					)
				: await metricsService.getCumulativeStateTimeItemsForPortfolio(
						entity.id,
						stateName,
						startDate,
						endDate,
						activeItemIds,
					);
		setDrillDownState(stateName);
		setDrillDownItems(response.items);
		setDrillDownOpen(true);
	};

	const drillDownWorkItems = useMemo<IWorkItem[]>(
		() =>
			drillDownItems.map((item) => ({
				id: item.workItemId,
				referenceId: item.referenceId,
				name: item.title,
				type: item.type,
				state: item.state,
				stateCategory: item.stateCategory,
				url: item.url,
				startedDate: new Date(0),
				closedDate: new Date(0),
				cycleTime: 0,
				workItemAge: 0,
				parentWorkItemReference: "",
				isBlocked: false,
			})),
		[drillDownItems],
	);

	const drillDownDaysById = useMemo(
		() =>
			new Map(
				drillDownItems.map((item) => [
					item.workItemId,
					Math.round(item.daysContributed * 10) / 10,
				]),
			),
		[drillDownItems],
	);

	const { selectedCategory, setSelectedCategory } = useCategorySelection(
		ownerType,
		entity.id,
	);
	const { showTips, toggleShowTips } = useShowTips(ownerType, entity.id);

	const workItemLookup = useMemo(
		() =>
			buildWorkItemLookup(
				throughputData,
				wipOverTimeData,
				cycleTimeData as unknown as IWorkItem[],
				inProgressItems,
				allFeaturesForSizeChart,
			),
		[
			throughputData,
			wipOverTimeData,
			cycleTimeData,
			inProgressItems,
			allFeaturesForSizeChart,
		],
	);

	const ragTerms: RagTerms = {
		workItem: getTerm(TERMINOLOGY_KEYS.WORK_ITEM),
		workItems: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
		feature: getTerm(TERMINOLOGY_KEYS.FEATURE),
		features: getTerm(TERMINOLOGY_KEYS.FEATURES),
		cycleTime: getTerm(TERMINOLOGY_KEYS.CYCLE_TIME),
		throughput: getTerm(TERMINOLOGY_KEYS.THROUGHPUT),
		wip: getTerm(TERMINOLOGY_KEYS.WIP),
		workItemAge: getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE),
		blocked: getTerm(TERMINOLOGY_KEYS.BLOCKED),
		sle: getTerm(TERMINOLOGY_KEYS.SLE),
	};

	const blockedItems = inProgressItems.filter((item) => item.isBlocked);
	const staleItems = inProgressItems.filter(
		(item) =>
			deriveStaleness(
				item,
				stalenessThresholdDays,
				blockedStalenessThresholdDays,
			).isStale,
	);
	const hasStaleConfig =
		(stalenessThresholdDays ?? 0) > 0 ||
		(blockedStalenessThresholdDays ?? 0) > 0;

	const loadBalanceData = useMemo(
		() =>
			deriveLoadBalanceMatrixData({
				endDate,
				currentWip: inProgressItems.length,
				currentTotalWorkItemAge: totalWorkItemAge,
				wipPbcData,
				totalWorkItemAgePbcData,
			}),
		[
			endDate,
			inProgressItems.length,
			totalWorkItemAge,
			wipPbcData,
			totalWorkItemAgePbcData,
		],
	);

	const widgetNodes = buildWidgetNodes({
		entity,
		title,
		ownerType,
		startDate,
		endDate,
		inProgressItems,
		blockedItems,
		staleItems,
		blockedTerm,
		featuresInProgress,
		featureWip,
		featureTerm,
		featuresTerm,
		predictabilityData,
		percentileValues,
		workItemAgePercentilesValues,
		perStatePercentileValues,
		serviceLevelExpectation,
		cycleTimeData: cycleTimeData as unknown as IWorkItem[],
		namedCycleTimeDefinitions,
		onFetchNamedCycleTimePercentiles,
		throughputData,
		wipOverTimeData,
		allFeaturesForSizeChart,
		sizePercentileValues,
		featureSizeEstimationData,
		estimationVsCycleTimeData,
		blackoutPeriods,
		doingStates,
		workItemLookup,
		metricsService,
		throughputTerm,
		workItemAgeTerm,
		workInProgressTerm,
		getTerm,
		totalWorkItemAge,
		throughputPbcData,
		wipPbcData,
		totalWorkItemAgePbcData,
		cycleTimePbcData,
		featureSizePbcData,
		arrivalsData,
		arrivalsPbcData,
		throughputInfo,
		arrivalsInfo,
		featureSizePercentilesInfo,
		loadBalanceData,
		isPremium,
		hasForecastFilter,
		forecastFilterConditions,
		stalenessThresholdDays,
		blockedStalenessThresholdDays,
		cumulativeStateTime,
		displayedCumulativeStateTime:
			scopedCumulativeStateTime ??
			narrowedCumulativeStateTime ??
			cumulativeStateTime,
		cumulativeScopeDefinitionId,
		onCumulativeScopeChange: handleCumulativeScopeChange,
		cumulativeStateTimeCandidates: cumulativeCandidates,
		cumulativeStateTimeCandidatesLoaded: cumulativeCandidatesLoaded,
		cumulativeStateTimeSelectedItemIds: selectedItemIds,
		onCumulativeStateTimeSelectionChange:
			handleCumulativeStateTimeSelectionChange,
		onCumulativeStateTimePickerOpen: handleCumulativeStateTimePickerOpen,
		onCumulativeStateTimeBarClick: (stateName) => {
			void handleCumulativeStateTimeBarClick(stateName);
		},
		waitStates,
		stateMappings,
		refetchThroughputPbc,
		blockedCountHistory,
	});

	const widgetFooters = buildWidgetFooters({
		wipCount: inProgressItems.length,
		systemWipLimit:
			entity.systemWIPLimit > 0 ? entity.systemWIPLimit : undefined,
		blockedCount: blockedItems.length,
		maxBlockedAgeDays: computeMaxBlockedAgeDays(blockedItems, Date.now()),
		blockedStalenessThresholdDays: blockedStalenessThresholdDays ?? 0,
		hasBlockedConfig,
		staleCount: staleItems.length,
		hasStaleConfig,
		terms: ragTerms,
		featuresInProgress,
		featureWip,
		predictabilityScore: predictabilityData?.predictabilityScore ?? null,
		sle: serviceLevelExpectation,
		percentileValues,
		startedTotal: arrivalsData?.total ?? 0,
		closedTotal: throughputData?.total ?? 0,
		totalWorkItemAge,
		currentWip: inProgressItems.length,
		sleDays:
			entity.serviceLevelExpectationRange > 0
				? entity.serviceLevelExpectationRange
				: undefined,
		throughputValues: throughputData
			? Array.from({ length: throughputData.history }, (_, i) =>
					throughputData.getValueOnDay(i),
				)
			: [],
		blackoutDayIndices: throughputData?.blackoutDayIndices ?? [],
		cycleTimes: (cycleTimeData as unknown as IWorkItem[]).map(
			(item) => item.cycleTime,
		),
		featureSizes: (allFeaturesForSizeChart as unknown as IFeature[])
			.filter(
				(item) =>
					item.size !== undefined &&
					(item.stateCategory === "ToDo" || item.stateCategory === "Doing"),
			)
			.map((item) => item.size),
		agingItems: inProgressItems.map((item) => ({
			workItemAge: item.workItemAge,
			isBlocked: item.isBlocked,
			isStale: deriveStaleness(
				item,
				stalenessThresholdDays,
				blockedStalenessThresholdDays,
			).isStale,
		})),
		wipOverTimeValues: wipOverTimeData
			? Array.from({ length: wipOverTimeData.history }, (_, i) =>
					wipOverTimeData.getValueOnDay(i),
				)
			: [],
		totalAgeStart: totalWorkItemAgePbcData?.dataPoints[0]?.yValue ?? 0,
		totalAgeEnd:
			totalWorkItemAgePbcData?.dataPoints[
				totalWorkItemAgePbcData.dataPoints.length - 1
			]?.yValue ?? 0,
		unlinkedCount: [
			...(cycleTimeData as unknown as IWorkItem[]),
			...inProgressItems,
		].filter((item) => !item.parentWorkItemReference).length,
		workDistTotalCount:
			(cycleTimeData as unknown as IWorkItem[]).length + inProgressItems.length,
		distributionRate: new Set(
			[...(cycleTimeData as unknown as IWorkItem[]), ...inProgressItems]
				.map((item) => item.parentWorkItemReference)
				.filter(Boolean),
		).size,
		sizePercentileValues,
		estimationStatus: estimationVsCycleTimeData?.status ?? "NotConfigured",
		estimationDataPoints: (estimationVsCycleTimeData?.dataPoints ?? []).map(
			(dp) => ({
				estimate: dp.estimationNumericValue,
				cycleTime: dp.cycleTime,
			}),
		),
		throughputPbcData,
		wipPbcData,
		totalWorkItemAgePbcData,
		cycleTimePbcData,
		featureSizePbcData,
		arrivalsPbcData,
		arrivalsValues: arrivalsData
			? Array.from({ length: arrivalsData.history }, (_, i) =>
					arrivalsData.getValueOnDay(i),
				)
			: [],
		arrivalsBlackoutDayIndices: arrivalsData?.blackoutDayIndices ?? [],
		loadBalanceBaselineAvailable: loadBalanceData.baselineAvailable,
		loadBalanceAverageWip: loadBalanceData.averageWip,
		loadBalanceAverageTotalWorkItemAge: loadBalanceData.averageTotalWorkItemAge,
		cumulativeStateTime,
	});

	const widgetViewData = buildViewData({
		title,
		inProgressItems,
		blockedItems,
		staleItems,
		featuresInProgress,
		cycleTimeData: cycleTimeData as unknown as IWorkItem[],
		throughputData,
		wipOverTimeData,
		allFeaturesForSizeChart,
		serviceLevelExpectation,
		estimationVsCycleTimeData,
		arrivalsData,
		workItemLookup,
		stalenessThresholdDays,
		blockedStalenessThresholdDays,
		terms: {
			workItems: ragTerms.workItems,
			features: ragTerms.features,
			cycleTime: ragTerms.cycleTime,
			workItemAge: ragTerms.workItemAge,
			blocked: ragTerms.blocked,
		},
	});

	const widgetTrends: Record<string, TrendPayload | undefined> = {
		totalThroughput: throughputInfo
			? TotalThroughputWidget.getTrendPayload(throughputInfo).trendPayload
			: undefined,
		totalArrivals: arrivalsInfo
			? TotalArrivalsWidget.getTrendPayload(arrivalsInfo).trendPayload
			: undefined,
		featureSizePercentiles: featureSizePercentilesInfo
			? FeatureSizePercentilesWidget.getTrendPayload(featureSizePercentilesInfo)
					.trendPayload
			: undefined,
		wipOverview: wipOverviewInfo?.comparison,
		blockedOverview: computeBlockedTrend(
			blockedCountHistory,
			startDate,
			endDate,
		),
		featuresWorkedOnOverview: featuresWorkedOnInfo?.comparison,
		totalWorkItemAge: totalWorkItemAgeInfo?.comparison,
		predictabilityScore: predictabilityScoreInfo?.comparison,
		percentiles: cycleTimePercentilesInfo?.comparison,
	};

	const activeWidgets = getWidgetsForCategory(selectedCategory, ownerType);
	const dashboardItems: DashboardItem[] = activeWidgets
		.filter((w) => widgetNodes[w.widgetKey] != null)
		.map((w) => ({
			id: w.widgetKey,
			size: w.size,
			node: (
				<WidgetShell
					widgetKey={w.widgetKey}
					showTips={showTips}
					header={widgetFooters[w.widgetKey]}
					info={getWidgetInfo(w.widgetKey)}
					viewData={widgetViewData[w.widgetKey]}
					trend={widgetTrends[w.widgetKey]}
				>
					{widgetNodes[w.widgetKey]}
				</WidgetShell>
			),
		}));

	return (
		<Grid container spacing={2}>
			<DashboardHeader
				startDate={startDate}
				endDate={endDate}
				onStartDateChange={handleStartDateChange}
				onEndDateChange={handleEndDateChange}
				selectedCategory={selectedCategory}
				onSelectCategory={setSelectedCategory}
				showTips={showTips}
				onToggleTips={toggleShowTips}
			/>

			<Dashboard items={dashboardItems} />

			<WorkItemsDialog
				open={drillDownOpen}
				title={`${getTerm(TERMINOLOGY_KEYS.WORK_ITEMS)} contributing to ${drillDownState ?? ""}`}
				items={drillDownWorkItems}
				highlightColumn={{
					title: "Days Contributed",
					description: "days",
					valueGetter: (workItem) => drillDownDaysById.get(workItem.id) ?? 0,
				}}
				onClose={() => setDrillDownOpen(false)}
			/>
		</Grid>
	);
};
