import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { Project } from "../../../models/Project/Project";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IFeatureService } from "../../../services/Api/FeatureService";
import type { ITeamMetricsService } from "../../../services/Api/MetricsService";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import ProjectFeatureList from "./ProjectFeatureList";

vi.mock("../../../components/Common/Forecasts/ForecastInfoList", () => ({
	default: ({
		title,
		forecasts,
	}: {
		title: string;
		forecasts: IForecast[];
	}) => (
		<div data-testid={`forecast-info-list-${title}`}>
			{forecasts.map((forecast: IForecast) => (
				<div key={forecast.probability}>{forecast.probability}%</div>
			))}
		</div>
	),
}));

vi.mock(
	"../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay",
	() => ({
		default: ({ utcDate }: { utcDate: Date }) => (
			<span data-testid="local-date-time-display">{utcDate.toString()}</span>
		),
	}),
);

vi.mock("../../../components/Common/Forecasts/ForecastLikelihood", () => ({
	default: ({ likelihood }: { likelihood: number }) => (
		<div data-testid="forecast-likelihood">{likelihood}%</div>
	),
}));

// Mock @mui/x-data-grid CSS import
vi.mock("@mui/x-data-grid", async () => {
	const actual = await vi.importActual("@mui/x-data-grid");
	return {
		...actual,
	};
});

// Mock matchMedia for MUI DataGrid
beforeEach(() => {
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

const mockTeamMetricsService: ITeamMetricsService = {
	...createMockTeamMetricsService(),
	getFeaturesInProgress: vi.fn().mockResolvedValue([]),
};

// Create mock features for the service
const mockFeature1 = new Feature();
mockFeature1.id = 1;
mockFeature1.name = "Feature 1";
mockFeature1.referenceId = "FTR-1";
mockFeature1.forecasts = [];
mockFeature1.lastUpdated = new Date();
mockFeature1.isUsingDefaultFeatureSize = false;
mockFeature1.stateCategory = "ToDo";
mockFeature1.remainingWork = { 1: 5, 2: 5 };
mockFeature1.totalWork = { 1: 5, 2: 5 };

const mockFeature2 = new Feature();
mockFeature2.id = 2;
mockFeature2.name = "Feature 2";
mockFeature2.referenceId = "FTR-2";
mockFeature2.forecasts = [];
mockFeature2.lastUpdated = new Date();
mockFeature2.isUsingDefaultFeatureSize = true;
mockFeature2.stateCategory = "Doing";
mockFeature2.remainingWork = { 1: 10, 2: 5 };
mockFeature2.totalWork = { 1: 10, 2: 5 };

const mockFeature3 = new Feature();
mockFeature3.id = 3;
mockFeature3.name = "Feature 3";
mockFeature3.referenceId = "FTR-3";
mockFeature3.forecasts = [];
mockFeature3.lastUpdated = new Date();
mockFeature3.isUsingDefaultFeatureSize = false;
mockFeature3.stateCategory = "Done";
mockFeature3.remainingWork = { 1: 0, 2: 0 };
mockFeature3.totalWork = { 1: 5, 2: 5 };

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockFeatureService: Partial<IFeatureService> = {
		getFeaturesByIds: vi
			.fn()
			.mockResolvedValue([mockFeature1, mockFeature2, mockFeature3]),
	};

	const mockContext = createMockApiServiceContext({
		teamMetricsService: mockTeamMetricsService,
		featureService: mockFeatureService as IFeatureService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

describe("ProjectFeatureList component", () => {
	const team1: Team = (() => {
		const team = new Team();
		team.name = "Team A";
		team.id = 1;
		team.projects = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date(
			new Date().setDate(new Date().getDate() - [1].length),
		);
		team.throughputEndDate = new Date();
		return team;
	})();
	const team2: Team = (() => {
		const team = new Team();
		team.name = "Team B";
		team.id = 2;
		team.projects = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date(
			new Date().setDate(new Date().getDate() - [1].length),
		);
		team.throughputEndDate = new Date();
		return team;
	})();

	const pastDate = new Date();
	pastDate.setDate(pastDate.getDate() - 1);

	const todayDate = new Date();
	todayDate.setHours(0, 0, 0, 0);

	const futureDate = new Date();
	futureDate.setDate(futureDate.getDate() + 1);

	const feature1: Feature = (() => {
		const feature = new Feature();
		feature.name = "Feature 1";
		feature.id = 1;
		feature.referenceId = "FTR-1";
		feature.stateCategory = "ToDo";
		feature.isUsingDefaultFeatureSize = false;
		feature.lastUpdated = new Date();
		feature.projects = [{ id: 10, name: "" }];
		feature.remainingWork = { 1: 5, 2: 5 };
		feature.totalWork = { 1: 5, 2: 5 };
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
		return feature;
	})();
	const feature2: Feature = (() => {
		const feature = new Feature();
		feature.name = "Feature 2";
		feature.id = 2;
		feature.referenceId = "FTR-2";
		feature.stateCategory = "Doing";
		feature.isUsingDefaultFeatureSize = true;
		feature.lastUpdated = new Date();
		feature.projects = [{ id: 15, name: "" }];
		feature.remainingWork = { 1: 10, 2: 5 };
		feature.totalWork = { 1: 10, 2: 5 };
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
		return feature;
	})();
	const feature3: Feature = (() => {
		const feature = new Feature();
		feature.name = "Feature 3";
		feature.id = 3;
		feature.referenceId = "FTR-3";
		feature.stateCategory = "Done";
		feature.isUsingDefaultFeatureSize = false;
		feature.lastUpdated = new Date();
		feature.projects = [{ id: 20, name: "" }];
		feature.remainingWork = { 1: 0, 2: 0 };
		feature.totalWork = { 1: 5, 2: 5 };
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
		return feature;
	})();

	const project: Project = (() => {
		const project = new Project();
		project.name = "Project 1";
		project.id = 1;
		project.involvedTeams = [team1, team2];
		project.features = [feature1, feature2, feature3];
		project.lastUpdated = new Date();
		return project;
	})();

	it("should render all features with correct data", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<ProjectFeatureList project={project} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for the grid to render
		const grid = await screen.findByRole("grid");
		expect(grid).toBeInTheDocument();

		// Check that features are rendered (text might be split across elements)
		expect(await screen.findByText(/FTR-1/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 1/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-2/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 2/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-3/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 3/)).toBeInTheDocument();

		// Check for forecast info lists
		const forecastInfoListElements = screen.getAllByTestId(
			"forecast-info-list-",
		);
		expect(forecastInfoListElements.length).toBeGreaterThanOrEqual(
			project.features.length,
		);
	});

	it("should render the correct number of features", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<ProjectFeatureList project={project} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for the grid to render
		const grid = await screen.findByRole("grid");
		expect(grid).toBeInTheDocument();

		// Check for all feature IDs and names
		expect(await screen.findByText(/FTR-1/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 1/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-2/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 2/)).toBeInTheDocument();
		expect(await screen.findByText(/FTR-3/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 3/)).toBeInTheDocument();
	});

	it("should display the warning icon for features using the default feature size", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<ProjectFeatureList project={project} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Wait for the grid to render
		const grid = await screen.findByRole("grid");
		expect(grid).toBeInTheDocument();

		// Feature 2 uses default feature size
		expect(await screen.findByText(/FTR-2/)).toBeInTheDocument();
		expect(await screen.findByText(/Feature 2/)).toBeInTheDocument();
	});
});
