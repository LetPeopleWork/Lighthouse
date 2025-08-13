import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, type Mock, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import {
	ForecastPredictabilityScore,
	type IForecastPredictabilityScore,
} from "../../../models/Forecasts/ForecastPredictabilityScore";
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
		title,
		items,
		idealWip,
	}: {
		title: string;
		items: IWorkItem[];
		idealWip: number;
	}) => (
		<div data-testid={`items-in-progress-${title}`}>
			<div data-testid={`items-count-${title}`}>{items.length}</div>
			<div data-testid={`ideal-wip-${title}`}>{idealWip}</div>
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
		} as IMetricsService<T> & {
			getSizePercentiles?: (
				id: number,
				startDate: Date,
				endDate: Date,
			) => Promise<IPercentileValue[]>;
		};
	}

	const mockMetricsService = createMockMetricsService<IWorkItem>();

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
		const projectMetricsService = createMockMetricsService<IFeature>();

		const mockFeatureData = mockCycleTimeData.map((item) => ({
			...item,
			size: 5, // example size value
		}));
		projectMetricsService.getCycleTimeData = vi
			.fn()
			.mockResolvedValue(mockFeatureData);

		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={projectMetricsService}
				title="Features"
				defaultDateRange={90}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Check DateRangeSelector is rendered
		expect(screen.getByTestId("date-range-selector")).toBeInTheDocument();

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

		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={projectMetricsService}
				title="Features"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Wait for component to load and getSizePercentiles to be called
		await waitFor(() => {
			expect(projectMetricsService.getSizePercentiles).toHaveBeenCalledWith(
				mockProject.id,
				expect.any(Date),
				expect.any(Date),
			);
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
		render(
			<BaseMetricsView
				entity={mockTeam}
				metricsService={mockMetricsService}
				title="Work Items"
				defaultDateRange={30}
				doingStates={["To Do", "In Progress", "Review"]}
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

	it("updates data when start date changes", async () => {
		render(
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
		render(
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

	it("initializes with the specified default date range", () => {
		render(
			<BaseMetricsView
				entity={mockProject}
				metricsService={mockMetricsService}
				title="Features"
				defaultDateRange={60}
				doingStates={["To Do", "In Progress", "Review"]}
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
				doingStates={["To Do", "In Progress", "Review"]}
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
			getMultiItemForecastPredictabilityScore: vi
				.fn()
				.mockRejectedValue(new Error("API error")),
		};

		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});

		render(
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
				doingStates={["To Do", "In Progress", "Review"]}
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
			render(
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
			render(
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
			render(
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
			render(
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
			const { rerender } = render(
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

			// Change entity
			rerender(
				<BaseMetricsView
					entity={mockTeam}
					metricsService={mockMetricsService}
					title="Work Items"
					defaultDateRange={30}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
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

			render(
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

			render(
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

			render(
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
			render(
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
			};

			const consoleSpy = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});

			render(
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
});
