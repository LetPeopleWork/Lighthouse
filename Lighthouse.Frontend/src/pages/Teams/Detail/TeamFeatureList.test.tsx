import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IFeatureService } from "../../../services/Api/FeatureService";
import type { ITeamMetricsService } from "../../../services/Api/MetricsService";
import {
	createMockApiServiceContext,
	createMockFeatureService,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import TeamFeatureList from "./TeamFeatureList";

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			if (key === "feature") return "Feature";
			if (key === "features") return "Features";
			if (key === "portfolio") return "Portfolio";
			if (key === "portfolios") return "Portfolios";
			return "Unknown";
		},
	}),
}));

const mockTeamMetricsService: ITeamMetricsService =
	createMockTeamMetricsService();

const mockFeatureService: IFeatureService = createMockFeatureService();

const mockGetFeaturesInProgress = vi.fn();
mockTeamMetricsService.getFeaturesInProgress = mockGetFeaturesInProgress;
mockGetFeaturesInProgress.mockResolvedValue([]);

const mockGetFeaturesByIds = vi.fn();
mockFeatureService.getFeaturesByIds = mockGetFeaturesByIds;

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		teamMetricsService: mockTeamMetricsService,
		featureService: mockFeatureService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

describe("TeamFeatureList component", () => {
	const team: Team = (() => {
		const team = new Team();
		team.name = "Team A";
		team.id = 1;
		team.portfolios = [];
		team.features = [
			{ id: 1, name: "Feature 1" },
			{ id: 2, name: "Feature 2" },
			{ id: 3, name: "Feature 3" },
		];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date(
			new Date().setDate(new Date().getDate() - 30),
		);
		team.throughputEndDate = new Date();
		return team;
	})();

	const mockFeatures: Feature[] = [
		(() => {
			const feature = new Feature();
			feature.name = "Feature 1";
			feature.id = 1;
			feature.referenceId = "FTR-1";
			feature.stateCategory = "ToDo";
			feature.lastUpdated = new Date();
			feature.isUsingDefaultFeatureSize = false;
			feature.projects = [{ id: 0, name: "Test Project" }];
			feature.remainingWork = { 1: 10 };
			feature.totalWork = { 1: 10 };
			feature.forecasts = [
				(() => {
					const forecast = new WhenForecast();
					forecast.probability = 80;
					forecast.expectedDate = new Date();
					return forecast;
				})(),
			];
			feature.startedDate = new Date("2023-07-01");
			feature.closedDate = new Date("2023-07-10");
			feature.cycleTime = 9;
			feature.workItemAge = 10;
			feature.url = "";
			return feature;
		})(),
		(() => {
			const feature = new Feature();
			feature.name = "Feature 2";
			feature.id = 2;
			feature.referenceId = "FTR-2";
			feature.stateCategory = "Doing";
			feature.lastUpdated = new Date();
			feature.isUsingDefaultFeatureSize = true;
			feature.projects = [{ id: 0, name: "Test Project" }];
			feature.remainingWork = { 1: 5 };
			feature.totalWork = { 1: 10 };
			feature.forecasts = [
				(() => {
					const forecast = new WhenForecast();
					forecast.probability = 60;
					forecast.expectedDate = new Date();
					return forecast;
				})(),
			];
			feature.startedDate = new Date("2023-07-01");
			feature.closedDate = new Date("2023-07-09");
			feature.cycleTime = 8;
			feature.workItemAge = 9;
			feature.url = "";
			return feature;
		})(),
		(() => {
			const feature = new Feature();
			feature.name = "Feature 3";
			feature.id = 3;
			feature.referenceId = "FTR-3";
			feature.stateCategory = "Done";
			feature.lastUpdated = new Date();
			feature.isUsingDefaultFeatureSize = false;
			feature.projects = [{ id: 0, name: "Test Project" }];
			feature.remainingWork = { 1: 0 };
			feature.totalWork = { 1: 10 };
			feature.forecasts = [
				(() => {
					const forecast = new WhenForecast();
					forecast.probability = 100;
					forecast.expectedDate = new Date();
					return forecast;
				})(),
			];
			feature.startedDate = new Date("2023-07-01");
			feature.closedDate = new Date("2023-07-08");
			feature.cycleTime = 7;
			feature.workItemAge = 8;
			feature.url = "";
			return feature;
		})(),
	];

	beforeEach(() => {
		mockGetFeaturesByIds.mockResolvedValue(mockFeatures);
		// Mock matchMedia for MUI DataGrid
		Object.defineProperty(globalThis, "matchMedia", {
			writable: true,
			value: vi.fn().mockImplementation((query) => ({
				matches: false,
				media: query,
				onchange: null,
				addListener: vi.fn(),
				removeListener: vi.fn(),
				addEventListener: vi.fn(),
				removeEventListener: vi.fn(),
				dispatchEvent: vi.fn(),
			})),
		});
		// Clear localStorage before each test
		localStorage.clear();
	});

	it("should render all features with correct data", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for features to load
		await screen.findByText(/FTR-1/);

		// Uncheck the toggle to show all features including completed ones
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		// Check for the feature names using flexible regex matching
		expect(screen.getByText(/FTR-1/)).toBeInTheDocument();
		expect(screen.getByText(/Feature 1/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-2/)).toBeInTheDocument();
		expect(screen.getByText(/Feature 2/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-3/)).toBeInTheDocument();
		expect(screen.getByText(/Feature 3/)).toBeInTheDocument();
	});

	it("should render the correct number of features", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for features to load
		await screen.findByText(/FTR-1/);

		// Uncheck the toggle to show all features including completed ones
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		// Check for feature names
		expect(screen.getByText(/FTR-1/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-2/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-3/)).toBeInTheDocument();
	});

	it("should render appropriate table headers", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for DataGrid to render
		await screen.findByText(/Feature Name/);

		expect(screen.getByText("Feature Name")).toBeInTheDocument();
		expect(screen.getByText("Progress")).toBeInTheDocument();
		expect(screen.getByText("Parent")).toBeInTheDocument();
		expect(screen.getByText("Portfolios")).toBeInTheDocument();
		expect(screen.getByText("Forecasts")).toBeInTheDocument();
	});

	it("should render toggle for hide completed features", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for component to render
		await screen.findByTestId("hide-completed-features-toggle");

		expect(
			screen.getByTestId("hide-completed-features-toggle"),
		).toBeInTheDocument();
	});

	it("should have toggle checked by default (hide completed features)", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for toggle to render
		const toggle = await screen.findByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');

		expect(switchInput).toBeChecked();
	});

	it("should hide completed features by default", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for features to load
		await screen.findByText(/FTR-1/);

		// Non-completed features should be visible (Feature 1 and 2)
		expect(screen.getByText(/FTR-1/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-2/)).toBeInTheDocument();

		// Completed feature should not be visible (Feature 3)
		expect(screen.queryByText(/FTR-3/)).not.toBeInTheDocument();
	});

	it("should show completed features when toggle is unchecked", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for features to load
		await screen.findByText(/FTR-1/);

		// Completed feature should not be visible initially
		expect(screen.queryByText(/FTR-3/)).not.toBeInTheDocument();

		// Click the toggle to show completed features
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		// Now all features should be visible
		expect(screen.getByText(/FTR-1/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-2/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-3/)).toBeInTheDocument();
	});

	it("should save toggle preference to localStorage when changed", async () => {
		const { default: userEvent } = await import("@testing-library/user-event");
		const user = userEvent.setup();

		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for component to render
		await screen.findByTestId("hide-completed-features-toggle");

		// Toggle should be checked by default, localStorage should have "true"
		expect(
			localStorage.getItem("lighthouse_hide_completed_features_team_1"),
		).toBe("true");

		// Click the toggle to show completed features
		const toggle = screen.getByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');
		await user.click(switchInput as HTMLElement);

		// localStorage should now be "false"
		expect(
			localStorage.getItem("lighthouse_hide_completed_features_team_1"),
		).toBe("false");
	});

	it("should load toggle preference from localStorage on mount", async () => {
		// Set localStorage to false before rendering
		localStorage.setItem("lighthouse_hide_completed_features_team_1", "false");

		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for toggle to render
		const toggle = await screen.findByTestId("hide-completed-features-toggle");
		const switchInput = toggle.querySelector('input[type="checkbox"]');

		// Toggle should be unchecked based on localStorage
		expect(switchInput).not.toBeChecked();

		// All features should be visible
		await screen.findByText(/FTR-3/);
		expect(screen.getByText(/FTR-1/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-2/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-3/)).toBeInTheDocument();
	});
});
