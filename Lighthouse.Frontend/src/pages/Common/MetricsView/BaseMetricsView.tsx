import { Grid } from "@mui/material";
import { type ReactNode, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import CycleTimePercentiles from "../../../components/Common/Charts/CycleTimePercentiles";
import CycleTimeScatterPlotChart from "../../../components/Common/Charts/CycleTimeScatterPlotChart";
import EstimationVsCycleTimeChart from "../../../components/Common/Charts/EstimationVsCycleTimeChart";
import FeatureSizeScatterPlotChart from "../../../components/Common/Charts/FeatureSizeScatterPlotChart";
import LineRunChart from "../../../components/Common/Charts/LineRunChart";
import ProcessBehaviourChart, {
	ProcessBehaviourChartType,
} from "../../../components/Common/Charts/ProcessBehaviourChart";
import StackedAreaChart from "../../../components/Common/Charts/StackedAreaChart";
import StartedVsFinishedDisplay from "../../../components/Common/Charts/StartedVsFinishedDisplay";
import TotalWorkItemAgeRunChart from "../../../components/Common/Charts/TotalWorkItemAgeRunChart";
import TotalWorkItemAgeWidget from "../../../components/Common/Charts/TotalWorkItemAgeWidget";
import WorkDistributionChart from "../../../components/Common/Charts/WorkDistributionChart";
import WorkItemAgingChart from "../../../components/Common/Charts/WorkItemAgingChart";
import { useMetricsData } from "../../../hooks/useMetricsData";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import type { IFeature } from "../../../models/Feature";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IEstimationVsCycleTimeResponse } from "../../../models/Metrics/EstimationVsCycleTimeData";
import type { IFeatureSizeEstimationResponse } from "../../../models/Metrics/FeatureSizeEstimationData";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useTerminology } from "../../../services/TerminologyContext";
import { appColors } from "../../../utils/theme/colors";
import BlockedOverviewWidget from "./BlockedOverviewWidget";
import { getWidgetsForCategory } from "./categoryMetadata";
import type { DashboardItem } from "./Dashboard";
import Dashboard from "./Dashboard";
import DashboardHeader from "./DashboardHeader";
import FeaturesWorkedOnWidget from "./FeaturesWorkedOnWidget";
import PredictabilityScoreDetailsWidget from "./PredictabilityScoreDetailsWidget";
import PredictabilityScoreOverviewWidget from "./PredictabilityScoreOverviewWidget";
import {
	computeBlockedOverviewRag,
	computeCycleTimePercentilesRag,
	computeCycleTimeScatterplotRag,
	computeEstimationVsCycleTimeRag,
	computeFeatureSizeRag,
	computeFeaturesWorkedOnRag,
	computePbcRag,
	computePredictabilityScoreRag,
	computeSimplifiedCfdRag,
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
	readonly ragStatus: "red" | "amber" | "green";
	readonly tipText: string;
};

type RagInputs = {
	readonly wipCount: number;
	readonly systemWipLimit: number | undefined;
	readonly blockedCount: number;
	readonly hasBlockedConfig: boolean;
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
	}>;
	readonly wipOverTimeValues: ReadonlyArray<number>;
	readonly totalAgeStart: number;
	readonly totalAgeEnd: number;
	readonly unlinkedCount: number;
	readonly workDistTotalCount: number;
	readonly distributionRate: number;
	readonly featureSizeTarget: IPercentileValue | null;
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
		startedVsFinished: computeStartedVsClosedRag(
			inputs.startedTotal,
			inputs.closedTotal,
			inputs.systemWipLimit,
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
			inputs.featureSizeTarget,
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
	};
}

type ViewDataInputs = {
	readonly title: string;
	readonly inProgressItems: IWorkItem[];
	readonly blockedItems: IWorkItem[];
	readonly featuresInProgress: IWorkItem[] | undefined;
	readonly cycleTimeData: IWorkItem[];
	readonly startedItems: RunChartData | null;
	readonly throughputData: RunChartData | null;
	readonly wipOverTimeData: RunChartData | null;
	readonly allFeaturesForSizeChart: IFeature[];
	readonly serviceLevelExpectation: IPercentileValue | null;
	readonly estimationVsCycleTimeData: IEstimationVsCycleTimeResponse | null;
	readonly workItemLookup: Map<number, IWorkItem>;
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
		if (inputs.startedItems) {
			const startedWorkItems = extractWorkItems(
				inputs.startedItems.workItemsPerUnitOfTime,
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
		},
		blockedOverview: {
			title: `${terms.blocked} ${terms.workItems}`,
			items: inputs.blockedItems,
			highlightColumn: ageHighlight,
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
		startedVsFinished: {
			title: `Started and Closed ${terms.workItems}`,
			items: startedVsFinishedItems,
			highlightColumn: ageCycleHighlight,
		},
		totalWorkItemAge: {
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
	};
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- generic helper avoids coupling to specific entity/metrics type
function buildWidgetNodes(ctx: {
	entity: IFeatureOwner;
	title: string;
	startDate: Date;
	inProgressItems: IWorkItem[];
	blockedItems: IWorkItem[];
	blockedTerm: string;
	featuresInProgress: IWorkItem[] | undefined;
	featureWip: number | undefined;
	featureTerm: string;
	predictabilityData: IForecastPredictabilityScore | null;
	percentileValues: IPercentileValue[];
	serviceLevelExpectation: IPercentileValue | null;
	cycleTimeData: IWorkItem[];
	startedItems: RunChartData | null;
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
		featuresWorkedOnOverview: ctx.featuresInProgress ? (
			<FeaturesWorkedOnWidget
				featureCount={ctx.featuresInProgress.length}
				featureWip={ctx.featureWip}
				title={`${ctx.featureTerm} being Worked On`}
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
			/>
		),
		percentiles: (
			<CycleTimePercentiles percentileValues={ctx.percentileValues} />
		),
		startedVsFinished: (
			<StartedVsFinishedDisplay
				startedItems={ctx.startedItems}
				closedItems={ctx.throughputData}
			/>
		),
		totalWorkItemAge: (
			<TotalWorkItemAgeWidget
				entityId={ctx.entity.id}
				metricsService={ctx.metricsService}
			/>
		),
		throughput: ctx.throughputData ? (
			<BarRunChart
				title={`${ctx.title} Completed`}
				startDate={ctx.startDate}
				chartData={ctx.throughputData}
				displayTotal={true}
			/>
		) : null,
		cycleScatter: (
			<CycleTimeScatterPlotChart
				cycleTimeDataPoints={ctx.cycleTimeData}
				percentileValues={ctx.percentileValues}
				serviceLevelExpectation={ctx.serviceLevelExpectation}
				blackoutPeriods={ctx.blackoutPeriods}
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
			/>
		),
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
			ctx.throughputData && ctx.startedItems ? (
				<StackedAreaChart
					title="Simplified Cumulative Flow Diagram"
					startDate={ctx.startDate}
					areas={[
						{
							index: 1,
							title: "Doing",
							area: ctx.startedItems,
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
	};

	const pbcConfigs = [
		{
			id: "throughputPbc",
			data: ctx.throughputPbcData,
			titleSuffix: ctx.throughputTerm,
			type: ProcessBehaviourChartType.Throughput,
		},
		{
			id: "wipPbc",
			data: ctx.wipPbcData,
			titleSuffix: ctx.workInProgressTerm,
			type: ProcessBehaviourChartType.WorkInProgress,
		},
		{
			id: "totalWorkItemAgePbc",
			data: ctx.totalWorkItemAgePbcData,
			titleSuffix: `Total ${ctx.workItemAgeTerm}`,
			type: ProcessBehaviourChartType.TotalWorkItemAge,
		},
		{
			id: "cycleTimePbc",
			data: ctx.cycleTimePbcData,
			titleSuffix: ctx.getTerm(TERMINOLOGY_KEYS.CYCLE_TIME),
			type: ProcessBehaviourChartType.CycleTime,
		},
		{
			id: "featureSizePbc",
			data: ctx.featureSizePbcData,
			titleSuffix: `${ctx.featureTerm} Size`,
			type: ProcessBehaviourChartType.FeatureSize,
		},
	];

	for (const config of pbcConfigs) {
		if (config.data) {
			nodes[config.id] = (
				<ProcessBehaviourChart
					data={config.data}
					title={config.titleSuffix}
					workItemLookup={ctx.workItemLookup}
					type={config.type}
				/>
			);
		}
	}

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
}: BaseMetricsViewProps<T, E>) => {
	const [searchParams, setSearchParams] = useSearchParams();

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
		sizePercentileValues,
		allFeaturesForSizeChart,
		startedItems,
		predictabilityData,
		throughputPbcData,
		wipPbcData,
		totalWorkItemAgePbcData,
		cycleTimePbcData,
		featureSizePbcData,
		estimationVsCycleTimeData,
		featureSizeEstimationData,
		serviceLevelExpectation,
		featureSizeTarget,
		totalWorkItemAge,
	} = useMetricsData(entity, metricsService, startDate, endDate);

	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const workInProgressTerm = getTerm(TERMINOLOGY_KEYS.WORK_IN_PROGRESS);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const ownerType: "team" | "portfolio" =
		"getFeaturesInProgress" in metricsService ? "team" : "portfolio";
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

	const widgetNodes = buildWidgetNodes({
		entity,
		title,
		startDate,
		inProgressItems,
		blockedItems,
		blockedTerm,
		featuresInProgress,
		featureWip,
		featureTerm,
		predictabilityData,
		percentileValues,
		serviceLevelExpectation,
		cycleTimeData: cycleTimeData as unknown as IWorkItem[],
		startedItems,
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
	});

	const widgetFooters = buildWidgetFooters({
		wipCount: inProgressItems.length,
		systemWipLimit:
			entity.systemWIPLimit > 0 ? entity.systemWIPLimit : undefined,
		blockedCount: blockedItems.length,
		hasBlockedConfig,
		terms: ragTerms,
		featuresInProgress,
		featureWip,
		predictabilityScore: predictabilityData?.predictabilityScore ?? null,
		sle: serviceLevelExpectation,
		percentileValues,
		startedTotal: startedItems?.total ?? 0,
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
		})),
		wipOverTimeValues: wipOverTimeData
			? Array.from({ length: wipOverTimeData.history }, (_, i) =>
					wipOverTimeData.getValueOnDay(i),
				)
			: [],
		totalAgeStart: wipOverTimeData?.getValueOnDay(0) ?? 0,
		totalAgeEnd: wipOverTimeData
			? wipOverTimeData.getValueOnDay(Math.max(0, wipOverTimeData.history - 1))
			: 0,
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
		featureSizeTarget,
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
	});

	const widgetViewData = buildViewData({
		title,
		inProgressItems,
		blockedItems,
		featuresInProgress,
		cycleTimeData: cycleTimeData as unknown as IWorkItem[],
		startedItems,
		throughputData,
		wipOverTimeData,
		allFeaturesForSizeChart,
		serviceLevelExpectation,
		estimationVsCycleTimeData,
		workItemLookup,
		terms: {
			workItems: ragTerms.workItems,
			features: ragTerms.features,
			cycleTime: ragTerms.cycleTime,
			workItemAge: ragTerms.workItemAge,
			blocked: ragTerms.blocked,
		},
	});

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
		</Grid>
	);
};
