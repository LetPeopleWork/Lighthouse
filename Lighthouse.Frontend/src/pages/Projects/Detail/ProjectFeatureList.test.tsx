import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../../models/Feature";
import type { IForecast } from "../../../models/Forecasts/IForecast";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { Milestone } from "../../../models/Project/Milestone";
import { Project } from "../../../models/Project/Project";
import { Team } from "../../../models/Team/Team";
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

describe("ProjectFeatureList component", () => {
	const team1: Team = new Team(
		"Team A",
		1,
		[],
		[],
		1,
		["FTR-1"],
		new Date(),
		[1],
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
		["FTR-1"],
		new Date(),
		[1],
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
	);

	const project: Project = new Project(
		"Project 1",
		1,
		[team1, team2],
		[feature1, feature2],
		[milestone1, milestone2, milestone3],
		new Date(),
	);

	it("should only render milestones that are today or in the future", async () => {
		render(
			<MemoryRouter>
				<ProjectFeatureList project={project} />
			</MemoryRouter>,
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
			<MemoryRouter>
				<ProjectFeatureList project={project} />
			</MemoryRouter>,
		);

		for (const feature of project.features) {
			const featureRow = screen.getByText(feature.name).closest("tr");
			expect(featureRow).toBeInTheDocument();

			if (!featureRow) {
				throw new Error(`Feature row for ${feature.name} not found`);
			}
			const withinRow = within(featureRow);

			const forecastInfoListElement = withinRow.getByTestId(
				"forecast-info-list-",
			);
			expect(forecastInfoListElement).toBeInTheDocument();

			const forecastLikelihoodElements = withinRow.getAllByTestId(
				"forecast-likelihood",
			);
			project.milestones
				.filter((milestone) => milestone.date >= new Date())
				.forEach((milestone, index) => {
					expect(forecastLikelihoodElements[index]).toBeInTheDocument();
					expect(forecastLikelihoodElements[index]).toHaveTextContent(
						`${feature.getMilestoneLikelihood(milestone.id)}%`,
					);
				});

			const localDateTimeDisplayElement = withinRow.getByTestId(
				"local-date-time-display",
			);
			expect(localDateTimeDisplayElement).toBeInTheDocument();
		}
	});

	it("should render the correct number of features", () => {
		render(
			<MemoryRouter>
				<ProjectFeatureList project={project} />
			</MemoryRouter>,
		);

		const featureRows = screen.getAllByRole("row");
		expect(featureRows).toHaveLength(project.features.length + 1);
	});

	it("should display the warning icon for features using the default feature size", () => {
		render(
			<MemoryRouter>
				<ProjectFeatureList project={project} />
			</MemoryRouter>,
		);

		const featureRowWithDefaultSize = screen
			.getByText(feature2.name)
			.closest("tr");
		expect(featureRowWithDefaultSize).toBeInTheDocument();

		if (featureRowWithDefaultSize) {
			const warningIcons = within(featureRowWithDefaultSize).queryAllByRole(
				"button",
			);
			const warningIcon = warningIcons[0];
			expect(warningIcon).toBeInTheDocument();
		}
	});
});
