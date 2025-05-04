import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IMilestone } from "../../../models/Project/Milestone";
import { Milestone } from "../../../models/Project/Milestone";
import { Project } from "../../../models/Project/Project";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import { Team } from "../../../models/Team/Team";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import ProjectForecastView from "./ProjectForecastView";

// Mock the child components
vi.mock("../../../components/Common/Milestones/MilestonesComponent", () => ({
	default: ({
		milestones,
		onAddMilestone,
		onRemoveMilestone,
		onUpdateMilestone,
	}: {
		milestones: IMilestone[];
		onAddMilestone: (milestone: IMilestone) => void;
		onRemoveMilestone: (name: string) => void;
		onUpdateMilestone: (
			name: string,
			updatedMilestone: Partial<IMilestone>,
		) => void;
	}) => (
		<div data-testid="milestones-component">
			<div data-testid="milestones-count">{milestones.length}</div>
			<button
				type="button"
				data-testid="add-milestone-button"
				onClick={() =>
					onAddMilestone({ id: 999, name: "New Milestone", date: new Date() })
				}
			>
				Add Milestone
			</button>
			{milestones.map((milestone) => (
				<div key={milestone.name} data-testid={`milestone-${milestone.name}`}>
					<span>{milestone.name}</span>
					<button
						type="button"
						data-testid={`remove-milestone-${milestone.name}`}
						onClick={() => onRemoveMilestone(milestone.name)}
					>
						Remove
					</button>
					<button
						type="button"
						data-testid={`update-milestone-${milestone.name}`}
						onClick={() =>
							onUpdateMilestone(milestone.name, {
								name: `${milestone.name}-updated`,
							})
						}
					>
						Update
					</button>
				</div>
			))}
		</div>
	),
}));

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

	const mockMilestone1 = (() => {
		const milestone = new Milestone();
		milestone.id = 1;
		milestone.name = "Milestone 1";
		milestone.date = new Date();
		return milestone;
	})();
	const mockMilestone2 = (() => {
		const milestone = new Milestone();
		milestone.id = 2;
		milestone.name = "Milestone 2";
		milestone.date = new Date();
		return milestone;
	})();

	const mockProject = (() => {
		const project = new Project();
		project.name = "Test Project";
		project.id = 1;
		project.involvedTeams = [mockTeam1, mockTeam2];
		project.milestones = [mockMilestone1, mockMilestone2];
		project.lastUpdated = new Date();
		return project;
	})();

	const mockProjectSettings: IProjectSettings = {
		id: 1,
		name: "Test Project Settings",
		workItemTypes: ["User Story", "Bug"],
		milestones: [mockMilestone1, mockMilestone2],
		workItemQuery: "query",
		unparentedItemsQuery: "query",
		involvedTeams: [mockTeam1, mockTeam2],
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Closed"],
		overrideRealChildCountStates: [],
		tags: [],
		usePercentileToCalculateDefaultAmountOfWorkItems: false,
		defaultAmountOfWorkItemsPerFeature: 5,
		defaultWorkItemPercentile: 85,
		historicalFeaturesWorkItemQuery: "query",
		workTrackingSystemConnectionId: 1,
	};

	const mockTeamSettings1: ITeamSettings = {
		id: 1,
		name: "Team A Settings",
		throughputHistory: 30,
		useFixedDatesForThroughput: false,
		throughputHistoryStartDate: new Date(),
		throughputHistoryEndDate: new Date(),
		featureWIP: 5,
		workItemQuery: "query",
		workItemTypes: ["User Story"],
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Closed"],
		tags: [],
		workTrackingSystemConnectionId: 1,
		relationCustomField: "",
		automaticallyAdjustFeatureWIP: false,
	};

	const mockTeamSettings2: ITeamSettings = {
		id: 2,
		name: "Team B Settings",
		throughputHistory: 30,
		useFixedDatesForThroughput: false,
		throughputHistoryStartDate: new Date(),
		throughputHistoryEndDate: new Date(),
		featureWIP: 5,
		workItemQuery: "query",
		workItemTypes: ["User Story"],
		toDoStates: ["New"],
		doingStates: ["Active"],
		doneStates: ["Closed"],
		tags: [],
		workTrackingSystemConnectionId: 1,
		relationCustomField: "",
		automaticallyAdjustFeatureWIP: false,
	};

	const mockInvolvedTeams: ITeamSettings[] = [
		mockTeamSettings1,
		mockTeamSettings2,
	];

	const mockOnMilestonesChanged = vi.fn().mockResolvedValue(undefined);
	const mockOnTeamSettingsChange = vi.fn().mockResolvedValue(undefined);

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders all components correctly", () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					projectSettings={mockProjectSettings}
					involvedTeams={mockInvolvedTeams}
					onMilestonesChanged={mockOnMilestonesChanged}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		expect(screen.getByTestId("milestones-component")).toBeInTheDocument();
		expect(screen.getByTestId("involved-teams-list")).toBeInTheDocument();
		expect(screen.getByTestId("project-feature-list")).toBeInTheDocument();

		// Check if data is passed correctly
		expect(screen.getByTestId("milestones-count").textContent).toBe("2");
		expect(screen.getByTestId("teams-count").textContent).toBe("2");
		expect(screen.getByTestId("project-name").textContent).toBe("Test Project");
	});

	it("handles null projectSettings correctly", () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					projectSettings={null}
					involvedTeams={mockInvolvedTeams}
					onMilestonesChanged={mockOnMilestonesChanged}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		expect(screen.getByTestId("milestones-component")).toBeInTheDocument();
		expect(screen.getByTestId("milestones-count").textContent).toBe("0");
	});

	it("adds a milestone correctly", async () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					projectSettings={mockProjectSettings}
					involvedTeams={mockInvolvedTeams}
					onMilestonesChanged={mockOnMilestonesChanged}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		const addButton = screen.getByTestId("add-milestone-button");
		fireEvent.click(addButton);

		await waitFor(() => {
			expect(mockOnMilestonesChanged).toHaveBeenCalledTimes(1);
			const updatedSettings = mockOnMilestonesChanged.mock.calls[0][0];
			expect(updatedSettings.milestones).toHaveLength(3);
			expect(updatedSettings.milestones[2].name).toBe("New Milestone");
		});
	});

	it("removes a milestone correctly", async () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					projectSettings={mockProjectSettings}
					involvedTeams={mockInvolvedTeams}
					onMilestonesChanged={mockOnMilestonesChanged}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		const removeButton = screen.getByTestId("remove-milestone-Milestone 1");
		fireEvent.click(removeButton);

		await waitFor(() => {
			expect(mockOnMilestonesChanged).toHaveBeenCalledTimes(1);
			const updatedSettings = mockOnMilestonesChanged.mock.calls[0][0];
			expect(updatedSettings.milestones).toHaveLength(1);
			expect(updatedSettings.milestones[0].name).toBe("Milestone 2");
		});
	});

	it("updates a milestone correctly", async () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					projectSettings={mockProjectSettings}
					involvedTeams={mockInvolvedTeams}
					onMilestonesChanged={mockOnMilestonesChanged}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		const updateButton = screen.getByTestId("update-milestone-Milestone 2");
		fireEvent.click(updateButton);

		await waitFor(() => {
			expect(mockOnMilestonesChanged).toHaveBeenCalledTimes(1);
			const updatedSettings = mockOnMilestonesChanged.mock.calls[0][0];
			expect(updatedSettings.milestones).toHaveLength(2);
			expect(updatedSettings.milestones[1].name).toBe("Milestone 2-updated");
		});
	});

	it("updates team settings correctly", async () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					projectSettings={mockProjectSettings}
					involvedTeams={mockInvolvedTeams}
					onMilestonesChanged={mockOnMilestonesChanged}
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

	it("doesn't call onMilestonesChanged when projectSettings is null", async () => {
		render(
			<MemoryRouter>
				<ProjectForecastView
					project={mockProject}
					projectSettings={null}
					involvedTeams={mockInvolvedTeams}
					onMilestonesChanged={mockOnMilestonesChanged}
					onTeamSettingsChange={mockOnTeamSettingsChange}
				/>
			</MemoryRouter>,
		);

		const addButton = screen.getByTestId("add-milestone-button");
		fireEvent.click(addButton);

		// Wait a bit to make sure the function isn't called
		await new Promise((resolve) => setTimeout(resolve, 100));
		expect(mockOnMilestonesChanged).not.toHaveBeenCalled();
	});
});
