import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: true },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

import type { IFeature } from "../../../models/Feature";
import { ForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IFeatureSizeEstimationResponse } from "../../../models/Metrics/FeatureSizeEstimationData";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { Portfolio } from "../../../models/Portfolio/Portfolio";
import { Team } from "../../../models/Team/Team";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { generateWorkItemMapForRunChart } from "../../../tests/TestDataProvider";
import { BaseMetricsView } from "./BaseMetricsView";
import { getWidgetsForCategory } from "./categoryMetadata";

// Mock the components used in BaseMetricsView
vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: ({
		title,
		chartData,
	}: {
		title: string;
		chartData: RunChartData;
	}) => (
		<div data-testid={`bar-run-chart-${title}`}>
			<div data-testid="chart-data-count">{chartData.history}</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/LineRunChart", () => ({
	default: ({
		title,
		chartData,
	}: {
		title: string;
		chartData: RunChartData;
	}) => (
		<div data-testid={`line-run-chart-${title}`}>
			<div data-testid="chart-data-count">{chartData.history}</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/CycleTimeScatterPlotChart", () => ({
	default: ({
		cycleTimeDataPoints,
		percentileValues,
		serviceLevelExpectation,
	}: {
		cycleTimeDataPoints: IWorkItem[];
		percentileValues: IPercentileValue[];
		serviceLevelExpectation: IPercentileValue | null;
	}) => (
		<div data-testid="cycle-time-scatter-plot">
			<div data-testid="cycle-time-data-points-count">
				{cycleTimeDataPoints.length}
			</div>
			<div data-testid="percentile-values-count">{percentileValues.length}</div>
			<div data-testid="service-level-expectation">
				{serviceLevelExpectation
					? `${serviceLevelExpectation.percentile}:${serviceLevelExpectation.value}`
					: "none"}
			</div>
		</div>
	),
}));

vi.mock(
	"../../../components/Common/Charts/FeatureSizeScatterPlotChart",
	() => ({
		default: ({
			sizeDataPoints,
			sizePercentileValues,
		}: {
			sizeDataPoints: IFeature[];
			sizePercentileValues?: IPercentileValue[];
		}) => (
			<div data-testid="feature-size-scatter-plot">
				<div data-testid="size-data-points-count">{sizeDataPoints.length}</div>
				<div data-testid="size-percentile-values-count">
					{sizePercentileValues?.length || 0}
				</div>
			</div>
		),
	}),
);

vi.mock("../../../components/Common/Charts/CycleTimePercentiles", () => ({
	default: ({
		percentileValues,
		namedCycleTimeDefinitions,
		scopeDefinitionId,
		onScopeChange,
	}: {
		percentileValues: IPercentileValue[];
		namedCycleTimeDefinitions?: { id: number; name: string }[];
		scopeDefinitionId?: number | null;
		onScopeChange?: (definitionId: number | null) => void;
	}) => (
		<div data-testid="cycle-time-percentiles">
			<div data-testid="percentile-values-count">{percentileValues.length}</div>
			<div data-testid="percentile-scope">{scopeDefinitionId ?? "default"}</div>
			<div data-testid="percentile-defs">
				{(namedCycleTimeDefinitions ?? []).map((d) => d.name).join(",")}
			</div>
			<button
				type="button"
				data-testid="percentile-select-named"
				onClick={() =>
					onScopeChange?.(namedCycleTimeDefinitions?.[0]?.id ?? null)
				}
			>
				select named
			</button>
			<button
				type="button"
				data-testid="percentile-select-second-named"
				onClick={() =>
					onScopeChange?.(namedCycleTimeDefinitions?.[1]?.id ?? null)
				}
			>
				select second named
			</button>
			<button
				type="button"
				data-testid="percentile-select-default"
				onClick={() => onScopeChange?.(null)}
			>
				select default
			</button>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/CumulativeStateTimeChart", () => ({
	default: ({
		data,
		onBarClick,
		pickerSlot,
		completionFilterEnabled = false,
		waitStates = [],
		stateMappings = [],
	}: {
		data: { states: { state: string }[] };
		onBarClick?: (stateName: string) => void;
		pickerSlot?: ReactNode;
		completionFilterEnabled?: boolean;
		waitStates?: string[];
		stateMappings?: { name: string }[];
	}) => (
		<div data-testid="cumulative-state-time-chart">
			<div data-testid="cumulative-wait-states">{waitStates.join(",")}</div>
			<div data-testid="cumulative-state-mappings">
				{stateMappings.map((mapping) => mapping.name).join(",")}
			</div>
			<div data-testid="cumulative-displayed-state-count">
				{data.states.length}
			</div>
			<div data-testid="cumulative-first-state">
				{data.states[0]?.state ?? "none"}
			</div>
			<button
				type="button"
				data-testid="cumulative-bar-click-proxy"
				onClick={() => onBarClick?.(data.states[0]?.state ?? "Doing")}
			>
				bar
			</button>
			{completionFilterEnabled && (
				<>
					<button type="button" aria-label="Completed visibility toggle">
						Completed
					</button>
					<button type="button" aria-label="Ongoing visibility toggle">
						Ongoing
					</button>
				</>
			)}
			{pickerSlot}
		</div>
	),
}));

vi.mock(
	"../../../components/Common/DateRangeSelector/DateRangeSelector",
	() => ({
		default: ({
			startDate,
			endDate,
			onStartDateChange,
			onEndDateChange,
		}: {
			startDate: Date;
			endDate: Date;
			onStartDateChange: (date: Date | null) => void;
			onEndDateChange: (date: Date | null) => void;
		}) => (
			<div data-testid="date-range-selector">
				<span data-testid="start-date">{startDate.toISOString()}</span>
				<span data-testid="end-date">{endDate.toISOString()}</span>
				<button
					type="button"
					data-testid="change-start-date"
					onClick={() => {
						const newDate = new Date(startDate);
						newDate.setDate(newDate.getDate() - 30);
						onStartDateChange(newDate);
					}}
				>
					Change Start Date
				</button>
				<button
					type="button"
					data-testid="change-end-date"
					onClick={() => {
						const newDate = new Date(endDate);
						newDate.setDate(newDate.getDate() - 30);
						onEndDateChange(newDate);
					}}
				>
					Change End Date
				</button>
			</div>
		),
	}),
);

vi.mock("./WipOverviewWidget", () => ({
	default: ({
		wipCount,
		systemWipLimit,
		title,
	}: {
		wipCount: number;
		systemWipLimit?: number;
		title?: string;
	}) => (
		<div data-testid="wip-overview-widget">
			<div data-testid="wip-overview-count">{wipCount}</div>
			<div data-testid="wip-overview-limit">{systemWipLimit ?? ""}</div>
			<div data-testid="wip-overview-title">{title}</div>
		</div>
	),
}));

vi.mock("./BlockedOverviewWidget", () => ({
	default: ({
		blockedCount,
		title,
	}: {
		blockedCount: number;
		title?: string;
	}) => (
		<div data-testid="blocked-overview-widget">
			<div data-testid="blocked-overview-count">{blockedCount}</div>
			<div data-testid="blocked-overview-title">{title}</div>
		</div>
	),
}));

vi.mock("./FeaturesWorkedOnWidget", () => ({
	default: ({
		featureCount,
		featureWip,
		title,
	}: {
		featureCount: number;
		featureWip?: number;
		title?: string;
	}) => (
		<div data-testid="features-worked-on-widget">
			<div data-testid="features-worked-on-count">{featureCount}</div>
			<div data-testid="features-worked-on-wip">{featureWip ?? ""}</div>
			<div data-testid="features-worked-on-title">{title}</div>
		</div>
	),
}));

vi.mock("./PredictabilityScoreOverviewWidget", () => ({
	default: ({ score }: { score: number | null }) => (
		<div data-testid="predictability-score-widget">
			<div data-testid="predictability-score-value">{score ?? "loading"}</div>
		</div>
	),
}));

vi.mock("./PredictabilityScoreDetailsWidget", () => ({
	default: () => (
		<div data-testid="predictability-score-details-widget">
			Predictability Score Details
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/StackedAreaChart", () => ({
	default: ({
		title,
		areas,
	}: {
		title: string;
		areas: Array<{ name: string; data: number[] }>;
	}) => (
		<div data-testid={`stacked-area-chart-${title}`}>
			<div data-testid="areas-count">{areas.length}</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/WorkItemAgingChart", () => ({
	default: ({
		inProgressItems,
		percentileValues,
		serviceLevelExpectation,
		workItemAgePercentileValues = [],
	}: {
		inProgressItems: IWorkItem[];
		percentileValues: IPercentileValue[];
		serviceLevelExpectation: IPercentileValue | null;
		workItemAgePercentileValues?: IPercentileValue[];
	}) => (
		<div data-testid="work-item-aging-chart">
			<div data-testid="aging-in-progress-items-count">
				{inProgressItems.length}
			</div>
			<div data-testid="aging-percentile-values-count">
				{percentileValues.length}
			</div>
			<div data-testid="aging-work-item-age-percentile-values-count">
				{workItemAgePercentileValues.length}
			</div>
			<div data-testid="aging-service-level-expectation">
				{serviceLevelExpectation
					? `${serviceLevelExpectation.percentile}:${serviceLevelExpectation.value}`
					: "none"}
			</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/LoadBalanceMatrixChart", () => ({
	default: ({ data }: { data: { points: Array<{ date: Date }> } }) => (
		<div data-testid="load-balance-matrix-chart">
			<div data-testid="load-balance-point-count">{data.points.length}</div>
			<div data-testid="load-balance-today-date">
				{data.points[0]?.date?.toISOString() ?? "none"}
			</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/WorkDistributionChart", () => ({
	default: ({
		workItems,
		title,
	}: {
		workItems: IWorkItem[];
		title: string;
	}) => (
		<div data-testid="work-distribution-chart">
			<div data-testid="distribution-title">{title}</div>
			<div data-testid="distribution-work-items-count">{workItems.length}</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/TotalWorkItemAgeWidget", () => ({
	default: ({
		entityId,
		metricsService,
	}: {
		entityId: number;
		metricsService: IMetricsService<IWorkItem>;
	}) => (
		<div data-testid="total-work-item-age-widget">
			<div data-testid="widget-entity-id">{entityId}</div>
			<div data-testid="widget-has-service">
				{metricsService ? "has-service" : "no-service"}
			</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/TotalWorkItemAgeRunChart", () => ({
	default: ({
		title,
		startDate,
		wipOverTimeData,
	}: {
		title: string;
		startDate: Date;
		wipOverTimeData: RunChartData;
	}) => (
		<div data-testid={`total-work-item-age-run-chart-${title}`}>
			<div data-testid="age-chart-data-count">{wipOverTimeData.history}</div>
			<div data-testid="age-chart-start-date">{startDate.toISOString()}</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/ProcessBehaviourChart", () => ({
	default: ({
		data,
		title,
		filterToggle,
	}: {
		data: ProcessBehaviourChartData;
		title: string;
		filterToggle?: ReactNode;
	}) => (
		<div data-testid={`process-behaviour-chart-${title}`}>
			<div data-testid={`pbc-status-${title}`}>{data.status}</div>
			<div data-testid={`pbc-data-points-${title}`}>
				{data.dataPoints.length}
			</div>
			{filterToggle ? (
				<div data-testid={`pbc-filter-toggle-${title}`}>{filterToggle}</div>
			) : null}
		</div>
	),
	ProcessBehaviourChartType: {
		Throughput: "Throughput",
		WorkInProgress: "WorkInProgress",
		TotalWorkItemAge: "TotalWorkItemAge",
		CycleTime: "CycleTime",
	},
}));

vi.mock("./DashboardHeader", () => ({
	default: ({
		startDate,
		endDate,
		onStartDateChange,
		onEndDateChange,
	}: {
		startDate: Date;
		endDate: Date;
		onStartDateChange: (d: Date | null) => void;
		onEndDateChange: (d: Date | null) => void;
	}) => (
		<div>
			<div data-testid="dashboard-header">header</div>
			<button type="button" data-testid="dashboard-date-range-toggle">
				Toggle
			</button>
			<div data-testid="date-range-selector">
				<span data-testid="start-date">{startDate.toISOString()}</span>
				<span data-testid="end-date">{endDate.toISOString()}</span>
				<button
					type="button"
					data-testid="change-start-date"
					onClick={() => {
						const newDate = new Date(startDate);
						newDate.setDate(newDate.getDate() - 30);
						onStartDateChange(newDate);
					}}
				>
					Change Start Date
				</button>
				<button
					type="button"
					data-testid="change-end-date"
					onClick={() => {
						const newDate = new Date(endDate);
						newDate.setDate(newDate.getDate() - 30);
						onEndDateChange(newDate);
					}}
				>
					Change End Date
				</button>
			</div>
		</div>
	),
}));

vi.mock("./Dashboard", () => ({
	default: ({
		items,
	}: {
		items?: Array<{ id: string | number; node: ReactNode }>;
	}) => (
		<div>
			<div data-testid="dashboard-component">dashboard</div>
			{items?.map((it) => (
				<div
					key={String(it.id)}
					data-testid={`dashboard-item-${String(it.id)}`}
				>
					{it.node}
				</div>
			))}
		</div>
	),
}));

vi.mock("./WidgetShell", () => ({
	default: ({
		widgetKey,
		children,
		header,
		info,
		viewData,
		trend,
	}: {
		widgetKey: string;
		children: ReactNode;
		header?: { ragStatus: string; tipText: string };
		info?: { description: string; learnMoreUrl: string };
		viewData?: {
			title: string;
			items: IWorkItem[];
			highlightColumn?: {
				title: string;
				description: string;
				valueGetter: (item: IWorkItem) => number;
			};
			sle?: number;
		};
		trend?: { direction: string; metricLabel: string };
	}) => (
		<div data-testid={`widget-shell-${widgetKey}`}>
			{header && (
				<div data-testid={`widget-header-${widgetKey}`}>
					<span data-testid={`widget-rag-${widgetKey}`}>
						{header.ragStatus}
					</span>
					<span data-testid={`widget-tip-${widgetKey}`}>{header.tipText}</span>
				</div>
			)}
			{trend && trend.direction !== "none" && (
				<div data-testid={`widget-trend-${widgetKey}`}>
					<span data-testid={`widget-trend-direction-${widgetKey}`}>
						{trend.direction}
					</span>
					<span data-testid={`widget-trend-label-${widgetKey}`}>
						{trend.metricLabel}
					</span>
				</div>
			)}
			{info && (
				<div data-testid={`widget-info-${widgetKey}`}>
					<span data-testid={`widget-info-desc-${widgetKey}`}>
						{info.description}
					</span>
					<a
						data-testid={`widget-info-link-${widgetKey}`}
						href={info.learnMoreUrl}
					>
						Learn more about {info.description}
					</a>
				</div>
			)}
			{viewData && (
				<div data-testid={`widget-view-data-${widgetKey}`}>
					<span data-testid={`widget-view-data-title-${widgetKey}`}>
						{viewData.title}
					</span>
					<span data-testid={`widget-view-data-count-${widgetKey}`}>
						{viewData.items.length}
					</span>
					<span data-testid={`widget-view-data-highlight-title-${widgetKey}`}>
						{viewData.highlightColumn?.title ?? ""}
					</span>
					<span data-testid={`widget-view-data-highlight-values-${widgetKey}`}>
						{(viewData.highlightColumn
							? viewData.items.map((item) =>
									viewData.highlightColumn?.valueGetter(item),
								)
							: []
						).join(",")}
					</span>
					<span data-testid={`widget-view-data-sle-${widgetKey}`}>
						{viewData.sle ?? "none"}
					</span>
				</div>
			)}
			{children}
		</div>
	),
}));

describe("BaseMetricsView component", () => {
	// Helper function to render component with router context
	const renderWithRouter = (component: ReactNode) => {
		return render(<MemoryRouter>{component}</MemoryRouter>);
	};

	// Create RunChartData with correct properties
	const mockItemsCompletedData: RunChartData = new RunChartData(
		generateWorkItemMapForRunChart([3, 5]),
		2, // history
		8, // total
	);

	const mockItemsInProgressData: RunChartData = new RunChartData(
		generateWorkItemMapForRunChart([2, 4]),
		2, // history
		6, // total
	);

	// Create mock started items data
	const mockStartedItemsData: RunChartData = new RunChartData(
		generateWorkItemMapForRunChart([4, 6]),
		2, // history
		10, // total
	);

	// Create mock work items with correct properties
	const mockInProgressItems: IWorkItem[] = [
		{
			id: 1,
			name: "Item 1",
			state: "In Progress",
			stateCategory: "Doing" as StateCategory,
			type: "Story",
			referenceId: "ITEM-1",
			url: "https://example.com/work/1",
			startedDate: new Date("2023-01-01"),
			closedDate: new Date(),
			cycleTime: 0,
			workItemAge: 10,
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 2,
			name: "Item 2",
			state: "In Progress",
			stateCategory: "Doing" as StateCategory,
			type: "Bug",
			referenceId: "ITEM-2",
			url: "https://example.com/work/2",
			startedDate: new Date("2023-01-01"),
			closedDate: new Date(),
			cycleTime: 0,
			workItemAge: 8,
			parentWorkItemReference: "",
			isBlocked: false,
		},
	];

	const mockCycleTimeData: IWorkItem[] = [
		{
			id: 3,
			name: "Item 3",
			state: "Done",
			stateCategory: "Done" as StateCategory,
			type: "Story",
			referenceId: "ITEM-3",
			url: "https://example.com/work/3",
			startedDate: new Date("2023-01-01"),
			closedDate: new Date("2023-01-10"),
			cycleTime: 9,
			workItemAge: 9,
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 4,
			name: "Item 4",
			state: "Done",
			stateCategory: "Done" as StateCategory,
			type: "Bug",
			referenceId: "ITEM-4",
			url: "https://example.com/work/4",
			startedDate: new Date("2023-01-05"),
			closedDate: new Date("2023-01-15"),
			cycleTime: 10,
			workItemAge: 10,
			parentWorkItemReference: "",
			isBlocked: false,
		},
	];

	const mockPercentileValues: IPercentileValue[] = [
		{ percentile: 50, value: 9 },
		{ percentile: 85, value: 12 },
		{ percentile: 95, value: 15 },
	];

	// Mock metrics service
	const baselineMissingPbcData: ProcessBehaviourChartData = {
		status: "BaselineMissing",
		statusReason: "No Baseline Configured",
		xAxisKind: "Date",
		average: 0,
		upperNaturalProcessLimit: 0,
		lowerNaturalProcessLimit: 0,
		baselineConfigured: false,
		dataPoints: [],
	};

	function createMockMetricsService<
		T extends IWorkItem | IFeature = IWorkItem,
	>() {
		return {
			getThroughput: vi.fn().mockResolvedValue(mockItemsCompletedData),
			getArrivals: vi.fn().mockResolvedValue(mockStartedItemsData),
			getWorkInProgressOverTime: vi
				.fn()
				.mockResolvedValue(mockItemsInProgressData),
			getInProgressItems: vi.fn().mockResolvedValue(mockInProgressItems),
			getCycleTimeData: vi.fn().mockResolvedValue(mockCycleTimeData),
			getCycleTimePercentiles: vi.fn().mockResolvedValue(mockPercentileValues),
			getWorkItemAgePercentiles: vi
				.fn()
				.mockResolvedValue(mockPercentileValues),
			getAgeInStatePercentiles: vi.fn().mockResolvedValue([]),
			getMultiItemForecastPredictabilityScore: vi.fn().mockResolvedValue(
				new ForecastPredictabilityScore(
					[
						{ percentile: 50, value: 10 },
						{ percentile: 85, value: 15 },
					],
					0.73,
					new Map([]),
				),
			),
			getSizePercentiles: vi.fn().mockResolvedValue([
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 10 },
				{ percentile: 95, value: 15 },
			]),
			getAllFeaturesForSizeChart: vi.fn().mockResolvedValue(
				mockCycleTimeData.map((item) => ({
					...item,
					size: 5,
					stateCategory: "Done",
				})),
			),
			getTotalWorkItemAge: vi.fn().mockResolvedValue(150),
			getEstimationVsCycleTimeData: vi.fn().mockResolvedValue({
				status: "NotConfigured",
				diagnostics: {
					totalCount: 0,
					mappedCount: 0,
					unmappedCount: 0,
					invalidCount: 0,
				},
				estimationUnit: null,
				useNonNumericEstimation: false,
				categoryValues: [],
				dataPoints: [],
			}),
			getThroughputPbc: vi.fn().mockResolvedValue(baselineMissingPbcData),
			getWipPbc: vi.fn().mockResolvedValue(baselineMissingPbcData),
			getTotalWorkItemAgePbc: vi.fn().mockResolvedValue(baselineMissingPbcData),
			getCycleTimePbc: vi.fn().mockResolvedValue(baselineMissingPbcData),
			getFeatureSizePbc: vi.fn().mockResolvedValue(baselineMissingPbcData),
			getArrivalsPbc: vi.fn().mockResolvedValue(baselineMissingPbcData),
			getThroughputInfo: vi.fn().mockResolvedValue({
				total: 0,
				dailyAverage: 0,
				comparison: { direction: "none", metricLabel: "Total Throughput" },
			}),
			getArrivalsInfo: vi.fn().mockResolvedValue({
				total: 0,
				dailyAverage: 0,
				comparison: { direction: "none", metricLabel: "Total Arrivals" },
			}),
			getFeatureSizeEstimation: vi.fn().mockResolvedValue({
				status: "NotConfigured",
				estimationUnit: null,
				useNonNumericEstimation: false,
				categoryValues: [],
				featureEstimations: [],
			}),
			getFeatureSizePercentilesInfo: vi.fn().mockResolvedValue({
				percentiles: [],
				comparison: {
					direction: "none",
					metricLabel: "Feature Size Percentiles",
				},
			}),
			getWipOverviewInfo: vi.fn().mockResolvedValue({
				count: 0,
				comparison: { direction: "none", metricLabel: "WIP" },
			}),
			getFeaturesWorkedOnInfo: vi.fn().mockResolvedValue({
				count: 0,
				comparison: {
					direction: "none",
					metricLabel: "Features Being Worked On",
				},
			}),
			getTotalWorkItemAgeInfo: vi.fn().mockResolvedValue({
				totalAge: 0,
				comparison: {
					direction: "none",
					metricLabel: "Total Work Item Age",
				},
			}),
			getPredictabilityScoreInfo: vi.fn().mockResolvedValue({
				score: 0,
				comparison: {
					direction: "none",
					metricLabel: "Predictability Score",
				},
			}),
			getCycleTimePercentilesInfo: vi.fn().mockResolvedValue({
				percentiles: [],
				comparison: {
					direction: "none",
					metricLabel: "Cycle Time Percentiles",
				},
			}),
			getCumulativeStateTimeForTeam: vi.fn().mockResolvedValue({ states: [] }),
			getCumulativeStateTimeItemsForTeam: vi
				.fn()
				.mockResolvedValue({ state: "", items: [] }),
			getCumulativeStateTimeItemsForPortfolio: vi
				.fn()
				.mockResolvedValue({ state: "", items: [] }),
			getCumulativeStateTimeCandidatesForTeam: vi
				.fn()
				.mockResolvedValue({ items: [] }),
			getCumulativeStateTimeCandidatesForPortfolio: vi
				.fn()
				.mockResolvedValue({ items: [] }),
			getFlowEfficiencyInfoForTeam: vi.fn(),
			getFlowEfficiencyInfoForPortfolio: vi.fn(),
			getBlockedCountHistory: vi.fn().mockResolvedValue([]),
			getBlockedItemsAtDate: vi.fn().mockResolvedValue([]),
		} as IMetricsService<T> & {
			getSizePercentiles?: (
				id: number,
				startDate: Date,
				endDate: Date,
			) => Promise<IPercentileValue[]>;
			getAllFeaturesForSizeChart?: (
				id: number,
				startDate: Date,
				endDate: Date,
			) => Promise<IFeature[]>;
			getFeatureSizePbc?: (
				id: number,
				startDate: Date,
				endDate: Date,
			) => Promise<ProcessBehaviourChartData>;
			getFeatureSizeEstimation?: (
				id: number,
				startDate: Date,
				endDate: Date,
			) => Promise<IFeatureSizeEstimationResponse>;
			getFeatureSizePercentilesInfo?: (
				id: number,
				startDate: Date,
				endDate: Date,
			) => Promise<unknown>;
		};
	}

	const mockMetricsService = createMockMetricsService<IWorkItem>();

	// Create two types of entities to test with
	const mockProject = (() => {
		const project = new Portfolio();
		project.name = "Test Project";
		project.id = 1;
		project.lastUpdated = new Date();
		project.serviceLevelExpectationProbability = 85;
		project.serviceLevelExpectationRange = 14;
		return project;
	})();

	const mockTeam = (() => {
		const team = new Team();
		team.name = "Test Team";
		team.id = 2;
		team.featureWip = 3;
		team.lastUpdated = new Date();
		team.serviceLevelExpectationProbability = 80;
		team.serviceLevelExpectationRange = 10;
		return team;
	})();

	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
	});

	it("renders all components correctly with Project entity", async () => {
		const projectMetricsService = createMockMetricsService<IFeature>();

		const mockFeatureData = mockCycleTimeData.map((item) => ({
			...item,
			size: 5, // example size value
		}));
		projectMetricsService.getCycleTimeData = vi
			.fn()
			.mockResolvedValue(mockFeatureData);

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={projectMetricsService}
				title="Features"
				defaultDateRange={90}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Open header date-range popover and check DateRangeSelector is rendered
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		expect(
			await screen.findByTestId("date-range-selector"),
		).toBeInTheDocument();

		// Check all service calls are made
		await waitFor(() => {
			expect(projectMetricsService.getThroughput).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(projectMetricsService.getInProgressItems).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
			);
			expect(
				projectMetricsService.getWorkInProgressOverTime,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(projectMetricsService.getCycleTimeData).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(
				projectMetricsService.getCycleTimePercentiles,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(
				projectMetricsService.getMultiItemForecastPredictabilityScore,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			if (projectMetricsService.getAllFeaturesForSizeChart) {
				expect(
					projectMetricsService.getAllFeaturesForSizeChart,
				).toHaveBeenCalledWith(
					mockProject.id,
					expect.any(Date),
					expect.any(Date),
				);
			}
			if (projectMetricsService.getSizePercentiles) {
				expect(projectMetricsService.getSizePercentiles).toHaveBeenCalledWith(
					mockProject.id,
					expect.any(Date),
					expect.any(Date),
				);
			}
		});

		// Check components are rendered with correct data (flow-overview category by default)
		await waitFor(() => {
			expect(screen.getByTestId("wip-overview-widget")).toBeInTheDocument();
			expect(screen.getByTestId("wip-overview-count")).toHaveTextContent("2");
			expect(screen.getByTestId("cycle-time-percentiles")).toBeInTheDocument();
			expect(
				screen.getByTestId("total-work-item-age-widget"),
			).toBeInTheDocument();
		});
	});

	it("passes size percentile values to FeatureSizeScatterPlotChart when using Project entity", async () => {
		// featureSize widget is in the portfolio category
		localStorage.setItem(
			`lighthouse:metrics:portfolio:${mockProject.id}:category`,
			"portfolio",
		);

		const projectMetricsService = createMockMetricsService<IFeature>();

		const mockFeatureData = mockCycleTimeData.map((item) => ({
			...item,
			size: 5, // example size value
		}));
		projectMetricsService.getCycleTimeData = vi
			.fn()
			.mockResolvedValue(mockFeatureData);

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={projectMetricsService}
				title="Features"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for component to load and size chart methods to be called
		await waitFor(() => {
			if (projectMetricsService.getSizePercentiles) {
				expect(projectMetricsService.getSizePercentiles).toHaveBeenCalledWith(
					mockProject.id,
					expect.any(Date),
					expect.any(Date),
				);
			}
			if (projectMetricsService.getAllFeaturesForSizeChart) {
				expect(
					projectMetricsService.getAllFeaturesForSizeChart,
				).toHaveBeenCalledWith(
					mockProject.id,
					expect.any(Date),
					expect.any(Date),
				);
			}
		});

		// Check that FeatureSizeScatterPlotChart receives the size percentile values
		await waitFor(() => {
			expect(
				screen.getByTestId("feature-size-scatter-plot"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("size-percentile-values-count"),
			).toHaveTextContent("3");
		});
	});

	it("renders all components correctly with Team entity", async () => {
		renderWithRouter(
			<BaseMetricsView
				entity={mockTeam}
				metricsService={mockMetricsService}
				title="Work Items"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Open header date-range popover and check DateRangeSelector is rendered
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		expect(
			await screen.findByTestId("date-range-selector"),
		).toBeInTheDocument();

		// Check all service calls are made
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalledWith(
				mockTeam.id,
				expect.any(Date),
				expect.any(Date),
			);
		});

		// Check components are rendered with correct data
		await waitFor(() => {
			expect(screen.getByTestId("wip-overview-widget")).toBeInTheDocument();
			expect(screen.getByTestId("wip-overview-count")).toHaveTextContent("2");
			expect(screen.getByTestId("cycle-time-percentiles")).toBeInTheDocument();
		});
	});

	it("renders Total Work Item Age Over Time run chart correctly in flow-metrics category", async () => {
		// totalWorkItemAgeOverTime is in flow-metrics category
		localStorage.setItem(
			`lighthouse:metrics:portfolio:${mockProject.id}:category`,
			"flow-metrics",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for components to render
		await waitFor(() => {
			// Check Total Work Item Age Run Chart is rendered (in flow-metrics)
			expect(
				screen.getByTestId(
					"total-work-item-age-run-chart-Features Total Work Item Age Over Time",
				),
			).toBeInTheDocument();
			expect(screen.getByTestId("age-chart-data-count")).toHaveTextContent("2");
		});
	});

	it("renders load balance matrix in flow-metrics with baseline-missing red RAG guidance", async () => {
		localStorage.setItem(
			`lighthouse:metrics:portfolio:${mockProject.id}:category`,
			"flow-metrics",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("widget-shell-loadBalanceMatrix"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("load-balance-matrix-chart"),
			).toBeInTheDocument();
			expect(screen.getByTestId("load-balance-point-count")).toHaveTextContent(
				"6",
			);
			expect(
				screen.getByTestId("widget-rag-loadBalanceMatrix"),
			).toHaveTextContent("red");
			expect(
				screen.getByTestId("widget-tip-loadBalanceMatrix"),
			).toHaveTextContent("baseline");
		});
	});

	it("renders load balance matrix for team owner in flow-metrics", async () => {
		localStorage.setItem(
			`lighthouse:metrics:team:${mockTeam.id}:category`,
			"flow-metrics",
		);

		const teamMetricsService = {
			...createMockMetricsService<IWorkItem>(),
			getFeaturesInProgress: vi.fn().mockResolvedValue([]),
		};

		renderWithRouter(
			<BaseMetricsView
				entity={mockTeam}
				metricsService={teamMetricsService}
				title="Work Items"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("widget-shell-loadBalanceMatrix"),
			).toBeInTheDocument();
		});
	});

	it("uses selected end date as load balance matrix today point", async () => {
		localStorage.setItem(
			`lighthouse:metrics:portfolio:${mockProject.id}:category`,
			"flow-metrics",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("load-balance-today-date")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		const previousEnd = new Date(
			screen.getByTestId("end-date").textContent ?? "",
		);
		const expectedEnd = new Date(previousEnd);
		expectedEnd.setDate(expectedEnd.getDate() - 30);

		fireEvent.click(screen.getByTestId("change-end-date"));

		await waitFor(() => {
			expect(screen.getByTestId("load-balance-today-date")).toHaveTextContent(
				expectedEnd.toISOString(),
			);
			expect(mockMetricsService.getInProgressItems).toHaveBeenCalledWith(
				mockProject.id,
				expectedEnd,
			);
			expect(mockMetricsService.getTotalWorkItemAge).toHaveBeenCalledWith(
				mockProject.id,
				expectedEnd,
			);
		});
	});

	it("updates data when start date changes", async () => {
		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={90}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for initial render
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalled();
		});

		// Reset mock call counts
		vi.clearAllMocks();

		// Open date-range popover then change start date
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		await screen.findByTestId("start-date");
		// Change start date
		fireEvent.click(screen.getByTestId("change-start-date"));

		// Verify all metrics are fetched again with new date
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalledTimes(1);
			expect(
				mockMetricsService.getWorkInProgressOverTime,
			).toHaveBeenCalledTimes(1);
			expect(mockMetricsService.getCycleTimeData).toHaveBeenCalledTimes(1);
			expect(mockMetricsService.getCycleTimePercentiles).toHaveBeenCalledTimes(
				1,
			);
			expect(mockMetricsService.getArrivals).toHaveBeenCalledTimes(1);
			expect(
				mockMetricsService.getMultiItemForecastPredictabilityScore,
			).toHaveBeenCalledTimes(1);
		});
	});

	it("updates data when end date changes", async () => {
		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={90}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for initial render
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalled();
		});

		// Reset mock call counts
		vi.clearAllMocks();

		// Open date-range popover then change end date
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		await screen.findByTestId("start-date");
		// Change end date
		fireEvent.click(screen.getByTestId("change-end-date"));

		// Verify all metrics are fetched again with new date
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalledTimes(1);
			expect(
				mockMetricsService.getWorkInProgressOverTime,
			).toHaveBeenCalledTimes(1);
			expect(mockMetricsService.getCycleTimeData).toHaveBeenCalledTimes(1);
			expect(mockMetricsService.getCycleTimePercentiles).toHaveBeenCalledTimes(
				1,
			);
			expect(mockMetricsService.getArrivals).toHaveBeenCalledTimes(1);
			expect(
				mockMetricsService.getMultiItemForecastPredictabilityScore,
			).toHaveBeenCalledTimes(1);
		});
	});

	it("initializes with the specified default date range", async () => {
		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={60}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Open the date-range popover to access the mocked selector
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		await screen.findByTestId("date-range-selector");
		const startDateElement = screen.getByTestId("start-date");
		const endDateElement = screen.getByTestId("end-date");

		const startDate = new Date(startDateElement.textContent ?? "");
		const endDate = new Date(endDateElement.textContent ?? "");
		const today = new Date();

		// Set both dates to midnight for comparison
		today.setHours(0, 0, 0, 0);
		endDate.setHours(0, 0, 0, 0);

		const daysDifference = Math.floor(
			(endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24) + 1,
		);

		// Check that start date is roughly 60 days before end date (allowing for some hours difference)
		expect(daysDifference).toBeCloseTo(60, 0);

		// Check that end date is today
		expect(endDate.getDate()).toBe(today.getDate());
		expect(endDate.getMonth()).toBe(today.getMonth());
		expect(endDate.getFullYear()).toBe(today.getFullYear());
	});

	it("falls back to default date range of 30 days when not specified", async () => {
		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				doingStates={["To Do", "In Progress", "Review"]}
				// No defaultDateRange provided
			/>,
		);

		// Open the date-range popover to access the mocked selector
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		await screen.findByTestId("date-range-selector");
		const startDateElement = screen.getByTestId("start-date");
		const endDateElement = screen.getByTestId("end-date");

		const startDate = new Date(startDateElement.textContent ?? "");
		const endDate = new Date(endDateElement.textContent ?? "");

		const daysDifference = Math.round(
			(endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24),
		);

		// Check that start date is roughly 30 days before end date
		expect(daysDifference).toBeCloseTo(30, 0);
	});

	it("handles API errors gracefully", async () => {
		const errorMetricsService: IMetricsService<IWorkItem> = {
			getThroughput: vi.fn().mockRejectedValue(new Error("API error")),
			getWorkInProgressOverTime: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getInProgressItems: vi.fn().mockRejectedValue(new Error("API error")),
			getCycleTimeData: vi.fn().mockRejectedValue(new Error("API error")),
			getCycleTimePercentiles: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getWorkItemAgePercentiles: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getAgeInStatePercentiles: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getMultiItemForecastPredictabilityScore: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getTotalWorkItemAge: vi.fn().mockRejectedValue(new Error("API error")),
			getThroughputPbc: vi.fn().mockRejectedValue(new Error("API error")),
			getWipPbc: vi.fn().mockRejectedValue(new Error("API error")),
			getTotalWorkItemAgePbc: vi.fn().mockRejectedValue(new Error("API error")),
			getCycleTimePbc: vi.fn().mockRejectedValue(new Error("API error")),
			getEstimationVsCycleTimeData: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getArrivals: vi.fn().mockRejectedValue(new Error("API error")),
			getArrivalsPbc: vi.fn().mockRejectedValue(new Error("API error")),
			getThroughputInfo: vi.fn().mockRejectedValue(new Error("API error")),
			getArrivalsInfo: vi.fn().mockRejectedValue(new Error("API error")),
			getWipOverviewInfo: vi.fn().mockRejectedValue(new Error("API error")),
			getTotalWorkItemAgeInfo: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getPredictabilityScoreInfo: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getCycleTimePercentilesInfo: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getCumulativeStateTimeForTeam: vi.fn().mockResolvedValue({ states: [] }),
			getCumulativeStateTimeItemsForTeam: vi
				.fn()
				.mockResolvedValue({ state: "", items: [] }),
			getCumulativeStateTimeItemsForPortfolio: vi
				.fn()
				.mockResolvedValue({ state: "", items: [] }),
			getCumulativeStateTimeCandidatesForTeam: vi
				.fn()
				.mockResolvedValue({ items: [] }),
			getCumulativeStateTimeCandidatesForPortfolio: vi
				.fn()
				.mockResolvedValue({ items: [] }),
			getFlowEfficiencyInfoForTeam: vi.fn(),
			getFlowEfficiencyInfoForPortfolio: vi.fn(),
			getBlockedCountHistory: vi.fn().mockResolvedValue([]),
			getBlockedItemsAtDate: vi.fn().mockResolvedValue([]),
		};

		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={errorMetricsService}
				title="Features"
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for errors to be logged
		await waitFor(() => {
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting throughput:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting Work Items in progress:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error fetching Cycle Time data:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error fetching arrivals data:",
				expect.any(Error),
			);
		});

		// The component should still render the container structure; open popover and check
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		expect(
			await screen.findByTestId("date-range-selector"),
		).toBeInTheDocument();

		consoleSpy.mockRestore();
	});

	it("renders split overview widgets when features are provided (team context)", async () => {
		const teamMetricsService = {
			...createMockMetricsService<IWorkItem>(),
			getFeaturesInProgress: vi.fn().mockResolvedValue(mockInProgressItems),
		};

		// Overview category to see all overview widgets
		localStorage.setItem(
			`lighthouse:metrics:team:${mockTeam.id}:category`,
			"flow-overview",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={mockTeam}
				metricsService={teamMetricsService}
				title="Work Items"
				featuresInProgress={mockInProgressItems}
				featureWip={3}
				hasBlockedConfig={true}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("wip-overview-widget")).toBeInTheDocument();
			expect(screen.getByTestId("blocked-overview-widget")).toBeInTheDocument();
			expect(
				screen.getByTestId("features-worked-on-widget"),
			).toBeInTheDocument();
			expect(screen.getByTestId("features-worked-on-count")).toHaveTextContent(
				"2",
			);
			expect(screen.getByTestId("features-worked-on-wip")).toHaveTextContent(
				"3",
			);
		});
	});

	it("counts stale items in the stale overview widget excluding blocked items", async () => {
		const tenDaysAgo = new Date();
		tenDaysAgo.setDate(tenDaysAgo.getDate() - 10);

		const staleScenarioItems: IWorkItem[] = [
			{
				...mockInProgressItems[0],
				id: 11,
				isBlocked: false,
				currentStateEnteredAt: tenDaysAgo,
			},
			{
				...mockInProgressItems[0],
				id: 12,
				isBlocked: false,
				currentStateEnteredAt: tenDaysAgo,
			},
			{
				...mockInProgressItems[0],
				id: 13,
				isBlocked: true,
				currentStateEnteredAt: tenDaysAgo,
			},
		];

		const team = new Team();
		team.name = "Stale Team";
		team.id = 77;
		team.systemWIPLimit = 6;
		team.lastUpdated = new Date();

		const teamMetricsService = {
			...createMockMetricsService<IWorkItem>(),
			getFeaturesInProgress: vi.fn().mockResolvedValue([]),
			getInProgressItems: vi.fn().mockResolvedValue(staleScenarioItems),
		};

		localStorage.setItem(
			`lighthouse:metrics:team:${team.id}:category`,
			"flow-overview",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={team}
				metricsService={teamMetricsService}
				title="Work Items"
				doingStates={["To Do", "In Progress", "Review"]}
				stalenessThresholdDays={1}
				blockedStalenessThresholdDays={0}
			/>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("stale-overview-count")).toHaveTextContent("2");
		});

		expect(
			screen.getByTestId("widget-view-data-count-staleOverview"),
		).toHaveTextContent("2");
		expect(screen.getByTestId("widget-rag-staleOverview")).toHaveTextContent(
			"red",
		);
	});

	it("counts blocked items across To Do and In Progress, not just the WIP set", async () => {
		// Two blocked items come back from the blocked-eligible endpoint (one In Progress, one still
		// in To Do — blocked before it started), but only one is in the WIP (in-progress) set. The
		// overview count must be 2, proving it is not derived from WIP alone (the reported bug).
		const blockedAcrossCategories: IWorkItem[] = [
			{ ...mockInProgressItems[0], id: 31, isBlocked: true },
			{ ...mockInProgressItems[0], id: 32, isBlocked: true },
		];

		const team = new Team();
		team.name = "Blocked Category Team";
		team.id = 76;
		team.systemWIPLimit = 6;
		team.lastUpdated = new Date();

		const teamMetricsService = {
			...createMockMetricsService<IWorkItem>(),
			getFeaturesInProgress: vi.fn().mockResolvedValue([]),
			getInProgressItems: vi
				.fn()
				.mockResolvedValue([blockedAcrossCategories[0]]),
			getBlockedItemsAtDate: vi.fn().mockResolvedValue(blockedAcrossCategories),
		};

		localStorage.setItem(
			`lighthouse:metrics:team:${team.id}:category`,
			"flow-overview",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={team}
				metricsService={teamMetricsService}
				title="Work Items"
				doingStates={["To Do", "In Progress", "Review"]}
				hasBlockedConfig={true}
			/>,
		);

		await waitFor(() => {
			expect(screen.getByTestId("blocked-overview-count")).toHaveTextContent(
				"2",
			);
		});
	});

	it("the blocked-over-time widget renders the rag-status chip driven by max blocked age", async () => {
		const fifteenDaysAgo = new Date();
		fifteenDaysAgo.setDate(fifteenDaysAgo.getDate() - 15);

		// A single blocked item — the max-blocked-age RAG on the over-time widget must be
		// RED because it has been blocked 15 days, past the 10-day staleness threshold.
		// The overview count tile keeps its own count-based RAG (covered in ragRules.test.ts).
		const blockedScenarioItems: IWorkItem[] = [
			{
				...mockInProgressItems[0],
				id: 21,
				isBlocked: true,
				blockedSince: fifteenDaysAgo.toISOString(),
			},
		];

		const team = new Team();
		team.name = "Blocked Age Team";
		team.id = 88;
		team.systemWIPLimit = 6;
		team.lastUpdated = new Date();

		const teamMetricsService = {
			...createMockMetricsService<IWorkItem>(),
			getFeaturesInProgress: vi.fn().mockResolvedValue([]),
			getInProgressItems: vi.fn().mockResolvedValue(blockedScenarioItems),
			// The blocked overview/RAG sources its items from the blocked-eligible (To Do + In
			// Progress) endpoint, so the aged blocked item must come back from here.
			getBlockedItemsAtDate: vi.fn().mockResolvedValue(blockedScenarioItems),
		};

		localStorage.setItem(
			`lighthouse:metrics:team:${team.id}:category`,
			"flow-metrics",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={team}
				metricsService={teamMetricsService}
				title="Work Items"
				doingStates={["To Do", "In Progress", "Review"]}
				hasBlockedConfig={true}
				blockedStalenessThresholdDays={10}
			/>,
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("widget-rag-blockedCountHistory"),
			).toHaveTextContent("red");
		});
	});

	it("renders flow overview trend chrome for team overview widgets", async () => {
		const team = new Team();
		team.name = "Trend Team";
		team.id = 99;
		team.featureWip = 3;
		team.systemWIPLimit = 6;
		team.lastUpdated = new Date();
		team.serviceLevelExpectationProbability = 80;
		team.serviceLevelExpectationRange = 10;

		const teamMetricsService = {
			...createMockMetricsService<IWorkItem>(),
			getFeaturesInProgress: vi.fn().mockResolvedValue(mockInProgressItems),
			getWipOverviewInfo: vi.fn().mockResolvedValue({
				count: 2,
				comparison: { direction: "up", metricLabel: "WIP" },
			}),
			getFeaturesWorkedOnInfo: vi.fn().mockResolvedValue({
				count: 2,
				comparison: {
					direction: "flat",
					metricLabel: "Features Being Worked On",
				},
			}),
			getTotalWorkItemAgeInfo: vi.fn().mockResolvedValue({
				totalAge: 18,
				comparison: {
					direction: "down",
					metricLabel: "Total Work Item Age",
				},
			}),
			getPredictabilityScoreInfo: vi.fn().mockResolvedValue({
				score: 0.53,
				comparison: {
					direction: "up",
					metricLabel: "Predictability Score",
				},
			}),
			getThroughputInfo: vi.fn().mockResolvedValue({
				total: 8,
				dailyAverage: 0.8,
				comparison: { direction: "up", metricLabel: "Total Throughput" },
			}),
			getArrivalsInfo: vi.fn().mockResolvedValue({
				total: 10,
				dailyAverage: 1,
				comparison: { direction: "down", metricLabel: "Total Arrivals" },
			}),
		};

		localStorage.setItem(
			`lighthouse:metrics:team:${team.id}:category`,
			"flow-overview",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={team}
				metricsService={teamMetricsService}
				title="Work Items"
				featuresInProgress={mockInProgressItems}
				featureWip={3}
				hasBlockedConfig={true}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("widget-trend-direction-wipOverview"),
			).toHaveTextContent("up");
			expect(
				screen.getByTestId("widget-trend-direction-featuresWorkedOnOverview"),
			).toHaveTextContent("flat");
			expect(
				screen.getByTestId("widget-trend-direction-totalWorkItemAge"),
			).toHaveTextContent("down");
			expect(
				screen.getByTestId("widget-trend-direction-predictabilityScore"),
			).toHaveTextContent("up");
			expect(
				screen.getByTestId("widget-trend-direction-totalThroughput"),
			).toHaveTextContent("up");
			expect(
				screen.getByTestId("widget-trend-direction-totalArrivals"),
			).toHaveTextContent("down");
		});
	});

	it("the blockedOverview widget renders the widget-trend-* chrome when a trend payload is supplied", async () => {
		const dateDaysAgo = (days: number): string => {
			const date = new Date();
			date.setDate(date.getDate() - days);
			return date.toISOString().split("T")[0];
		};

		// One snapshot on/before the previous-period boundary (baseline) and one
		// within the current period (current) → a non-"none" previous-period trend.
		const blockedCountHistory = [
			{ recordedAt: dateDaysAgo(40), blockedCount: 2 },
			{ recordedAt: dateDaysAgo(0), blockedCount: 5 },
		];

		const team = new Team();
		team.name = "Blocked Trend Team";
		team.id = 111;
		team.systemWIPLimit = 6;
		team.lastUpdated = new Date();

		const teamMetricsService = {
			...createMockMetricsService<IWorkItem>(),
			getFeaturesInProgress: vi.fn().mockResolvedValue([]),
			getBlockedCountHistory: vi.fn().mockResolvedValue(blockedCountHistory),
		};

		localStorage.setItem(
			`lighthouse:metrics:team:${team.id}:category`,
			"flow-overview",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={team}
				metricsService={teamMetricsService}
				title="Work Items"
				hasBlockedConfig={true}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("widget-trend-blockedOverview"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("widget-trend-direction-blockedOverview"),
			).toHaveTextContent("up");
		});
	});

	it("renders predictability score trend for portfolio flow overview", async () => {
		const project = new Portfolio();
		project.name = "Trend Project";
		project.id = 100;
		project.lastUpdated = new Date();
		project.systemWIPLimit = 6;
		project.serviceLevelExpectationProbability = 85;
		project.serviceLevelExpectationRange = 14;

		const projectMetricsService = {
			...createMockMetricsService<IFeature>(),
			getPredictabilityScoreInfo: vi.fn().mockResolvedValue({
				score: 0.53,
				comparison: {
					direction: "up",
					metricLabel: "Predictability Score",
				},
			}),
		};

		localStorage.setItem(
			`lighthouse:metrics:portfolio:${project.id}:category`,
			"flow-overview",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={project}
				metricsService={projectMetricsService}
				title="Features"
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("widget-trend-direction-predictabilityScore"),
			).toHaveTextContent("up");
		});
	});

	it("renders overview rag chips for total throughput and arrivals", async () => {
		const team = new Team();
		team.name = "Rag Team";
		team.id = 101;
		team.featureWip = 3;
		team.systemWIPLimit = 6;
		team.lastUpdated = new Date();
		team.serviceLevelExpectationProbability = 80;
		team.serviceLevelExpectationRange = 10;

		localStorage.setItem(
			`lighthouse:metrics:team:${team.id}:category`,
			"flow-overview",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={team}
				metricsService={createMockMetricsService<IWorkItem>()}
				title="Work Items"
				hasBlockedConfig={true}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		await waitFor(() => {
			expect(
				screen.getByTestId("widget-rag-totalThroughput"),
			).toHaveTextContent("red");
			expect(screen.getByTestId("widget-rag-totalArrivals")).toHaveTextContent(
				"red",
			);
		});
	});

	it("doesn't set serviceLevelExpectation when entity lacks SLE values", async () => {
		const projectWithoutSLE = new Portfolio();
		projectWithoutSLE.id = 5;
		projectWithoutSLE.name = "Project without SLE";
		projectWithoutSLE.lastUpdated = new Date();
		projectWithoutSLE.serviceLevelExpectationProbability = 0;
		projectWithoutSLE.serviceLevelExpectationRange = 0;

		// cycleScatter is in flow-metrics category where SLE testid is visible
		localStorage.setItem(
			`lighthouse:metrics:portfolio:${projectWithoutSLE.id}:category`,
			"flow-metrics",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={projectWithoutSLE}
				metricsService={mockMetricsService}
				title="Features"
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for the component to render without SLE value
		await waitFor(() => {
			expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
				"none",
			);
		});
	});

	it("doesn't set serviceLevelExpectation when entity has partial SLE values", async () => {
		const projectWithPartialSLE = new Portfolio();
		projectWithPartialSLE.id = 6;
		projectWithPartialSLE.name = "Project with partial SLE";
		projectWithPartialSLE.lastUpdated = new Date();
		projectWithPartialSLE.serviceLevelExpectationProbability = 85;
		projectWithPartialSLE.serviceLevelExpectationRange = 0;

		// cycleScatter is in flow-metrics category where SLE testid is visible
		localStorage.setItem(
			`lighthouse:metrics:portfolio:${projectWithPartialSLE.id}:category`,
			"flow-metrics",
		);

		renderWithRouter(
			<BaseMetricsView
				entity={projectWithPartialSLE}
				metricsService={mockMetricsService}
				title="Features"
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for the component to render without SLE value
		await waitFor(() => {
			expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
				"none",
			);
		});
	});

	describe("Predictability Score functionality", () => {
		it("fetches predictability data on initial load", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Check that predictability data is fetched
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledWith(
					mockProject.id,
					expect.any(Date),
					expect.any(Date),
				);
			});
		});

		it("refetches predictability data when start date changes", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for initial load
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledTimes(1);
			});

			// Reset mock call counts
			vi.clearAllMocks();

			// Open date-range popover then change start date
			fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
			await screen.findByTestId("start-date");
			// Change start date
			fireEvent.click(screen.getByTestId("change-start-date"));

			// Verify predictability data is fetched again with new date
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledTimes(1);
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledWith(
					mockProject.id,
					expect.any(Date),
					expect.any(Date),
				);
			});
		});

		it("refetches predictability data when end date changes", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for initial load
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledTimes(1);
			});

			// Reset mock call counts
			vi.clearAllMocks();

			// Open date-range popover then change end date
			fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
			await screen.findByTestId("start-date");
			// Change end date
			fireEvent.click(screen.getByTestId("change-end-date"));

			// Verify predictability data is fetched again with new date
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledTimes(1);
			});
		});

		it("refetches predictability data when entity changes", async () => {
			const { rerender } = renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for initial load
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledTimes(1);
			});

			// Reset mock call counts
			vi.clearAllMocks();

			// Change entity - must wrap in MemoryRouter for rerender
			rerender(
				<MemoryRouter>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			// Verify predictability data is fetched again with new entity
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledTimes(1);
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledWith(mockTeam.id, expect.any(Date), expect.any(Date));
			});
		});
		it("handles predictability data fetch errors gracefully", async () => {
			const errorPredictabilityService: IMetricsService<IWorkItem> = {
				...mockMetricsService,
				getMultiItemForecastPredictabilityScore: vi
					.fn()
					.mockRejectedValue(new Error("Predictability API error")),
			};

			const consoleSpy = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={errorPredictabilityService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for error to be logged
			await waitFor(() => {
				expect(consoleSpy).toHaveBeenCalledWith(
					"Error fetching predictability data:",
					expect.any(Error),
				);
			});

			consoleSpy.mockRestore();
		});

		it("handles null predictability data correctly", async () => {
			const nullPredictabilityService: IMetricsService<IWorkItem> = {
				...mockMetricsService,
				getMultiItemForecastPredictabilityScore: vi
					.fn()
					.mockResolvedValue(null),
			};

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={nullPredictabilityService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Component should render without errors when predictability data is null
			await waitFor(() => {
				expect(
					nullPredictabilityService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalled();
			});
		});

		it("calls predictability service with correct date parameters", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for service call and verify date parameters
			await waitFor(() => {
				expect(
					mockMetricsService.getMultiItemForecastPredictabilityScore,
				).toHaveBeenCalledWith(
					mockProject.id,
					expect.any(Date),
					expect.any(Date),
				);

				// Get the actual call arguments
				const calls = (
					mockMetricsService.getMultiItemForecastPredictabilityScore as Mock
				).mock.calls;
				expect(calls[0][0]).toBe(mockProject.id);
				expect(calls[0][1]).toBeInstanceOf(Date);
				expect(calls[0][2]).toBeInstanceOf(Date);
			});
		});

		it("includes predictability data fetch in error handling test", async () => {
			// This test verifies that the existing error handling test includes predictability data
			const errorMetricsService: IMetricsService<IWorkItem> = {
				getThroughput: vi.fn().mockRejectedValue(new Error("API error")),
				getWorkInProgressOverTime: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getInProgressItems: vi.fn().mockRejectedValue(new Error("API error")),
				getCycleTimeData: vi.fn().mockRejectedValue(new Error("API error")),
				getCycleTimePercentiles: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getWorkItemAgePercentiles: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getAgeInStatePercentiles: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getMultiItemForecastPredictabilityScore: vi
					.fn()
					.mockRejectedValue(new Error("Predictability API error")),
				getTotalWorkItemAge: vi.fn().mockRejectedValue(new Error("API error")),
				getThroughputPbc: vi.fn().mockRejectedValue(new Error("API error")),
				getWipPbc: vi.fn().mockRejectedValue(new Error("API error")),
				getTotalWorkItemAgePbc: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getCycleTimePbc: vi.fn().mockRejectedValue(new Error("API error")),
				getEstimationVsCycleTimeData: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getArrivals: vi.fn().mockRejectedValue(new Error("API error")),
				getArrivalsPbc: vi.fn().mockRejectedValue(new Error("API error")),
				getThroughputInfo: vi.fn().mockRejectedValue(new Error("API error")),
				getArrivalsInfo: vi.fn().mockRejectedValue(new Error("API error")),
				getWipOverviewInfo: vi.fn().mockRejectedValue(new Error("API error")),
				getTotalWorkItemAgeInfo: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getPredictabilityScoreInfo: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getCycleTimePercentilesInfo: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getCumulativeStateTimeForTeam: vi
					.fn()
					.mockResolvedValue({ states: [] }),
				getCumulativeStateTimeItemsForTeam: vi
					.fn()
					.mockResolvedValue({ state: "", items: [] }),
				getCumulativeStateTimeItemsForPortfolio: vi
					.fn()
					.mockResolvedValue({ state: "", items: [] }),
				getCumulativeStateTimeCandidatesForTeam: vi
					.fn()
					.mockResolvedValue({ items: [] }),
				getCumulativeStateTimeCandidatesForPortfolio: vi
					.fn()
					.mockResolvedValue({ items: [] }),
				getFlowEfficiencyInfoForTeam: vi.fn(),
				getFlowEfficiencyInfoForPortfolio: vi.fn(),
				getBlockedCountHistory: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
				getBlockedItemsAtDate: vi
					.fn()
					.mockRejectedValue(new Error("API error")),
			};

			const consoleSpy = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={errorMetricsService}
					title="Features"
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for predictability error to be logged
			await waitFor(() => {
				expect(consoleSpy).toHaveBeenCalledWith(
					"Error fetching predictability data:",
					expect.any(Error),
				);
			});

			consoleSpy.mockRestore();
		});
	});

	describe("WorkDistributionChart functionality", () => {
		beforeEach(() => {
			// workDistribution is in the portfolio category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"portfolio",
			);
			// mockMetricsService lacks getFeaturesInProgress so ownerType is "portfolio" even for mockTeam
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockTeam.id}:category`,
				"portfolio",
			);
		});

		it("renders WorkDistributionChart with combined cycle time and in-progress data", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched and WorkDistributionChart to render
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
				// Verify the chart receives the correct title
				expect(screen.getByTestId("distribution-title")).toHaveTextContent(
					"Work Distribution",
				);
				// Verify the chart receives combined data (cycleTimeData + inProgressItems)
				// mockCycleTimeData has 2 items, mockInProgressItems has 2 items = 4 total
				expect(
					screen.getByTestId("distribution-work-items-count"),
				).toHaveTextContent("4");
			});
		});

		it("passes correct combined data when cycle time data changes", async () => {
			const customCycleTimeData: IWorkItem[] = [
				{
					id: 5,
					name: "Item 5",
					state: "Done",
					stateCategory: "Done" as StateCategory,
					type: "Story",
					referenceId: "ITEM-5",
					url: "https://example.com/work/5",
					startedDate: new Date("2023-01-01"),
					closedDate: new Date("2023-01-08"),
					cycleTime: 7,
					workItemAge: 7,
					parentWorkItemReference: "PARENT-1",
					isBlocked: false,
				},
				{
					id: 6,
					name: "Item 6",
					state: "Done",
					stateCategory: "Done" as StateCategory,
					type: "Bug",
					referenceId: "ITEM-6",
					url: "https://example.com/work/6",
					startedDate: new Date("2023-01-02"),
					closedDate: new Date("2023-01-12"),
					cycleTime: 10,
					workItemAge: 10,
					parentWorkItemReference: "PARENT-2",
					isBlocked: false,
				},
				{
					id: 7,
					name: "Item 7",
					state: "Done",
					stateCategory: "Done" as StateCategory,
					type: "Task",
					referenceId: "ITEM-7",
					url: "https://example.com/work/7",
					startedDate: new Date("2023-01-03"),
					closedDate: new Date("2023-01-09"),
					cycleTime: 6,
					workItemAge: 6,
					parentWorkItemReference: "PARENT-1",
					isBlocked: false,
				},
			];

			const customMetricsService = createMockMetricsService<IWorkItem>();
			customMetricsService.getCycleTimeData = vi
				.fn()
				.mockResolvedValue(customCycleTimeData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={customMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
				// Verify the chart receives combined data
				// customCycleTimeData has 3 items, mockInProgressItems has 2 items = 5 total
				expect(
					screen.getByTestId("distribution-work-items-count"),
				).toHaveTextContent("5");
			});
		});

		it("passes correct combined data when in-progress items change", async () => {
			const customInProgressItems: IWorkItem[] = [
				{
					id: 8,
					name: "Item 8",
					state: "In Progress",
					stateCategory: "Doing" as StateCategory,
					type: "Story",
					referenceId: "ITEM-8",
					url: "https://example.com/work/8",
					startedDate: new Date("2023-01-01"),
					closedDate: new Date(),
					cycleTime: 0,
					workItemAge: 15,
					parentWorkItemReference: "PARENT-3",
					isBlocked: false,
				},
			];

			const customMetricsService = createMockMetricsService<IWorkItem>();
			customMetricsService.getInProgressItems = vi
				.fn()
				.mockResolvedValue(customInProgressItems);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={customMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
			});

			// Verify the chart receives combined data
			// mockCycleTimeData has 2 items, customInProgressItems has 1 item = 3 total
			expect(
				screen.getByTestId("distribution-work-items-count"),
			).toHaveTextContent("3");
		});

		it("handles empty cycle time data", async () => {
			const emptyMetricsService = createMockMetricsService<IWorkItem>();
			emptyMetricsService.getCycleTimeData = vi.fn().mockResolvedValue([]);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={emptyMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
			});

			// Verify the chart receives only in-progress items
			// Empty cycleTimeData + mockInProgressItems (2 items) = 2 total
			expect(
				screen.getByTestId("distribution-work-items-count"),
			).toHaveTextContent("2");
		});

		it("handles empty in-progress items", async () => {
			const emptyProgressMetricsService = createMockMetricsService<IWorkItem>();
			emptyProgressMetricsService.getInProgressItems = vi
				.fn()
				.mockResolvedValue([]);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={emptyProgressMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
			});

			// Verify the chart receives only cycle time data
			// mockCycleTimeData (2 items) + empty inProgressItems = 2 total
			expect(
				screen.getByTestId("distribution-work-items-count"),
			).toHaveTextContent("2");
		});

		it("handles both empty cycle time and in-progress data", async () => {
			const emptyMetricsService = createMockMetricsService<IWorkItem>();
			emptyMetricsService.getCycleTimeData = vi.fn().mockResolvedValue([]);
			emptyMetricsService.getInProgressItems = vi.fn().mockResolvedValue([]);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={emptyMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
			});

			// Verify the chart receives empty data
			expect(
				screen.getByTestId("distribution-work-items-count"),
			).toHaveTextContent("0");
		});

		it("updates WorkDistributionChart when date range changes", async () => {
			const updateableService = createMockMetricsService<IWorkItem>();
			let callCount = 0;

			// Mock to return different data on subsequent calls
			updateableService.getCycleTimeData = vi.fn().mockImplementation(() => {
				callCount++;
				if (callCount === 1) {
					return Promise.resolve(mockCycleTimeData); // 2 items
				}
				// Return more items on second call
				return Promise.resolve([
					...mockCycleTimeData,
					{
						id: 10,
						name: "Item 10",
						state: "Done",
						stateCategory: "Done" as StateCategory,
						type: "Story",
						referenceId: "ITEM-10",
						url: "https://example.com/work/10",
						startedDate: new Date("2023-01-01"),
						closedDate: new Date("2023-01-11"),
						cycleTime: 10,
						workItemAge: 10,
						parentWorkItemReference: "PARENT-4",
						isBlocked: false,
					},
				]);
			});

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={updateableService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for initial data
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
				// Initial: 2 cycle time + 2 in progress = 4
				expect(
					screen.getByTestId("distribution-work-items-count"),
				).toHaveTextContent("4");
			});

			// Change date range
			fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
			await screen.findByTestId("start-date");
			fireEvent.click(screen.getByTestId("change-start-date"));

			// Wait for updated data
			await waitFor(() => {
				// After update: 3 cycle time + 2 in progress = 5
				expect(
					screen.getByTestId("distribution-work-items-count"),
				).toHaveTextContent("5");
			});
		});

		it("displays work distribution chart with Team entity", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={mockMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
			});

			// Verify the chart is rendered with correct data
			expect(screen.getByTestId("distribution-title")).toHaveTextContent(
				"Work Distribution",
			);
			expect(
				screen.getByTestId("distribution-work-items-count"),
			).toHaveTextContent("4"); // 2 cycle time + 2 in progress
		});

		it("properly types work items as IWorkItem[] when passing to chart", async () => {
			// This test verifies type casting works correctly with generic T
			const featureMetricsService = createMockMetricsService<IFeature>();
			const mockFeatureData = mockCycleTimeData.map((item) => ({
				...item,
				size: 5,
			}));
			featureMetricsService.getCycleTimeData = vi
				.fn()
				.mockResolvedValue(mockFeatureData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={featureMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched
			await waitFor(() => {
				expect(
					screen.getByTestId("work-distribution-chart"),
				).toBeInTheDocument();
			});

			// Verify the chart receives the data despite type being T (IFeature)
			// Cast to IWorkItem[] should work since IFeature extends IWorkItem
			expect(
				screen.getByTestId("distribution-work-items-count"),
			).toHaveTextContent("4");
		});
	});

	describe("URL State Management", () => {
		it("initializes dates from URL parameters when provided", async () => {
			const initialRoute =
				"/teams/1/metrics?startDate=2025-01-01&endDate=2025-01-31";

			render(
				<MemoryRouter initialEntries={[initialRoute]}>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			// Wait for component to render
			await waitFor(() => {
				expect(screen.getByTestId("start-date")).toBeInTheDocument();
			});

			// Verify dates are initialized from URL
			const startDateElement = screen.getByTestId("start-date");
			const endDateElement = screen.getByTestId("end-date");

			expect(startDateElement.textContent).toContain("2025-01-01");
			expect(endDateElement.textContent).toContain("2025-01-31");
		});

		it("falls back to default dates when URL parameters are missing", async () => {
			const initialRoute = "/teams/1/metrics"; // No date params

			render(
				<MemoryRouter initialEntries={[initialRoute]}>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("start-date")).toBeInTheDocument();
			});

			const startDateElement = screen.getByTestId("start-date");
			const endDateElement = screen.getByTestId("end-date");

			// Should use default: 30 days ago and today
			const expectedStart = new Date();
			expectedStart.setDate(expectedStart.getDate() - 30);

			const startDateText = startDateElement.textContent || "";
			const endDateText = endDateElement.textContent || "";

			// Verify the dates are set (exact values depend on test execution time)
			expect(startDateText).toBeTruthy();
			expect(endDateText).toBeTruthy();

			// The end date should be close to today
			const endDate = new Date(endDateText);
			const now = new Date();
			const diffInDays = Math.abs(
				(endDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24),
			);
			expect(diffInDays).toBeLessThan(1); // Within 1 day
		});

		it("falls back to defaults when URL parameters contain invalid dates", async () => {
			const initialRoute =
				"/teams/1/metrics?startDate=invalid-date&endDate=not-a-date";

			render(
				<MemoryRouter initialEntries={[initialRoute]}>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("start-date")).toBeInTheDocument();
			});

			// Should fall back to defaults (same behavior as missing params)
			const startDateElement = screen.getByTestId("start-date");
			const endDateElement = screen.getByTestId("end-date");

			expect(startDateElement.textContent).toBeTruthy();
			expect(endDateElement.textContent).toBeTruthy();
		});

		it("updates URL when start date changes", async () => {
			render(
				<MemoryRouter initialEntries={["/teams/1/metrics"]}>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("change-start-date")).toBeInTheDocument();
			});

			// Get initial dates
			const initialStartDate = screen.getByTestId("start-date").textContent;

			// Change start date
			fireEvent.click(screen.getByTestId("change-start-date"));

			// Wait for update
			await waitFor(() => {
				const newStartDate = screen.getByTestId("start-date").textContent;
				expect(newStartDate).not.toBe(initialStartDate);
			});

			// Verify the date changed (the mock DateRangeSelector subtracts 30 days)
			const newStartDate = screen.getByTestId("start-date").textContent;
			expect(newStartDate).not.toBe(initialStartDate);
		});

		it("updates URL when end date changes", async () => {
			render(
				<MemoryRouter initialEntries={["/teams/1/metrics"]}>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("change-end-date")).toBeInTheDocument();
			});

			// Get initial state
			const initialEndDate = screen.getByTestId("end-date").textContent;

			// Change end date
			fireEvent.click(screen.getByTestId("change-end-date"));

			// Wait for update and verify the date changed
			await waitFor(() => {
				const newEndDate = screen.getByTestId("end-date").textContent;
				expect(newEndDate).not.toBe(initialEndDate);
			});
		});

		it("sets both date parameters in URL when either date changes", async () => {
			const initialRoute = "/teams/1/metrics";

			render(
				<MemoryRouter initialEntries={[initialRoute]}>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={30}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("change-start-date")).toBeInTheDocument();
			});

			// Get initial dates
			const initialStartDate = screen.getByTestId("start-date").textContent;

			// Change start date
			fireEvent.click(screen.getByTestId("change-start-date"));

			// Wait for both dates to be present in the display
			await waitFor(() => {
				const newStartDate = screen.getByTestId("start-date").textContent;
				expect(newStartDate).not.toBe(initialStartDate);
			});

			// Both dates should still be displayed (URL should have both params)
			expect(screen.getByTestId("start-date").textContent).toBeTruthy();
			expect(screen.getByTestId("end-date").textContent).toBeTruthy();
		});

		it("uses custom defaultDateRange when specified", async () => {
			const customDateRange = 60;
			const initialRoute = "/teams/1/metrics"; // No date params

			render(
				<MemoryRouter initialEntries={[initialRoute]}>
					<BaseMetricsView
						entity={mockTeam}
						metricsService={mockMetricsService}
						title="Work Items"
						defaultDateRange={customDateRange}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</MemoryRouter>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("start-date")).toBeInTheDocument();
			});

			const startDateElement = screen.getByTestId("start-date");
			const endDateElement = screen.getByTestId("end-date");

			// Calculate expected start date (60 days ago)
			const expectedStart = new Date();
			expectedStart.setDate(expectedStart.getDate() - customDateRange);

			const actualStart = new Date(startDateElement.textContent || "");
			const actualEnd = new Date(endDateElement.textContent || "");

			// Verify the start date is approximately 60 days ago
			const diffInDays =
				(actualEnd.getTime() - actualStart.getTime()) / (1000 * 60 * 60 * 24);
			expect(Math.abs(diffInDays - customDateRange)).toBeLessThan(1); // Within 1 day tolerance
		});
	});

	describe("M3 RAG Footers — Flow Throughput and Cycle Widgets", () => {
		beforeEach(() => {
			// throughput/cycleScatter/stacked widgets are in flow-metrics category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);
			// mockMetricsService lacks getFeaturesInProgress so ownerType is "portfolio" even for mockTeam
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockTeam.id}:category`,
				"flow-metrics",
			);
		});

		it("shows green RAG for throughput when no consecutive zero periods", async () => {
			// Default mock: throughputData = [3, 5] → no zeros → Green
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-throughput"),
				).toBeInTheDocument();
				expect(screen.getByTestId("widget-rag-throughput")).toHaveTextContent(
					"green",
				);
			});
		});

		it("shows red RAG for throughput when consecutive zeros exist", async () => {
			const zeroThroughputData = new RunChartData(
				generateWorkItemMapForRunChart([0, 0, 0, 0, 5]),
				5,
				5,
			);
			const svc = createMockMetricsService<IWorkItem>();
			svc.getThroughput = vi.fn().mockResolvedValue(zeroThroughputData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={svc}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("widget-rag-throughput")).toHaveTextContent(
					"red",
				);
			});
		});

		it("shows green RAG for cycle time percentiles when actual is below SLE", async () => {
			// SLE = {85, 14}, percentiles has {85, 12} → (12-14)/14 = -14% → Green
			// percentiles widget is in flow-overview category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-overview",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-percentiles"),
				).toBeInTheDocument();
				expect(screen.getByTestId("widget-rag-percentiles")).toHaveTextContent(
					"green",
				);
			});
		});

		it("shows red RAG for cycle time percentiles when actual exceeds SLE by >15%", async () => {
			// SLE = {85, 14}, create percentiles where 85th = 17 → (17-14)/14 = 21% → Red
			// percentiles widget is in flow-overview category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-overview",
			);

			const svc = createMockMetricsService<IWorkItem>();
			svc.getCycleTimePercentiles = vi.fn().mockResolvedValue([
				{ percentile: 50, value: 9 },
				{ percentile: 85, value: 17 },
				{ percentile: 95, value: 20 },
			]);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={svc}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("widget-rag-percentiles")).toHaveTextContent(
					"red",
				);
			});
		});

		it("shows green RAG for cycle time scatterplot when all items within SLE", async () => {
			// SLE value = 14, cycleTimes = [9, 10] → 0% above → Green
			// cycleScatter is in flow-metrics category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-cycleScatter"),
				).toBeInTheDocument();
				expect(screen.getByTestId("widget-rag-cycleScatter")).toHaveTextContent(
					"green",
				);
			});
		});

		it("shows red RAG for total work item age when no WIP limit is set", async () => {
			// mockProject.systemWIPLimit = 0 → undefined → Red
			// totalWorkItemAge widget is in flow-overview category (default)
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-overview",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-totalWorkItemAge"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-rag-totalWorkItemAge"),
				).toHaveTextContent("red");
			});
		});

		it("shows green RAG for total work item age when within healthy range", async () => {
			const teamWithWipAndSle = new Team();
			teamWithWipAndSle.name = "Healthy Team";
			teamWithWipAndSle.id = 100;
			teamWithWipAndSle.systemWIPLimit = 5;
			teamWithWipAndSle.lastUpdated = new Date();
			teamWithWipAndSle.serviceLevelExpectationProbability = 85;
			teamWithWipAndSle.serviceLevelExpectationRange = 14;

			// ref = 5 * 14 = 70. totalAge = 20, currentWip = 2, tomorrow = 22 < 70 → Green
			const svc = createMockMetricsService<IWorkItem>();
			svc.getTotalWorkItemAge = vi.fn().mockResolvedValue(20);

			renderWithRouter(
				<BaseMetricsView
					entity={teamWithWipAndSle}
					metricsService={svc}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-totalWorkItemAge"),
				).toHaveTextContent("green");
			});
		});
	});

	describe("Process Behaviour Charts", () => {
		// PBC widgets are in the predictability category
		beforeEach(() => {
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"predictability",
			);
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockTeam.id}:category`,
				"predictability",
			);
		});

		const readyPbcData: ProcessBehaviourChartData = {
			status: "Ready",
			statusReason: "",
			xAxisKind: "Date",
			average: 5,
			upperNaturalProcessLimit: 10,
			lowerNaturalProcessLimit: 0,
			baselineConfigured: true,
			dataPoints: [
				{
					xValue: "2025-01-01",
					yValue: 5,
					specialCauses: ["None"],
					workItemIds: [1, 2],
				},
				{
					xValue: "2025-01-02",
					yValue: 6,
					specialCauses: ["None"],
					workItemIds: [3],
				},
			],
		};

		const insufficientDataPbcData: ProcessBehaviourChartData = {
			status: "InsufficientData",
			statusReason: "Need at least 8 data points in baseline period",
			xAxisKind: "Date",
			average: 0,
			upperNaturalProcessLimit: 0,
			lowerNaturalProcessLimit: 0,
			baselineConfigured: true,
			dataPoints: [],
		};

		it("shows PBC widgets even when status is BaselineMissing", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={mockMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// PBC widgets should be present even with BaselineMissing status
			await waitFor(() => {
				expect(
					screen.getByTestId("process-behaviour-chart-Throughput"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Work In Progress"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Total Work Item Age"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Cycle Time"),
				).toBeInTheDocument();
			});

			// Verify status is passed through
			expect(screen.getByTestId("pbc-status-Throughput")).toHaveTextContent(
				"BaselineMissing",
			);
		});

		it("shows PBC widgets when baseline is configured and data is ready", async () => {
			const pbcMetricsService = createMockMetricsService<IWorkItem>();
			pbcMetricsService.getThroughputPbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);
			pbcMetricsService.getWipPbc = vi.fn().mockResolvedValue(readyPbcData);
			pbcMetricsService.getTotalWorkItemAgePbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);
			pbcMetricsService.getCycleTimePbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={pbcMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("process-behaviour-chart-Throughput"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Work In Progress"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Total Work Item Age"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Cycle Time"),
				).toBeInTheDocument();
			});
		});

		it("shows PBC widgets with InsufficientData status (baseline is configured but not enough data)", async () => {
			const pbcMetricsService = createMockMetricsService<IWorkItem>();
			pbcMetricsService.getThroughputPbc = vi
				.fn()
				.mockResolvedValue(insufficientDataPbcData);
			pbcMetricsService.getWipPbc = vi
				.fn()
				.mockResolvedValue(insufficientDataPbcData);
			pbcMetricsService.getTotalWorkItemAgePbc = vi
				.fn()
				.mockResolvedValue(insufficientDataPbcData);
			pbcMetricsService.getCycleTimePbc = vi
				.fn()
				.mockResolvedValue(insufficientDataPbcData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={pbcMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("process-behaviour-chart-Throughput"),
				).toBeInTheDocument();
			});

			// Verify status is passed through
			expect(screen.getByTestId("pbc-status-Throughput")).toHaveTextContent(
				"InsufficientData",
			);
		});

		it("calls PBC service methods with correct parameters", async () => {
			const pbcMetricsService = createMockMetricsService<IWorkItem>();

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={pbcMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(pbcMetricsService.getThroughputPbc).toHaveBeenCalledWith(
					mockTeam.id,
					expect.any(Date),
					expect.any(Date),
				);
				expect(pbcMetricsService.getWipPbc).toHaveBeenCalledWith(
					mockTeam.id,
					expect.any(Date),
					expect.any(Date),
				);
				expect(pbcMetricsService.getTotalWorkItemAgePbc).toHaveBeenCalledWith(
					mockTeam.id,
					expect.any(Date),
					expect.any(Date),
				);
				expect(pbcMetricsService.getCycleTimePbc).toHaveBeenCalledWith(
					mockTeam.id,
					expect.any(Date),
					expect.any(Date),
				);
			});
		});

		it("shows PBC widgets for Portfolio entity when baseline is configured", async () => {
			const pbcMetricsService = createMockMetricsService<IFeature>();
			delete (pbcMetricsService as unknown as Record<string, unknown>)
				.getFeaturesInProgress;
			pbcMetricsService.getThroughputPbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);
			pbcMetricsService.getWipPbc = vi.fn().mockResolvedValue(readyPbcData);
			pbcMetricsService.getTotalWorkItemAgePbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);
			pbcMetricsService.getCycleTimePbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={pbcMetricsService}
					title="Features"
					defaultDateRange={90}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("process-behaviour-chart-Throughput"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Work In Progress"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Total Work Item Age"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Cycle Time"),
				).toBeInTheDocument();
			});
		});

		it("shows all PBC widgets regardless of status in mixed scenario", async () => {
			const pbcMetricsService = createMockMetricsService<IWorkItem>();
			pbcMetricsService.getThroughputPbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);
			pbcMetricsService.getWipPbc = vi
				.fn()
				.mockResolvedValue(baselineMissingPbcData);
			pbcMetricsService.getTotalWorkItemAgePbc = vi
				.fn()
				.mockResolvedValue(insufficientDataPbcData);
			pbcMetricsService.getCycleTimePbc = vi
				.fn()
				.mockResolvedValue(baselineMissingPbcData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={pbcMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				// All PBC widgets should be shown regardless of status
				expect(
					screen.getByTestId("process-behaviour-chart-Throughput"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Work In Progress"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Total Work Item Age"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("process-behaviour-chart-Cycle Time"),
				).toBeInTheDocument();
			});

			// Verify mixed statuses are passed through
			expect(screen.getByTestId("pbc-status-Throughput")).toHaveTextContent(
				"Ready",
			);
			expect(
				screen.getByTestId("pbc-status-Work In Progress"),
			).toHaveTextContent("BaselineMissing");
			expect(
				screen.getByTestId("pbc-status-Total Work Item Age"),
			).toHaveTextContent("InsufficientData");
		});

		it("passes correct data to PBC chart components", async () => {
			const pbcMetricsService = createMockMetricsService<IWorkItem>();
			pbcMetricsService.getThroughputPbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);
			pbcMetricsService.getWipPbc = vi.fn().mockResolvedValue(readyPbcData);
			pbcMetricsService.getTotalWorkItemAgePbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);
			pbcMetricsService.getCycleTimePbc = vi
				.fn()
				.mockResolvedValue(readyPbcData);

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={pbcMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("pbc-status-Throughput")).toHaveTextContent(
					"Ready",
				);
				expect(
					screen.getByTestId("pbc-data-points-Throughput"),
				).toHaveTextContent("2");
			});
		});

		it("renders the Raw/Filtered toggle on the Throughput PBC only when the team has a forecast filter", async () => {
			const conditions = [
				{
					fieldKey: "workitem.type" as const,
					operator: "equals" as const,
					value: "Bug",
				},
			];

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={mockMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
					hasForecastFilter={true}
					forecastFilterConditions={conditions}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("pbc-filter-toggle-Throughput"),
				).toBeInTheDocument();
			});

			expect(
				screen.queryByTestId("pbc-filter-toggle-Work In Progress"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("pbc-filter-toggle-Cycle Time"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("pbc-filter-toggle-Total Work Item Age"),
			).not.toBeInTheDocument();
		});

		it("refetches throughput PBC with view=filtered when the toggle flips to Filtered", async () => {
			const user = userEvent.setup();
			const conditions = [
				{
					fieldKey: "workitem.type" as const,
					operator: "equals" as const,
					value: "Bug",
				},
			];
			const pbcMetricsService = createMockMetricsService<IWorkItem>();

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={pbcMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
					hasForecastFilter={true}
					forecastFilterConditions={conditions}
				/>,
			);

			await waitFor(() => {
				expect(pbcMetricsService.getThroughputPbc).toHaveBeenCalledWith(
					mockTeam.id,
					expect.any(Date),
					expect.any(Date),
				);
			});

			(pbcMetricsService.getThroughputPbc as Mock).mockClear();

			await user.click(screen.getByLabelText(/use filtered throughput/i));

			await waitFor(() => {
				expect(pbcMetricsService.getThroughputPbc).toHaveBeenCalledWith(
					mockTeam.id,
					expect.any(Date),
					expect.any(Date),
					"filtered",
				);
			});
		});
	});

	describe("M4 RAG Footers — Aging and Flow Stability Widgets", () => {
		beforeEach(() => {
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockTeam.id}:category`,
				"flow-metrics",
			);
		});
		it("shows green RAG for aging chart when SLE and blocked config present and items healthy", async () => {
			// mockProject has SLE = {85, 14}. inProgressItems have workItemAge = 10, 8 (both below SLE and threshold)
			// aging widget is in flow-metrics category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					hasBlockedConfig={true}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("widget-header-aging")).toBeInTheDocument();
				expect(screen.getByTestId("widget-rag-aging")).toHaveTextContent(
					"green",
				);
			});
		});

		it("shows red RAG for aging chart when no blocked config", async () => {
			// aging widget is in flow-metrics category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					hasBlockedConfig={false}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("widget-rag-aging")).toHaveTextContent("red");
			});
		});

		it("shows red RAG for WIP over time when no WIP limit is set", async () => {
			// mockProject.systemWIPLimit = 0 → undefined → Red
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-wipOverTime"),
				).toBeInTheDocument();
				expect(screen.getByTestId("widget-rag-wipOverTime")).toHaveTextContent(
					"red",
				);
			});
		});

		it("shows green RAG for total age over time when start and end are equal", async () => {
			(mockMetricsService.getTotalWorkItemAgePbc as Mock).mockResolvedValue({
				dataPoints: [{ yValue: 50 }, { yValue: 40 }, { yValue: 50 }],

				upperControlLimit: 100,
				lowerControlLimit: 0,
				average: 50,
			});

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-totalWorkItemAgeOverTime"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-rag-totalWorkItemAgeOverTime"),
				).toHaveTextContent("green");
			});
		});

		it("shows red RAG for simplified CFD when no WIP limit", async () => {
			// stacked widget is in throughput category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);

			// Same as startedVsClosed → no WIP limit → Red
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("widget-header-stacked")).toBeInTheDocument();
				expect(screen.getByTestId("widget-rag-stacked")).toHaveTextContent(
					"red",
				);
			});
		});
	});

	describe("M5 RAG Footers — Portfolio and Correlation Widgets", () => {
		beforeEach(() => {
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"portfolio",
			);
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockTeam.id}:category`,
				"portfolio",
			);
		});
		it("shows red RAG for estimation vs cycle time when not configured", async () => {
			// Default mock returns status "NotConfigured" → widget node is null → won't render in dashboard
			// Use "Ready" status with no correlation data to get a red RAG for different reason
			const svc = createMockMetricsService<IWorkItem>();
			svc.getEstimationVsCycleTimeData = vi.fn().mockResolvedValue({
				status: "Ready",
				diagnostics: {
					totalCount: 4,
					mappedCount: 4,
					unmappedCount: 0,
					invalidCount: 0,
				},
				estimationUnit: "Story Points",
				useNonNumericEstimation: false,
				categoryValues: [],
				dataPoints: [
					{
						workItemIds: [1],
						estimationNumericValue: 10,
						estimationDisplayValue: "10",
						cycleTime: 2,
					},
					{
						workItemIds: [2],
						estimationNumericValue: 1,
						estimationDisplayValue: "1",
						cycleTime: 20,
					},
					{
						workItemIds: [3],
						estimationNumericValue: 8,
						estimationDisplayValue: "8",
						cycleTime: 1,
					},
					{
						workItemIds: [4],
						estimationNumericValue: 2,
						estimationDisplayValue: "2",
						cycleTime: 25,
					},
				],
			});

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={svc}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-estimationVsCycleTime"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-rag-estimationVsCycleTime"),
				).toHaveTextContent("red");
			});
		});

		it("shows green RAG for feature size when no percentile data is available", async () => {
			// sizePercentileValues is empty by default → no thresholds → green
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-featureSize"),
				).toBeInTheDocument();
				expect(screen.getByTestId("widget-rag-featureSize")).toHaveTextContent(
					"green",
				);
			});
		});

		it("shows green RAG for work distribution when zero items", async () => {
			const svc = createMockMetricsService<IWorkItem>();
			svc.getInProgressItems = vi.fn().mockResolvedValue([]);
			svc.getCycleTimeData = vi.fn().mockResolvedValue([]);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={svc}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-workDistribution"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-rag-workDistribution"),
				).toHaveTextContent("green");
			});
		});
	});

	describe("M6 RAG Footers — PBC Widgets", () => {
		beforeEach(() => {
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"predictability",
			);
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockTeam.id}:category`,
				"predictability",
			);
		});

		const readyPbcWithLargeChange: ProcessBehaviourChartData = {
			status: "Ready",
			statusReason: "",
			xAxisKind: "Date",
			average: 5,
			upperNaturalProcessLimit: 10,
			lowerNaturalProcessLimit: 0,
			baselineConfigured: true,
			dataPoints: [
				{
					xValue: "2025-01-01",
					yValue: 15,
					specialCauses: ["LargeChange"],
					workItemIds: [1],
				},
			],
		};

		const readyPbcGreen: ProcessBehaviourChartData = {
			status: "Ready",
			statusReason: "",
			xAxisKind: "Date",
			average: 5,
			upperNaturalProcessLimit: 10,
			lowerNaturalProcessLimit: 0,
			baselineConfigured: true,
			dataPoints: [
				{
					xValue: "2025-01-01",
					yValue: 5,
					specialCauses: ["None"],
					workItemIds: [1],
				},
			],
		};

		it("shows red RAG for PBC when baseline missing", async () => {
			// Default mocks have baselineMissingPbcData → Red
			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={mockMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-throughputPbc"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-rag-throughputPbc"),
				).toHaveTextContent("red");
			});
		});

		it("shows red RAG for PBC when LargeChange signal present", async () => {
			const svc = createMockMetricsService<IWorkItem>();
			svc.getThroughputPbc = vi.fn().mockResolvedValue(readyPbcWithLargeChange);

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={svc}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-throughputPbc"),
				).toHaveTextContent("red");
			});
		});

		it("shows green RAG for PBC when no special causes", async () => {
			const svc = createMockMetricsService<IWorkItem>();
			svc.getThroughputPbc = vi.fn().mockResolvedValue(readyPbcGreen);
			svc.getWipPbc = vi.fn().mockResolvedValue(readyPbcGreen);
			svc.getTotalWorkItemAgePbc = vi.fn().mockResolvedValue(readyPbcGreen);
			svc.getCycleTimePbc = vi.fn().mockResolvedValue(readyPbcGreen);

			renderWithRouter(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={svc}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-throughputPbc"),
				).toHaveTextContent("green");
				expect(screen.getByTestId("widget-rag-wipPbc")).toHaveTextContent(
					"green",
				);
				expect(
					screen.getByTestId("widget-rag-totalWorkItemAgePbc"),
				).toHaveTextContent("green");
				expect(screen.getByTestId("widget-rag-cycleTimePbc")).toHaveTextContent(
					"green",
				);
			});
		});
	});

	describe("Widget Info Metadata Integration", () => {
		it("passes info metadata through the widget shell for rendered widgets", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// flow-overview default category includes wipOverview which should have info
			await waitFor(() => {
				expect(
					screen.getByTestId("widget-info-wipOverview"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-info-desc-wipOverview"),
				).toHaveTextContent(/WIP/i);
				expect(
					screen.getByTestId("widget-info-link-wipOverview"),
				).toHaveAttribute("href", expect.stringContaining("widgets.html#"));
			});
		});
	});

	describe("Work Item Age Percentiles widget chrome", () => {
		// Regression: the workItemAgePercentiles widget shipped without any
		// chrome wiring, so WidgetShell rendered no header bar (no info button,
		// no view-data button). Per the docs this widget has info + view-data
		// but intentionally no status indicator and no trend.
		it("wires info and view-data chrome but no status badge or trend", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// flow-overview default category includes workItemAgePercentiles
			await waitFor(() => {
				expect(
					screen.getByTestId("widget-info-workItemAgePercentiles"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-info-link-workItemAgePercentiles"),
				).toHaveAttribute(
					"href",
					expect.stringContaining("widgets.html#work-item-age-percentiles"),
				);
				expect(
					screen.getByTestId("widget-view-data-workItemAgePercentiles"),
				).toBeInTheDocument();
			});

			// Documented behaviour: no RAG status indicator and no trend arrow
			expect(
				screen.queryByTestId("widget-rag-workItemAgePercentiles"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("widget-trend-workItemAgePercentiles"),
			).not.toBeInTheDocument();
		});
	});

	describe("View Data Shell Wiring", () => {
		it("passes viewData to cycle time percentiles widget", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// flow-overview default category includes percentiles
			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-percentiles"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-view-data-count-percentiles"),
				).toHaveTextContent("2");
			});
		});

		it("passes viewData to wip overview widget with in-progress items", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-wipOverview"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-view-data-count-wipOverview"),
				).toHaveTextContent("2");
			});
		});

		it("passes viewData to throughput widget", async () => {
			// throughput is in the throughput category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-throughput"),
				).toBeInTheDocument();
			});
		});

		it("passes viewData to cycle time scatter plot widget", async () => {
			// cycleScatter is in the flow-metrics category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-cycleScatter"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-view-data-count-cycleScatter"),
				).toHaveTextContent("2");
			});
		});

		it("passes viewData to work item aging widget", async () => {
			// aging is in the flow-metrics category
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-metrics",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-aging"),
				).toBeInTheDocument();
				expect(
					screen.getByTestId("widget-view-data-count-aging"),
				).toHaveTextContent("2");
			});
		});
	});

	describe("Cumulative state time item picker orchestration", () => {
		const balancedSystemicStates = {
			states: [
				{
					state: "Backlog",
					workflowOrder: 0,
					totalDays: 10,
					completedContributionDays: 6,
					ongoingContributionDays: 4,
					itemCount: 4,
					completedItemCount: 2,
					ongoingItemCount: 2,
					meanDays: 2.5,
					medianDays: 2,
				},
				{
					state: "Doing",
					workflowOrder: 1,
					totalDays: 10,
					completedContributionDays: 6,
					ongoingContributionDays: 4,
					itemCount: 4,
					completedItemCount: 2,
					ongoingItemCount: 2,
					meanDays: 2.5,
					medianDays: 2,
				},
				{
					state: "Review",
					workflowOrder: 2,
					totalDays: 10,
					completedContributionDays: 7,
					ongoingContributionDays: 3,
					itemCount: 4,
					completedItemCount: 3,
					ongoingItemCount: 1,
					meanDays: 2.5,
					medianDays: 2,
				},
			],
		};

		const lopsidedNarrowedStates = {
			states: [
				{
					state: "Doing",
					workflowOrder: 0,
					totalDays: 90,
					completedContributionDays: 80,
					ongoingContributionDays: 10,
					itemCount: 1,
					completedItemCount: 1,
					ongoingItemCount: 0,
					meanDays: 90,
					medianDays: 90,
				},
			],
		};

		const candidatesResponse = {
			items: [
				{
					workItemId: 42,
					referenceId: "PICK-42",
					title: "Pick me",
					workItemType: "Story",
				},
			],
		};

		const buildCumulativePickerService = () => {
			const getCumulativeStateTimeForTeam = vi
				.fn()
				.mockImplementation((_id, _start, _end, itemIds?: number[]) =>
					Promise.resolve(
						itemIds && itemIds.length > 0
							? lopsidedNarrowedStates
							: balancedSystemicStates,
					),
				);
			return {
				...createMockMetricsService<IWorkItem>(),
				getCumulativeStateTimeForTeam,
				getCumulativeStateTimeCandidatesForTeam: vi
					.fn()
					.mockResolvedValue(candidatesResponse),
				getCumulativeStateTimeCandidatesForPortfolio: vi
					.fn()
					.mockResolvedValue(candidatesResponse),
				getCumulativeStateTimeItemsForTeam: vi
					.fn()
					.mockResolvedValue({ state: "Doing", items: [] }),
				getCumulativeStateTimeItemsForPortfolio: vi
					.fn()
					.mockResolvedValue({ state: "Doing", items: [] }),
				getFlowEfficiencyInfoForTeam: vi.fn(),
				getFlowEfficiencyInfoForPortfolio: vi.fn(),
				getBlockedCountHistory: vi.fn().mockResolvedValue([]),
			};
		};

		it("threads waitStates and stateMappings into the cumulative chart", async () => {
			const team = new Team();
			team.name = "Wait Team";
			team.id = 777;
			team.systemWIPLimit = 6;
			team.lastUpdated = new Date();

			const service = buildCumulativePickerService();
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${team.id}:category`,
				"flow-metrics",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["Doing", "Review"]}
					waitStates={["Review", "Waiting Cluster"]}
					stateMappings={[{ name: "Waiting Cluster", states: ["Blocked"] }]}
				/>,
			);

			await waitFor(() => {
				expect(screen.getByTestId("cumulative-wait-states")).toHaveTextContent(
					"Review,Waiting Cluster",
				);
			});
			expect(screen.getByTestId("cumulative-state-mappings")).toHaveTextContent(
				"Waiting Cluster",
			);
		});

		async function selectFirstCandidate(
			user: ReturnType<typeof userEvent.setup>,
		) {
			const combobox = await screen.findByRole("combobox", {
				name: /select contributing/i,
			});
			await user.click(combobox);
			await user.click(await screen.findByRole("option", { name: /Pick me/i }));
		}

		it("keeps the systemic RAG status even when a selection narrows the displayed bars", async () => {
			const team = new Team();
			team.name = "Picker Team";
			team.id = 321;
			team.systemWIPLimit = 6;
			team.lastUpdated = new Date();

			const service = buildCumulativePickerService();
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${team.id}:category`,
				"flow-metrics",
			);

			const user = userEvent.setup();
			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["Doing", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-stateTimeCumulative"),
				).toHaveTextContent("green");
			});

			await selectFirstCandidate(user);

			await waitFor(() => {
				expect(
					screen.getByTestId("cumulative-displayed-state-count"),
				).toHaveTextContent("1");
			});

			expect(service.getCumulativeStateTimeForTeam).toHaveBeenCalledWith(
				team.id,
				expect.any(Date),
				expect.any(Date),
				[42],
			);
			expect(
				screen.getByTestId("widget-rag-stateTimeCumulative"),
			).toHaveTextContent("green");
		});

		it("passes the active selection to the drill-down items fetch", async () => {
			const team = new Team();
			team.name = "Drill Team";
			team.id = 654;
			team.systemWIPLimit = 6;
			team.lastUpdated = new Date();

			const service = buildCumulativePickerService();
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${team.id}:category`,
				"flow-metrics",
			);

			const user = userEvent.setup();
			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["Doing", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-stateTimeCumulative"),
				).toBeInTheDocument();
			});

			await selectFirstCandidate(user);

			await waitFor(() => {
				expect(service.getCumulativeStateTimeForTeam).toHaveBeenCalledWith(
					team.id,
					expect.any(Date),
					expect.any(Date),
					[42],
				);
			});

			await user.click(screen.getByTestId("cumulative-bar-click-proxy"));

			await waitFor(() => {
				expect(
					service.getCumulativeStateTimeItemsForPortfolio,
				).toHaveBeenCalledWith(
					team.id,
					expect.any(String),
					expect.any(Date),
					expect.any(Date),
					[42],
				);
			});
		});

		it("shows the completion toggle only while no work item is picked", async () => {
			const team = new Team();
			team.name = "Toggle Team";
			team.id = 888;
			team.systemWIPLimit = 6;
			team.lastUpdated = new Date();

			const service = buildCumulativePickerService();
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${team.id}:category`,
				"flow-metrics",
			);

			const user = userEvent.setup();
			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["Doing", "Review"]}
				/>,
			);

			expect(
				await screen.findByRole("button", {
					name: "Completed visibility toggle",
				}),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "Ongoing visibility toggle" }),
			).toBeInTheDocument();

			await selectFirstCandidate(user);

			await waitFor(() => {
				expect(
					screen.getByTestId("cumulative-displayed-state-count"),
				).toHaveTextContent("1");
			});

			expect(
				screen.queryByRole("button", { name: "Completed visibility toggle" }),
			).not.toBeInTheDocument();
			expect(
				screen.queryByRole("button", { name: "Ongoing visibility toggle" }),
			).not.toBeInTheDocument();
		});

		it("maps the drill-down payload into rows with a linked name and rounded days contributed", async () => {
			const team = new Team();
			team.name = "Drill Fields Team";
			team.id = 777;
			team.systemWIPLimit = 6;
			team.lastUpdated = new Date();

			const itemsResponse = {
				state: "Doing",
				items: [
					{
						workItemId: 1001,
						referenceId: "DRILL-1",
						title: "Investigate flow",
						type: "Story",
						state: "Doing",
						stateCategory: "Doing",
						url: "https://example.test/items/DRILL-1",
						daysContributed: 10.174160150462964,
					},
				],
			};

			const service = {
				...buildCumulativePickerService(),
				getCumulativeStateTimeItemsForTeam: vi
					.fn()
					.mockResolvedValue(itemsResponse),
				getCumulativeStateTimeItemsForPortfolio: vi
					.fn()
					.mockResolvedValue(itemsResponse),
			};
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${team.id}:category`,
				"flow-metrics",
			);

			const user = userEvent.setup();
			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["Doing", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-stateTimeCumulative"),
				).toBeInTheDocument();
			});

			await user.click(screen.getByTestId("cumulative-bar-click-proxy"));

			const nameLink = await screen.findByRole("link", {
				name: "Investigate flow",
			});
			expect(nameLink).toHaveAttribute(
				"href",
				"https://example.test/items/DRILL-1",
			);
			expect(screen.getByText("DRILL-1")).toBeInTheDocument();
			expect(screen.getByTestId("additionalColumnContent")).toHaveTextContent(
				"10.2",
			);
		});
	});

	describe("Work Item Age Percentiles in Portfolio scope", () => {
		const portfolioWorkItemAgeValues: IPercentileValue[] = [
			{ percentile: 50, value: 4 },
			{ percentile: 85, value: 9 },
			{ percentile: 95, value: 13 },
		];

		const makePortfolio = (id: number): Portfolio => {
			const portfolio = new Portfolio();
			portfolio.name = "Age Portfolio";
			portfolio.id = id;
			portfolio.lastUpdated = new Date();
			portfolio.serviceLevelExpectationProbability = 85;
			portfolio.serviceLevelExpectationRange = 14;
			return portfolio;
		};

		it("renders the card over portfolio WIP and reuses the aging-chart selector with portfolio age values", async () => {
			const portfolio = makePortfolio(420);
			const portfolioService = {
				...createMockMetricsService<IFeature>(),
				getWorkItemAgePercentiles: vi
					.fn()
					.mockResolvedValue(portfolioWorkItemAgeValues),
			};
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${portfolio.id}:category`,
				"flow-overview",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={portfolio}
					metricsService={portfolioService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(portfolioService.getWorkItemAgePercentiles).toHaveBeenCalledWith(
					portfolio.id,
					expect.any(Date),
					expect.any(Date),
				);
			});

			expect(
				await screen.findByText("Work Item Age Percentiles"),
			).toBeInTheDocument();
			expect(screen.getByText("4 days")).toBeInTheDocument();
			expect(screen.getByText("9 days")).toBeInTheDocument();
			expect(screen.getByText("13 days")).toBeInTheDocument();

			localStorage.setItem(
				`lighthouse:metrics:portfolio:${portfolio.id}:category`,
				"flow-metrics",
			);
			renderWithRouter(
				<BaseMetricsView
					entity={portfolio}
					metricsService={portfolioService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("aging-work-item-age-percentile-values-count"),
				).toHaveTextContent("3");
			});
		});

		it("shows the graceful empty card and no aging age values for an empty portfolio WIP", async () => {
			const portfolio = makePortfolio(421);
			const portfolioService = {
				...createMockMetricsService<IFeature>(),
				getInProgressItems: vi.fn().mockResolvedValue([]),
				getWorkItemAgePercentiles: vi.fn().mockResolvedValue([]),
			};
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${portfolio.id}:category`,
				"flow-overview",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={portfolio}
					metricsService={portfolioService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			expect(
				await screen.findByText("Work Item Age Percentiles"),
			).toBeInTheDocument();
			expect(screen.getByText("No work in progress")).toBeInTheDocument();

			localStorage.setItem(
				`lighthouse:metrics:portfolio:${portfolio.id}:category`,
				"flow-metrics",
			);
			renderWithRouter(
				<BaseMetricsView
					entity={portfolio}
					metricsService={portfolioService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("aging-work-item-age-percentile-values-count"),
				).toHaveTextContent("0");
			});
		});
	});
	describe("Cycle Time Percentiles named scope (lifted state)", () => {
		const namedDefinition = {
			id: 10,
			name: "Concept to Cash",
			startState: "Planned",
			endState: "Done",
			isValid: true,
		};

		function renderPercentilesWidget() {
			const service = {
				...createMockMetricsService<IWorkItem>(),
				getFeaturesInProgress: vi.fn().mockResolvedValue([]),
			};
			const team = new Team();
			team.name = "Phoenix";
			team.id = 7;
			team.featureWip = 3;
			team.lastUpdated = new Date();
			team.serviceLevelExpectationProbability = 80;
			team.serviceLevelExpectationRange = 10;

			localStorage.setItem(
				`lighthouse:metrics:team:${team.id}:category`,
				"flow-overview",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					doingStates={["To Do", "In Progress", "Review"]}
					cycleTimeDefinitions={[namedDefinition]}
				/>,
			);

			return { service, team };
		}

		const namedItems: IWorkItem[] = [
			{
				...mockCycleTimeData[0],
				id: 101,
				cycleTime: 9,
				namedCycleTimes: [{ definitionId: namedDefinition.id, days: 4 }],
			},
			{
				...mockCycleTimeData[1],
				id: 102,
				cycleTime: 10,
				namedCycleTimes: [{ definitionId: namedDefinition.id, days: 7 }],
			},
			{
				...mockCycleTimeData[0],
				id: 103,
				cycleTime: 11,
				namedCycleTimes: [{ definitionId: 999, days: 3 }],
			},
		];

		function renderWithNamedItems() {
			const service = {
				...createMockMetricsService<IWorkItem>(),
				getFeaturesInProgress: vi.fn().mockResolvedValue([]),
				getCycleTimeData: vi.fn().mockResolvedValue(namedItems),
			};
			const team = new Team();
			team.name = "Phoenix";
			team.id = 7;
			team.featureWip = 3;
			team.lastUpdated = new Date();
			team.serviceLevelExpectationProbability = 80;
			team.serviceLevelExpectationRange = 10;

			localStorage.setItem(
				`lighthouse:metrics:team:${team.id}:category`,
				"flow-overview",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					doingStates={["To Do", "In Progress", "Review"]}
					cycleTimeDefinitions={[namedDefinition]}
				/>,
			);

			return { service, team };
		}

		it("View Data Default selection: the highlight column shows the cycle time for all closed items", async () => {
			renderWithNamedItems();

			await screen.findByTestId("cycle-time-percentiles");

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-highlight-title-percentiles"),
				).toHaveTextContent("Cycle Time");
			});
			expect(
				screen.getByTestId("widget-view-data-count-percentiles"),
			).toHaveTextContent("3");
			expect(
				screen.getByTestId("widget-view-data-highlight-values-percentiles"),
			).toHaveTextContent("9,10,11");
			expect(
				screen.getByTestId("widget-view-data-sle-percentiles"),
			).not.toHaveTextContent("none");
		});

		it("View Data Named selection: highlight column titled with the definition name and valued from namedCycleTimes", async () => {
			renderWithNamedItems();

			await screen.findByTestId("percentile-select-named");
			await userEvent.click(screen.getByTestId("percentile-select-named"));

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-highlight-title-percentiles"),
				).toHaveTextContent("Concept to Cash");
			});
			expect(
				screen.getByTestId("widget-view-data-highlight-values-percentiles"),
			).toHaveTextContent("4,7");
		});

		it("View Data Named selection: rows filtered to the named population equal to the percentile population", async () => {
			renderWithNamedItems();

			await screen.findByTestId("percentile-select-named");
			await userEvent.click(screen.getByTestId("percentile-select-named"));

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-count-percentiles"),
				).toHaveTextContent("2");
			});
		});

		it("View Data Named selection: no SLE line is drawn in the dialog", async () => {
			renderWithNamedItems();

			await screen.findByTestId("percentile-select-named");
			await userEvent.click(screen.getByTestId("percentile-select-named"));

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-highlight-title-percentiles"),
				).toHaveTextContent("Concept to Cash");
			});
			expect(
				screen.getByTestId("widget-view-data-sle-percentiles"),
			).toHaveTextContent("none");
		});

		it("threads the named definitions into the widget and keeps the default RAG SLE-anchored", async () => {
			renderPercentilesWidget();

			await screen.findByTestId("cycle-time-percentiles");
			await waitFor(() => {
				expect(screen.getByTestId("percentile-defs")).toHaveTextContent(
					"Concept to Cash",
				);
			});

			expect(screen.getByTestId("percentile-scope")).toHaveTextContent(
				"default",
			);
			expect(
				screen.getByTestId("widget-rag-percentiles"),
			).not.toHaveTextContent("none");
		});

		it("fetches percentiles with the definition id and neutralizes the RAG footer on a named selection", async () => {
			const { service, team } = renderPercentilesWidget();

			await screen.findByTestId("percentile-select-named");
			(service.getCycleTimePercentiles as Mock).mockClear();

			await userEvent.click(screen.getByTestId("percentile-select-named"));

			await waitFor(() => {
				expect(service.getCycleTimePercentiles).toHaveBeenCalledWith(
					team.id,
					expect.any(Date),
					expect.any(Date),
					namedDefinition.id,
				);
			});

			await waitFor(() => {
				expect(screen.getByTestId("widget-rag-percentiles")).toHaveTextContent(
					"none",
				);
				expect(screen.getByTestId("widget-tip-percentiles")).toHaveTextContent(
					"Named Cycle Times have no SLE target",
				);
			});
		});

		it("restores the SLE-anchored RAG footer when the selection returns to Default", async () => {
			renderPercentilesWidget();

			await screen.findByTestId("percentile-select-named");
			await userEvent.click(screen.getByTestId("percentile-select-named"));
			await waitFor(() => {
				expect(screen.getByTestId("widget-rag-percentiles")).toHaveTextContent(
					"none",
				);
			});

			await userEvent.click(screen.getByTestId("percentile-select-default"));

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-percentiles"),
				).not.toHaveTextContent("none");
			});
			expect(screen.getByTestId("percentile-scope")).toHaveTextContent(
				"default",
			);
		});

		it("keeps the Default trend info fetch free of a definition id", async () => {
			const { service } = renderPercentilesWidget();

			await screen.findByTestId("cycle-time-percentiles");

			await waitFor(() => {
				expect(service.getCycleTimePercentilesInfo).toHaveBeenCalledWith(
					expect.any(Number),
					expect.any(Date),
					expect.any(Date),
				);
			});
			expect(service.getCycleTimePercentilesInfo).not.toHaveBeenCalledWith(
				expect.any(Number),
				expect.any(Date),
				expect.any(Date),
				expect.anything(),
			);
		});

		it("routes the trend footer through the named definition on selection", async () => {
			const { service, team } = renderPercentilesWidget();

			await screen.findByTestId("percentile-select-named");
			(service.getCycleTimePercentilesInfo as Mock).mockResolvedValue({
				percentiles: [],
				comparison: { direction: "up", metricLabel: "Named Cycle Time Trend" },
			});

			await userEvent.click(screen.getByTestId("percentile-select-named"));

			await waitFor(() => {
				expect(service.getCycleTimePercentilesInfo).toHaveBeenCalledWith(
					team.id,
					expect.any(Date),
					expect.any(Date),
					namedDefinition.id,
				);
			});
			await waitFor(() => {
				expect(
					screen.getByTestId("widget-trend-label-percentiles"),
				).toHaveTextContent("Named Cycle Time Trend");
			});
		});

		// Mutation-testing regressions: the lifted selection's plumbing was unpinned -
		// the named values could go unapplied, survive a return to Default, or resolve
		// against the wrong definition, all without a failing test.
		it("shows the named percentile values once they arrive", async () => {
			const { service } = renderPercentilesWidget();

			await screen.findByTestId("percentile-select-named");
			(service.getCycleTimePercentiles as Mock).mockResolvedValue([
				{ percentile: 85, value: 42 },
			]);

			await userEvent.click(screen.getByTestId("percentile-select-named"));

			// The Default fetch supplies 3 values; the named one supplies 1. Never
			// applying the response would leave the Default 3 on screen.
			await waitFor(() => {
				expect(screen.getByTestId("percentile-values-count")).toHaveTextContent(
					"1",
				);
			});
		});

		it("clears the named values when the selection returns to Default", async () => {
			const { service } = renderPercentilesWidget();

			await screen.findByTestId("percentile-select-named");
			(service.getCycleTimePercentiles as Mock).mockResolvedValue([
				{ percentile: 85, value: 42 },
			]);

			await userEvent.click(screen.getByTestId("percentile-select-named"));
			await waitFor(() => {
				expect(screen.getByTestId("percentile-values-count")).toHaveTextContent(
					"1",
				);
			});

			await userEvent.click(screen.getByTestId("percentile-select-default"));

			// Returning to Default must drop the scoped values, not merely relabel the
			// scope - otherwise the named numbers stay on screen under "Default".
			await waitFor(() => {
				expect(screen.getByTestId("percentile-values-count")).toHaveTextContent(
					"3",
				);
			});
			expect(screen.getByTestId("percentile-scope")).toHaveTextContent(
				"default",
			);
		});

		it("resolves View Data against the selected definition, not the first one", async () => {
			const secondDefinition = {
				id: 20,
				name: "Commit to Deploy",
				startState: "Planned",
				endState: "Done",
				isValid: true,
			};
			const items: IWorkItem[] = [
				{
					...mockCycleTimeData[0],
					id: 201,
					cycleTime: 9,
					namedCycleTimes: [
						{ definitionId: namedDefinition.id, days: 4 },
						{ definitionId: secondDefinition.id, days: 8 },
					],
				},
			];
			const service = {
				...createMockMetricsService<IWorkItem>(),
				getFeaturesInProgress: vi.fn().mockResolvedValue([]),
				getCycleTimeData: vi.fn().mockResolvedValue(items),
			};
			const team = new Team();
			team.name = "Phoenix";
			team.id = 7;
			team.featureWip = 3;
			team.lastUpdated = new Date();
			localStorage.setItem(
				`lighthouse:metrics:team:${team.id}:category`,
				"flow-overview",
			);

			renderWithRouter(
				<BaseMetricsView
					entity={team}
					metricsService={service}
					title="Work Items"
					doingStates={["To Do", "In Progress", "Review"]}
					cycleTimeDefinitions={[namedDefinition, secondDefinition]}
				/>,
			);

			await screen.findByTestId("percentile-select-second-named");
			await userEvent.click(
				screen.getByTestId("percentile-select-second-named"),
			);

			// Looking up the definition by anything other than the selected id would
			// title the column "Concept to Cash" and show that definition's 4 days.
			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-highlight-title-percentiles"),
				).toHaveTextContent("Commit to Deploy");
			});
			expect(
				screen.getByTestId("widget-view-data-highlight-values-percentiles"),
			).toHaveTextContent("8");
		});

		// Adversarial-review regressions: under a named selection the widget must never
		// present Default data as if it were the named window's.
		it("falls back to Default rather than showing Default numbers under a named selection when the fetch fails", async () => {
			const { service } = renderPercentilesWidget();

			await screen.findByTestId("percentile-select-named");
			(service.getCycleTimePercentiles as Mock).mockRejectedValue(
				new Error("backend down"),
			);

			await userEvent.click(screen.getByTestId("percentile-select-named"));

			// The selector snaps back to Default, so the numbers on screen and the
			// scope label agree. Leaving the scope named would show default-window
			// percentiles under a named heading.
			await waitFor(() => {
				expect(screen.getByTestId("percentile-scope")).toHaveTextContent(
					"default",
				);
			});
			expect(
				screen.getByTestId("widget-rag-percentiles"),
			).not.toHaveTextContent("none");
		});

		it("ignores a stale named response that resolves after a newer selection", async () => {
			const { service } = renderPercentilesWidget();

			await screen.findByTestId("percentile-select-named");

			let resolveStale: ((value: IPercentileValue[]) => void) | undefined;
			(service.getCycleTimePercentiles as Mock).mockReturnValueOnce(
				new Promise<IPercentileValue[]>((resolve) => {
					resolveStale = resolve;
				}),
			);

			await userEvent.click(screen.getByTestId("percentile-select-named"));
			await userEvent.click(screen.getByTestId("percentile-select-default"));

			await waitFor(() => {
				expect(screen.getByTestId("percentile-scope")).toHaveTextContent(
					"default",
				);
			});

			// The in-flight named request now lands. It belongs to a superseded
			// generation, so it must not repopulate the scoped values.
			resolveStale?.([{ percentile: 85, value: 99 }]);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-rag-percentiles"),
				).not.toHaveTextContent("none");
			});
			expect(screen.getByTestId("percentile-scope")).toHaveTextContent(
				"default",
			);
			// The Default fetch supplies 3 percentiles; the stale named response holds
			// exactly 1, so a leak would show up here as a count of 1.
			expect(screen.getByTestId("percentile-values-count")).toHaveTextContent(
				"3",
			);
		});
	});

	/**
	 * DISTILL RED-pending specs — Story 5508 (widget-loose-ends) slice 01, US-01.
	 *
	 * D8: Total Throughput and Total Arrivals are the only Flow Overview widgets backed by an item
	 * set that cannot show it. Both item sources already exist at the buildViewData call site —
	 * `throughputItems` and the arrivals extraction the sibling `arrivals` entry already uses — so
	 * this is registration, not a new query. If it turns out to need a new fetch, slice 01's
	 * learning hypothesis has fired and slices 03-05 need re-estimating.
	 *
	 * describe.skip = RED scaffold; DELIVER enables it (ADR-025).
	 */
	describe("View Data on Total Throughput and Total Arrivals (Story 5508 slice 01)", () => {
		beforeEach(() => {
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-overview",
			);
		});

		const renderOverview = (svc = mockMetricsService) =>
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={svc}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

		it("exposes the completed items behind Total Throughput, with the cycle time highlight (AC1)", async () => {
			renderOverview();

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-totalThroughput"),
				).toBeInTheDocument();
			});

			// Amended during DELIVER: this originally compared against
			// `widget-view-data-count-throughput`, but the sibling `throughput` CHART widget
			// is not rendered in the flow-overview category, so the oracle referenced markup
			// this screen never produces. Asserted against the fixture instead — the
			// throughput run chart carries [3, 5], i.e. 8 completed items.
			// See distill/upstream-issues.md UPSTREAM-5.
			expect(
				screen.getByTestId("widget-view-data-count-totalThroughput"),
			).toHaveTextContent("8");
			expect(
				screen.getByTestId("widget-view-data-highlight-title-totalThroughput"),
			).toHaveTextContent("Cycle Time");
		});

		it("exposes the arrival items behind Total Arrivals (AC2)", async () => {
			renderOverview();

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-totalArrivals"),
				).toBeInTheDocument();
			});

			// Amended during DELIVER for the same reason as AC1 above — the sibling
			// `arrivals` chart widget is not on the flow-overview screen. The arrivals run
			// chart fixture carries [4, 6], i.e. 10 started items.
			expect(
				screen.getByTestId("widget-view-data-count-totalArrivals"),
			).toHaveTextContent("10");
		});

		it("still offers the drill-through on an empty range, with an empty item set (AC3)", async () => {
			const svc = createMockMetricsService<IWorkItem>();
			svc.getThroughput = vi
				.fn()
				.mockResolvedValue(
					new RunChartData(generateWorkItemMapForRunChart([]), 0, 0),
				);
			svc.getArrivals = vi
				.fn()
				.mockResolvedValue(
					new RunChartData(generateWorkItemMapForRunChart([]), 0, 0),
				);

			renderOverview(svc);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-view-data-totalThroughput"),
				).toBeInTheDocument();
			});

			expect(
				screen.getByTestId("widget-view-data-count-totalThroughput"),
			).toHaveTextContent("0");
			expect(
				screen.getByTestId("widget-view-data-count-totalArrivals"),
			).toHaveTextContent("0");
		});
	});

	/**
	 * DISTILL RED-pending specs — Story 5508 (widget-loose-ends) slice 04, US-05.
	 *
	 * The widget-level half of slice 04: the rule itself is pinned in ragRules.test.ts; here we pin
	 * that it is REGISTERED, that the trend policy flipped from "none" to "previous-period", and
	 * that both render through the EXISTING WidgetShell chrome rather than a new component.
	 *
	 * HARD DEPENDENCY on slice 03 — the previous-period value is the as-of-date computation
	 * evaluated at `startDate − 1 day`. Before slice 03 lands, both periods age to today and every
	 * trend reads flat, so enabling this block earlier would pass for the wrong reason.
	 *
	 * describe.skip = RED scaffold; DELIVER enables it (ADR-025).
	 */
	describe.skip("Work Item Age Percentiles status and trend (Story 5508 slice 04)", () => {
		beforeEach(() => {
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-overview",
			);
		});

		it("renders a RAG status chip on the widget (AC1-AC3)", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-workItemAgePercentiles"),
				).toBeInTheDocument();
			});

			expect(
				screen.getByTestId("widget-rag-workItemAgePercentiles"),
			).toHaveTextContent(/red|amber|green/);
		});

		it("carries a non-empty tip so colour is never the only signal (AC5, CI3)", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-tip-workItemAgePercentiles").textContent,
				).not.toBe("");
			});
		});

		it("renders a previous-period trend through the existing WidgetShell chrome (AC4)", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-trend-workItemAgePercentiles"),
				).toBeInTheDocument();
			});

			expect(
				screen.getByTestId("widget-trend-direction-workItemAgePercentiles"),
			).toHaveTextContent(/up|down|flat/);
		});

		/**
		 * AC3b at the RENDERED widget — added 2026-07-19 by the second-pass review gate.
		 *
		 * The AC3/AC3b split is pinned at the rule level in ragRules.test.ts, but nothing pinned it
		 * at the surface the user actually reads. A rule can return "none" correctly while the widget
		 * still paints an Act chip, because the mapping from rule result to chip lives here, not in
		 * the rule. Without this, the false "define an SLE" instruction could reappear at the only
		 * layer that matters and both the rule tests and the widget tests would stay green.
		 */
		it("shows no Act status and never the define-an-SLE tip when nothing is in progress (AC3b)", async () => {
			const emptyWipService = {
				...mockMetricsService,
				getWorkItemAgePercentiles: vi.fn().mockResolvedValue([]),
			};

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={emptyWipService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-workItemAgePercentiles"),
				).toBeInTheDocument();
			});

			expect(
				screen.getByTestId("widget-rag-workItemAgePercentiles"),
			).not.toHaveTextContent(/red|amber|green/);
			expect(
				screen.getByTestId("widget-tip-workItemAgePercentiles").textContent,
			).not.toMatch(/service level expectation|SLE/i);
		});
	});

	/**
	 * DISTILL RED-pending specs — Story 5508 (widget-loose-ends) slice 05, US-06.
	 *
	 * D7: Flow Efficiency is the only Flow Overview widget off the shared data path — it self-fetches
	 * in its own effect and colours its own number, which is exactly why buildWidgetFooters has no
	 * `flowEfficiency` key to emit. Moving the fetch into the BaseMetricsView data layer is what
	 * earns it the chip; the computeFlowEfficiencyRag rule itself is reused verbatim.
	 *
	 * The last test here is the KPI assertion — "100% of Flow Overview widgets expose a RAG status" —
	 * expressed as a structural test over getWidgetsForCategory so a future widget cannot silently
	 * ship without a status (AC7).
	 *
	 * describe.skip = RED scaffold; DELIVER enables it (ADR-025).
	 */
	describe("Flow Efficiency status via the shared data path (Story 5508 slice 05)", () => {
		beforeEach(() => {
			localStorage.setItem(
				`lighthouse:metrics:portfolio:${mockProject.id}:category`,
				"flow-overview",
			);
		});

		const configuredFlowEfficiencyService = () => {
			const svc = createMockMetricsService<IWorkItem>();
			svc.getFlowEfficiencyInfoForPortfolio = vi.fn().mockResolvedValue({
				isConfigured: true,
				hasDataInScope: true,
				efficiencyPercent: 72,
			});
			return svc;
		};

		const renderOverview = (svc: IMetricsService<IWorkItem>) =>
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={svc}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

		it("renders the status returned by the unchanged rule (AC1)", async () => {
			renderOverview(configuredFlowEfficiencyService());

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-flowEfficiency"),
				).toBeInTheDocument();
			});

			expect(screen.getByTestId("widget-rag-flowEfficiency")).toHaveTextContent(
				/red|amber|green/,
			);
		});

		it("renders no status colour when wait states are not configured (AC2)", async () => {
			const svc = createMockMetricsService<IWorkItem>();
			svc.getFlowEfficiencyInfoForPortfolio = vi.fn().mockResolvedValue({
				isConfigured: false,
				hasDataInScope: false,
				efficiencyPercent: 0,
			});

			renderOverview(svc);

			await waitFor(() => {
				expect(
					screen.getByTestId("flow-efficiency-not-configured"),
				).toBeInTheDocument();
			});

			expect(
				screen.queryByTestId("widget-header-flowEfficiency"),
			).not.toBeInTheDocument();
		});

		it("renders no status colour when there is no data in scope (AC3)", async () => {
			const svc = createMockMetricsService<IWorkItem>();
			svc.getFlowEfficiencyInfoForPortfolio = vi.fn().mockResolvedValue({
				isConfigured: true,
				hasDataInScope: false,
				efficiencyPercent: 0,
			});

			renderOverview(svc);

			await waitFor(() => {
				expect(
					screen.getByTestId("flow-efficiency-no-data"),
				).toBeInTheDocument();
			});

			expect(
				screen.queryByTestId("widget-header-flowEfficiency"),
			).not.toBeInTheDocument();
		});

		it("fetches flow efficiency exactly once, through the shared data layer (AC4)", async () => {
			// The lift is the point: one shared data path, not one more round trip (D18). If the
			// widget still self-fetches, this count goes up as the view re-renders.
			const svc = configuredFlowEfficiencyService();

			renderOverview(svc);

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-flowEfficiency"),
				).toBeInTheDocument();
			});

			expect(svc.getFlowEfficiencyInfoForPortfolio).toHaveBeenCalledTimes(1);
		});

		it("gives every Flow Overview widget a status, so none can ship silently (AC7 — KPI)", async () => {
			renderOverview(configuredFlowEfficiencyService());

			await waitFor(() => {
				expect(
					screen.getByTestId("widget-header-flowEfficiency"),
				).toBeInTheDocument();
			});

			const portfolioWidgetKeys = getWidgetsForCategory(
				"flow-overview",
				"portfolio",
			).map((placement) => placement.widgetKey);

			for (const widgetKey of portfolioWidgetKeys) {
				expect(
					screen.queryByTestId(`widget-header-${widgetKey}`),
					`Flow Overview widget "${widgetKey}" has no RAG footer registered in buildWidgetFooters`,
				).toBeInTheDocument();
			}
		});
	});
});
