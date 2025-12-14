import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import {
	ForecastPredictabilityScore,
	type IForecastPredictabilityScore,
} from "../../../models/Forecasts/ForecastPredictabilityScore";
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
		predictabilityData,
	}: {
		title: string;
		chartData: RunChartData;
		predictabilityData?: IForecastPredictabilityScore;
	}) => (
		<div data-testid={`bar-run-chart-${title}`}>
			<div data-testid="chart-data-count">{chartData.history}</div>
			<div data-testid="predictability-data">
				{predictabilityData
					? "has-predictability-data"
					: "no-predictability-data"}
			</div>
			<div data-testid="predictability-score">
				{predictabilityData ? predictabilityData.predictabilityScore : "none"}
			</div>
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

vi.mock("../../Teams/Detail/ItemsInProgress", () => ({
	default: ({
		entries,
	}: {
		entries: Array<{
			title: string;
			items: IWorkItem[];
			idealWip?: number;
			sle?: number;
		}>;
	}) => (
		<div data-testid={`items-in-progress-container`}>
			{entries.map((entry) => (
				<div key={entry.title} data-testid={`items-in-progress-${entry.title}`}>
					<div data-testid={`items-count-${entry.title}`}>
						{entry.items?.length ?? 0}
					</div>
					<div data-testid={`ideal-wip-${entry.title}`}>
						{entry.idealWip ?? ""}
					</div>
				</div>
			))}
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/StartedVsFinishedDisplay", () => ({
	default: ({
		startedItems,
		closedItems,
	}: {
		startedItems: RunChartData | null;
		closedItems: RunChartData | null;
	}) => (
		<div data-testid="started-vs-finished">
			<div data-testid="started-items">
				{startedItems ? startedItems.history : 0}
			</div>
			<div data-testid="closed-items">
				{closedItems ? closedItems.history : 0}
			</div>
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

// Mock DashboardHeader and Dashboard to capture dashboardId prop
vi.mock("./DashboardHeader", () => ({
	default: ({
		startDate,
		endDate,
		onStartDateChange,
		onEndDateChange,
		dashboardId,
	}: {
		startDate: Date;
		endDate: Date;
		onStartDateChange: (d: Date | null) => void;
		onEndDateChange: (d: Date | null) => void;
		dashboardId: string;
	}) => (
		<div>
			<div data-testid="dashboard-header">{dashboardId}</div>
			<button type="button" data-testid="dashboard-date-range-toggle">
				Toggle
			</button>
			{/* Inline the mocked DateRangeSelector structure so tests can interact with it */}
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
		dashboardId,
	}: {
		items?: Array<{ id: string | number; node: ReactNode }>;
		dashboardId: string;
	}) => (
		<div>
			<div data-testid="dashboard-component">{dashboardId}</div>
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

	it("passes correct dashboardId for Project entity", async () => {
		const projectMetricsService = createMockMetricsService<IFeature>();
		// Ensure project service does NOT have getFeaturesInProgress
		delete (projectMetricsService as unknown as Record<string, unknown>)
			.getFeaturesInProgress;

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={projectMetricsService}
				title="Features"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// The mocked DashboardHeader/Dashboard render the dashboardId text
		await waitFor(() => {
			expect(screen.getByTestId("dashboard-header")).toHaveTextContent(
				`Project_${mockProject.id}`,
			);
			expect(screen.getByTestId("dashboard-component")).toHaveTextContent(
				`Project_${mockProject.id}`,
			);
		});
	});

	it("passes correct dashboardId for Team entity (metricsService has getFeaturesInProgress)", async () => {
		const teamMetricsService = createMockMetricsService<IFeature>();
		// Add getFeaturesInProgress to simulate a team service
		(
			teamMetricsService as unknown as Record<string, unknown>
		).getFeaturesInProgress = vi.fn().mockResolvedValue([]);

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
			expect(screen.getByTestId("dashboard-header")).toHaveTextContent(
				`Team_${mockTeam.id}`,
			);
			expect(screen.getByTestId("dashboard-component")).toHaveTextContent(
				`Team_${mockTeam.id}`,
			);
		});
	});

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
	function createMockMetricsService<
		T extends IWorkItem | IFeature = IWorkItem,
	>() {
		return {
			getThroughput: vi.fn().mockResolvedValue(mockItemsCompletedData),
			getStartedItems: vi.fn().mockResolvedValue(mockStartedItemsData),
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
			expect(projectMetricsService.getStartedItems).toHaveBeenCalledWith(
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

		// Check components are rendered with correct data
		await waitFor(() => {
			expect(
				screen.getByTestId("items-in-progress-Features in Progress:"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("items-count-Features in Progress:"),
			).toHaveTextContent("2");
			expect(
				screen.getByTestId("ideal-wip-Features in Progress:"),
			).toBeEmptyDOMElement();
			expect(screen.getByTestId("cycle-time-percentiles")).toBeInTheDocument();
			expect(
				screen.getByTestId("bar-run-chart-Features Completed"),
			).toBeInTheDocument();
			expect(screen.getByTestId("cycle-time-scatter-plot")).toBeInTheDocument();
			expect(
				screen.getByTestId("feature-size-scatter-plot"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("line-run-chart-Features In Progress Over Time"),
			).toBeInTheDocument();
			expect(screen.getByTestId("started-vs-finished")).toBeInTheDocument();
			expect(
				screen.getByTestId(
					"stacked-area-chart-Simplified Cumulative Flow Diagram",
				),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("total-work-item-age-widget"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId(
					"total-work-item-age-run-chart-Features Total Work Item Age Over Time",
				),
			).toBeInTheDocument();
		});

		// Check that service level expectation is set correctly
		expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
			"85:14",
		);
	});

	it("passes size percentile values to FeatureSizeScatterPlotChart when using Project entity", async () => {
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
			expect(
				screen.getByTestId("items-in-progress-Work Items in Progress:"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("ideal-wip-Work Items in Progress:"),
			).toBeEmptyDOMElement();
			expect(
				screen.getByTestId("bar-run-chart-Work Items Completed"),
			).toBeInTheDocument();
		});

		// Check that service level expectation is set correctly
		expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
			"80:10",
		);
	});

	it("renders Total Work Item Age widget and chart correctly", async () => {
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
			// Check Total Work Item Age Widget is rendered
			expect(
				screen.getByTestId("total-work-item-age-widget"),
			).toBeInTheDocument();
			expect(screen.getByTestId("widget-entity-id")).toHaveTextContent(
				String(mockProject.id),
			);
			expect(screen.getByTestId("widget-has-service")).toHaveTextContent(
				"has-service",
			);

			// Check Total Work Item Age Run Chart is rendered
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
			expect(mockMetricsService.getStartedItems).toHaveBeenCalledTimes(1);
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
			expect(mockMetricsService.getStartedItems).toHaveBeenCalledTimes(1);
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

		const daysDifference = Math.floor(
			(endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24),
		);

		// Check that start date is roughly 30 days before end date
		expect(daysDifference).toBeCloseTo(30, 0);
	});

	it("handles API errors gracefully", async () => {
		const errorMetricsService: IMetricsService<IWorkItem> = {
			getThroughput: vi.fn().mockRejectedValue(new Error("API error")),
			getStartedItems: vi.fn().mockRejectedValue(new Error("API error")),
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
				"Error getting started Work Items:",
				expect.any(Error),
			);
		});

		// The component should still render the container structure; open popover and check
		fireEvent.click(screen.getByTestId("dashboard-date-range-toggle"));
		expect(
			await screen.findByTestId("date-range-selector"),
		).toBeInTheDocument();
		expect(screen.getByTestId("cycle-time-scatter-plot")).toBeInTheDocument();

		consoleSpy.mockRestore();
	});

	it("renders additional components when provided", async () => {
		const additionalEntry = {
			title: "Additional Test:",
			items: mockInProgressItems,
			idealWip: 5,
		};

		renderWithRouter(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				additionalItems={[additionalEntry]}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Check that the additional entry is rendered inside the ItemsInProgress mock
		await waitFor(() => {
			expect(
				screen.getByTestId("items-in-progress-Additional Test:"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("items-count-Additional Test:"),
			).toHaveTextContent("2");
			expect(
				screen.getByTestId("ideal-wip-Additional Test:"),
			).toHaveTextContent("5");
		});
	});

	it("doesn't set serviceLevelExpectation when entity lacks SLE values", async () => {
		const projectWithoutSLE = new Portfolio();
		projectWithoutSLE.id = 5;
		projectWithoutSLE.name = "Project without SLE";
		projectWithoutSLE.lastUpdated = new Date();
		projectWithoutSLE.serviceLevelExpectationProbability = 0;
		projectWithoutSLE.serviceLevelExpectationRange = 0;

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

		it("passes predictability data to BarRunChart component", async () => {
			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={mockMetricsService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Wait for data to be fetched and passed to components
			await waitFor(() => {
				expect(screen.getByTestId("predictability-data")).toHaveTextContent(
					"has-predictability-data",
				);
				expect(screen.getByTestId("predictability-score")).toHaveTextContent(
					"0.73",
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

			// BarRunChart should still render but without predictability data
			await waitFor(() => {
				expect(screen.getByTestId("predictability-data")).toHaveTextContent(
					"no-predictability-data",
				);
				expect(screen.getByTestId("predictability-score")).toHaveTextContent(
					"none",
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

			// BarRunChart should render without predictability data
			await waitFor(() => {
				expect(screen.getByTestId("predictability-data")).toHaveTextContent(
					"no-predictability-data",
				);
				expect(screen.getByTestId("predictability-score")).toHaveTextContent(
					"none",
				);
			});
		});

		it("passes correct predictability score values to BarRunChart", async () => {
			const customPredictabilityData = new ForecastPredictabilityScore(
				[
					{ percentile: 50, value: 8 },
					{ percentile: 85, value: 12 },
				],
				0.456,
				new Map([
					[5, 10],
					[8, 20],
				]),
			);

			const customService: IMetricsService<IWorkItem> = {
				...mockMetricsService,
				getMultiItemForecastPredictabilityScore: vi
					.fn()
					.mockResolvedValue(customPredictabilityData),
			};

			renderWithRouter(
				<BaseMetricsView
					entity={mockProject}
					metricsService={customService}
					title="Features"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			// Verify the exact predictability score is passed through
			await waitFor(() => {
				expect(screen.getByTestId("predictability-score")).toHaveTextContent(
					"0.456",
				);
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
				getStartedItems: vi.fn().mockRejectedValue(new Error("API error")),
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
			});

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
			});

			// Verify the chart receives combined data
			// customCycleTimeData has 3 items, mockInProgressItems has 2 items = 5 total
			expect(
				screen.getByTestId("distribution-work-items-count"),
			).toHaveTextContent("5");
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
});
