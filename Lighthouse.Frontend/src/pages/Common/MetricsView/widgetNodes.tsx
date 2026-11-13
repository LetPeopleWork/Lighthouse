import type { ReactNode } from "react";
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
import type { WorkItemsDialogOwnerKind } from "../../../components/Common/WorkItemsDialog/workItemsDialogContexts";
import {
	type MetricsOwnerRequest,
	metricsOwnerKeyOf,
} from "../../../hooks/metricsOwnerKey";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import type { IStateMapping } from "../../../models/Common/StateMapping";
import type { IFeature } from "../../../models/Feature";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { ICumulativeStateTimeResponse } from "../../../models/Metrics/CumulativeStateTime";
import type { ICumulativeStateTimeCandidateRow } from "../../../models/Metrics/CumulativeStateTimeCandidates";
import type { IEstimationVsCycleTimeResponse } from "../../../models/Metrics/EstimationVsCycleTimeData";
import type { IFeatureSizeEstimationResponse } from "../../../models/Metrics/FeatureSizeEstimationData";
import type { IFlowEfficiencyInfo } from "../../../models/Metrics/FlowEfficiencyInfo";
import type {
	IArrivalsInfo,
	IFeatureSizePercentilesInfo,
	IThroughputInfo,
} from "../../../models/Metrics/InfoWidgetData";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { ISleRisk } from "../../../models/Metrics/SleRisk";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IPerStatePercentileValues } from "../../../models/PerStatePercentileValues";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import type { SleRiskAtRiskSummary } from "../../../utils/charts/sleRisk";
import { appColors } from "../../../utils/theme/colors";
import BlockedOverviewWidget from "./BlockedOverviewWidget";
import FeatureSizePercentilesWidget from "./FeatureSizePercentilesWidget";
import FeaturesWorkedOnWidget from "./FeaturesWorkedOnWidget";
import FlowEfficiencyOverviewWidget from "./FlowEfficiencyOverviewWidget";
import type { LoadBalanceMatrixData } from "./loadBalanceMatrix";
import PbcOverTimeWidget from "./PbcOverTimeWidget";
import PercentilesOverTimeWidget from "./PercentilesOverTimeWidget";
import PredictabilityScoreDetailsWidget from "./PredictabilityScoreDetailsWidget";
import PredictabilityScoreOverviewWidget from "./PredictabilityScoreOverviewWidget";
import SleRiskWidget from "./SleRiskWidget";
import StaleOverviewWidget from "./StaleOverviewWidget";
import ThroughputRunChartCard from "./ThroughputRunChartCard";
import TotalArrivalsWidget from "./TotalArrivalsWidget";
import TotalThroughputWidget from "./TotalThroughputWidget";
import WipOverviewWidget from "./WipOverviewWidget";

function buildPbcNode(
	data: ProcessBehaviourChartData | null,
	titleSuffix: string,
	type: ProcessBehaviourChartType,
	workItemLookup: Map<number, IWorkItem>,
	ownerKind: WorkItemsDialogOwnerKind,
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
			ownerKind={ownerKind}
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
	ownerType: WorkItemsDialogOwnerKind;
	throughputTerm: string;
	workItemAgeTerm: string;
	workInProgressTerm: string;
	featureTerm: string;
	getTerm: (key: string) => string;
	isPremium: boolean;
	hasForecastFilter: boolean;
	forecastFilterConditions: readonly EvaluatorCondition[];
	setThroughputPbcView: (view: "raw" | "filtered") => void;
};

function buildPbcNodes(ctx: PbcNodesCtx): Record<string, ReactNode | null> {
	const throughputPbcFilterToggle = (
		<ThroughputChartFilterToggle
			isPremium={ctx.isPremium}
			hasFilter={ctx.hasForecastFilter}
			onChange={(filtered) => {
				ctx.setThroughputPbcView(filtered ? "filtered" : "raw");
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
			type: ProcessBehaviourChartType.Arrivals,
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
			ctx.ownerType,
			config.filterToggle,
		);
	}
	return result;
}

function buildOverviewScoreNodes(ctx: {
	flowEfficiencyInfo: IFlowEfficiencyInfo | null;
	predictabilityData: IForecastPredictabilityScore | null;
	totalWorkItemAge: number | null;
}): Record<string, ReactNode | null> {
	return {
		flowEfficiency: ctx.flowEfficiencyInfo ? (
			<FlowEfficiencyOverviewWidget info={ctx.flowEfficiencyInfo} />
		) : null,
		predictabilityScore: ctx.predictabilityData ? (
			<PredictabilityScoreOverviewWidget
				score={ctx.predictabilityData.predictabilityScore}
			/>
		) : null,
		totalWorkItemAge:
			ctx.totalWorkItemAge === null ? null : (
				<TotalWorkItemAgeWidget totalAge={ctx.totalWorkItemAge} />
			),
	};
}

function buildInfoNodes(ctx: {
	throughputInfo: IThroughputInfo | null;
	arrivalsInfo: IArrivalsInfo | null;
	featureSizePercentilesInfo: IFeatureSizePercentilesInfo | null;
}): Record<string, ReactNode | null> {
	return {
		totalThroughput: ctx.throughputInfo ? (
			<TotalThroughputWidget data={ctx.throughputInfo} />
		) : null,
		totalArrivals: ctx.arrivalsInfo ? (
			<TotalArrivalsWidget data={ctx.arrivalsInfo} />
		) : null,
		featureSizePercentiles: ctx.featureSizePercentilesInfo ? (
			<FeatureSizePercentilesWidget data={ctx.featureSizePercentilesInfo} />
		) : null,
	};
}

export function buildWidgetNodes(ctx: {
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
	sleRangeInDays: number | undefined;
	cycleTimeData: IWorkItem[];
	namedCycleTimeDefinitions: INamedCycleTimeDefinition[];
	onFetchNamedCycleTimePercentiles: (
		definitionId: number,
	) => Promise<IPercentileValue[]>;
	cycleTimePercentileValues: IPercentileValue[];
	percentilesScopeDefinitionId: number | null;
	onPercentilesScopeChange: (definitionId: number | null) => void;
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
	setThroughputPbcView: (view: "raw" | "filtered") => void;
	blockedCountHistory: BlockedCountSnapshot[] | null;
	flowEfficiencyInfo: IFlowEfficiencyInfo | null;
	sleRiskAtRisk: SleRiskAtRiskSummary | undefined;
	sleRiskValues: ISleRisk[];
}): Record<string, ReactNode | null> {
	const filteredViewOwner: MetricsOwnerRequest = {
		ownerType: ctx.ownerType,
		...metricsOwnerKeyOf(ctx.entity),
	};
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
		sleRisk: <SleRiskWidget atRisk={ctx.sleRiskAtRisk} />,
		blockedOverview: (
			<BlockedOverviewWidget
				blockedCount={ctx.blockedItems.length}
				title={ctx.blockedTerm}
			/>
		),
		staleOverview: <StaleOverviewWidget staleCount={ctx.staleItems.length} />,
		featuresWorkedOnOverview: ctx.featuresInProgress ? (
			<FeaturesWorkedOnWidget
				featureCount={ctx.featuresInProgress.length}
				featureWip={ctx.featureWip}
				title={`${ctx.featuresTerm} being Worked On`}
			/>
		) : null,
		predictabilityScoreDetails: (
			<PredictabilityScoreDetailsWidget
				predictabilityData={ctx.predictabilityData}
				owner={filteredViewOwner}
				metricsService={ctx.metricsService}
				startDate={ctx.startDate}
				endDate={ctx.endDate}
				isPremium={ctx.isPremium}
				hasForecastFilter={ctx.hasForecastFilter}
			/>
		),
		percentiles: (
			<CycleTimePercentiles
				percentileValues={ctx.cycleTimePercentileValues}
				namedCycleTimeDefinitions={ctx.namedCycleTimeDefinitions}
				scopeDefinitionId={ctx.percentilesScopeDefinitionId}
				onScopeChange={ctx.onPercentilesScopeChange}
			/>
		),
		workItemAgePercentiles: (
			<WorkItemAgePercentiles
				percentileValues={ctx.workItemAgePercentilesValues}
			/>
		),
		throughput: ctx.throughputData ? (
			<ThroughputRunChartCard
				owner={filteredViewOwner}
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
				ownerKind={ctx.ownerType}
			/>
		),
		workDistribution: (
			<WorkDistributionChart
				workItems={
					[...ctx.cycleTimeData, ...ctx.inProgressItems] as IWorkItem[]
				}
				title="Work Distribution"
				ownerKind={ctx.ownerType}
			/>
		),
		aging: (
			// The items are a snapshot as of endDate, and so is the currentStateEnteredAt they
			// carry, so `now` must be endDate too. Measuring time-in-state against today would add
			// every day since the range ended.
			<WorkItemAgingChart
				inProgressItems={ctx.inProgressItems}
				sleRiskValues={ctx.sleRiskValues}
				percentileValues={ctx.percentileValues}
				serviceLevelExpectation={ctx.serviceLevelExpectation}
				sleRangeInDays={ctx.sleRangeInDays}
				doingStates={ctx.doingStates}
				perStatePercentileValues={ctx.perStatePercentileValues}
				workItemAgePercentileValues={ctx.workItemAgePercentilesValues}
				now={ctx.endDate}
				ownerKind={ctx.ownerType}
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
				dialogContext="inProgress"
				ownerKind={ctx.ownerType}
			/>
		) : null,
		totalWorkItemAgeOverTime: ctx.wipOverTimeData ? (
			<TotalWorkItemAgeRunChart
				title={`${ctx.title} Total Work Item Age Over Time`}
				startDate={ctx.startDate}
				wipOverTimeData={ctx.wipOverTimeData}
				ownerKind={ctx.ownerType}
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
					ownerKind={ctx.ownerType}
				/>
			) : null,
		featureSize: (
			<FeatureSizeScatterPlotChart
				sizeDataPoints={ctx.allFeaturesForSizeChart}
				sizePercentileValues={ctx.sizePercentileValues}
				estimationData={ctx.featureSizeEstimationData ?? undefined}
				ownerKind={ctx.ownerType}
			/>
		),
		arrivals: ctx.arrivalsData ? (
			<BarRunChart
				title={`${ctx.title} Started`}
				startDate={ctx.startDate}
				chartData={ctx.arrivalsData}
				displayTotal={true}
				dialogContext="arrivals"
				ownerKind={ctx.ownerType}
			/>
		) : null,
		blockedCountHistory: (
			<BlockedItemsOverTimeChart
				snapshots={ctx.blockedCountHistory}
				metricsService={ctx.metricsService}
				ownerId={ctx.entity.id}
				title={`${ctx.blockedTerm} Over Time`}
				ownerKind={ctx.ownerType}
			/>
		),
		percentilesOverTime: (
			<PercentilesOverTimeWidget
				owner={metricsOwnerKeyOf(ctx.entity)}
				metricsService={ctx.metricsService}
				startDate={ctx.startDate}
				endDate={ctx.endDate}
			/>
		),
		pbcOverTime: (
			<PbcOverTimeWidget
				owner={metricsOwnerKeyOf(ctx.entity)}
				metricsService={ctx.metricsService}
				startDate={ctx.startDate}
				endDate={ctx.endDate}
				ownerType={ctx.ownerType}
			/>
		),
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

	Object.assign(
		nodes,
		buildInfoNodes(ctx),
		buildPbcNodes(ctx),
		buildOverviewScoreNodes(ctx),
	);

	return nodes;
}
