import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
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

describe("GeneralSettingsComponent", () => {
	const mockOnSettingsChange = vi.fn();
	const mockOnWorkTrackingSystemChange = vi.fn();
	const mockOnNewWorkTrackingSystemConnectionAdded = vi.fn();

	const testSettings = createMockTeamSettings();
	testSettings.name = "Test Settings";
	testSettings.workItemQuery = "Test Query";

	const mockWorkTrackingSystems: IWorkTrackingSystemConnection[] = [
		{
			id: 1,
			name: "Test System 1",
			workTrackingSystem: "Jira",
			options: [],
			dataSourceType: "Query",
			authenticationMethodKey: "jira.cloud",
		},
		{
			id: 2,
			name: "Test System 2",
			workTrackingSystem: "AzureDevOps",
			options: [],
			dataSourceType: "File",
			authenticationMethodKey: "ado.pat",
		},
	];

	beforeEach(() => {
		mockOnSettingsChange.mockClear();
		mockOnWorkTrackingSystemChange.mockClear();
		mockOnNewWorkTrackingSystemConnectionAdded.mockClear();
	});

	it("renders correctly with provided settings", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		expect(screen.getByLabelText("Name")).toHaveValue("Test Settings");
		expect(screen.getByLabelText("Work Item Query")).toHaveValue("Test Query");
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
				onSettingsChange={mockOnSettingsChange}
			/>,
		);

		fireEvent.change(screen.getByLabelText("Work Item Query"), {
			target: { value: "Updated Query" },
		});

		expect(mockOnSettingsChange).toHaveBeenCalledWith(
			"workItemQuery",
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
		expect(screen.getByLabelText("Work Item Query")).toHaveValue("");
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
		expect(screen.getByText(/Add New.*System/)).toBeInTheDocument();
	});

	it("clears workItemQuery when switching between different data source types", async () => {
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
							selectedWorkTrackingSystem={mockWorkTrackingSystems[0]} // Query type
							onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
							onNewWorkTrackingSystemConnectionAdded={
								mockOnNewWorkTrackingSystemConnectionAdded
							}
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
		await userEvent.click(screen.getByText("Test System 2")); // Switch to File type

		expect(mockOnWorkTrackingSystemChange).toHaveBeenCalled();
		// Should clear workItemQuery when switching from Query to File type
		expect(mockOnSettingsChange).toHaveBeenCalledWith("workItemQuery", "");
	});

	it("preserves workItemQuery when switching between same data source types", async () => {
		const sameTypeSystem: IWorkTrackingSystemConnection = {
			id: 3,
			name: "Test System 3",
			workTrackingSystem: "Linear", // Valid WorkTrackingSystemType
			options: [],
			dataSourceType: "Query", // Same as Test System 1
			authenticationMethodKey: "linear.apikey",
		};

		const systemsWithSameType = [...mockWorkTrackingSystems, sameTypeSystem];

		const mockWorkTrackingSystemService = createMockWorkTrackingSystemService();
		mockWorkTrackingSystemService.getWorkTrackingSystems = vi
			.fn()
			.mockResolvedValue(systemsWithSameType);

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
							workTrackingSystems={systemsWithSameType}
							selectedWorkTrackingSystem={mockWorkTrackingSystems[0]} // Query type
							onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
							onNewWorkTrackingSystemConnectionAdded={
								mockOnNewWorkTrackingSystemConnectionAdded
							}
							showWorkTrackingSystemSelection={true}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		const selectElement = screen.getByRole("combobox");

		await userEvent.click(selectElement);
		await waitFor(() => {
			expect(screen.getByText("Test System 3")).toBeInTheDocument();
		});
		await userEvent.click(screen.getByText("Test System 3")); // Switch to another Query type

		expect(mockOnWorkTrackingSystemChange).toHaveBeenCalled();
		// Should NOT clear workItemQuery when switching between same data source types
		expect(mockOnSettingsChange).not.toHaveBeenCalledWith("workItemQuery", "");
	});

	it("preserves workItemQuery when no previous system was selected", async () => {
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
							onNewWorkTrackingSystemConnectionAdded={
								mockOnNewWorkTrackingSystemConnectionAdded
							}
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
		// Should NOT clear workItemQuery when no previous system was selected
		expect(mockOnSettingsChange).not.toHaveBeenCalledWith("workItemQuery", "");
	});

	it("opens ModifyTrackingSystemConnectionDialog when Add New button is clicked", async () => {
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
							onNewWorkTrackingSystemConnectionAdded={
								mockOnNewWorkTrackingSystemConnectionAdded
							}
							showWorkTrackingSystemSelection={true}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		const addButton = screen.getByRole("button", { name: /Add New.*System/ });
		await userEvent.click(addButton);

		// Check that the dialog appears by looking for dialog title
		await waitFor(() => {
			expect(screen.getByText("Create New Connection")).toBeInTheDocument();
		});
	});

	it("shows file upload component when selected work tracking system has File dataSourceType", () => {
		const fileBasedSystem = mockWorkTrackingSystems[1]; // Test System 2 has dataSourceType: "File"
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
							selectedWorkTrackingSystem={fileBasedSystem}
							onWorkTrackingSystemChange={mockOnWorkTrackingSystemChange}
							onNewWorkTrackingSystemConnectionAdded={
								mockOnNewWorkTrackingSystemConnectionAdded
							}
							showWorkTrackingSystemSelection={true}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		// Should show file upload component
		expect(screen.getByText(/Upload/)).toBeInTheDocument();

		// Should NOT show work item query field when using file-based system
		expect(screen.queryByLabelText(/Work Item Query/)).not.toBeInTheDocument();
	});

	it("hides work item query field when showWorkTrackingSystemSelection is false", () => {
		render(
			<GeneralSettingsComponent
				settings={testSettings}
				onSettingsChange={mockOnSettingsChange}
				showWorkTrackingSystemSelection={false}
			/>,
		);

		expect(screen.getByLabelText("Work Item Query")).toBeInTheDocument();
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
});
