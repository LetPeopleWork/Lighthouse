import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
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
	default: ({ percentileValues }: { percentileValues: IPercentileValue[] }) => (
		<div data-testid="cycle-time-percentiles">
			<div data-testid="percentile-values-count">{percentileValues.length}</div>
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
	}: {
		inProgressItems: IWorkItem[];
		percentileValues: IPercentileValue[];
		serviceLevelExpectation: IPercentileValue | null;
	}) => (
		<div data-testid="work-item-aging-chart">
			<div data-testid="aging-in-progress-items-count">
				{inProgressItems.length}
			</div>
			<div data-testid="aging-percentile-values-count">
				{percentileValues.length}
			</div>
			<div data-testid="aging-service-level-expectation">
				{serviceLevelExpectation
					? `${serviceLevelExpectation.percentile}:${serviceLevelExpectation.value}`
					: "none"}
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
	}: {
		data: ProcessBehaviourChartData;
		title: string;
	}) => (
		<div data-testid={`process-behaviour-chart-${title}`}>
			<div data-testid={`pbc-status-${title}`}>{data.status}</div>
			<div data-testid={`pbc-data-points-${title}`}>
				{data.dataPoints.length}
			</div>
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
		viewData?: { title: string; items: unknown[] };
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
						Learn More
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

		it("shows red RAG for feature size when no target is set", async () => {
			// featureSizeTarget = null → Red
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
					"red",
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
});
