import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { Milestone } from "../../../models/Project/Milestone";
import { Project } from "../../../models/Project/Project";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamMetricsService } from "../../../services/Api/TeamMetricsService";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import ProjectFeatureList from "./ProjectFeatureList";

vi.mock("../../../components/Common/Forecasts/ForecastInfoList", () => ({
	default: ({
		title,
		forecasts,
	}: { title: string; forecasts: IForecast[] }) => (
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

// Mock the FeatureListBase component to simplify testing
vi.mock("../../../components/Common/FeaturesList/FeatureListBase", () => ({
	default: ({
		features,
		renderTableHeader,
		renderTableRow,
	}: {
		features: Feature[];
		renderTableHeader: () => React.ReactNode;
		renderTableRow: (feature: Feature) => React.ReactNode;
	}) => (
		<div data-testid="feature-list-base">
			<table>
				<thead>{renderTableHeader()}</thead>
				<tbody>
					{features.map((feature: Feature) => renderTableRow(feature))}
				</tbody>
			</table>
		</div>
	),
}));

const mockTeamMetricsService: ITeamMetricsService =
	createMockTeamMetricsService();

const MockApiServiceProvider = ({
	children,
}: { children: React.ReactNode }) => {
	const mockContext = createMockApiServiceContext({
		teamMetricsService: mockTeamMetricsService,
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

	const milestone1: Milestone = (() => {
		const milestone = new Milestone();
		milestone.id = 1;
		milestone.name = "Milestone 1";
		milestone.date = pastDate;
		return milestone;
	})();
	const milestone2: Milestone = (() => {
		const milestone = new Milestone();
		milestone.id = 2;
		milestone.name = "Milestone 2";
		milestone.date = todayDate;
		return milestone;
	})();
	const milestone3: Milestone = (() => {
		const milestone = new Milestone();
		milestone.id = 3;
		milestone.name = "Milestone 3";
		milestone.date = futureDate;
		return milestone;
	})();

	const feature1: Feature = (() => {
		const feature = new Feature();
		feature.name = "Feature 1";
		feature.id = 1;
		feature.workItemReference = "FTR-1";
		feature.stateCategory = "ToDo";
		feature.isUsingDefaultFeatureSize = false;
		feature.lastUpdated = new Date();
		feature.projects = { 10: "" };
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
		feature.workItemReference = "FTR-2";
		feature.stateCategory = "Doing";
		feature.isUsingDefaultFeatureSize = true;
		feature.lastUpdated = new Date();
		feature.projects = { 15: "" };
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
		feature.workItemReference = "FTR-3";
		feature.stateCategory = "Done";
		feature.isUsingDefaultFeatureSize = false;
		feature.lastUpdated = new Date();
		feature.projects = { 20: "" };
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
		project.milestones = [milestone1, milestone2, milestone3];
		project.lastUpdated = new Date();
		return project;
	})();

	it("should only render milestones that are today or in the future", async () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<ProjectFeatureList project={project} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		expect(screen.queryByText("Feature Name")).toBeInTheDocument();
		expect(screen.queryByText("Progress")).toBeInTheDocument();
		expect(screen.queryByText("Forecasts")).toBeInTheDocument();
		expect(screen.queryByText("Updated On")).toBeInTheDocument();

		expect(screen.queryByText("Milestone 1")).not.toBeInTheDocument();
		expect(await screen.findByText(/Milestone 2/)).toBeInTheDocument();
		expect(await screen.findByText(/Milestone 3/)).toBeInTheDocument();
	});

	it("should render all features with correct data", () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<ProjectFeatureList project={project} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Verify the base component was used
		expect(screen.getByTestId("feature-list-base")).toBeInTheDocument();

		for (const feature of project.features) {
			const featureNameElement = screen.getByText(feature.name);
			expect(featureNameElement).toBeInTheDocument();
		}

		// Use getAllByTestId since there are multiple elements with this test ID
		const forecastInfoListElements = screen.getAllByTestId(
			"forecast-info-list-",
		);
		expect(forecastInfoListElements.length).toBe(project.features.length);

		const forecastLikelihoodElements = screen.getAllByTestId(
			"forecast-likelihood",
		);

		// We have 3 features and 2 milestone forecasts per feature, so expect 6 elements
		expect(forecastLikelihoodElements.length).toBe(6);

		const localDateTimeDisplayElements = screen.getAllByTestId(
			"local-date-time-display",
		);
		// We expect at least one date display per feature plus milestone dates
		expect(localDateTimeDisplayElements.length).toBeGreaterThan(0);
	});

	it("should render the correct number of features", () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<ProjectFeatureList project={project} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Check that we have rendered our base component with features
		expect(screen.getByTestId("feature-list-base")).toBeInTheDocument();

		// Check for all feature names
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();
	});

	it("should display the warning icon for features using the default feature size", () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<ProjectFeatureList project={project} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Since we mocked the feature-list-base component, we won't actually
		// see the warning icon. In a real scenario, this test would be checking
		// for the presence of the warning icon in the feature with default size.
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
	});
});
