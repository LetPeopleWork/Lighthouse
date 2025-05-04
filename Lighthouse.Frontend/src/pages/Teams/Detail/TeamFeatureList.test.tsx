import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamMetricsService } from "../../../services/Api/TeamMetricsService";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import TeamFeatureList from "./TeamFeatureList";

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
					{features.map((feature: Feature) => (
						<tr key={feature.id}>{renderTableRow(feature)}</tr>
					))}
				</tbody>
			</table>
		</div>
	),
}));

const mockTeamMetricsService: ITeamMetricsService =
	createMockTeamMetricsService();

const mockGetFeaturesInProgress = vi.fn();
mockTeamMetricsService.getFeaturesInProgress = mockGetFeaturesInProgress;
mockGetFeaturesInProgress.mockResolvedValue([]);

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

describe("TeamFeatureList component", () => {
	const team: Team = (() => {
		const team = new Team();
		team.name = "Team A";
		team.id = 1;
		team.projects = [];
		team.features = [
			(() => {
				const feature = new Feature();
				feature.name = "Feature 1";
				feature.id = 1;
				feature.workItemReference = "FTR-1";
				feature.stateCategory = "ToDo";
				feature.lastUpdated = new Date();
				feature.isUsingDefaultFeatureSize = false;
				feature.projects = { 0: "" };
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
				return feature;
			})(),
			(() => {
				const feature = new Feature();
				feature.name = "Feature 2";
				feature.id = 2;
				feature.workItemReference = "FTR-2";
				feature.stateCategory = "Doing";
				feature.lastUpdated = new Date();
				feature.isUsingDefaultFeatureSize = true;
				feature.projects = { 0: "" };
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
				return feature;
			})(),
			(() => {
				const feature = new Feature();
				feature.name = "Feature 3";
				feature.id = 3;
				feature.workItemReference = "FTR-3";
				feature.stateCategory = "Done";
				feature.lastUpdated = new Date();
				feature.isUsingDefaultFeatureSize = false;
				feature.projects = { 0: "" };
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
				return feature;
			})(),
		];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date(
			new Date().setDate(new Date().getDate() - [1].length),
		);
		team.throughputEndDate = new Date();
		return team;
	})();

	it("should render all features with correct data", () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Verify the base component was used
		expect(screen.getByTestId("feature-list-base")).toBeInTheDocument();

		for (const feature of team.features) {
			const featureNameElement = screen.getByText(feature.name);
			expect(featureNameElement).toBeInTheDocument();
		}

		const forecastInfoListElements = screen.getAllByTestId((id) =>
			id.startsWith("forecast-info-list-"),
		);
		expect(forecastInfoListElements.length).toBe(team.features.length);

		const localDateTimeDisplayElements = screen.getAllByTestId(
			"local-date-time-display",
		);
		expect(localDateTimeDisplayElements.length).toBe(team.features.length);
	});

	it("should render the correct number of features", () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		// Check that our base component is rendered
		expect(screen.getByTestId("feature-list-base")).toBeInTheDocument();

		// Check for feature names
		expect(screen.getByText("Feature 1")).toBeInTheDocument();
		expect(screen.getByText("Feature 2")).toBeInTheDocument();
		expect(screen.getByText("Feature 3")).toBeInTheDocument();
	});

	it("should render appropriate table headers", () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		expect(screen.getByText("Feature Name")).toBeInTheDocument();
		expect(screen.getByText("Progress")).toBeInTheDocument();
		expect(screen.getByText("Forecasts")).toBeInTheDocument();
		expect(screen.getByText("Projects")).toBeInTheDocument();
		expect(screen.getByText("Updated On")).toBeInTheDocument();
	});
});
