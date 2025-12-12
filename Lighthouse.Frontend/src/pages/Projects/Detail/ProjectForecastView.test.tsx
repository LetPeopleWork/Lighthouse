import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Project } from "../../../models/Project/Project";
import { Team } from "../../../models/Team/Team";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ProjectForecastView from "./ProjectForecastView";

vi.mock("./InvolvedTeamsList", () => ({
	default: ({
		teams,
		onTeamUpdated,
	}: {
		teams: ITeamSettings[];
		onTeamUpdated: (team: ITeamSettings) => void;
	}) => (
		<div data-testid="involved-teams-list">
			<div data-testid="teams-count">{teams.length}</div>
			{teams.map((team) => (
				<div key={team.id} data-testid={`team-${team.id}`}>
					<span>{team.name}</span>
					<button
						type="button"
						data-testid={`update-team-${team.id}`}
						onClick={() =>
							onTeamUpdated({ ...team, name: `${team.name}-updated` })
						}
					>
						Update Team
					</button>
				</div>
			))}
		</div>
	),
}));

vi.mock("./ProjectFeatureList", () => ({
	default: ({ project }: { project: Project }) => (
		<div data-testid="project-feature-list">
			<div data-testid="project-name">{project.name}</div>
			<div data-testid="features-count">{project.features.length}</div>
		</div>
	),
}));

describe("ProjectForecastView component", () => {
	const mockTeam1: Team = (() => {
		const team = new Team();
		team.name = "Team A";
		team.id = 1;
		team.projects = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date();
		team.throughputEndDate = new Date();
		return team;
	})();

	const mockTeam2: Team = (() => {
		const team = new Team();
		team.name = "Team B";
		team.id = 2;
		team.projects = [];
		team.features = [];
		team.featureWip = 1;
		team.lastUpdated = new Date();
		team.useFixedDatesForThroughput = false;
		team.throughputStartDate = new Date();
		team.throughputEndDate = new Date();
		return team;
	})();

	const mockProject = (() => {
		const project = new Project();
		project.name = "Test Project";
		project.id = 1;
		project.involvedTeams = [mockTeam1, mockTeam2];
		project.lastUpdated = new Date();
		return project;
	})();

	const mockTeamSettings1 = createMockTeamSettings();
	mockTeamSettings1.id = 1;
	mockTeamSettings1.name = "Team A Settings";

	const mockTeamSettings2 = createMockTeamSettings();
	mockTeamSettings2.id = 2;
	mockTeamSettings2.name = "Team B Settings";

	const mockInvolvedTeams: ITeamSettings[] = [
		mockTeamSettings1,
		mockTeamSettings2,
	];

	const mockOnTeamSettingsChange = vi.fn().mockResolvedValue(undefined);

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders all components correctly", () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					involvedTeams={mockInvolvedTeams}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		expect(screen.getByTestId("involved-teams-list")).toBeInTheDocument();
		expect(screen.getByTestId("project-feature-list")).toBeInTheDocument();

		// Check if data is passed correctly
		expect(screen.getByTestId("teams-count").textContent).toBe("2");
		expect(screen.getByTestId("project-name").textContent).toBe("Test Project");
	});

	it("updates team settings correctly", async () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					involvedTeams={mockInvolvedTeams}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		const updateTeamButton = screen.getByTestId("update-team-1");
		fireEvent.click(updateTeamButton);

		await waitFor(() => {
			expect(mockOnTeamSettingsChange).toHaveBeenCalledTimes(1);
			const updatedTeamSettings = mockOnTeamSettingsChange.mock.calls[0][0];
			expect(updatedTeamSettings.id).toBe(1);
			expect(updatedTeamSettings.name).toBe("Team A Settings-updated");
		});
	});
});
