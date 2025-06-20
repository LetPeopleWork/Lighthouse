import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { Project } from "../../../models/Project/Project";
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
		title,
		items,
		idealWip,
	}: {
		title: string;
		items: IWorkItem[];
		idealWip: number;
	}) => (
		<div data-testid={`items-in-progress-${title}`}>
			<div data-testid="items-count">{items.length}</div>
			<div data-testid="ideal-wip">{idealWip}</div>
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

describe("BaseMetricsView component", () => {
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
		},
	];

	const mockPercentileValues: IPercentileValue[] = [
		{ percentile: 50, value: 9 },
		{ percentile: 85, value: 12 },
		{ percentile: 95, value: 15 },
	];

	// Mock metrics service
	const mockMetricsService: IMetricsService<IWorkItem> = {
		getThroughput: vi.fn().mockResolvedValue(mockItemsCompletedData),
		getStartedItems: vi.fn().mockResolvedValue(mockStartedItemsData),
		getWorkInProgressOverTime: vi
			.fn()
			.mockResolvedValue(mockItemsInProgressData),
		getInProgressItems: vi.fn().mockResolvedValue(mockInProgressItems),
		getCycleTimeData: vi.fn().mockResolvedValue(mockCycleTimeData),
		getCycleTimePercentiles: vi.fn().mockResolvedValue(mockPercentileValues),
	};

	// Create two types of entities to test with
	const mockProject = (() => {
		const project = new Project();
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
		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={90}
			/>,
		);

		// Check DateRangeSelector is rendered
		expect(screen.getByTestId("date-range-selector")).toBeInTheDocument();

		// Check all service calls are made
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(mockMetricsService.getInProgressItems).toHaveBeenCalledWith(
				mockProject.id,
			);
			expect(mockMetricsService.getWorkInProgressOverTime).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(mockMetricsService.getCycleTimeData).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(mockMetricsService.getCycleTimePercentiles).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
			expect(mockMetricsService.getStartedItems).toHaveBeenCalledWith(
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
			expect(screen.getByTestId("ideal-wip")).toBeEmptyDOMElement();
			expect(screen.getByTestId("cycle-time-percentiles")).toBeInTheDocument();
			expect(
				screen.getByTestId("bar-run-chart-Features Completed"),
			).toBeInTheDocument();
			expect(screen.getByTestId("cycle-time-scatter-plot")).toBeInTheDocument();
			expect(
				screen.getByTestId("line-run-chart-Features In Progress Over Time"),
			).toBeInTheDocument();
			expect(screen.getByTestId("started-vs-finished")).toBeInTheDocument();
			expect(
				screen.getByTestId(
					"stacked-area-chart-Simplified Cumulative Flow Diagram",
				),
			).toBeInTheDocument();
		});

		// Check that service level expectation is set correctly
		expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
			"85:14",
		);
	});

	it("renders all components correctly with Team entity", async () => {
		render(
			<BaseMetricsView
				entity={mockTeam}
				metricsService={mockMetricsService}
				title="Work Items"
				defaultDateRange={30}
			/>,
		);

		// Check DateRangeSelector is rendered
		expect(screen.getByTestId("date-range-selector")).toBeInTheDocument();

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
			expect(screen.getByTestId("ideal-wip")).toBeEmptyDOMElement();
			expect(
				screen.getByTestId("bar-run-chart-Work Items Completed"),
			).toBeInTheDocument();
		});

		// Check that service level expectation is set correctly
		expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
			"80:10",
		);
	});

	it("updates data when start date changes", async () => {
		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={90}
			/>,
		);

		// Wait for initial render
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalled();
		});

		// Reset mock call counts
		vi.clearAllMocks();

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
		});
	});

	it("updates data when end date changes", async () => {
		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={90}
			/>,
		);

		// Wait for initial render
		await waitFor(() => {
			expect(mockMetricsService.getThroughput).toHaveBeenCalled();
		});

		// Reset mock call counts
		vi.clearAllMocks();

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
		});
	});

	it("initializes with the specified default date range", () => {
		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={60}
			/>,
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
			(endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24) + 1,
		);

		// Check that start date is roughly 60 days before end date (allowing for some hours difference)
		expect(daysDifference).toBeCloseTo(60, 0);

		// Check that end date is today
		expect(endDate.getDate()).toBe(today.getDate());
		expect(endDate.getMonth()).toBe(today.getMonth());
		expect(endDate.getFullYear()).toBe(today.getFullYear());
	});

	it("falls back to default date range of 30 days when not specified", () => {
		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				// No defaultDateRange provided
			/>,
		);

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
		};

		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});

		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={errorMetricsService}
				title="Features"
			/>,
		);

		// Wait for errors to be logged
		await waitFor(() => {
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting throughput:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting items in progress:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error fetching cycle time data:",
				expect.any(Error),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error getting started items:",
				expect.any(Error),
			);
		});

		// The component should still render the container structure
		expect(screen.getByTestId("date-range-selector")).toBeInTheDocument();
		expect(screen.getByTestId("cycle-time-scatter-plot")).toBeInTheDocument();

		consoleSpy.mockRestore();
	});

	it("renders additional components when provided", async () => {
		const additionalComponent = () => (
			<div data-testid="additional-component">Extra Content</div>
		);

		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				renderAdditionalComponents={additionalComponent}
			/>,
		);

		// Check that the additional component is rendered
		await waitFor(() => {
			expect(screen.getByTestId("additional-component")).toBeInTheDocument();
			expect(screen.getByTestId("additional-component")).toHaveTextContent(
				"Extra Content",
			);
		});
	});

	it("doesn't set serviceLevelExpectation when entity lacks SLE values", async () => {
		const projectWithoutSLE = new Project();
		projectWithoutSLE.id = 5;
		projectWithoutSLE.name = "Project without SLE";
		projectWithoutSLE.lastUpdated = new Date();
		projectWithoutSLE.serviceLevelExpectationProbability = 0;
		projectWithoutSLE.serviceLevelExpectationRange = 0;

		render(
			<BaseMetricsView
				entity={projectWithoutSLE}
				metricsService={mockMetricsService}
				title="Features"
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
		const projectWithPartialSLE = new Project();
		projectWithPartialSLE.id = 6;
		projectWithPartialSLE.name = "Project with partial SLE";
		projectWithPartialSLE.lastUpdated = new Date();
		projectWithPartialSLE.serviceLevelExpectationProbability = 85;
		projectWithPartialSLE.serviceLevelExpectationRange = 0;

		render(
			<BaseMetricsView
				entity={projectWithPartialSLE}
				metricsService={mockMetricsService}
				title="Features"
			/>,
		);

		// Wait for the component to render without SLE value
		await waitFor(() => {
			expect(screen.getByTestId("service-level-expectation")).toHaveTextContent(
				"none",
			);
		});
	});
});
