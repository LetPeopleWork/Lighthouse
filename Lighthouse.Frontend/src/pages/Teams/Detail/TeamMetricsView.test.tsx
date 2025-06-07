import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamMetricsService } from "../../../services/Api/MetricsService";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import TeamMetricsView from "./TeamMetricsView";

interface BaseMetricsViewProps {
	entity: { name: string };
	title: string;
	defaultDateRange: number;
	renderAdditionalComponents?: () => React.ReactNode;
}

vi.mock("../../Common/MetricsView/BaseMetricsView", () => ({
	BaseMetricsView: ({
		entity,
		title,
		defaultDateRange,
		renderAdditionalComponents,
	}: BaseMetricsViewProps) => (
		<div data-testid="base-metrics-view">
			<div data-testid="entity-name">{entity.name}</div>
			<div data-testid="metrics-title">{title}</div>
			<div data-testid="default-date-range">{defaultDateRange}</div>
			{renderAdditionalComponents && (
				<div data-testid="additional-components">
					{renderAdditionalComponents()}
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
		const mockContext = createMockApiServiceContext({
			teamMetricsService: mockTeamMetricsService,
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

		// Act
		setupTest(team);

		// Assert
		expect(screen.getByTestId("base-metrics-view")).toBeInTheDocument();
		expect(screen.getByTestId("entity-name")).toHaveTextContent("Test Team");
		expect(screen.getByTestId("metrics-title")).toHaveTextContent("Work Items");
		expect(screen.getByTestId("default-date-range")).toHaveTextContent("30");
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
				"Error fetching features in progress:",
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
});
