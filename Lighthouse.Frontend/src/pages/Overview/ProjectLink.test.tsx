import { render, screen } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router-dom";
import { describe, expect, test } from "vitest";
import { Project } from "../../models/Project/Project";
import ProjectLink from "./ProjectLink";

const project: Project = new Project();
project.name = "My Project";
project.id = 12;

describe("ProjectLink", () => {
	test("renders without crashing", () => {
		render(
			<Router>
				<ProjectLink project={project} />
			</Router>,
		);
		const projectLinkElement = screen.getByRole("link");
		expect(projectLinkElement).toBeInTheDocument();
	});

	test("creates correct link to Project", () => {
		render(
			<Router>
				<ProjectLink project={project} />
			</Router>,
		);
		const projectLinkElement = screen.getByRole("link");
		expect(projectLinkElement).toHaveAttribute(
			"href",
			`/projects/${project.id}`,
		);
	});
});
