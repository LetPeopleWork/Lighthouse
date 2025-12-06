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
		team.projects = [];
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
	});

	it("should render all features with correct data", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for features to load
		await screen.findByText(/FTR-1/);

		// Check for the feature names using flexible regex matching
		expect(screen.getByText(/FTR-1/)).toBeInTheDocument();
		expect(screen.getByText(/Feature 1/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-2/)).toBeInTheDocument();
		expect(screen.getByText(/Feature 2/)).toBeInTheDocument();
		expect(screen.getByText(/FTR-3/)).toBeInTheDocument();
		expect(screen.getByText(/Feature 3/)).toBeInTheDocument();
	});

	it("should render the correct number of features", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for features to load
		await screen.findByText(/FTR-1/);

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
  expect(screen.getByText("Updated On")).toBeInTheDocument();
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
});
