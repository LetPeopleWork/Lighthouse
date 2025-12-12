import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ConfigurationExport } from "../../../../../models/Configuration/ConfigurationExport";
import type { ConfigurationValidation } from "../../../../../models/Configuration/ConfigurationValidation";
import type { IProjectSettings } from "../../../../../models/Project/ProjectSettings";
import type { ITeamSettings } from "../../../../../models/Team/TeamSettings";
import type { IConfigurationService } from "../../../../../services/Api/ConfigurationService";
import ImportSettingsStep from "./ImportSettingsStep";

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

// Create a mock configuration service
const createMockConfigurationService = () => {
	return {
		exportConfiguration: vi.fn(),
		clearConfiguration: vi.fn(),
		validateConfiguration: vi.fn().mockResolvedValue({}),
	};
};

describe("ImportSettingsStep", () => {
	const mockOnNext = vi.fn();
	const mockOnClose = vi.fn();
	const mockConfigurationService: IConfigurationService =
		createMockConfigurationService();

	// Helper function to create a valid file with text() method
	const createMockFile = (content: object): File => {
		const fileContent = JSON.stringify(content);
		// Create the File and add text method
		const file = new File([fileContent], "config.json", {
			type: "application/json",
		}) as File & { text: () => Promise<string> };
		// Mock the text() method
		file.text = () => Promise.resolve(fileContent);
		return file;
	};

	// Mock validation response
	const mockValidationResponse: ConfigurationValidation = {
		workTrackingSystems: [
			{
				id: 1,
				name: "Work Tracking System 1",
				status: "New",
				errorMessage: "",
			},
		],
		teams: [{ id: 1, name: "Team 1", status: "Update", errorMessage: "" }],
		projects: [{ id: 1, name: "Project 1", status: "New", errorMessage: "" }],
	};

	// Mock configuration export
	const mockConfigExport: ConfigurationExport = {
		workTrackingSystems: [
			{
				id: 1,
				name: "Work Tracking System 1",
				dataSourceType: "Query",
				workTrackingSystem: "AzureDevOps",
				options: [],
			},
		],
		teams: [
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
		],
		projects: [
			{
				id: 1,
				name: "Project 1",
				workTrackingSystemConnectionId: 1,
				states: [],
				stateMappings: [],
				teams: [],
			} as unknown as IProjectSettings,
		],
	};
	beforeEach(() => {
		vi.clearAllMocks();

		mockConfigurationService.validateConfiguration = vi
			.fn()
			.mockResolvedValue(mockValidationResponse);
	});

	it("renders the file selection button", () => {
		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		expect(screen.getByText("Select Configuration File")).toBeInTheDocument();
		expect(
			screen.getByText("Delete Existing Configuration"),
		).toBeInTheDocument();
		expect(
			screen.getByTestId("clear-configuration-checkbox"),
		).not.toBeChecked();
	});

	it("handles file selection and validation", async () => {
		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		// Create a mock file and trigger the file selection
		const file = createMockFile(mockConfigExport);
		const fileInput = screen.getByTestId("file-input");

		// Fire the file change event
		fireEvent.change(fileInput, { target: { files: [file] } });

		// Wait for the validation to complete
		await waitFor(() => {
			expect(mockConfigurationService.validateConfiguration).toHaveBeenCalled();
			expect(screen.getByText("workTrackingSystems")).toBeInTheDocument();
			expect(screen.getByText("teams")).toBeInTheDocument();
			expect(screen.getByText("Portfolios")).toBeInTheDocument();
			expect(screen.getByText("Work Tracking System 1")).toBeInTheDocument();
			expect(screen.getByText("Team 1")).toBeInTheDocument();
			expect(screen.getByText("Project 1")).toBeInTheDocument();
			expect(screen.getByTestId("next-button")).not.toBeDisabled();
		});
	});

	it("disables the Next button when there are validation errors", async () => {
		// Create mock validation response with error
		const validationWithError: ConfigurationValidation = {
			...mockValidationResponse,
			projects: [
				{
					id: 1,
					name: "Project 1",
					status: "Error",
					errorMessage: "Work Tracking System Not Found",
				},
			],
		};

		mockConfigurationService.validateConfiguration = vi
			.fn()
			.mockResolvedValue(validationWithError);

		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		// Create a mock file and trigger the file selection
		const file = createMockFile(mockConfigExport);
		const fileInput = screen.getByTestId("file-input");

		// Fire the file change event
		fireEvent.change(fileInput, { target: { files: [file] } });

		// Wait for the validation to complete
		await waitFor(() => {
			expect(mockConfigurationService.validateConfiguration).toHaveBeenCalled();
			expect(screen.getByText("Error")).toBeInTheDocument();
			expect(
				screen.getByText("Work Tracking System Not Found"),
			).toBeInTheDocument();
			expect(screen.getByTestId("next-button")).toBeDisabled();
		});
	});

	it("calls onNext with the correct parameters when Next button is clicked", async () => {
		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		// Create a mock file and trigger the file selection
		const file = createMockFile(mockConfigExport);
		const fileInput = screen.getByTestId("file-input");

		// Fire the file change event
		fireEvent.change(fileInput, { target: { files: [file] } });

		// Wait for the validation to complete
		await waitFor(() => {
			expect(screen.getByTestId("next-button")).not.toBeDisabled();
		});

		// Click the Next button
		fireEvent.click(screen.getByTestId("next-button"));

		expect(mockOnNext).toHaveBeenCalledWith(
			expect.arrayContaining([
				expect.objectContaining({ name: "Work Tracking System 1" }),
			]), // newWorkTrackingSystems
			[], // updatedWorkTrackingSystems
			[], // newTeams
			expect.arrayContaining([expect.objectContaining({ name: "Team 1" })]), // updatedTeams
			expect.arrayContaining([expect.objectContaining({ name: "Project 1" })]), // newProjects
			[], // updatedProjects
			expect.any(Map), // workTrackingSystemsIdMapping
			expect.any(Map), // teamIdMapping
			false, // clearConfiguration
		);
	});

	it("transforms Update status to New when clearConfiguration is checked", async () => {
		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		// Create a mock file and trigger the file selection
		const file = createMockFile(mockConfigExport);
		const fileInput = screen.getByTestId("file-input");

		// Check the clear configuration checkbox
		const clearConfigCheckbox = screen.getByTestId(
			"clear-configuration-checkbox",
		);
		fireEvent.click(clearConfigCheckbox);

		// Fire the file change event
		fireEvent.change(fileInput, { target: { files: [file] } });

		// Wait for the validation to complete
		await waitFor(() => {
			expect(mockConfigurationService.validateConfiguration).toHaveBeenCalled();
			expect(screen.getByTestId("next-button")).not.toBeDisabled();
		});

		// Click the Next button
		fireEvent.click(screen.getByTestId("next-button"));

		expect(mockOnNext).toHaveBeenCalledWith(
			expect.arrayContaining([
				expect.objectContaining({ name: "Work Tracking System 1" }),
			]), // newWorkTrackingSystems
			[], // updatedWorkTrackingSystems
			expect.arrayContaining([expect.objectContaining({ name: "Team 1" })]), // newTeams - now this is a new team due to the transformation
			[], // updatedTeams
			expect.arrayContaining([expect.objectContaining({ name: "Project 1" })]), // newProjects
			[], // updatedProjects
			expect.any(Map), // workTrackingSystemsIdMapping
			expect.any(Map), // teamIdMapping
			true, // clearConfiguration
		);
	});
	it("handles error when file has invalid JSON", async () => {
		// Create a file with invalid JSON content
		// Using type assertion to add the text method to the File mock
		const invalidFile = new File(["not valid json"], "invalid.json", {
			type: "application/json",
		}) as File & { text: () => Promise<string> };
		invalidFile.text = () => Promise.resolve("not valid json");

		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		const fileInput = screen.getByTestId("file-input");

		// Fire the file change event with invalid file
		fireEvent.change(fileInput, { target: { files: [invalidFile] } });

		// Wait for the validation to complete
		await waitFor(() => {
			// Check for any alert showing an error
			expect(screen.getByRole("alert")).toBeInTheDocument();
			// Verify the Next button is disabled when there's an error
			expect(screen.getByTestId("next-button")).toBeDisabled();
		});
	});

	it("handles empty file list", async () => {
		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		const fileInput = screen.getByTestId("file-input");

		// Fire the file change event with empty file list
		fireEvent.change(fileInput, { target: { files: [] } });

		// The component should not crash and the next button should remain disabled
		expect(screen.getByTestId("next-button")).toBeDisabled();
	});

	it("calls onClose when Close button is clicked", () => {
		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		fireEvent.click(screen.getByText("Close"));

		expect(mockOnClose).toHaveBeenCalled();
	});
	it("removes secret options from updated work tracking systems", async () => {
		const configExportWithSecretOptions: ConfigurationExport = {
			...mockConfigExport,
			workTrackingSystems: [
				{
					id: 1,
					name: "Work Tracking System 1",
					dataSourceType: "Query",
					workTrackingSystem: "AzureDevOps",
					options: [
						{
							key: "url",
							value: "https://dev.azure.com",
							isSecret: false,
							isOptional: false,
						},
						{
							key: "token",
							value: "secret-token",
							isSecret: true,
							isOptional: false,
						},
					],
				},
			],
		};

		// Mock validation response with Update status for the work tracking system
		const validationResponseWithUpdate: ConfigurationValidation = {
			...mockValidationResponse,
			workTrackingSystems: [
				{
					id: 1,
					name: "Work Tracking System 1",
					status: "Update",
					errorMessage: "",
				},
			],
		};

		mockConfigurationService.validateConfiguration = vi
			.fn()
			.mockResolvedValue(validationResponseWithUpdate);

		render(
			<ImportSettingsStep
				configurationService={mockConfigurationService}
				onNext={mockOnNext}
				onClose={mockOnClose}
			/>,
		);

		// Create a mock file and trigger the file selection
		const file = createMockFile(configExportWithSecretOptions);
		const fileInput = screen.getByTestId("file-input");

		// Fire the file change event
		fireEvent.change(fileInput, { target: { files: [file] } });

		// Wait for the validation to complete
		await waitFor(() => {
			expect(mockConfigurationService.validateConfiguration).toHaveBeenCalled();
			expect(screen.getByTestId("next-button")).not.toBeDisabled();
		});

		// Click the Next button
		fireEvent.click(screen.getByTestId("next-button"));

		expect(mockOnNext).toHaveBeenCalledWith(
			[], // newWorkTrackingSystems
			expect.arrayContaining([
				expect.objectContaining({
					name: "Work Tracking System 1",
					options: expect.arrayContaining([
						expect.objectContaining({
							key: "url",
							value: "https://dev.azure.com",
							isSecret: false,
						}),
					]),
				}),
			]), // updatedWorkTrackingSystems - with only non-secret options
			[], // newTeams
			expect.arrayContaining([expect.objectContaining({ name: "Team 1" })]), // updatedTeams
			expect.arrayContaining([expect.objectContaining({ name: "Project 1" })]), // newProjects
			[], // updatedProjects
			expect.any(Map), // workTrackingSystemsIdMapping
			expect.any(Map), // teamIdMapping
			false, // clearConfiguration
		);

		// Verify that the secret option was removed
		const callArgs = mockOnNext.mock.calls[0];
		const updatedWorkTrackingSystems = callArgs[1];
		expect(updatedWorkTrackingSystems[0].options).toHaveLength(1);
		expect(updatedWorkTrackingSystems[0].options[0].key).toBe("url");
		expect(updatedWorkTrackingSystems[0].options[0].isSecret).toBe(false);
	});
});
