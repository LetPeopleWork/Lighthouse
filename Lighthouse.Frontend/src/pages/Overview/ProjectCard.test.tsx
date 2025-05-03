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
	const project: Project = new Project(
		"Project Alpha",
		1,
		[
			new Team(
				"Team A",
				1,
				[],
				[],
				1,
				new Date(),
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
				new Date(),
				false,
				new Date(new Date().setDate(new Date().getDate() - [1].length)),
				new Date(),
			),
		],
		[
			new Feature(
				"Feature 1",
				0,
				"FTR-0",
				"In Progress",
				"Feature",
				new Date(),
				false,
				{ 1: "Project Alpha" },
				{ 1: 7, 2: 3 },
				{ 1: 7, 2: 3 },
				{ 0: 74.5 },
				[
					new WhenForecast(50, new Date("2025-08-04")),
					new WhenForecast(70, new Date("2025-06-25")),
					new WhenForecast(85, new Date("2025-07-25")),
					new WhenForecast(95, new Date("2025-08-19")),
				],
				null,
				"Doing",
				new Date("2025-06-01"),
				new Date("2025-08-01"),
				60,
				60,
			),
			new Feature(
				"Feature 2",
				1,
				"FTR-1",
				"Done",
				"Feature",
				new Date(),
				false,
				{ 1: "Project Alpha" },
				{ 1: 0, 2: 0 },
				{ 1: 5, 2: 2 },
				{ 0: 100 },
				[],
				null,
				"Done",
				new Date("2025-05-01"),
				new Date("2025-06-01"),
				30,
				30,
			),
		],
		[new Milestone(1, "Milestone 1", new Date(Date.now() + 14 * 24 * 60 * 60))],
		new Date("2024-06-01"),
	);

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
		expect(screen.getByText("FTR-0 - Feature 1")).toBeInTheDocument();
		expect(screen.getByText("FTR-1 - Feature 2")).toBeInTheDocument();

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
		const singleFeatureProject = new Project(
			project.name,
			project.id,
			project.involvedTeams,
			[project.features[0]], // Only one feature
			project.milestones,
			project.lastUpdated,
		);

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
});
