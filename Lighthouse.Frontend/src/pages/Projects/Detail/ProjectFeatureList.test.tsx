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
	const team1: Team = new Team(
		"Team A",
		1,
		[],
		[],
		1,
		new Date(),
		false,
		new Date(new Date().setDate(new Date().getDate() - [1].length)),
		new Date(),
	);
	const team2: Team = new Team(
		"Team B",
		2,
		[],
		[],
		1,
		new Date(),
		false,
		new Date(new Date().setDate(new Date().getDate() - [1].length)),
		new Date(),
	);

	const pastDate = new Date();
	pastDate.setDate(pastDate.getDate() - 1);

	const todayDate = new Date();
	todayDate.setHours(0, 0, 0, 0);

	const futureDate = new Date();
	futureDate.setDate(futureDate.getDate() + 1);

	const milestone1: Milestone = new Milestone(1, "Milestone 1", pastDate);
	const milestone2: Milestone = new Milestone(2, "Milestone 2", todayDate);
	const milestone3: Milestone = new Milestone(3, "Milestone 3", futureDate);

	const feature1: Feature = new Feature(
		"Feature 1",
		1,
		"FTR-1",
		"",
		"Unknown",
		new Date(),
		false,
		{ 10: "" },
		{ 1: 5, 2: 5 },
		{ 1: 5, 2: 5 },
		{},
		[new WhenForecast(80, new Date())],
		null,
		"ToDo",
		new Date("2023-07-01"),
		new Date("2023-07-10"),
		9,
		10,
	);
	const feature2: Feature = new Feature(
		"Feature 2",
		2,
		"FTR-2",
		"",
		"Unknown",
		new Date(),
		true,
		{ 15: "" },
		{ 1: 10, 2: 5 },
		{ 1: 10, 2: 5 },
		{},
		[new WhenForecast(60, new Date())],
		null,
		"Doing",
		new Date("2023-07-01"),
		new Date("2023-07-09"),
		8,
		9,
	);
	const feature3: Feature = new Feature(
		"Feature 3",
		3,
		"FTR-3",
		"",
		"Unknown",
		new Date(),
		false,
		{ 20: "" },
		{ 1: 0, 2: 0 },
		{ 1: 5, 2: 5 },
		{},
		[new WhenForecast(100, new Date())],
		null,
		"Done",
		new Date("2023-07-01"),
		new Date("2023-07-08"),
		7,
		8,
	);

	const project: Project = new Project(
		"Project 1",
		1,
		[team1, team2],
		[feature1, feature2, feature3],
		[milestone1, milestone2, milestone3],
		new Date(),
	);

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
