import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { Project } from "../../../models/Project/Project";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IProjectMetricsService } from "../../../services/Api/MetricsService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import ProjectMetricsView from "./ProjectMetricsView";

// Mock the components used in ProjectMetricsView
vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: ({
		title,
		chartData,
	}: { title: string; chartData: RunChartData }) => (
		<div data-testid={`bar-run-chart-${title}`}>
			<div data-testid="chart-data-count">
				{chartData.valuePerUnitOfTime.length}
			</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/LineRunChart", () => ({
	default: ({
		title,
		chartData,
	}: { title: string; chartData: RunChartData }) => (
		<div data-testid={`line-run-chart-${title}`}>
			<div data-testid="chart-data-count">
				{chartData.valuePerUnitOfTime.length}
			</div>
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
	default: ({ title, items }: { title: string; items: IWorkItem[] }) => (
		<div data-testid={`items-in-progress-${title}`}>
			<div data-testid="items-count">{items.length}</div>
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
				{startedItems ? startedItems.valuePerUnitOfTime.length : 0}
			</div>
			<div data-testid="closed-items">
				{closedItems ? closedItems.valuePerUnitOfTime.length : 0}
			</div>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/StackedAreaChart", () => ({
	default: ({
		title,
		areas,
	}: { title: string; areas: Array<{ name: string; data: number[] }> }) => (
		<div data-testid={`stacked-area-chart-${title}`}>
			<div data-testid="areas-count">{areas.length}</div>
		</div>
	),
}));

describe("ProjectMetricsView component", () => {
	const mockProject = (() => {
		const project = new Project();
		project.name = "Test Project";
		project.id = 1;
		project.lastUpdated = new Date();
		return project;
	})();

	// Create RunChartData with correct properties
	const mockFeaturesCompletedData: RunChartData = new RunChartData(
		[3, 5], // valuePerUnitOfTime
		2, // history
		8, // total
	);

	const mockFeaturesInProgressData: RunChartData = new RunChartData(
		[2, 4], // valuePerUnitOfTime
		2, // history
		6, // total
	);

	// Create mock started items data
	const mockStartedItemsData: RunChartData = new RunChartData(
		[4, 6], // valuePerUnitOfTime
		2, // history
		10, // total
	);

	// Create mock work items with correct properties
	const mockInProgressFeatures: IWorkItem[] = [
		{
			id: 1,
			name: "Feature 1",
			state: "In Progress",
			stateCategory: "Doing" as StateCategory,
			type: "Story",
			workItemReference: "PROJ-1",
			url: "https://example.com/work/1",
			startedDate: new Date("2023-01-01"),
			closedDate: new Date(), // No closed date yet as it's in progress
			cycleTime: 0,
			workItemAge: 10,
		},
		{
			id: 2,
			name: "Feature 2",
			state: "In Progress",
			stateCategory: "Doing" as StateCategory,
			type: "Bug",
			workItemReference: "PROJ-2",
			url: "https://example.com/work/2",
			startedDate: new Date("2023-01-01"),
			closedDate: new Date(),
			cycleTime: 0,
			workItemAge: 8,
		},
	];

	const mockCycleTimeData: IWorkItem[] = [
		{
			id: 3,
			name: "Feature 3",
			state: "Done",
			stateCategory: "Done" as StateCategory,
			type: "Story",
			workItemReference: "PROJ-3",
			url: "https://example.com/work/3",
			startedDate: new Date("2023-01-01"),
			closedDate: new Date("2023-01-10"),
			cycleTime: 9,
			workItemAge: 9,
		},
		{
			id: 4,
			name: "Feature 4",
			state: "Done",
			stateCategory: "Done" as StateCategory,
			type: "Bug",
			workItemReference: "PROJ-4",
			url: "https://example.com/work/4",
			startedDate: new Date("2023-01-05"),
			closedDate: new Date("2023-01-15"),
			cycleTime: 10,
			workItemAge: 10,
		},
	];

	const mockPercentileValues: IPercentileValue[] = [
		{ percentile: 50, value: 9 },
		{ percentile: 85, value: 12 },
		{ percentile: 95, value: 15 },
	];

	// Mock service and context
	const mockProjectMetricsService: IProjectMetricsService = {
		getThroughput: vi.fn().mockResolvedValue(mockFeaturesCompletedData),

		getStartedItems: vi.fn().mockResolvedValue(mockStartedItemsData),

		getWorkInProgressOverTime: vi
			.fn()
			.mockResolvedValue(mockFeaturesInProgressData),
		getInProgressItems: vi.fn().mockResolvedValue(mockInProgressFeatures),
		getCycleTimeData: vi.fn().mockResolvedValue(mockCycleTimeData),
		getCycleTimePercentiles: vi.fn().mockResolvedValue(mockPercentileValues),
	};

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders all components correctly with data", async () => {
		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={mockProject} />
			</ApiServiceContext.Provider>,
		);

		// Check DateRangeSelector is rendered
		expect(screen.getByTestId("date-range-selector")).toBeInTheDocument();

		// Check all service calls are made
		await waitFor(() => {
			expect(mockProjectMetricsService.getThroughput).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(mockProjectMetricsService.getInProgressItems).toHaveBeenCalledWith(
				mockProject.id,
			);
			expect(
				mockProjectMetricsService.getWorkInProgressOverTime,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(mockProjectMetricsService.getCycleTimeData).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(
				mockProjectMetricsService.getCycleTimePercentiles,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(mockProjectMetricsService.getStartedItems).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
		});

		// Check components are rendered with correct data
		await waitFor(() => {
			expect(
				screen.getByTestId("items-in-progress-Features in Progress:"),
			).toBeInTheDocument();
			expect(screen.getByTestId("items-count")).toHaveTextContent("2");
			expect(screen.getByTestId("cycle-time-percentiles")).toBeInTheDocument();
			expect(
				screen.getByTestId("bar-run-chart-Features Completed"),
			).toBeInTheDocument();
			expect(screen.getByTestId("cycle-time-scatter-plot")).toBeInTheDocument();
			expect(
				screen.getByTestId("line-run-chart-Features In Progress Over Time"),
			).toBeInTheDocument();
			expect(screen.getByTestId("started-vs-finished")).toBeInTheDocument();
			expect(screen.getByTestId("started-items")).toHaveTextContent("2");
			expect(
				screen.getByTestId(
					"stacked-area-chart-Simplified Cumulative Flow Diagram",
				),
			).toBeInTheDocument();
		});
	});

	it("updates data when start date changes", async () => {
		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={mockProject} />
			</ApiServiceContext.Provider>,
		);

		// Wait for initial render
		await waitFor(() => {
			expect(
				screen.getByTestId("bar-run-chart-Features Completed"),
			).toBeInTheDocument();
		});

		// Reset mock call counts
		vi.clearAllMocks();

		// Change start date
		fireEvent.click(screen.getByTestId("change-start-date"));

		// Verify all metrics are fetched again with new date
		await waitFor(() => {
			expect(mockProjectMetricsService.getThroughput).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getWorkInProgressOverTime,
			).toHaveBeenCalledTimes(1);
			expect(mockProjectMetricsService.getCycleTimeData).toHaveBeenCalledTimes(
				1,
			);
			expect(
				mockProjectMetricsService.getCycleTimePercentiles,
			).toHaveBeenCalledTimes(1);
			expect(mockProjectMetricsService.getStartedItems).toHaveBeenCalledTimes(
				1,
			);
		});
	});

	it("updates data when end date changes", async () => {
		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={mockProject} />
			</ApiServiceContext.Provider>,
		);

		// Wait for initial render
		await waitFor(() => {
			expect(
				screen.getByTestId("bar-run-chart-Features Completed"),
			).toBeInTheDocument();
		});

		// Reset mock call counts
		vi.clearAllMocks();

		// Change end date
		fireEvent.click(screen.getByTestId("change-end-date"));

		// Verify all metrics are fetched again with new date
		await waitFor(() => {
			expect(mockProjectMetricsService.getThroughput).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getWorkInProgressOverTime,
			).toHaveBeenCalledTimes(1);
			expect(mockProjectMetricsService.getCycleTimeData).toHaveBeenCalledTimes(
				1,
			);
			expect(
				mockProjectMetricsService.getCycleTimePercentiles,
			).toHaveBeenCalledTimes(1);
			expect(mockProjectMetricsService.getStartedItems).toHaveBeenCalledTimes(
				1,
			);
		});
	});

	it("handles API errors gracefully", async () => {
		const errorProjectMetricsService: IProjectMetricsService = {
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
		};

		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});

		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: errorProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={mockProject} />
			</ApiServiceContext.Provider>,
		);

		// Wait for errors to be logged
		await waitFor(() => {
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting features completed:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting features in progress:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error fetching cycle time data:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting throughput:",
				expect.any(Error),
			);
		});

		// Verify none of the data-dependent components are rendered
		expect(
			screen.queryByTestId("bar-run-chart-Features Completed"),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("line-run-chart-Features In Progress Over Time"),
		).not.toBeInTheDocument();

		// But the container components should still be rendered
		expect(screen.getByTestId("date-range-selector")).toBeInTheDocument();
		expect(screen.getByTestId("cycle-time-scatter-plot")).toBeInTheDocument();

		consoleSpy.mockRestore();
	});

	it("initializes with default date range (90 days)", () => {
		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={mockProject} />
			</ApiServiceContext.Provider>,
		);

		const startDateElement = screen.getByTestId("start-date");
		const endDateElement = screen.getByTestId("end-date");

		const startDate = new Date(startDateElement.textContent ?? "");
		const endDate = new Date(endDateElement.textContent ?? "");
		const today = new Date();

		// Set both dates to midnight for comparison
		today.setHours(0, 0, 0, 0);
		endDate.setHours(0, 0, 0, 0);

		const daysDifference = Math.floor(
			(endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24),
		);

		// Check that start date is roughly 90 days before end date (allowing for some hours difference)
		expect(daysDifference).toBeCloseTo(90, 0);

		// Check that end date is today
		expect(endDate.getDate()).toBe(today.getDate());
		expect(endDate.getMonth()).toBe(today.getMonth());
		expect(endDate.getFullYear()).toBe(today.getFullYear());
	});

	it("renders StartedVsFinishedDisplay component with correct data", async () => {
		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={mockProject} />
			</ApiServiceContext.Provider>,
		);

		// Check that StartedVsFinishedDisplay component is rendered with the correct data
		await waitFor(() => {
			expect(screen.getByTestId("started-vs-finished")).toBeInTheDocument();
			expect(screen.getByTestId("started-items")).toHaveTextContent("2"); // From mock data length
			expect(screen.getByTestId("closed-items")).toHaveTextContent("2"); // From mock data length
		});
	});

	it("renders StackedAreaChart with started and completed items data", async () => {
		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={mockProject} />
			</ApiServiceContext.Provider>,
		);

		// Check that StackedAreaChart component is rendered with the correct data
		await waitFor(() => {
			expect(
				screen.getByTestId(
					"stacked-area-chart-Simplified Cumulative Flow Diagram",
				),
			).toBeInTheDocument();
			expect(screen.getByTestId("areas-count")).toHaveTextContent("2"); // Should have 2 areas (Doing and Done)
		});
	});

	it("sets serviceLevelExpectation when project has valid SLE values", async () => {
		// Create a project with SLE values
		const projectWithSLE = new Project();
		projectWithSLE.id = 2;
		projectWithSLE.name = "Project with SLE";
		projectWithSLE.lastUpdated = new Date();
		projectWithSLE.serviceLevelExpectationProbability = 85;
		projectWithSLE.serviceLevelExpectationRange = 14;

		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={projectWithSLE} />
			</ApiServiceContext.Provider>,
		);

		// Wait for the component to render with the SLE value
		await waitFor(() => {
			expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
				"85:14",
			);
		});
	});

	it("doesn't set serviceLevelExpectation when project lacks valid SLE values", async () => {
		// Create a project with missing SLE values (both = 0)
		const projectWithoutSLE = new Project();
		projectWithoutSLE.id = 3;
		projectWithoutSLE.name = "Project without SLE";
		projectWithoutSLE.lastUpdated = new Date();
		projectWithoutSLE.serviceLevelExpectationProbability = 0;
		projectWithoutSLE.serviceLevelExpectationRange = 0;

		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={projectWithoutSLE} />
			</ApiServiceContext.Provider>,
		);

		// Wait for the component to render without SLE value
		await waitFor(() => {
			expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
				"none",
			);
		});
	});

	it("doesn't set serviceLevelExpectation when project has partial SLE values", async () => {
		// Create a project with only one valid SLE value
		const projectWithPartialSLE = new Project();
		projectWithPartialSLE.id = 4;
		projectWithPartialSLE.name = "Project with partial SLE";
		projectWithPartialSLE.lastUpdated = new Date();
		projectWithPartialSLE.serviceLevelExpectationProbability = 85;
		projectWithPartialSLE.serviceLevelExpectationRange = 0;

		const mockApiContext = createMockApiServiceContext({
			projectMetricsService: mockProjectMetricsService,
		});

		render(
			<ApiServiceContext.Provider value={mockApiContext}>
				<ProjectMetricsView project={projectWithPartialSLE} />
			</ApiServiceContext.Provider>,
		);

		// Wait for the component to render without SLE value
		await waitFor(() => {
			expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
				"none",
			);
		});
	});
});
