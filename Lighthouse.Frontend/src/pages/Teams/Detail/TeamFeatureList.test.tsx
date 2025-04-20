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
	const team: Team = new Team(
		"Team A",
		1,
		[],
		[
			new Feature(
				"Feature 1",
				1,
				"FTR-1",
				"",
				"Unknown",
				new Date(),
				false,
				{ 0: "" },
				{ 1: 10 },
				{ 1: 10 },
				{},
				[new WhenForecast(80, new Date())],
				null,
				"ToDo",
				new Date("2023-07-01"),
				new Date("2023-07-10"),
				9,
				10,
			),
			new Feature(
				"Feature 2",
				2,
				"FTR-2",
				"",
				"Unknown",
				new Date(),
				true,
				{ 0: "" },
				{ 1: 5 },
				{ 1: 10 },
				{},
				[new WhenForecast(60, new Date())],
				null,
				"Doing",
				new Date("2023-07-01"),
				new Date("2023-07-09"),
				8,
				9,
			),
			new Feature(
				"Feature 3",
				3,
				"FTR-3",
				"",
				"Unknown",
				new Date(),
				false,
				{ 0: "" },
				{ 1: 0 },
				{ 1: 10 },
				{},
				[new WhenForecast(100, new Date())],
				null,
				"Done",
				new Date("2023-07-01"),
				new Date("2023-07-08"),
				7,
				8,
			),
		],
		1,
		new Date(),
		false,
		new Date(new Date().setDate(new Date().getDate() - [1].length)),
		new Date(),
	);

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
