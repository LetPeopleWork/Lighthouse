import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
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

		for (const feature of team.features) {
			const featureNameElement = screen.getByText(feature.name);
			expect(featureNameElement).toBeInTheDocument();

			const forecastInfoListElements = screen.getAllByTestId((id) =>
				id.startsWith("forecast-info-list-"),
			);

			for (const element of forecastInfoListElements) {
				expect(element).toBeInTheDocument();
			}

			const localDateTimeDisplayElements = screen.getAllByTestId((id) =>
				id.startsWith("local-date-time-display"),
			);

			for (const localDateTimeDisplayElement of localDateTimeDisplayElements) {
				expect(localDateTimeDisplayElement).toBeInTheDocument();
			}
		}
	});

	it("should render the correct number of features", () => {
		render(
			<MockApiServiceProvider>
				<MemoryRouter>
					<TeamFeatureList team={team} />
				</MemoryRouter>
			</MockApiServiceProvider>,
		);

		const featureRows = screen.getAllByRole("row");
		expect(featureRows).toHaveLength(team.features.length + 1); // Including the header row
	});
});
