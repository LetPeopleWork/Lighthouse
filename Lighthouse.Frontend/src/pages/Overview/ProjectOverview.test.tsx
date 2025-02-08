import { render, screen } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { Feature } from "../../models/Feature";
import { WhenForecast } from "../../models/Forecasts/WhenForecast";
import { Milestone } from "../../models/Project/Milestone";
import { Project } from "../../models/Project/Project";
import { Team } from "../../models/Team/Team";
import ProjectOverview from "./ProjectOverview";

describe("ProjectOverview component", () => {
	const projects: Project[] = [
		new Project(
			"Project Alpha",
			1,
			[
				new Team(
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
				),
				new Team(
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
				),
			],
			[
				new Feature(
					"Feature 1",
					1,
					"FTR-1",
					"",
					new Date("2024-06-01"),
					false,
					{ 1: "Project Alpha" },
					{},
					{},
					{},
					[
						new WhenForecast(50, new Date("2024-01-01")),
						new WhenForecast(70, new Date("2024-02-01")),
						new WhenForecast(85, new Date("2024-03-01")),
						new WhenForecast(95, new Date("2024-04-01")),
					],
				),
			],
			[
				new Milestone(
					0,
					"Milestone 1",
					new Date(Date.now() + 14 * 24 * 60 * 60),
				),
			],
			new Date("2024-06-01"),
		),
		new Project(
			"Project Beta",
			2,
			[
				new Team(
					"Team C",
					3,
					[],
					[],
					2,
					["FTR-1", "FTR-3"],
					new Date(),
					[1],
					false,
					new Date(new Date().setDate(new Date().getDate() - [1].length)),
					new Date(),
				),
			],
			[
				new Feature(
					"Feature 3",
					3,
					"FTR-3",
					"",
					new Date("2024-06-01"),
					true,
					{ 2: "Project Beta" },
					{},
					{},
					{},
					[
						new WhenForecast(50, new Date("2024-01-01")),
						new WhenForecast(70, new Date("2024-02-01")),
						new WhenForecast(85, new Date("2024-03-01")),
						new WhenForecast(95, new Date("2024-04-01")),
					],
				),
			],
			[
				new Milestone(
					1,
					"Milestone 2",
					new Date(Date.now() + 14 * 24 * 60 * 60),
				),
			],
			new Date("2024-06-01"),
		),
	];

	it("should render all projects when no filter is applied", () => {
		const { container } = render(
			<Router>
				<ProjectOverview projects={projects} filterText="" />
			</Router>,
		);

		// Check if all projects are rendered
		for (const project of projects) {
			const projectCard = container.querySelector(
				`[data-testid="project-card-${project.id}"]`,
			);
			expect(projectCard).toBeInTheDocument();
		}

		// Check if no projects found message is not rendered
		const noProjectsMessage = screen.queryByTestId("no-projects-message");
		expect(noProjectsMessage).not.toBeInTheDocument();
	});

	it("should render filtered projects based on filterText", () => {
		const { container } = render(
			<Router>
				<ProjectOverview projects={projects} filterText="A" />
			</Router>,
		);

		// Check if only projects matching filterText are rendered
		const filteredProjects = projects.filter((project) =>
			project.name.toLowerCase().includes("a"),
		);

		for (const project of filteredProjects) {
			const projectCard = container.querySelector(
				`[data-testid="project-card-${project.id}"]`,
			);
			expect(projectCard).toBeInTheDocument();
		}

		// Check if no projects found message is not rendered
		const noProjectsMessage = screen.queryByTestId("no-projects-message");
		expect(noProjectsMessage).not.toBeInTheDocument();
	});

	it("should render no projects found message when no projects match the filter", () => {
		render(
			<Router>
				<ProjectOverview projects={projects} filterText="XYZ" />
			</Router>,
		);

		// Check if no projects found message is rendered
		const noProjectsMessage = screen.getByText(
			"No projects found matching the filter.",
		);
		expect(noProjectsMessage).toBeInTheDocument();

		// Check if project cards are not rendered
		for (const project of projects) {
			const projectCard = screen.queryByTestId(`project-card-${project.id}`);
			expect(projectCard).not.toBeInTheDocument();
		}
	});
});
