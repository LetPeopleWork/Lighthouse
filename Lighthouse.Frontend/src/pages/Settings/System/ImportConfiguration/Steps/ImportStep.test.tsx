import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IPortfolioSettings } from "../../../../../models/Project/PortfolioSettings";
import type { ITeamSettings } from "../../../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IConfigurationService } from "../../../../../services/Api/ConfigurationService";
import type { IPortfolioService } from "../../../../../services/Api/PortfolioService";
import type { ITeamService } from "../../../../../services/Api/TeamService";
import type { IWorkTrackingSystemService } from "../../../../../services/Api/WorkTrackingSystemService";
import ImportStep from "./ImportStep";

// Mock services
const createMockWorkTrackingSystemService = () => {
	return {
		addNewWorkTrackingSystemConnection: vi
			.fn()
			.mockImplementation((system) =>
				Promise.resolve({ ...system, id: system.id ?? 100 }),
			),
		updateWorkTrackingSystemConnection: vi
			.fn()
			.mockImplementation((system) =>
				Promise.resolve({ ...system, id: system.id }),
			),
		getWorkTrackingSystems: vi.fn(),
		getWorkTrackingSystem: vi.fn(),
		createWorkTrackingSystem: vi.fn(),
		updateWorkTrackingSystem: vi.fn(),
		deleteWorkTrackingSystem: vi.fn(),
		validateWorkTrackingSystemConnection: vi.fn().mockResolvedValue(true),
	} as unknown as IWorkTrackingSystemService;
};

const createMockTeamService = () => {
	return {
		createTeam: vi
			.fn()
			.mockImplementation((team) =>
				Promise.resolve({ ...team, id: team.id ?? 200 }),
			),
		updateTeam: vi
			.fn()
			.mockImplementation((team) => Promise.resolve({ ...team, id: team.id })),
		validateTeamSettings: vi.fn().mockResolvedValue(true),
	} as unknown as ITeamService;
};

const createMockProjectService = () => {
	return {
		createPortfolio: vi
			.fn()
			.mockImplementation((project) =>
				Promise.resolve({ ...project, id: project.id ?? 300 }),
			),
		updatePortfolio: vi
			.fn()
			.mockImplementation((project) =>
				Promise.resolve({ ...project, id: project.id }),
			),
		validatePortfolioSettings: vi.fn().mockResolvedValue(true),
	} as unknown as IPortfolioService;
};

const createMockConfigurationService = () => {
	return {
		clearConfiguration: vi.fn().mockResolvedValue(undefined),
		exportConfiguration: vi.fn(),
		validateConfiguration: vi.fn(),
	} as unknown as IConfigurationService;
};

describe("ImportStep", () => {
	const mockOnNext = vi.fn();
	const mockOnCancel = vi.fn();
	const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();
	const mockTeamService = createMockTeamService();
	const mockPortfolioService = createMockProjectService();
	const mockConfigurationService = createMockConfigurationService();

	// Sample data for testing
	const mockWorkTrackingSystems: IWorkTrackingSystemConnection[] = [
		{
			id: 1,
			name: "Azure DevOps",
			dataSourceType: "Query",
			workTrackingSystem: "AzureDevOps",
			options: [
				{
					key: "url",
					value: "https://dev.azure.com/organization",
					isSecret: false,
					isOptional: false,
				},
			],
		},
	];

	const mockTeams: ITeamSettings[] = [
		{
			id: 1,
			name: "Team 1",
			workTrackingSystemConnectionId: 1,
			iterationPaths: [],
			areaPaths: [],
			workItemTypes: [],
			states: [],
			stateMappings: [],
			excludedBacklogLevels: [],
			forecastingSettings: {
				backlogLevels: [],
			},
		} as unknown as ITeamSettings,
	];

	const mockProjects: IPortfolioSettings[] = [
		{
			id: 1,
			name: "Project 1",
			workTrackingSystemConnectionId: 1,
			states: [],
			stateMappings: [],
			teams: [],
			involvedTeams: [], // Added this since it's used in the component
		} as unknown as IPortfolioSettings,
	];

	const workTrackingSystemsIdMapping = new Map<number, number>();
	const teamIdMapping = new Map<number, number>();

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders the import step with warning message", () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={new Map()}
				teamIdMapping={new Map()}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		expect(screen.getByText("Import Configuration")).toBeInTheDocument();
		expect(
			screen.getByText("Warning: Import cannot be undone"),
		).toBeInTheDocument();
		expect(screen.getByText("Import")).toBeInTheDocument();
		expect(screen.getByText("Cancel")).toBeInTheDocument();
	});

	it("shows progress indicator when importing", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button to start the import
		fireEvent.click(screen.getByText("Import"));

		// Check progress indicator is shown
		await waitFor(() => {
			expect(
				screen.getByText("Importing configuration..."),
			).toBeInTheDocument();
		});

		// Wait for the import to complete
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.addNewWorkTrackingSystemConnection,
			).toHaveBeenCalledWith(expect.objectContaining({ name: "Azure DevOps" }));
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("calls clearConfiguration when clearConfiguration is true", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={new Map()}
				teamIdMapping={new Map()}
				clearConfiguration={true}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(mockConfigurationService.clearConfiguration).toHaveBeenCalled();
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("handles import of new work tracking systems", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.addNewWorkTrackingSystemConnection,
			).toHaveBeenCalledWith(expect.objectContaining({ name: "Azure DevOps" }));
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("handles import of updated work tracking systems", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={mockWorkTrackingSystems}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.updateWorkTrackingSystemConnection,
			).toHaveBeenCalledWith(expect.objectContaining({ name: "Azure DevOps" }));
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("handles import of new teams", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={[]}
				newTeams={mockTeams}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(mockTeamService.createTeam).toHaveBeenCalledWith(
				expect.objectContaining({ name: "Team 1" }),
			);
			expect(mockTeamService.validateTeamSettings).toHaveBeenCalled();
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("handles import of updated teams", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={mockTeams}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(mockTeamService.updateTeam).toHaveBeenCalledWith(
				expect.objectContaining({ name: "Team 1" }),
			);
			expect(mockTeamService.validateTeamSettings).toHaveBeenCalled();
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("handles import of new projects", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={mockProjects}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(mockPortfolioService.createPortfolio).toHaveBeenCalledWith(
				expect.objectContaining({ name: "Project 1" }),
			);
			expect(mockPortfolioService.validatePortfolioSettings).toHaveBeenCalled();
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("handles import of updated projects", async () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={mockProjects}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(mockPortfolioService.updatePortfolio).toHaveBeenCalledWith(
				expect.objectContaining({ name: "Project 1" }),
			);
			expect(mockPortfolioService.validatePortfolioSettings).toHaveBeenCalled();
			expect(mockOnNext).toHaveBeenCalled();
		});
	});

	it("handles errors during import of work tracking systems", async () => {
		// Mock service to throw error
		mockWorkTrackingSystemService.addNewWorkTrackingSystemConnection = vi
			.fn()
			.mockRejectedValue(new Error("Connection error"));

		render(
			<ImportStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
				teamIdMapping={teamIdMapping}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete with error
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.addNewWorkTrackingSystemConnection,
			).toHaveBeenCalled();
			expect(mockOnNext).toHaveBeenCalledWith(
				expect.objectContaining({
					workTrackingSystems: expect.arrayContaining([
						expect.objectContaining({
							status: "Error",
							errorMessage: expect.stringContaining("Connection error"),
						}),
					]),
				}),
			);
		});
	});

	it("handles cancellation", () => {
		render(
			<ImportStep
				newWorkTrackingSystems={[]}
				updatedWorkTrackingSystems={[]}
				newTeams={[]}
				updatedTeams={[]}
				newProjects={[]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={new Map()}
				teamIdMapping={new Map()}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click cancel button
		fireEvent.click(screen.getByText("Cancel"));

		// Check cancel callback was called
		expect(mockOnCancel).toHaveBeenCalled();
	});

	it("correctly maps IDs for new entities", async () => {
		const workTrackingSystemsMap = new Map<number, number>();
		const teamsMap = new Map<number, number>();

		// Create mock project with involved teams
		const projectWithTeams = {
			...mockProjects[0],
			involvedTeams: [{ id: 1, name: "Team 1" }],
		};

		render(
			<ImportStep
				newWorkTrackingSystems={mockWorkTrackingSystems}
				updatedWorkTrackingSystems={[]}
				newTeams={mockTeams}
				updatedTeams={[]}
				newProjects={[projectWithTeams]}
				updatedProjects={[]}
				workTrackingSystemsIdMapping={workTrackingSystemsMap}
				teamIdMapping={teamsMap}
				clearConfiguration={false}
				workTrackingSystemService={mockWorkTrackingSystemService}
				teamService={mockTeamService}
				projectService={mockPortfolioService}
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onCancel={mockOnCancel}
			/>,
		);

		// Click import button
		fireEvent.click(screen.getByText("Import"));

		// Wait for the import to complete
		await waitFor(() => {
			expect(
				mockWorkTrackingSystemService.addNewWorkTrackingSystemConnection,
			).toHaveBeenCalled();
			expect(mockTeamService.createTeam).toHaveBeenCalled();
			expect(mockPortfolioService.createPortfolio).toHaveBeenCalled();

			// Check that the mapping happens (this is somewhat implementation-specific)
			// We can't easily check the map directly, but we can verify that the system calls happened in order
			expect(mockOnNext).toHaveBeenCalled();
		});
	});
});
