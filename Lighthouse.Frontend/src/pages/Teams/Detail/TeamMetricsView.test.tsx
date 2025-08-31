import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamMetricsService } from "../../../services/Api/MetricsService";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import TeamMetricsView from "./TeamMetricsView";

interface BaseMetricsViewProps {
	entity: { name: string };
	title: string;
	defaultDateRange: number;
	additionalItems?: Array<{ id: string; node: React.ReactNode }>;
}

vi.mock("../../Common/MetricsView/BaseMetricsView", () => ({
	BaseMetricsView: ({
		entity,
		title,
		defaultDateRange,
		additionalItems,
	}: BaseMetricsViewProps) => (
		<div data-testid="base-metrics-view">
			<div data-testid="entity-name">{entity.name}</div>
			<div data-testid="metrics-title">{title}</div>
			<div data-testid="default-date-range">{defaultDateRange}</div>
			{additionalItems && additionalItems.length > 0 && (
				<div data-testid="additional-components">
					{additionalItems.map((it) => {
						// If the item provides a React node, render it. Otherwise
						// treat it as an InProgressEntry-like object and render
						// a simple items-in-progress structure so tests can assert
						// against it (matches production which may pass data)
						const entry = it as {
							node?: React.ReactNode;
							id?: string | number;
							title?: string;
							items?: Array<unknown>;
							idealWip?: number;
						};

						return entry.node ? (
							entry.node
						) : (
							<div
								data-testid="items-in-progress"
								key={entry.id || entry.title}
							>
								<div data-testid="items-title">{entry.title}</div>
								<div data-testid="items-count">
									{(entry.items || []).length}
								</div>
								{entry.idealWip !== undefined && (
									<div data-testid="ideal-wip">{entry.idealWip}</div>
								)}
							</div>
						);
					})}
				</div>
			)}
		</div>
	),
}));

interface ItemsInProgressProps {
	title: string;
	items: Array<{ id: number; name: string }>;
	idealWip?: number;
}

vi.mock("./ItemsInProgress", () => ({
	default: ({ title, items, idealWip }: ItemsInProgressProps) => (
		<div data-testid="items-in-progress">
			<div data-testid="items-title">{title}</div>
			<div data-testid="items-count">{items.length}</div>
			{idealWip !== undefined && <div data-testid="ideal-wip">{idealWip}</div>}
		</div>
	),
}));

describe("TeamMetricsView component", () => {
	const mockTeamMetricsService: ITeamMetricsService =
		createMockTeamMetricsService();
	const mockGetFeaturesInProgress = vi.fn();

	beforeEach(() => {
		vi.clearAllMocks();
		mockTeamMetricsService.getFeaturesInProgress = mockGetFeaturesInProgress;
		mockGetFeaturesInProgress.mockResolvedValue([
			{ id: 1, name: "Feature 1" },
			{ id: 2, name: "Feature 2" },
		]);
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	const setupTest = (team: Team) => {
		const mockTeamService = createMockTeamService();
		mockTeamService.getTeamSettings = vi.fn().mockResolvedValue({
			doingStates: ["In Progress", "Review"],
		});

		const mockContext = createMockApiServiceContext({
			teamMetricsService: mockTeamMetricsService,
			teamService: mockTeamService,
		});

		return render(
			<ApiServiceContext.Provider value={mockContext}>
				<TeamMetricsView team={team} />
			</ApiServiceContext.Provider>,
		);
	};

	it("should render the BaseMetricsView with correct props", async () => {
		// Arrange
		const team = new Team();
		team.id = 1;
		team.name = "Test Team";
		team.featureWip = 3;
		team.useFixedDatesForThroughput = true;

		// Act
		setupTest(team);

		// Assert - wait for async operations to complete
		await waitFor(() => {
			expect(screen.getByTestId("base-metrics-view")).toBeInTheDocument();
			expect(screen.getByTestId("entity-name")).toHaveTextContent("Test Team");
			expect(screen.getByTestId("metrics-title")).toHaveTextContent(
				"Work Items",
			);
			expect(screen.getByTestId("default-date-range")).toHaveTextContent("30");
		});
	});

	it("should fetch features in progress when component mounts", async () => {
		// Arrange
		const team = new Team();
		team.id = 1;
		team.name = "Test Team";
		team.featureWip = 3;

		// Act
		setupTest(team);

		// Assert
		await waitFor(() => {
			expect(mockGetFeaturesInProgress).toHaveBeenCalledWith(team.id);
		});
	});

	it("should set featureWip to real number if team.featureWip > 0 and auto-adjust is false", async () => {
		const team = new Team();
		team.id = 2;
		team.name = "Team Wip Real";
		team.featureWip = 5;

		const mockTeamService = createMockTeamService();
		mockTeamService.getTeamSettings = vi.fn().mockResolvedValue({
			automaticallyAdjustFeatureWIP: false,
			doingStates: ["In Progress"],
		});

		const mockContext = createMockApiServiceContext({
			teamMetricsService: mockTeamMetricsService,
			teamService: mockTeamService,
		});

		render(
			<ApiServiceContext.Provider value={mockContext}>
				<TeamMetricsView team={team} />
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			const idealWip = screen.getByTestId("ideal-wip");
			expect(idealWip).toHaveTextContent("5");
		});
	});

	it("should set featureWip to undefined if team.featureWip = 0 and auto-adjust is false", async () => {
		const team = new Team();
		team.id = 3;
		team.name = "Team Wip Zero";
		team.featureWip = 0;

		const mockTeamService = createMockTeamService();
		mockTeamService.getTeamSettings = vi.fn().mockResolvedValue({
			automaticallyAdjustFeatureWIP: false,
			doingStates: ["In Progress"],
		});

		const mockContext = createMockApiServiceContext({
			teamMetricsService: mockTeamMetricsService,
			teamService: mockTeamService,
		});

		render(
			<ApiServiceContext.Provider value={mockContext}>
				<TeamMetricsView team={team} />
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			// idealWip should not be rendered if undefined
			const itemsInProgress = screen.getByTestId("items-in-progress");
			expect(
				itemsInProgress.querySelector("[data-testid='ideal-wip']"),
			).toBeNull();
		});
	});

	it("should set featureWip to undefined if team.featureWip > 0 but auto-adjust is true", async () => {
		const team = new Team();
		team.id = 4;
		team.name = "Team Wip Auto";
		team.featureWip = 7;

		const mockTeamService = createMockTeamService();
		mockTeamService.getTeamSettings = vi.fn().mockResolvedValue({
			automaticallyAdjustFeatureWIP: true,
			doingStates: ["In Progress"],
		});

		const mockContext = createMockApiServiceContext({
			teamMetricsService: mockTeamMetricsService,
			teamService: mockTeamService,
		});

		render(
			<ApiServiceContext.Provider value={mockContext}>
				<TeamMetricsView team={team} />
			</ApiServiceContext.Provider>,
		);

		await waitFor(() => {
			const itemsInProgress = screen.getByTestId("items-in-progress");
			expect(
				itemsInProgress.querySelector("[data-testid='ideal-wip']"),
			).toBeNull();
		});
	});

	it("should render items in progress with fetched features", async () => {
		// Arrange
		const team = new Team();
		team.id = 1;
		team.name = "Test Team";
		team.featureWip = 3;

		// Act
		setupTest(team);

		// Assert
		await waitFor(() => {
			const additionalComponents = screen.getByTestId("additional-components");
			expect(additionalComponents).toBeInTheDocument();

			const itemsInProgress = screen.getByTestId("items-in-progress");
			expect(itemsInProgress).toBeInTheDocument();
			expect(screen.getByTestId("items-title")).toHaveTextContent(
				"Features being Worked On:",
			);
			expect(screen.getByTestId("items-count")).toHaveTextContent("2");
			expect(screen.getByTestId("ideal-wip")).toHaveTextContent("3");
		});
	});

	it("should handle error when fetching features fails", async () => {
		// Arrange
		const team = new Team();
		team.id = 1;
		team.name = "Test Team";

		// Mock the console.error to avoid polluting test output
		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});
		mockGetFeaturesInProgress.mockRejectedValue(new Error("API Error"));

		// Act
		setupTest(team);

		// Assert
		await waitFor(() => {
			expect(mockGetFeaturesInProgress).toHaveBeenCalledWith(team.id);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error fetching Features in progress:",
				expect.any(Error),
			);
		});

		// Should still render the component with empty features
		await waitFor(() => {
			const itemsInProgress = screen.getByTestId("items-in-progress");
			expect(itemsInProgress).toBeInTheDocument();
			expect(screen.getByTestId("items-count")).toHaveTextContent("0");
		});

		consoleSpy.mockRestore();
	});

	it("should use a fixed 30 day date range when team has useFixedDatesForThroughput set to true", async () => {
		// Arrange
		const team = new Team();
		team.id = 1;
		team.name = "Test Team";
		team.useFixedDatesForThroughput = true;

		// Set throughput dates that would result in a different date range if used
		const startDate = new Date();
		startDate.setDate(startDate.getDate() - 60); // 60 days ago
		team.throughputStartDate = startDate;

		// Act
		setupTest(team);

		// Assert
		await waitFor(() => {
			expect(screen.getByTestId("default-date-range")).toHaveTextContent("30");
		});
	});

	it("should use days since throughputStartDate as date range when team is not using fixed dates", async () => {
		// Arrange
		const team = new Team();
		team.id = 1;
		team.name = "Test Team";
		team.useFixedDatesForThroughput = false;

		// Set throughput start date to 45 days ago
		const startDate = new Date();
		startDate.setDate(startDate.getDate() - 45);
		team.throughputStartDate = startDate;

		// Act
		setupTest(team);

		// Assert
		await waitFor(() => {
			expect(screen.getByTestId("default-date-range")).toHaveTextContent("45");
		});
	});
});
