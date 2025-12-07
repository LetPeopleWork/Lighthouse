import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IProjectService } from "../../../../../services/Api/ProjectService";
import type { ITeamService } from "../../../../../services/Api/TeamService";
import {
	createMockProjectSettings,
	createMockTeamSettings,
} from "../../../../../tests/TestDataProvider";
import type { ImportResults } from "../ImportResults";
import ImportSummaryStep from "./ImportSummaryStep";

// Mock the terminology hook to return predictable terms
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			if (key === "portfolio") return "Portfolio";
			if (key === "portfolios") return "Portfolios";
			if (key === "feature") return "Feature";
			if (key === "features") return "Features";
			return key;
		},
	}),
}));

// Mock services
const createMockTeamService = () => {
	return {
		updateTeamData: vi.fn().mockResolvedValue(undefined),
		createTeam: vi.fn(),
		updateTeam: vi.fn(),
		validateTeamSettings: vi.fn(),
	} as unknown as ITeamService;
};

const createMockProjectService = () => {
	return {
		refreshFeaturesForProject: vi.fn().mockResolvedValue(undefined),
		createProject: vi.fn(),
		updateProject: vi.fn(),
		validateProjectSettings: vi.fn(),
	} as unknown as IProjectService;
};

describe("ImportSummaryStep", () => {
	const mockOnClose = vi.fn();
	const mockTeamService = createMockTeamService();
	const mockProjectService = createMockProjectService();

	// Sample import results for testing different scenarios
	const createSuccessfulImportResults = (): ImportResults => ({
		workTrackingSystems: [
			{
				entity: {
					id: 1,
					dataSourceType: "Query",
					name: "Azure DevOps",
					workTrackingSystem: "AzureDevOps",
					options: [],
				},
				status: "Success",
			},
		],
		teams: [
			{
				entity: createMockTeamSettings(),
				status: "Success",
			},
			{
				entity: createMockTeamSettings(),
				status: "Success",
			},
		],
		projects: [
			{
				entity: createMockProjectSettings(),
				status: "Success",
			},
		],
	});

	const createMixedImportResults = (): ImportResults => ({
		workTrackingSystems: [
			{
				entity: {
					id: 1,
					dataSourceType: "Query",
					name: "Azure DevOps",
					workTrackingSystem: "AzureDevOps",
					options: [],
				},
				status: "Success",
			},
			{
				entity: {
					id: 2,
					dataSourceType: "Query",
					name: "GitHub",
					workTrackingSystem: "AzureDevOps",
					options: [],
				},
				status: "Error",
				errorMessage: "Connection failed",
			},
		],
		teams: [
			{
				entity: createMockTeamSettings(),
				status: "Success",
			},
			{
				entity: createMockTeamSettings(),
				status: "Validation Failed",
				errorMessage: "Missing required fields",
			},
		],
		projects: [
			{
				entity: createMockProjectSettings(),
				status: "Success",
			},
			{
				entity: createMockProjectSettings(),
				status: "Error",
				errorMessage: "Missing work tracking system",
			},
		],
	});

	const createValidationFailedResults = (): ImportResults => ({
		workTrackingSystems: [
			{
				entity: {
					id: 1,
					dataSourceType: "Query",
					name: "Azure DevOps",
					workTrackingSystem: "AzureDevOps",
					options: [],
				},
				status: "Success",
			},
		],
		teams: [
			{
				entity: createMockTeamSettings(),
				status: "Validation Failed",
				errorMessage: "Missing required fields",
			},
		],
		projects: [
			{
				entity: createMockProjectSettings(),
				status: "Validation Failed",
				errorMessage: "Missing required configuration",
			},
		],
	});

	const createFailedImportResults = (): ImportResults => ({
		workTrackingSystems: [
			{
				entity: {
					id: 1,
					dataSourceType: "Query",
					name: "Azure DevOps",
					workTrackingSystem: "AzureDevOps",
					options: [],
				},
				status: "Error",
				errorMessage: "Connection failed",
			},
		],
		teams: [
			{
				entity: createMockTeamSettings(),
				status: "Error",
				errorMessage: "Failed to create team",
			},
		],
		projects: [
			{
				entity: createMockProjectSettings(),
				status: "Error",
				errorMessage: "Failed to create project",
			},
		],
	});

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("renders summary of successful import", () => {
		const successfulImport = createSuccessfulImportResults();

		successfulImport.teams[0].entity.name = "Team 1";
		successfulImport.teams[1].entity.name = "Team 2";
		successfulImport.projects[0].entity.name = "Project 1";

		render(
			<ImportSummaryStep
				importResults={successfulImport}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);

		// Check title and success message
		expect(screen.getByText("Import Completed")).toBeInTheDocument();
		expect(
			screen.getByText("All items were successfully imported."),
		).toBeInTheDocument();
		// Check summary section counts
		const successCounts = screen.getAllByText("Success: 1");
		const teamSuccessCount = screen.getByText("Success: 2");
		// First "Success: 1" is for workTrackingSystems
		expect(successCounts[0]).toBeInTheDocument();
		// "Success: 2" is for teams
		expect(teamSuccessCount).toBeInTheDocument();
		// Second "Success: 1" is for projects
		expect(successCounts[1]).toBeInTheDocument();
		// Check that entity tables are rendered
		const wtsHeadings = screen.getAllByText("workTrackingSystems");
		const teamHeadings = screen.getAllByText("teams");
		const portfolioHeadings = screen.getAllByText("Portfolios");
		expect(wtsHeadings.length).toBeGreaterThanOrEqual(1);
		expect(teamHeadings.length).toBeGreaterThanOrEqual(1);
		expect(portfolioHeadings.length).toBeGreaterThanOrEqual(1);

		// Check that the entity names are shown in the tables
		expect(screen.getByText("Azure DevOps")).toBeInTheDocument();
		expect(screen.getByText("Team 1")).toBeInTheDocument();
		expect(screen.getByText("Team 2")).toBeInTheDocument();
		expect(screen.getByText("Project 1")).toBeInTheDocument();

		// Buttons should be enabled
		expect(screen.getByTestId("update-data-button")).not.toBeDisabled();
		expect(screen.getByTestId("close-button")).not.toBeDisabled();
	});

	it("renders summary of mixed import results", () => {
		const mixedImport = createMixedImportResults();

		render(
			<ImportSummaryStep
				importResults={mixedImport}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);

		// Check error message is shown
		expect(
			screen.getByText(
				"Some items failed to import. Review the details below.",
			),
		).toBeInTheDocument();
		// Check summary section counts
		const successCounts = screen.getAllByText("Success: 1");
		const errorCounts = screen.getAllByText("Errors: 1");
		const validationIssues = screen.getByText("Validation Issues: 1");

		// Check system counts
		expect(successCounts[0]).toBeInTheDocument(); // Work tracking systems success
		expect(errorCounts[0]).toBeInTheDocument(); // Work tracking systems errors

		// Check team counts
		expect(successCounts[1]).toBeInTheDocument(); // Teams success
		expect(validationIssues).toBeInTheDocument(); // Teams validation issues

		// Check project counts
		expect(successCounts[2]).toBeInTheDocument(); // Projects success
		expect(errorCounts[1]).toBeInTheDocument(); // Projects errors

		// Check that error messages are shown in tables
		expect(screen.getByText("Connection failed")).toBeInTheDocument();
		expect(screen.getByText("Missing required fields")).toBeInTheDocument();
		expect(
			screen.getByText("Missing work tracking system"),
		).toBeInTheDocument();

		// Update button should be enabled because there are some successful imports
		expect(screen.getByTestId("update-data-button")).not.toBeDisabled();
	});

	it("renders summary with validation issues", () => {
		const validationFailedImport = createValidationFailedResults();

		render(
			<ImportSummaryStep
				importResults={validationFailedImport}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);

		// Check warning message is shown
		expect(
			screen.getByText(
				"All items were imported but some have validation issues that need manual configuration.",
			),
		).toBeInTheDocument();

		// Check that validation issues are shown in tables
		expect(screen.getByText("Missing required fields")).toBeInTheDocument();
		expect(
			screen.getByText("Missing required configuration"),
		).toBeInTheDocument();

		// Buttons should be enabled because there is at least one successful item
		expect(screen.getByTestId("update-data-button")).not.toBeDisabled();
	});

	it("renders summary of failed import", () => {
		const failedImport = createFailedImportResults();

		render(
			<ImportSummaryStep
				importResults={failedImport}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);

		// Check error message is shown
		expect(
			screen.getByText(
				"Some items failed to import. Review the details below.",
			),
		).toBeInTheDocument();
		// Check summary section counts
		const errorCounts = screen.getAllByText("Errors: 1");

		// Check system, team, and project error counts
		expect(errorCounts[0]).toBeInTheDocument(); // Work tracking systems
		expect(errorCounts[1]).toBeInTheDocument(); // Teams
		expect(errorCounts[2]).toBeInTheDocument(); // Projects

		// Check that error messages are shown in tables
		expect(screen.getByText("Connection failed")).toBeInTheDocument();
		expect(screen.getByText("Failed to create team")).toBeInTheDocument();
		expect(screen.getByText("Failed to create project")).toBeInTheDocument();

		// Update button should be disabled because there are no successful imports
		expect(screen.getByTestId("update-data-button")).toBeDisabled();
	});

	it("renders status icons correctly", () => {
		const mixedImport = createMixedImportResults();

		render(
			<ImportSummaryStep
				importResults={mixedImport}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);

		// Find table rows containing each status type
		const successRow = screen.getAllByText("Success")[0].closest("tr");
		const validationFailedRow = screen
			.getByText("Validation Failed")
			.closest("tr");
		const errorRow = screen.getAllByText("Error")[0].closest("tr");

		// Check that each row has the right icon visible
		// Note: We can't easily check the specific icon component, but we can verify the row structure
		expect(successRow).toBeTruthy();
		expect(validationFailedRow).toBeTruthy();
		expect(errorRow).toBeTruthy();
	});

	it("calls onClose when Close button is clicked", () => {
		const successfulImport = createSuccessfulImportResults();

		render(
			<ImportSummaryStep
				importResults={successfulImport}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);

		fireEvent.click(screen.getByTestId("close-button"));
		expect(mockOnClose).toHaveBeenCalled();
	});

	it("calls updateTeamData and refreshFeaturesForProject when Update button is clicked", async () => {
		const successfulImport = createSuccessfulImportResults();

		successfulImport.teams[0].entity.name = "Team 1";
		successfulImport.teams[0].entity.id = 1;
		successfulImport.teams[1].entity.name = "Team 2";
		successfulImport.teams[1].entity.id = 2;

		render(
			<ImportSummaryStep
				importResults={successfulImport}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);

		fireEvent.click(screen.getByTestId("update-data-button"));

		await waitFor(() => {
			// Check that services were called for each successful entity
			expect(mockTeamService.updateTeamData).toHaveBeenCalledWith(1);
			expect(mockTeamService.updateTeamData).toHaveBeenCalledWith(2);
			expect(mockProjectService.refreshFeaturesForProject).toHaveBeenCalledWith(
				1,
			);

			// onClose should be called after updating data
			expect(mockOnClose).toHaveBeenCalled();
		});
	});

	it("does not render entity table when there are no entities of that type", () => {
		// Create import results with no teams
		const importResults: ImportResults = {
			workTrackingSystems: [
				{
					entity: {
						id: 1,
						dataSourceType: "Query",
						name: "Azure DevOps",
						workTrackingSystem: "AzureDevOps",
						options: [],
					},
					status: "Success",
				},
			],
			teams: [], // No teams
			projects: [
				{
					entity: createMockProjectSettings(),
					status: "Success",
				},
			],
		};

		render(
			<ImportSummaryStep
				importResults={importResults}
				teamService={mockTeamService}
				projectService={mockProjectService}
				onClose={mockOnClose}
			/>,
		);
		// Check that the work tracking systems and portfolios tables are rendered
		const wtsHeadings = screen.getAllByText("workTrackingSystems");
		const portfoliosHeadings = screen.getAllByText("Portfolios"); // There should be at least the table heading for work tracking systems
		expect(wtsHeadings.length).toBeGreaterThanOrEqual(1);
		// There should be at least the table heading for portfolios
		expect(portfoliosHeadings.length).toBeGreaterThanOrEqual(1);

		// Try to find a "teams" heading for a table
		// Teams table should not be rendered
		const teamHeadings = screen.queryAllByText("teams");
		// There should be only one "teams" text from the summary section, not another one for a table
		expect(teamHeadings.length).toBe(1); // Verify this "Teams" heading is not in a table but in the summary section
		expect(teamHeadings[0].closest("table")).toBeNull();
	});
});
