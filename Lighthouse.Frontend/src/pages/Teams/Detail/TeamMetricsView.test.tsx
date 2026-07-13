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
	featuresInProgress?: Array<{ id: number; name: string }>;
	featureWip?: number;
	hasBlockedConfig?: boolean;
	hasForecastFilter?: boolean;
	forecastFilterConditions?: ReadonlyArray<{
		fieldKey: string;
		operator: string;
		value: string;
	}>;
}

vi.mock("../../Common/MetricsView/BaseMetricsView", () => ({
	BaseMetricsView: ({
		entity,
		title,
		defaultDateRange,
		featuresInProgress,
		featureWip,
		hasBlockedConfig,
		hasForecastFilter,
		forecastFilterConditions,
	}: BaseMetricsViewProps) => (
		<div data-testid="base-metrics-view">
			<div data-testid="entity-name">{entity.name}</div>
			<div data-testid="metrics-title">{title}</div>
			<div data-testid="default-date-range">{defaultDateRange}</div>
			{featuresInProgress && (
				<div data-testid="features-in-progress">
					<div data-testid="features-count">{featuresInProgress.length}</div>
					{featureWip !== undefined && (
						<div data-testid="feature-wip">{featureWip}</div>
					)}
				</div>
			)}
			{hasBlockedConfig !== undefined && (
				<div data-testid="has-blocked-config">{String(hasBlockedConfig)}</div>
			)}
			<div data-testid="has-forecast-filter">
				{String(hasForecastFilter ?? false)}
			</div>
			<div data-testid="forecast-filter-condition-count">
				{forecastFilterConditions?.length ?? 0}
			</div>
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
			blockedStates: [],
			blockedTags: [],
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
			expect(mockGetFeaturesInProgress).toHaveBeenCalledWith(
				team.id,
				expect.any(Date),
			);
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
			blockedStates: [],
			blockedTags: [],
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
			const featureWipEl = screen.getByTestId("feature-wip");
			expect(featureWipEl).toHaveTextContent("5");
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
			blockedStates: [],
			blockedTags: [],
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
			// featureWip should not be rendered if undefined
			const featuresInProgress = screen.getByTestId("features-in-progress");
			expect(
				featuresInProgress.querySelector("[data-testid='feature-wip']"),
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
			blockedStates: [],
			blockedTags: [],
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
			const featuresInProgress = screen.getByTestId("features-in-progress");
			expect(
				featuresInProgress.querySelector("[data-testid='feature-wip']"),
			).toBeNull();
		});
	});

	it("should render features in progress with fetched features", async () => {
		// Arrange
		const team = new Team();
		team.id = 1;
		team.name = "Test Team";
		team.featureWip = 3;

		// Act
		setupTest(team);

		// Assert
		await waitFor(() => {
			const featuresInProgress = screen.getByTestId("features-in-progress");
			expect(featuresInProgress).toBeInTheDocument();
			expect(screen.getByTestId("features-count")).toHaveTextContent("2");
			expect(screen.getByTestId("feature-wip")).toHaveTextContent("3");
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
			expect(mockGetFeaturesInProgress).toHaveBeenCalledWith(
				team.id,
				expect.any(Date),
			);
			expect(consoleSpy).toHaveBeenCalledWith(
				"Error fetching Features in progress:",
				expect.any(Error),
			);
		});

		// Should still render the component with empty features
		await waitFor(() => {
			const featuresInProgress = screen.getByTestId("features-in-progress");
			expect(featuresInProgress).toBeInTheDocument();
			expect(screen.getByTestId("features-count")).toHaveTextContent("0");
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

	describe("forecast filter wiring", () => {
		const renderWithFilterSettings = (
			forecastFilterRuleSetJson: string | null,
		) => {
			const team = new Team();
			team.id = 1;
			team.name = "Filtered Team";

			const mockTeamService = createMockTeamService();
			mockTeamService.getTeamSettings = vi.fn().mockResolvedValue({
				automaticallyAdjustFeatureWIP: true,
				doingStates: ["In Progress"],
				blockedStates: [],
				blockedTags: [],
				forecastFilterRuleSetJson,
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

		it("passes hasForecastFilter=false when the team has no forecast filter configured", async () => {
			renderWithFilterSettings(null);
			await waitFor(() => {
				expect(screen.getByTestId("has-forecast-filter")).toHaveTextContent(
					"false",
				);
				expect(
					screen.getByTestId("forecast-filter-condition-count"),
				).toHaveTextContent("0");
			});
		});

		it("propagates parsed forecastFilter conditions when the team has rules", async () => {
			const ruleSet = JSON.stringify({
				version: 1,
				conditions: [
					{ fieldKey: "workitem.type", operator: "equals", value: "Bug" },
					{ fieldKey: "workitem.state", operator: "notequals", value: "Done" },
				],
			});

			renderWithFilterSettings(ruleSet);

			await waitFor(() => {
				expect(screen.getByTestId("has-forecast-filter")).toHaveTextContent(
					"true",
				);
				expect(
					screen.getByTestId("forecast-filter-condition-count"),
				).toHaveTextContent("2");
			});
		});
	});

	describe("blocked config wiring (ADO #5480)", () => {
		const renderWithBlockedSettings = (settings: {
			blockedStates?: string[];
			blockedTags?: string[];
			blockedRuleSetJson?: string | null;
		}) => {
			const team = new Team();
			team.id = 1;
			team.name = "Blocked Team";

			const mockTeamService = createMockTeamService();
			mockTeamService.getTeamSettings = vi.fn().mockResolvedValue({
				doingStates: ["In Progress"],
				blockedStates: settings.blockedStates ?? [],
				blockedTags: settings.blockedTags ?? [],
				blockedRuleSetJson: settings.blockedRuleSetJson,
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

		it("passes hasBlockedConfig=false when no rule set and no legacy fields are configured", async () => {
			renderWithBlockedSettings({ blockedRuleSetJson: null });

			await waitFor(() => {
				expect(screen.getByTestId("has-blocked-config")).toHaveTextContent(
					"false",
				);
			});
		});

		it("passes hasBlockedConfig=true from blockedRuleSetJson even when legacy blockedStates/blockedTags are empty", async () => {
			// Regression test for ADO #5480: a team configured purely through the
			// rule-based editor (Epic 5074) never populates the legacy blockedStates/
			// blockedTags arrays. hasBlockedConfig must be derived from the rule set
			// the backend already returns effective (legacy-synthesized) — not from
			// the legacy arrays, which stay empty forever for rule-only teams.
			const ruleSet = JSON.stringify({
				version: 1,
				mode: "or",
				conditions: [
					{ fieldKey: "workitem.state", operator: "equals", value: "Blocked" },
				],
			});

			renderWithBlockedSettings({
				blockedStates: [],
				blockedTags: [],
				blockedRuleSetJson: ruleSet,
			});

			await waitFor(() => {
				expect(screen.getByTestId("has-blocked-config")).toHaveTextContent(
					"true",
				);
			});
		});
	});
});
