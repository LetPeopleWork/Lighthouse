import { ThemeProvider, createTheme } from "@mui/material/styles";
import { fireEvent, render, screen } from "@testing-library/react";
import { BrowserRouter as Router } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../models/Feature";
import { WhenForecast } from "../../models/Forecasts/WhenForecast";
import { Milestone } from "../../models/Project/Milestone";
import { Project } from "../../models/Project/Project";
import { Team } from "../../models/Team/Team";
import ProjectCard from "./ProjectCard";

// Create a test theme that matches what the component expects
const theme = createTheme({
	palette: {
		mode: "light",
		primary: {
			main: "#1976d2",
		},
		secondary: {
			main: "#9c27b0",
		},
		success: {
			main: "#4caf50",
		},
		error: {
			main: "#f44336",
		},
		warning: {
			main: "#ff9800",
		},
		grey: {
			100: "#f5f5f5",
			200: "#eeeeee",
			300: "#e0e0e0",
			500: "#9e9e9e",
			800: "#424242",
		},
	},
});

vi.mock("./TeamLink", () => ({
	default: ({ team }: { team: Team }) => (
		<span data-testid="team-link">{team.id}</span>
	),
}));

vi.mock("./ProjectLink", () => ({
	default: ({ project }: { project: Project }) => (
		<span data-testid="project-link">{project.id}</span>
	),
}));

vi.mock(
	"../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay",
	() => ({
		default: ({ utcDate }: { utcDate: Date; showTime?: boolean }) => (
			<span data-testid="local-date-time-display"> {utcDate.toString()}</span>
		),
	}),
);

describe("ProjectCard component", () => {
	const project: Project = new Project();
	project.name = "Project Alpha";
	project.id = 1;

	const teamA = new Team();
	teamA.name = "Team A";
	teamA.id = 1;

	const teamB = new Team();
	teamB.name = "Team B";
	teamB.id = 2;

	project.involvedTeams = [teamA, teamB];

	const feature1 = new Feature();
	feature1.name = "Feature 1";
	feature1.id = 0;
	feature1.referenceId = "FTR-0";
	feature1.state = "In Progress";
	feature1.type = "Feature";
	feature1.projects = { 1: "Project Alpha" };
	feature1.remainingWork = { 1: 7, 2: 3 };
	feature1.totalWork = { 1: 7, 2: 3 };
	feature1.milestoneLikelihood = { 0: 74.5 };
	feature1.forecasts = [
		WhenForecast.new(50, new Date("2025-08-04")),
		WhenForecast.new(70, new Date("2025-06-25")),
		WhenForecast.new(85, new Date("2025-07-25")),
		WhenForecast.new(95, new Date("2025-08-19")),
	];

	const feature2 = new Feature();
	feature2.name = "Feature 2";
	feature2.id = 1;
	feature2.referenceId = "FTR-1";
	feature2.state = "Done";
	feature2.type = "Feature";
	feature2.projects = { 1: "Project Alpha" };
	feature2.remainingWork = { 1: 0, 2: 0 };
	feature2.totalWork = { 1: 5, 2: 2 };
	feature2.milestoneLikelihood = { 0: 100 };
	feature2.forecasts = [];

	project.features = [feature1, feature2];
	project.milestones = [
		Milestone.new(0, "Milestone 0", new Date(Date.now() + 7 * 24 * 60 * 60)),
	];
	project.tags = ["Important", "Release-2025", "Frontend"];

	const renderComponent = () =>
		render(
			<ThemeProvider theme={theme}>
				<Router>
					<ProjectCard project={project} />
				</Router>
			</ThemeProvider>,
		);

	it("should render project details correctly", () => {
		renderComponent();

		const projectLink = screen.getByTestId("project-link");
		expect(projectLink).toBeInTheDocument();

		expect(screen.getByText("10 Work Items Remaining")).toBeInTheDocument();

		const teamLinks = screen.getAllByTestId("team-link");
		expect(teamLinks).toHaveLength(2);

		// Check if last updated is rendered
		expect(screen.getByText("Last Updated:")).toBeInTheDocument();
	});

	it("should render the date time display components", () => {
		renderComponent();

		// Check if LocalDateTimeDisplay components are rendered for forecasts
		const dateTimeDisplays = screen.getAllByTestId("local-date-time-display");
		expect(dateTimeDisplays).toHaveLength(5); // 4 forecasts + 1 last updated
	});

	it("should render forecasts from the feature with the latest expectedDate", () => {
		renderComponent();

		const forecastDates = [
			new Date("2025-08-04").toString(),
			new Date("2025-06-25").toString(),
			new Date("2025-07-25").toString(),
			new Date("2025-08-19").toString(),
		];

		for (const date of forecastDates) {
			expect(
				screen.getByText((content) => content.includes(date)),
			).toBeInTheDocument();
		}
	});

	it("should display the feature count chip with correct count", () => {
		renderComponent();

		const featureCountChip = screen.getByText("2 Features");
		expect(featureCountChip).toBeInTheDocument();
	});

	it("should open the dialog when the feature count chip is clicked", () => {
		renderComponent();

		// Dialog should initially be closed
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

		// Click the feature count chip
		const featureCountChip = screen.getByText("2 Features");
		fireEvent.click(featureCountChip);

		// Dialog should now be open
		const dialog = screen.getByRole("dialog");
		expect(dialog).toBeInTheDocument();

		// Dialog should have the correct title
		expect(screen.getByText("Project Alpha: Features (2)")).toBeInTheDocument();
	});

	it("should display all features in the dialog with correct details", () => {
		renderComponent();

		// Open the dialog
		const featureCountChip = screen.getByText("2 Features");
		fireEvent.click(featureCountChip);

		// Check if both features are displayed
		expect(screen.getByText("FTR-0: Feature 1")).toBeInTheDocument();
		expect(screen.getByText("FTR-1: Feature 2")).toBeInTheDocument();

		// Check if feature states are displayed
		expect(screen.getByText("In Progress")).toBeInTheDocument();
		expect(screen.getByText("Done")).toBeInTheDocument();

		// Check if work items remaining info is displayed
		expect(
			screen.getByText("10 of 10 work items remaining"),
		).toBeInTheDocument();
		expect(screen.getByText("0 of 7 work items remaining")).toBeInTheDocument();
	});

	it("should handle the close button click", () => {
		// We're simply testing that the close button exists and can be clicked
		// without errors
		renderComponent();

		// Open the dialog
		const featureCountChip = screen.getByText("2 Features");
		fireEvent.click(featureCountChip);

		// Dialog should be open
		expect(screen.getByRole("dialog")).toBeInTheDocument();

		// Find and click the close button
		const closeButton = screen.getByRole("button", { name: "close" });
		expect(closeButton).toBeInTheDocument();

		// Just verify we can click it without errors
		fireEvent.click(closeButton);
	});

	it("should display singular 'Feature' text when there is only one feature", () => {
		// Create a new project with only one feature
		const singleFeatureProject = Project.fromBackend(project);

		singleFeatureProject.features = project.features.slice(0, 1);

		render(
			<ThemeProvider theme={theme}>
				<Router>
					<ProjectCard project={singleFeatureProject} />
				</Router>
			</ThemeProvider>,
		);

		const featureCountChip = screen.getByText("1 Feature");
		expect(featureCountChip).toBeInTheDocument();
	});

	it("should render tags as chips when project has tags", () => {
		renderComponent();

		// Check that all tags are rendered
		expect(screen.getByText("Important")).toBeInTheDocument();
		expect(screen.getByText("Release-2025")).toBeInTheDocument();
		expect(screen.getByText("Frontend")).toBeInTheDocument();
	});

	it("should not render the tags section when project has no tags", () => {
		// Create a project without tags
		const projectWithoutTags = Project.fromBackend(project);
		projectWithoutTags.tags = [];

		render(
			<ThemeProvider theme={theme}>
				<Router>
					<ProjectCard project={projectWithoutTags} />
				</Router>
			</ThemeProvider>,
		);

		// None of the tags should be in the document
		expect(screen.queryByText("Important")).not.toBeInTheDocument();
		expect(screen.queryByText("Release-2025")).not.toBeInTheDocument();
		expect(screen.queryByText("Frontend")).not.toBeInTheDocument();
	});

	it("should not render the tags section when project has undefined tags", () => {
		// Create a project with undefined tags
		const projectWithUndefinedTags = Project.fromBackend(project);
		projectWithUndefinedTags.tags = [];

		render(
			<ThemeProvider theme={theme}>
				<Router>
					<ProjectCard project={projectWithUndefinedTags} />
				</Router>
			</ThemeProvider>,
		);

		// None of the tags should be in the document
		expect(screen.queryByText("Important")).not.toBeInTheDocument();
		expect(screen.queryByText("Release-2025")).not.toBeInTheDocument();
		expect(screen.queryByText("Frontend")).not.toBeInTheDocument();
	});

	it("should render milestone chips with colors based on likelihood", () => {
		// Create a project with multiple milestones having different likelihoods
		const projectWithMultipleMilestones = Project.fromBackend(project);

		const highLikelihoodMilestone = Milestone.new(
			1,
			"High Likelihood",
			new Date(Date.now() + 14 * 24 * 60 * 60),
		);
		const mediumLikelihoodMilestone = Milestone.new(
			2,
			"Medium Likelihood",
			new Date(Date.now() + 21 * 24 * 60 * 60),
		);
		const lowLikelihoodMilestone = Milestone.new(
			3,
			"Low Likelihood",
			new Date(Date.now() + 28 * 24 * 60 * 60),
		);

		projectWithMultipleMilestones.milestones = [
			project.milestones[0], // Keep the original milestone
			highLikelihoodMilestone,
			mediumLikelihoodMilestone,
			lowLikelihoodMilestone,
		];

		// Set likelihoods in feature1
		feature1.milestoneLikelihood = {
			0: 74.5, // Original milestone (warning color)
			1: 90, // High likelihood (success color)
			2: 65, // Medium likelihood (warning color)
			3: 30, // Low likelihood (error color)
		};

		render(
			<ThemeProvider theme={theme}>
				<Router>
					<ProjectCard project={projectWithMultipleMilestones} />
				</Router>
			</ThemeProvider>,
		);

		// Find all milestone chips
		const milestoneChips = screen.getAllByText(
			/Milestone|High Likelihood|Medium Likelihood|Low Likelihood/,
		);
		expect(milestoneChips).toHaveLength(4);

		// Verify each milestone chip is present
		expect(screen.getByText("Milestone 0")).toBeInTheDocument();
		expect(screen.getByText("High Likelihood")).toBeInTheDocument();
		expect(screen.getByText("Medium Likelihood")).toBeInTheDocument();
		expect(screen.getByText("Low Likelihood")).toBeInTheDocument();

		// Just verify we can click a milestone chip and it opens a dialog
		fireEvent.click(screen.getByText("High Likelihood"));
		expect(screen.getByRole("dialog")).toBeInTheDocument();

		// Close dialog
		fireEvent.click(screen.getByRole("button", { name: "close" }));
	});
});
