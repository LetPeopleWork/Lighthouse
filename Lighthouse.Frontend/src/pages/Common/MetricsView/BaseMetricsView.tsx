import { Grid } from "@mui/material";
import { useCallback, useMemo, useState } from "react";
import type { EvaluatorCondition } from "../../../components/Common/Charts/ThroughputChart/evaluateCondition";
import WorkItemsDialog from "../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import { metricsOwnerKeyOf } from "../../../hooks/metricsOwnerKey";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useMetricsData } from "../../../hooks/useMetricsData";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { IFeature } from "../../../models/Feature";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type {
	ICycleTimeDefinition,
	INamedCycleTimeDefinition,
} from "../../../models/Metrics/NamedCycleTime";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useTerminology } from "../../../services/TerminologyContext";
import { sleRiskAtRiskSummary } from "../../../utils/charts/sleRisk";
import { formatLocalDate } from "../../../utils/date/localDate";
import { computeBlockedTrend } from "./blockedTrend";
import {
	getFetchKeysForCategories,
	getWidgetsForCategory,
} from "./categoryMetadata";
import type { DashboardItem } from "./Dashboard";
import Dashboard from "./Dashboard";
import DashboardHeader from "./DashboardHeader";
import {
	bodyOf,
	columnsStatusOf,
	frameStateOf,
	isPlaced,
} from "./dashboardFrame";
import FeatureSizePercentilesWidget from "./FeatureSizePercentilesWidget";
import { deriveLoadBalanceMatrixData } from "./loadBalanceMatrix";
import type { RagTerms } from "./ragTypes";
import TotalArrivalsWidget from "./TotalArrivalsWidget";
import TotalThroughputWidget from "./TotalThroughputWidget";
import type { TrendPayload } from "./trendTypes";
import {
	useCategorySelection,
	useVisitedCategories,
} from "./useCategorySelection";
import {
	useCumulativeScopeChoice,
	useCumulativeStateTimeCandidates,
} from "./useCumulativeStateTimeChoices";
import { useDateRange } from "./useDateRange";
import { useNamedPercentilesScope } from "./useNamedPercentilesScope";
import { useShowTips } from "./useShowTips";
import { useStateDrillDown } from "./useStateDrillDown";
import WidgetShell from "./WidgetShell";
import { buildWidgetFooters, computeMaxBlockedAgeDays } from "./widgetFooters";
import { getWidgetInfo, type WidgetInfoOwner } from "./widgetInfoMetadata";
import { buildWidgetNodes } from "./widgetNodes";
import type { WidgetStatus } from "./widgetStatus";
import { buildViewData } from "./widgetViewData";
import { computeWorkItemAgePercentilesTrend } from "./workItemAgePercentilesTrend";
import { buildWorkItemLookup } from "./workItemLookup";

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
	/** Whether the Doing columns have arrived; the aging chart never reads a pending or failed fetch as an empty Doing list. */
	doingStatesStatus?: WidgetStatus;
	hasForecastFilter?: boolean;
	forecastFilterConditions?: readonly EvaluatorCondition[];
	stalenessThresholdDays?: number;
	blockedStalenessThresholdDays?: number;
	waitStates?: string[];
	stateMappings?: IStateMapping[];
	cycleTimeDefinitions?: ICycleTimeDefinition[];
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
	doingStatesStatus = "ready",
	hasForecastFilter = false,
	forecastFilterConditions = [],
	stalenessThresholdDays,
	blockedStalenessThresholdDays,
	waitStates = [],
	stateMappings = [],
	cycleTimeDefinitions = [],
}: BaseMetricsViewProps<T, E>) => {
	const ownerType: "team" | "portfolio" =
		"getFeaturesInProgress" in metricsService ? "team" : "portfolio";

	const {
		startDate,
		endDate,
		pendingStartDate,
		pendingEndDate,
		isCommitPending,
		canStepForward,
		stepDays,
		stepWindow,
		presets,
		selectedPresetDays,
		applyPreset,
		handleStartDateChange,
		handleEndDateChange,
	} = useDateRange(ownerType, defaultDateRange);

	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? false;

	const { selectedCategory, setSelectedCategory } = useCategorySelection(
		ownerType,
		entity.id,
	);

	// Built from local Y/M/D, never toISOString: at a negative UTC offset the UTC day flips
	// first, which would read as a new question and refetch everything for no reason.
	const selectedQuestion = `${entity.id}:${formatLocalDate(startDate)}:${formatLocalDate(endDate)}`;

	// Only the categories the user has actually opened get their data fetched. The set grows as
	// they explore and resets when the question changes — a different entity or date window.
	const visitedCategories = useVisitedCategories(
		selectedCategory,
		selectedQuestion,
	);
	const activeFetchKeys = useMemo(
		() => getFetchKeysForCategories(visitedCategories, ownerType),
		[visitedCategories, ownerType],
	);

	const [selectedItemIds, setSelectedItemIds] = useState<number[]>([]);
	const {
		scopeDefinitionId: cumulativeScopeDefinitionId,
		chooseScope: handleCumulativeScopeChange,
	} = useCumulativeScopeChoice(selectedQuestion);

	const {
		blackoutPeriods,
		throughputData,
		wipOverTimeData,
		inProgressItems,
		blockedItems,
		cycleTimeData,
		percentileValues,
		workItemAgePercentilesValues,
		previousWorkItemAgePercentilesValues,
		perStatePercentileValues,
		sleRiskValues,
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
		cumulativeStateTimeForSelection,
		cumulativeStateTimeForScope,
		flowEfficiencyInfo,
		blockedCountHistory,
		setThroughputPbcView,
		fetchStates,
	} = useMetricsData(
		entity,
		metricsService,
		startDate,
		endDate,
		activeFetchKeys,
		{
			itemIds: selectedItemIds,
			scopeDefinitionId: cumulativeScopeDefinitionId,
		},
	);

	const sleRangeInDays =
		entity.serviceLevelExpectationRange > 0
			? entity.serviceLevelExpectationRange
			: undefined;

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

	const {
		percentilesScopeDefinitionId,
		scopedPercentileValues,
		scopedPercentilesInfo,
		handlePercentilesScopeChange,
	} = useNamedPercentilesScope({
		metricsService,
		entityId: entity.id,
		startDate,
		endDate,
	});

	const displayedPercentileValues = scopedPercentileValues ?? percentileValues;

	// Absent for a team that published no target, exactly as the dialog column is - both read the
	// same empty answer, so neither can appear without the other.
	const sleRiskAtRisk = useMemo(
		() =>
			sleRiskValues.length > 0
				? sleRiskAtRiskSummary(sleRiskValues)
				: undefined,
		[sleRiskValues],
	);

	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const workItemAgeTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM_AGE);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const workInProgressTerm = getTerm(TERMINOLOGY_KEYS.WORK_IN_PROGRESS);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const {
		candidates: cumulativeCandidates,
		candidatesLoaded: cumulativeCandidatesLoaded,
		onPickerOpen: handleCumulativeStateTimePickerOpen,
	} = useCumulativeStateTimeCandidates({
		metricsService,
		ownerType,
		owner: metricsOwnerKeyOf(entity),
		startDate,
		endDate,
		selectedQuestion,
	});

	const handleCumulativeStateTimeSelectionChange = setSelectedItemIds;

	const {
		drillDownState,
		drillDownOpen,
		onBarClick: handleCumulativeStateTimeBarClick,
		onClose: closeDrillDown,
		drillDownStatus,
		drillDownWorkItems,
		drillDownDaysColumn,
	} = useStateDrillDown({
		metricsService,
		ownerType,
		entity,
		startDate,
		endDate,
		selectedItemIds,
	});

	const { showTips, toggleShowTips } = useShowTips(ownerType, entity.id);

	const workItemLookup = useMemo(
		() =>
			buildWorkItemLookup(
				throughputData,
				wipOverTimeData,
				cycleTimeData as unknown as IWorkItem[],
				inProgressItems,
				allFeaturesForSizeChart,
				ownerType,
			),
		[
			throughputData,
			wipOverTimeData,
			cycleTimeData,
			inProgressItems,
			allFeaturesForSizeChart,
			ownerType,
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

	// blockedItems spans To Do + In Progress (from useMetricsData's blocked-eligible fetch), so the
	// overview count/RAG include items blocked while still in To Do — not just the WIP (in-progress) set.
	// The server judges staleness as of the range's end and an older server says nothing, which
	// counts as not stale.
	const staleItems = inProgressItems.filter((item) => item.isStale === true);
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
		sleRangeInDays,
		cycleTimeData: cycleTimeData as unknown as IWorkItem[],
		namedCycleTimeDefinitions,
		onFetchNamedCycleTimePercentiles,
		cycleTimePercentileValues: displayedPercentileValues,
		percentilesScopeDefinitionId,
		onPercentilesScopeChange: handlePercentilesScopeChange,
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
		cumulativeStateTime,
		displayedCumulativeStateTime:
			cumulativeStateTimeForScope ??
			cumulativeStateTimeForSelection ??
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
			handleCumulativeStateTimeBarClick(stateName);
		},
		waitStates,
		stateMappings,
		setThroughputPbcView,
		blockedCountHistory,
		flowEfficiencyInfo,
		sleRiskAtRisk,
		sleRiskValues,
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
		// Declared once here and shared by the risk column, the aging chart's dialog and the RAG
		// inputs below, which all have to name the same promise.

		percentilesScopeDefinitionId,
		percentileValues,
		startedTotal: arrivalsData?.total ?? 0,
		closedTotal: throughputData?.total ?? 0,
		totalWorkItemAge,
		currentWip: inProgressItems.length,
		// Read off the same summary the widget renders, never counted a second time here. Two
		// readings of one rule is how a count and the list behind it come to disagree.
		atRiskCount: sleRiskAtRisk?.count ?? 0,
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
			isStale: item.isStale === true,
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
		flowEfficiencyInfo,
	});

	const widgetViewData = buildViewData({
		title,
		endDate,
		ownerKind: ownerType,
		inProgressItems,
		blockedItems,
		staleItems,
		featuresInProgress,
		cycleTimeData: cycleTimeData as unknown as IWorkItem[],
		throughputData,
		wipOverTimeData,
		allFeaturesForSizeChart,
		serviceLevelExpectation,
		sleRangeInDays,
		percentilesScopeDefinitionId,
		namedCycleTimeDefinitions,
		estimationVsCycleTimeData,
		arrivalsData,
		workItemLookup,
		perStatePercentileValues,
		sleRiskValues,
		doingStates,
		terms: {
			workItem: ragTerms.workItem,
			workItems: ragTerms.workItems,
			features: ragTerms.features,
			cycleTime: ragTerms.cycleTime,
			workItemAge: ragTerms.workItemAge,
			blocked: ragTerms.blocked,
			sle: ragTerms.sle,
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
		percentiles: (scopedPercentilesInfo ?? cycleTimePercentilesInfo)
			?.comparison,
		workItemAgePercentiles: computeWorkItemAgePercentilesTrend(
			workItemAgePercentilesValues,
			previousWorkItemAgePercentilesValues,
		),
	};

	const infoOwner: WidgetInfoOwner = {
		kind: ownerType,
		terms: {
			feature: featureTerm,
			features: featuresTerm,
			team: getTerm(TERMINOLOGY_KEYS.TEAM),
			workItems: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
		},
	};
	const activeWidgets = getWidgetsForCategory(selectedCategory, ownerType);
	const dashboardItems: DashboardItem[] = activeWidgets
		.filter((w) => isPlaced(w.widgetKey, widgetNodes[w.widgetKey], fetchStates))
		.map((w) => {
			const columnsStatus = columnsStatusOf(w.widgetKey, doingStatesStatus);
			return {
				id: w.widgetKey,
				size: w.size,
				node: (
					<WidgetShell
						widgetKey={w.widgetKey}
						showTips={showTips}
						header={widgetFooters[w.widgetKey]}
						info={getWidgetInfo(w.widgetKey, infoOwner)}
						viewData={widgetViewData[w.widgetKey]}
						trend={widgetTrends[w.widgetKey]}
						{...frameStateOf(
							w.widgetKey,
							fetchStates,
							isCommitPending,
							columnsStatus,
						)}
					>
						{bodyOf(
							w.widgetKey,
							widgetNodes[w.widgetKey],
							fetchStates,
							columnsStatus,
						)}
					</WidgetShell>
				),
			};
		});

	return (
		<Grid container spacing={2}>
			<DashboardHeader
				startDate={startDate}
				endDate={endDate}
				onStartDateChange={handleStartDateChange}
				onEndDateChange={handleEndDateChange}
				presets={presets}
				selectedPresetDays={selectedPresetDays}
				onSelectPreset={applyPreset}
				pendingStartDate={pendingStartDate}
				pendingEndDate={pendingEndDate}
				isCommitPending={isCommitPending}
				stepDays={stepDays}
				canStepForward={canStepForward}
				onStepWindow={stepWindow}
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
				context="cumulativeStateTime"
				ownerKind={ownerType}
				status={drillDownStatus}
				daysContributedColumn={drillDownDaysColumn}
				onClose={closeDrillDown}
			/>
		</Grid>
	);
};
