import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IDataRetrievalSchema } from "../../../models/Common/DataRetrievalSchema";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockTerminologyService,
	createMockWorkTrackingSystemService,
} from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import GeneralSettingsComponent from "./GeneralSettingsComponent";

// Story #5610 slice 02, DD-8 / ADR-126 decision 4. Opening a picker needs a system administrator,
// and the button that leads to one has been rendered to everybody since the wizard shipped.
const rbac = vi.hoisted(() => ({ isSystemAdmin: true }));

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: true,
		isSystemAdmin: rbac.isSystemAdmin,
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: () => true,
		isPortfolioAdmin: () => true,
		summary: {
			isRbacEnabled: true,
			isSystemAdmin: rbac.isSystemAdmin,
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
}));

// Polyfill File.prototype.text() for tests
if (typeof File !== "undefined" && !File.prototype.text) {
	File.prototype.text = function () {
		return new Promise((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => resolve(reader.result as string);
			reader.onerror = () =>
				reject(new Error(reader.error?.message ?? "Failed to read file"));
			reader.readAsText(this);
		});
	};
}

describe("GeneralSettingsComponent", () => {
	const mockOnSettingsChange = vi.fn();
	const mockOnWorkTrackingSystemChange = vi.fn();

	const testSettings = createMockTeamSettings();
	testSettings.name = "Test Settings";
	testSettings.dataRetrievalValue = "Test Query";

	const mockWorkTrackingSystems: IWorkTrackingSystemConnection[] = [
		{
			id: 1,
			name: "Test System 1",
			workTrackingSystem: "Jira",
			options: [],
			writeBackMappingDefinitions: [],
			authenticationMethodKey: "jira.cloud",
			workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
			additionalFieldDefinitions: [],
		},
		{
			id: 2,
			name: "Test System 2",
			workTrackingSystem: "AzureDevOps",
			options: [],
			writeBackMappingDefinitions: [],
			authenticationMethodKey: "ado.pat",
			workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL Query",
			additionalFieldDefinitions: [],
		},
	];

	beforeEach(() => {
		mockOnSettingsChange.mockClear();
		mockOnWorkTrackingSystemChange.mockClear();
	});

	it("renders correctly with provided settings", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
				workTrackingSystems={mockWorkTrackingSystems}
				selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
			/>,
		);

		expect(screen.getByLabelText("Name")).toHaveValue("Test Settings");
		expect(screen.getByLabelText("JQL Query")).toHaveValue("Test Query");
	});

	it("calls onSettingsChange with correct arguments when name changes", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Name"), {
			target: { value: "Updated Settings" },
		});

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"name",
			"Updated Settings",
		);
	});

	it("calls onSettingsChange with correct arguments when work item query changes", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				workTrackingSystems={mockWorkTrackingSystems}
				selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("JQL Query"), {
			target: { value: "Updated Query" },
		});

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"dataRetrievalValue",
			"Updated Query",
		);
	});

	it("renders with custom title when provided", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
				title="Custom Title"
			/>,
		);

		expect(screen.getByText("Custom Title")).toBeInTheDocument();
	});

	it("handles null settings gracefully", () => {
		render(
			<GeneralSettingsComponent
				settings={null}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		expect(screen.getByLabelText("Name")).toHaveValue("");
	});

	it("shows work tracking system selection when showWorkTrackingSystemSelection is true", () => {
		const mockTerminologyService = createMockTerminologyService();
		mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
			{ key: "WORK_TRACKING_SYSTEM", value: "System" },
			{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
		]);

		const mockApiContext = createMockApiServiceContext({
			workTrackingSystemService: createMockWorkTrackingSystemService(),
			terminologyService: mockTerminologyService,
		});

		const queryClient = new QueryClient({
			defaultOptions: {
				queries: {
					retry: false,
				},
			},
		});

		render(
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TerminologyProvider>
						<GeneralSettingsComponent
							settings={testSettings}
							onSettingsChange={mockOnSettingsChange}
							workTrackingSystems={mockWorkTrackingSystems}
							selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
							onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
							showWorkTrackingSystemSelection={true}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		expect(screen.getByRole("combobox")).toBeInTheDocument();
	});

	it("calls onWorkTrackingSystemChange when switching work tracking systems", async () => {
		const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();
		mockWorkTrackingSystemService.getWorkTrackingSystems = vi
			.fn()
			.mockResolvedValue(mockWorkTrackingSystems);

		const mockTerminologyService = createMockTerminologyService();
		mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
			{ key: "WORK_TRACKING_SYSTEM", value: "System" },
			{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
		]);

		const mockApiContext = createMockApiServiceContext({
			workTrackingSystemService: mockWorkTrackingSystemService,
			terminologyService: mockTerminologyService,
		});

		const queryClient = new QueryClient({
			defaultOptions: {
				queries: {
					retry: false,
				},
			},
		});

		render(
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TerminologyProvider>
						<GeneralSettingsComponent
							settings={testSettings}
							onSettingsChange={mockOnSettingsChange}
							workTrackingSystems={mockWorkTrackingSystems}
							selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
							onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
							showWorkTrackingSystemSelection={true}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		const selectElement = screen.getByRole("combobox");

		await userEvent.click(selectElement);
		await waitFor(() => {
			expect(screen.getByText("Test System 2")).toBeInTheDocument();
		});
		await userEvent.click(screen.getByText("Test System 2"));

		expect(mockOnWorkTrackingSystemChange).toHaveBeenCalled();
	});

	it("preserves dataRetrievalValue when no previous system was selected", async () => {
		const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();
		mockWorkTrackingSystemService.getWorkTrackingSystems = vi
			.fn()
			.mockResolvedValue(mockWorkTrackingSystems);

		const mockTerminologyService = createMockTerminologyService();
		mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
			{ key: "WORK_TRACKING_SYSTEM", value: "System" },
			{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
		]);

		const mockApiContext = createMockApiServiceContext({
			workTrackingSystemService: mockWorkTrackingSystemService,
			terminologyService: mockTerminologyService,
		});

		const queryClient = new QueryClient({
			defaultOptions: {
				queries: {
					retry: false,
				},
			},
		});

		render(
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TerminologyProvider>
						<GeneralSettingsComponent
							settings={testSettings}
							onSettingsChange={mockOnSettingsChange}
							workTrackingSystems={mockWorkTrackingSystems}
							selectedWorkTrackingSystem={null} // No previous system
							onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
							showWorkTrackingSystemSelection={true}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		const selectElement = screen.getByRole("combobox");

		await userEvent.click(selectElement);
		await waitFor(() => {
			expect(screen.getByText("Test System 1")).toBeInTheDocument();
		});
		await userEvent.click(screen.getByText("Test System 1")); // Select first system when none was selected

		expect(mockOnWorkTrackingSystemChange).toHaveBeenCalled();
		// Should NOT clear dataRetrievalValue when no previous system was selected
		expect(mockOnSettingsChange).not.toHaveBeenCalledWith(
			"dataRetrievalValue",
			"",
		);
	});

	it("shows wizard button when selected work tracking system is CSV", () => {
		const mockTerminologyService = createMockTerminologyService();
		mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
			{ key: "WORK_TRACKING_SYSTEM", value: "System" },
			{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
		]);

		const mockApiContext = createMockApiServiceContext({
			workTrackingSystemService: createMockWorkTrackingSystemService(),
			terminologyService: mockTerminologyService,
		});

		const queryClient = new QueryClient({
			defaultOptions: {
				queries: {
					retry: false,
				},
			},
		});

		const csvSystem: IWorkTrackingSystemConnection = {
			id: 3,
			name: "CSV System",
			workTrackingSystem: "Csv",
			options: [],
			writeBackMappingDefinitions: [],
			authenticationMethodKey: "none",
			workTrackingSystemGetDataRetrievalDisplayName: () => "CSV File Content",
			additionalFieldDefinitions: [],
		};

		render(
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TerminologyProvider>
						<GeneralSettingsComponent
							settings={testSettings}
							onSettingsChange={mockOnSettingsChange}
							workTrackingSystems={[...mockWorkTrackingSystems, csvSystem]}
							selectedWorkTrackingSystem={csvSystem}
							onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
							showWorkTrackingSystemSelection={true}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		// Should show wizard button for CSV upload
		expect(
			screen.getByRole("button", { name: /Select CSV File/i }),
		).toBeInTheDocument();
	});

	it("handles missing onWorkTrackingSystemChange callback gracefully", () => {
		const mockTerminologyService = createMockTerminologyService();
		mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
			{ key: "WORK_TRACKING_SYSTEM", value: "System" },
			{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
		]);

		const mockApiContext = createMockApiServiceContext({
			workTrackingSystemService: createMockWorkTrackingSystemService(),
			terminologyService: mockTerminologyService,
		});

		const queryClient = new QueryClient({
			defaultOptions: {
				queries: {
					retry: false,
				},
			},
		});

		// Test without onWorkTrackingSystemChange callback
		expect(() => {
			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={testSettings}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={mockWorkTrackingSystems}
								selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
								showWorkTrackingSystemSelection={true}
								// onWorkTrackingSystemChange is intentionally omitted
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);
		}).not.toThrow();

		expect(screen.getByRole("combobox")).toBeInTheDocument();
	});

	describe("Wizard Functionality", () => {
		it("opens wizard button when CSV system is selected", async () => {
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getWorkTrackingSystems = vi
				.fn()
				.mockResolvedValue([]);

			const mockTerminologyService = createMockTerminologyService();
			mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
				{ key: "WORK_TRACKING_SYSTEM", value: "System" },
				{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
			]);

			const mockApiContext = createMockApiServiceContext({
				workTrackingSystemService: mockWorkTrackingSystemService,
				terminologyService: mockTerminologyService,
			});

			const queryClient = new QueryClient({
				defaultOptions: {
					queries: {
						retry: false,
					},
				},
			});

			const csvSystem: IWorkTrackingSystemConnection = {
				id: 3,
				name: "CSV System",
				workTrackingSystem: "Csv",
				options: [],
				writeBackMappingDefinitions: [],
				authenticationMethodKey: "none",
				workTrackingSystemGetDataRetrievalDisplayName: () => "CSV File Content",
				additionalFieldDefinitions: [],
			};

			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={testSettings}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={[csvSystem]}
								selectedWorkTrackingSystem={csvSystem}
								onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
								showWorkTrackingSystemSelection={true}
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);

			// The wizard button should be present for CSV system
			const wizardButton = await screen.findByRole("button", {
				name: /Select CSV File/i,
			});
			expect(wizardButton).toBeInTheDocument();
		});

		it("wizard button is clickable and interactive", async () => {
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getWorkTrackingSystems = vi
				.fn()
				.mockResolvedValue([]);

			const mockTerminologyService = createMockTerminologyService();
			mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
				{ key: "WORK_TRACKING_SYSTEM", value: "System" },
				{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
			]);

			const mockApiContext = createMockApiServiceContext({
				workTrackingSystemService: mockWorkTrackingSystemService,
				terminologyService: mockTerminologyService,
			});

			const queryClient = new QueryClient({
				defaultOptions: {
					queries: {
						retry: false,
					},
				},
			});

			const csvSystem: IWorkTrackingSystemConnection = {
				id: 3,
				name: "CSV System",
				workTrackingSystem: "Csv",
				options: [],
				writeBackMappingDefinitions: [],
				authenticationMethodKey: "none",
				workTrackingSystemGetDataRetrievalDisplayName: () => "CSV File Content",
				additionalFieldDefinitions: [],
			};

			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={testSettings}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={[csvSystem]}
								selectedWorkTrackingSystem={csvSystem}
								onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
								showWorkTrackingSystemSelection={true}
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);

			const wizardButton = await screen.findByRole("button", {
				name: /Select CSV File/i,
			});

			// Click the wizard button
			await userEvent.click(wizardButton);

			// After clicking, the wizard button should still be visible (until wizard completes)
			expect(wizardButton).toBeInTheDocument();
		});

		it("does not show wizard buttons when selected system has no wizards", () => {
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getWorkTrackingSystems = vi
				.fn()
				.mockResolvedValue([]);

			const mockTerminologyService = createMockTerminologyService();
			mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
				{ key: "WORK_TRACKING_SYSTEM", value: "System" },
				{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
			]);

			const mockApiContext = createMockApiServiceContext({
				workTrackingSystemService: mockWorkTrackingSystemService,
				terminologyService: mockTerminologyService,
			});

			const queryClient = new QueryClient({
				defaultOptions: {
					queries: {
						retry: false,
					},
				},
			});

			// Jira system has no registered wizards
			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={testSettings}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={mockWorkTrackingSystems}
								selectedWorkTrackingSystem={mockWorkTrackingSystems[0]} // Jira
								onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
								showWorkTrackingSystemSelection={true}
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);

			// Should not show any wizard buttons
			expect(
				screen.queryByRole("button", { name: /Upload/i }),
			).not.toBeInTheDocument();
		});

		it("does not show wizard buttons when no work tracking system is selected", () => {
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getWorkTrackingSystems = vi
				.fn()
				.mockResolvedValue([]);

			const mockTerminologyService = createMockTerminologyService();
			mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
				{ key: "WORK_TRACKING_SYSTEM", value: "System" },
				{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
			]);

			const mockApiContext = createMockApiServiceContext({
				workTrackingSystemService: mockWorkTrackingSystemService,
				terminologyService: mockTerminologyService,
			});

			const queryClient = new QueryClient({
				defaultOptions: {
					queries: {
						retry: false,
					},
				},
			});

			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={testSettings}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={mockWorkTrackingSystems}
								selectedWorkTrackingSystem={null} // No system selected
								onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
								showWorkTrackingSystemSelection={true}
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);

			// Should not show any wizard buttons
			expect(
				screen.queryByRole("button", { name: /Upload/i }),
			).not.toBeInTheDocument();
		});

		it("shows wizard buttons based on selected system regardless of showWorkTrackingSystemSelection", () => {
			// This test verifies that wizard buttons are controlled by whether a system
			// is selected, not by the showWorkTrackingSystemSelection flag.
			// This allows wizards to be used even when the selection UI is hidden.
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getWorkTrackingSystems = vi
				.fn()
				.mockResolvedValue([]);

			const mockTerminologyService = createMockTerminologyService();
			mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
				{ key: "WORK_TRACKING_SYSTEM", value: "System" },
				{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
			]);

			const mockApiContext = createMockApiServiceContext({
				workTrackingSystemService: mockWorkTrackingSystemService,
				terminologyService: mockTerminologyService,
			});

			const queryClient = new QueryClient({
				defaultOptions: {
					queries: {
						retry: false,
					},
				},
			});

			const csvSystem: IWorkTrackingSystemConnection = {
				id: 3,
				name: "CSV System",
				workTrackingSystem: "Csv",
				options: [],
				writeBackMappingDefinitions: [],
				authenticationMethodKey: "none",
				workTrackingSystemGetDataRetrievalDisplayName: () => "CSV File Content",
				additionalFieldDefinitions: [],
			};

			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={testSettings}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={[csvSystem]}
								selectedWorkTrackingSystem={csvSystem}
								onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
								showWorkTrackingSystemSelection={false} // Disabled
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);

			// Wizard buttons appear if a system with wizards is selected,
			// even when the selection dropdown UI is hidden
			expect(
				screen.getByRole("button", { name: /Select CSV File/i }),
			).toBeInTheDocument();
		});

		it("shows multiple wizard buttons when system has multiple wizards", async () => {
			// This is a future-proofing test for when systems have multiple wizards
			// Currently only CSV has one wizard, but this validates the rendering logic
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getWorkTrackingSystems = vi
				.fn()
				.mockResolvedValue([]);

			const mockTerminologyService = createMockTerminologyService();
			mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
				{ key: "WORK_TRACKING_SYSTEM", value: "System" },
				{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
			]);

			const mockApiContext = createMockApiServiceContext({
				workTrackingSystemService: mockWorkTrackingSystemService,
				terminologyService: mockTerminologyService,
			});

			const queryClient = new QueryClient({
				defaultOptions: {
					queries: {
						retry: false,
					},
				},
			});

			const csvSystem: IWorkTrackingSystemConnection = {
				id: 3,
				name: "CSV System",
				workTrackingSystem: "Csv",
				options: [],
				writeBackMappingDefinitions: [],
				authenticationMethodKey: "none",
				workTrackingSystemGetDataRetrievalDisplayName: () => "CSV File Content",
				additionalFieldDefinitions: [],
			};

			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={testSettings}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={[csvSystem]}
								selectedWorkTrackingSystem={csvSystem}
								onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
								showWorkTrackingSystemSelection={true}
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);

			// For CSV, we expect 1 wizard button
			const wizardButtons = await screen.findAllByRole("button", {
				name: /Select/i,
			});

			// Should have at least one wizard button for CSV
			expect(wizardButtons.length).toBeGreaterThanOrEqual(1);
		});
	});

	describe("handleWizardComplete integration", () => {
		it("updates all fields when CSV wizard completes with populated data", async () => {
			const mockWorkTrackingSystemService =
				createMockWorkTrackingSystemService();
			mockWorkTrackingSystemService.getWorkTrackingSystems = vi
				.fn()
				.mockResolvedValue([]);

			const mockTerminologyService = createMockTerminologyService();
			mockTerminologyService.getAllTerminology = vi.fn().mockResolvedValue([
				{ key: "WORK_TRACKING_SYSTEM", value: "System" },
				{ key: "WORK_TRACKING_SYSTEMS", value: "Systems" },
			]);

			const mockApiContext = createMockApiServiceContext({
				workTrackingSystemService: mockWorkTrackingSystemService,
				terminologyService: mockTerminologyService,
			});

			const queryClient = new QueryClient({
				defaultOptions: {
					queries: {
						retry: false,
					},
				},
			});

			const csvSystem: IWorkTrackingSystemConnection = {
				id: 3,
				name: "CSV System",
				workTrackingSystem: "Csv",
				options: [],
				writeBackMappingDefinitions: [],
				authenticationMethodKey: "none",
				workTrackingSystemGetDataRetrievalDisplayName: () => "CSV File Content",
				additionalFieldDefinitions: [],
			};

			// Start with settings that have existing values
			const settingsWithData = createMockTeamSettings();
			settingsWithData.name = "Test Settings";
			settingsWithData.dataRetrievalValue = "Existing Query";
			settingsWithData.workItemTypes = ["OldType"];
			settingsWithData.toDoStates = ["Old To Do"];
			settingsWithData.doingStates = ["Old In Progress"];
			settingsWithData.doneStates = ["Old Done"];

			render(
				<QueryClientProvider client={queryClient}>
					<ApiServiceContext.Provider value={mockApiContext}>
						<TerminologyProvider>
							<GeneralSettingsComponent
								settings={settingsWithData}
								onSettingsChange={mockOnSettingsChange}
								workTrackingSystems={[csvSystem]}
								selectedWorkTrackingSystem={csvSystem}
								showWorkTrackingSystemSelection={false}
							/>
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>,
			);

			// Open the wizard
			const uploadButton = screen.getByRole("button", {
				name: /Select CSV File/i,
			});
			await userEvent.click(uploadButton);

			await waitFor(() => {
				expect(screen.getAllByText(/Select CSV File/i).length).toBeGreaterThan(
					0,
				);
			});

			// Upload a CSV file with work item data
			const csvContent =
				"ID,Type,Status\n1,Story,To Do\n2,Bug,In Progress\n3,Task,Done";
			const file = new File([csvContent], "test.csv", { type: "text/csv" });

			const input = screen.getByLabelText("Choose File", {
				selector: "input[type='file']",
			});

			await userEvent.upload(input, file);

			await waitFor(() => {
				expect(screen.getByText("test.csv")).toBeInTheDocument();
			});

			// Complete the wizard
			const useFileButton = screen.getByRole("button", { name: "Use File" });
			await userEvent.click(useFileButton);

			// Verify that onSettingsChange was called with the dataRetrievalValue
			// (CSV wizard only populates dataRetrievalValue, leaves other arrays empty)
			await waitFor(() => {
				expect(mockOnSettingsChange).toHaveBeenCalledWith(
					"dataRetrievalValue",
					csvContent,
				);
			});

			// Verify that empty arrays from CSV wizard don't trigger updates
			// (preserving existing workItemTypes, states, etc.)
			expect(mockOnSettingsChange).not.toHaveBeenCalledWith(
				"workItemTypes",
				expect.anything(),
			);
			expect(mockOnSettingsChange).not.toHaveBeenCalledWith(
				"toDoStates",
				expect.anything(),
			);
			expect(mockOnSettingsChange).not.toHaveBeenCalledWith(
				"doingStates",
				expect.anything(),
			);
			expect(mockOnSettingsChange).not.toHaveBeenCalledWith(
				"doneStates",
				expect.anything(),
			);
		});
	});

	describe("Schema-driven behavior", () => {
		const linearTeamSystem: IWorkTrackingSystemConnection = {
			id: 4,
			name: "Linear System",
			workTrackingSystem: "Linear",
			options: [],
			writeBackMappingDefinitions: [],
			authenticationMethodKey: "linear.apikey",
			workTrackingSystemGetDataRetrievalDisplayName: () => "Linear Team",
			additionalFieldDefinitions: [],
		};

		it("hides data retrieval field when schema inputKind is none", () => {
			const settingsWithNoneSchema = {
				...createMockTeamSettings(),
				dataRetrievalSchema: {
					key: "linear.projects",
					displayLabel: "Linear Projects",
					inputKind: "none" as const,
					isRequired: false,
					isWorkItemTypesRequired: false,
					wizardHint: null,
				},
			};

			render(
				<GeneralSettingsComponent
					settings={settingsWithNoneSchema}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={[linearTeamSystem]}
					selectedWorkTrackingSystem={linearTeamSystem}
				/>,
			);

			expect(screen.getByLabelText("Name")).toBeInTheDocument();
			expect(
				screen.queryByLabelText("Linear Projects"),
			).not.toBeInTheDocument();
		});

		it("renders data retrieval field as read-only when schema inputKind is wizard-select", () => {
			const settingsWithWizardSchema = {
				...createMockTeamSettings(),
				dataRetrievalValue: "team-uuid-123",
				dataRetrievalSchema: {
					key: "linear.team",
					displayLabel: "Linear Team",
					inputKind: "wizard-select" as const,
					isRequired: true,
					isWorkItemTypesRequired: false,
					wizardHint: "linear-team-select",
				},
			};

			render(
				<GeneralSettingsComponent
					settings={settingsWithWizardSchema}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={[linearTeamSystem]}
					selectedWorkTrackingSystem={linearTeamSystem}
				/>,
			);

			const textField = screen.getByLabelText("Linear Team");
			expect(textField).toBeInTheDocument();
			expect(textField).toHaveValue("team-uuid-123");
			expect(textField).toHaveAttribute("readonly");
		});

		it("uses schema displayLabel for the data retrieval field label", () => {
			const settingsWithSchema = {
				...createMockTeamSettings(),
				dataRetrievalSchema: {
					key: "linear.team",
					displayLabel: "Linear Team",
					inputKind: "wizard-select" as const,
					isRequired: true,
					isWorkItemTypesRequired: false,
					wizardHint: "linear-team-select",
				},
			};

			render(
				<GeneralSettingsComponent
					settings={settingsWithSchema}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={[linearTeamSystem]}
					selectedWorkTrackingSystem={linearTeamSystem}
				/>,
			);

			expect(screen.getByLabelText("Linear Team")).toBeInTheDocument();
		});

		it("falls back to connection display name when schema has no displayLabel", () => {
			const settingsWithoutSchema = createMockTeamSettings();

			render(
				<GeneralSettingsComponent
					settings={settingsWithoutSchema}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={[linearTeamSystem]}
					selectedWorkTrackingSystem={linearTeamSystem}
				/>,
			);

			expect(screen.getByLabelText("Linear Team")).toBeInTheDocument();
		});
	});

	// Story #5610 slice 01, AC-A1 / AC-A2 / AC-A5. The guidance rides on the schema, so one shared
	// field renders it for whichever connector has something to say and nothing changes for the ones
	// that do not. Both surfaces the coach can reach — team settings and the create-team wizard —
	// render through this component.
	describe("telling a flow coach what the query field wants", () => {
		const WORKED_EXAMPLE = "active=true^assignment_group=Service Desk";

		const GUIDANCE =
			"A ServiceNow encoded query. In ServiceNow, filter a list, right-click the breadcrumb and choose Copy query. A field name ServiceNow does not know is dropped and the query widens to the whole table; a wrong value on a real field matches nothing.";

		const serviceNowSystem: IWorkTrackingSystemConnection = {
			id: 5,
			name: "Acme ServiceNow",
			workTrackingSystem: "ServiceNow",
			options: [],
			writeBackMappingDefinitions: [],
			authenticationMethodKey: "servicenow.basic",
			workTrackingSystemGetDataRetrievalDisplayName: () => "ServiceNow Query",
			additionalFieldDefinitions: [],
		};

		const aServiceNowTeam = () => ({
			...createMockTeamSettings(),
			dataRetrievalValue: "",
			dataRetrievalSchema: {
				key: "servicenow.query",
				displayLabel: "ServiceNow Query (Encoded Query)",
				inputKind: "freetext",
				isRequired: true,
				isWorkItemTypesRequired: true,
				wizardHint: null,
				placeholder: WORKED_EXAMPLE,
				helpText: GUIDANCE,
			} as unknown as IDataRetrievalSchema,
		});

		it("shows a worked example in the empty field and the guidance beneath it", () => {
			render(
				<GeneralSettingsComponent
					settings={aServiceNowTeam()}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={[serviceNowSystem]}
					selectedWorkTrackingSystem={serviceNowSystem}
				/>,
			);

			expect(
				screen.getByLabelText("ServiceNow Query (Encoded Query)"),
			).toHaveAttribute("placeholder", WORKED_EXAMPLE);
			expect(screen.getByText(GUIDANCE)).toBeInTheDocument();
		});

		// AC-A2. No layout shift and no empty helper row for a connector that has nothing to explain.
		it("leaves a connector with nothing to explain exactly as it was", () => {
			const { container } = render(
				<GeneralSettingsComponent
					settings={testSettings}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={mockWorkTrackingSystems}
					selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
				/>,
			);

			expect(screen.getByLabelText("JQL Query")).not.toHaveAttribute(
				"placeholder",
			);
			expect(container.querySelector(".MuiFormHelperText-root")).toBeNull();
		});
	});

	// Story #5610 slice 02, DD-8 / ADR-126 decision 4. Opening a picker is a system-administrator
	// action, and it always has been — the button simply never said so, and a user who could create
	// a team got a 403 rendered as "Failed to load boards. Please try again." Stated on Jira because
	// the constraint has been lying to Jira, Azure DevOps and Linear users since the wizard shipped.
	describe("who is offered a board picker", () => {
		beforeEach(() => {
			rbac.isSystemAdmin = true;
		});

		it("offers it to a system administrator", () => {
			render(
				<GeneralSettingsComponent
					settings={testSettings}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={mockWorkTrackingSystems}
					selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
					settingsContext="team"
				/>,
			);

			expect(
				screen.getByRole("button", { name: "Select Jira Board" }),
			).toBeInTheDocument();
		});

		// DISTILL scaffold for #5610 slice 02 - un-skip in DELIVER (ADR-025).
		it.skip("does not offer it to someone who cannot open it", () => {
			rbac.isSystemAdmin = false;

			render(
				<GeneralSettingsComponent
					settings={testSettings}
					onSettingsChange={mockOnSettingsChange}
					workTrackingSystems={mockWorkTrackingSystems}
					selectedWorkTrackingSystem={mockWorkTrackingSystems[0]}
					settingsContext="team"
				/>,
			);

			expect(
				screen.queryByRole("button", { name: "Select Jira Board" }),
			).not.toBeInTheDocument();
		});
	});
});
