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
	const team1 = new Team();
	team1.name = "Team A";
	team1.id = 1;

	const team2 = new Team();
	team2.name = "Team B";
	team2.id = 2;

	const feature1 = new Feature();
	feature1.name = "Feature 1";
	feature1.id = 1;
	feature1.projects = { 1: "Project Alpha" };
	feature1.forecasts = [
		WhenForecast.new(50, new Date("2024-01-01")),
		WhenForecast.new(70, new Date("2024-02-01")),
		WhenForecast.new(85, new Date("2024-03-01")),
		WhenForecast.new(95, new Date("2024-04-01")),
	];

	const project1 = new Project();
	project1.name = "Project Alpha";
	project1.id = 1;
	project1.involvedTeams = [team1, team2];
	project1.features = [feature1];
	project1.milestones = [
		Milestone.new(0, "Milestone 1", new Date(Date.now() + 14 * 24 * 60 * 60)),
	];

	const team3 = new Team();
	team3.name = "Team C";
	team3.id = 3;

	const feature2 = new Feature();
	feature2.name = "Feature 2";
	feature2.id = 2;
	feature2.projects = { 2: "Project Beta" };
	feature2.forecasts = [
		WhenForecast.new(50, new Date("2024-01-01")),
		WhenForecast.new(70, new Date("2024-02-01")),
		WhenForecast.new(85, new Date("2024-03-01")),
		WhenForecast.new(95, new Date("2024-04-01")),
	];

	const project2 = new Project();
	project2.name = "Project Beta";
	project2.id = 2;
	project2.involvedTeams = [team3];
	project2.features = [feature2];
	project2.milestones = [
		Milestone.new(1, "Milestone 2", new Date(Date.now() + 14 * 24 * 60 * 60)),
	];

	const projects: Project[] = [project1, project2];

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

	it("should display projects in alphabetical order by name", () => {
		const project1 = new Project();
		project1.name = "ZProject";

		const project2 = new Project();
		project2.name = "AProject";

		const project3 = new Project();
		project3.name = "MProject";

		const unsortedProjects = [project1, project2, project3];

		const { container } = render(
			<Router>
				<ProjectOverview projects={unsortedProjects} filterText="" />
			</Router>,
		);

		// Get all rendered project cards
		const projectCards = Array.from(
			container.querySelectorAll("[data-testid^='project-card-']"),
		);

		// Expected order after sorting
		const expectedOrder = ["AProject", "MProject", "ZProject"];

		// Verify the projects are rendered in the correct alphabetical order
		// We need to check each card in the DOM order to verify it matches our expected order
		expect(projectCards.length).toBe(expectedOrder.length);

		// Get the sorted projects for comparison
		const sortedProjects = [...unsortedProjects].sort((a, b) =>
			a.name.localeCompare(b.name),
		);

		// Check that each project is in the expected position
		for (let i = 0; i < projectCards.length; i++) {
			expect(projectCards[i].getAttribute("data-testid")).toBe(
				`project-card-${sortedProjects[i].id}`,
			);
		}
	});
});
