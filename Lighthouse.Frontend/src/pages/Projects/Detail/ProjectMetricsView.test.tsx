import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { Project } from "../../../models/Project/Project";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IProjectMetricsService } from "../../../services/Api/ProjectMetricsService";
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
	}: {
		cycleTimeDataPoints: IWorkItem[];
		percentileValues: IPercentileValue[];
	}) => (
		<div data-testid="cycle-time-scatter-plot">
			<div data-testid="cycle-time-data-points-count">
				{cycleTimeDataPoints.length}
			</div>
			<div data-testid="percentile-values-count">{percentileValues.length}</div>
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
		getThroughputForProject: vi
			.fn()
			.mockResolvedValue(mockFeaturesCompletedData),
		getFeaturesInProgressOverTimeForProject: vi
			.fn()
			.mockResolvedValue(mockFeaturesInProgressData),
		getInProgressFeaturesForProject: vi
			.fn()
			.mockResolvedValue(mockInProgressFeatures),
		getCycleTimeDataForProject: vi.fn().mockResolvedValue(mockCycleTimeData),
		getCycleTimePercentilesForProject: vi
			.fn()
			.mockResolvedValue(mockPercentileValues),
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
			expect(
				mockProjectMetricsService.getThroughputForProject,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(
				mockProjectMetricsService.getInProgressFeaturesForProject,
			).toHaveBeenCalledWith(mockProject.id);
			expect(
				mockProjectMetricsService.getFeaturesInProgressOverTimeForProject,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(
				mockProjectMetricsService.getCycleTimeDataForProject,
			).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(
				mockProjectMetricsService.getCycleTimePercentilesForProject,
			).toHaveBeenCalledWith(
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
			expect(
				mockProjectMetricsService.getThroughputForProject,
			).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getFeaturesInProgressOverTimeForProject,
			).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getCycleTimeDataForProject,
			).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getCycleTimePercentilesForProject,
			).toHaveBeenCalledTimes(1);
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
			expect(
				mockProjectMetricsService.getThroughputForProject,
			).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getFeaturesInProgressOverTimeForProject,
			).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getCycleTimeDataForProject,
			).toHaveBeenCalledTimes(1);
			expect(
				mockProjectMetricsService.getCycleTimePercentilesForProject,
			).toHaveBeenCalledTimes(1);
		});
	});

	it("handles API errors gracefully", async () => {
		const errorProjectMetricsService: IProjectMetricsService = {
			getThroughputForProject: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getFeaturesInProgressOverTimeForProject: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getInProgressFeaturesForProject: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getCycleTimeDataForProject: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
			getCycleTimePercentilesForProject: vi
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

		const startDate = new Date(startDateElement.textContent || "");
		const endDate = new Date(endDateElement.textContent || "");
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
});
