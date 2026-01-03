import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { ITeam } from "../../../models/Team/Team";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockLicensingService,
	createMockPortfolioService,
	createMockSuggestionService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import ModifyProjectSettings from "./ModifyProjectSettings";

vi.mock("../BaseSettings/GeneralSettingsComponent", () => ({
	__esModule: true,
	default: ({
		onSettingsChange,
		onWorkTrackingSystemChange,
		onNewWorkTrackingSystemConnectionAdded,
		showWorkTrackingSystemSelection,
	}: {
		onSettingsChange: (key: keyof IPortfolioSettings, value: string) => void;
		onWorkTrackingSystemChange?: (
			event: React.ChangeEvent<{ value: unknown }>,
		) => void;
		onNewWorkTrackingSystemConnectionAdded?: (
			connection: IWorkTrackingSystemConnection,
		) => void;
		showWorkTrackingSystemSelection?: boolean;
	}) => (
		<div>
			<div>GeneralSettingsComponent</div>
			<button type="button" onClick={() => onSettingsChange("name", "value")}>
				Change General
			</button>
			{showWorkTrackingSystemSelection && (
				<>
					<button
						type="button"
						onClick={() =>
							onWorkTrackingSystemChange?.({
								target: { value: "System 1" },
							} as React.ChangeEvent<{ value: unknown }>)
						}
					>
						Change Work Tracking System
					</button>
					<button
						type="button"
						onClick={() =>
							onNewWorkTrackingSystemConnectionAdded?.({
								id: 1,
								name: "New System",
								workTrackingSystem: "Jira",
								options: [],
								authenticationMethodKey: "jira.cloud",
								additionalFieldDefinitions: [],
								workTrackingSystemGetDataRetrievalDisplayName: () =>
									"JQL Query",
							} as IWorkTrackingSystemConnection)
						}
					>
						Add New Work Tracking System
					</button>
				</>
			)}
		</div>
	),
}));

vi.mock("../LoadingAnimation/LoadingAnimation", () => ({
	__esModule: true,
	default: ({
		isLoading,
		children,
	}: {
		isLoading: boolean;
		children: React.ReactNode;
	}) => <div>{isLoading ? "Loading..." : children}</div>,
}));

vi.mock("../WorkItemTypes/WorkItemTypesComponent", () => ({
	__esModule: true,
	default: ({
		onAddWorkItemType,
		onRemoveWorkItemType,
	}: {
		onAddWorkItemType: (type: string) => void;
		onRemoveWorkItemType: (type: string) => void;
	}) => (
		<div>
			<div>WorkItemTypesComponent</div>
			<button type="button" onClick={() => onAddWorkItemType("New Work Item")}>
				Add Work Item
			</button>
			<button
				type="button"
				onClick={() => onRemoveWorkItemType("Existing Work Item")}
			>
				Remove Work Item
			</button>
		</div>
	),
}));

vi.mock("../StatesList/StatesList", () => ({
	__esModule: true,
	default: ({
		onAddToDoState,
		onRemoveToDoState,
		onAddDoingState,
		onRemoveDoingState,
		onAddDoneState,
		onRemoveDoneState,
	}: {
		onAddToDoState: (state: string) => void;
		onRemoveToDoState: (state: string) => void;
		onAddDoingState: (state: string) => void;
		onRemoveDoingState: (state: string) => void;
		onAddDoneState: (state: string) => void;
		onRemoveDoneState: (state: string) => void;
	}) => (
		<div>
			<div>StatesList</div>
			<button type="button" onClick={() => onAddToDoState("New ToDo")}>
				Add ToDo
			</button>
			<button type="button" onClick={() => onRemoveToDoState("Existing ToDo")}>
				Remove ToDo
			</button>
			<button type="button" onClick={() => onAddDoingState("New Doing")}>
				Add Doing
			</button>
			<button
				type="button"
				onClick={() => onRemoveDoingState("Existing Doing")}
			>
				Remove Doing
			</button>
			<button type="button" onClick={() => onAddDoneState("New Done")}>
				Add Done
			</button>
			<button type="button" onClick={() => onRemoveDoneState("Existing Done")}>
				Remove Done
			</button>
		</div>
	),
}));

vi.mock("../Tags/TagsComponent", () => ({
	__esModule: true,
	default: ({
		onAddTag,
		onRemoveTag,
	}: {
		onAddTag: (tag: string) => void;
		onRemoveTag: (tag: string) => void;
	}) => (
		<div>
			<div>TagsComponent</div>
			<button type="button" onClick={() => onAddTag("New Tag")}>
				Add Tag
			</button>
			<button type="button" onClick={() => onRemoveTag("Existing Tag")}>
				Remove Tag
			</button>
		</div>
	),
}));

describe("ModifyProjectSettings", () => {
	const mockGetWorkTrackingSystems = vi.fn();
	const mockGetProjectSettings = vi.fn();
	const mockGetAllTeams = vi.fn();
	const mockSaveProjectSettings = vi.fn();
	const mockValidateProjectSettings = vi.fn();

	const mockLicensingService = createMockLicensingService();
	const mockSuggestionService = createMockSuggestionService();
	const mockTeamService = createMockTeamService();
	const mockPortfolioService = createMockPortfolioService();

	mockLicensingService.getLicenseStatus = vi.fn().mockResolvedValue({
		hasLicense: true,
		isValid: true,
		canUsePremiumFeatures: true,
	});
	mockTeamService.getTeams = vi.fn().mockResolvedValue([]);
	mockPortfolioService.getPortfolios = vi.fn().mockResolvedValue([]);
	mockSuggestionService.getStatesForProjects = vi.fn().mockResolvedValue({
		toDoStates: ["New", "Ready", "Backlog"],
		doingStates: ["Active", "In Progress", "In Review"],
		doneStates: ["Done", "Closed", "Completed"],
	});

	const mockApiServiceContext = createMockApiServiceContext({
		licensingService: mockLicensingService,
		suggestionService: mockSuggestionService,
		teamService: mockTeamService,
		portfolioService: mockPortfolioService,
	});

	const MockApiServiceProvider = ({
		children,
	}: {
		children: React.ReactNode;
	}) => {
		return (
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				{children}
			</ApiServiceContext.Provider>
		);
	};

	const renderWithProvider = (component: React.ReactElement) => {
		return render(<MockApiServiceProvider>{component}</MockApiServiceProvider>);
	};

	const workTrackingSystems: IWorkTrackingSystemConnection[] = [
		{
			id: 1,
			name: "System 1",
			options: [],
			workTrackingSystem: "Jira",
			authenticationMethodKey: "jira.cloud",
			additionalFieldDefinitions: [],
			workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
		},
		{
			id: 2,
			name: "System 2",
			options: [],
			workTrackingSystem: "AzureDevOps",
			authenticationMethodKey: "ado.pat",
			additionalFieldDefinitions: [],
			workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL Query",
		},
	];

	const teams: ITeam[] = [
		{
			id: 1,
			name: "Team 1",
			features: [],
			featureWip: 1,
			lastUpdated: new Date(),
			projects: [],
			remainingFeatures: 1,
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			),
			throughputEndDate: new Date(),
			tags: [],
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			workItemTypes: ["User Story", "Bug"],
		},
		{
			id: 2,
			name: "Team 2",
			features: [],
			featureWip: 1,
			lastUpdated: new Date(),
			projects: [],
			remainingFeatures: 1,
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			),
			throughputEndDate: new Date(),
			tags: [],
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			workItemTypes: ["Task", "Feature"],
		},
	];

	const projectSettings = createMockProjectSettings();
	projectSettings.involvedTeams = teams;

	beforeEach(() => {
		mockGetWorkTrackingSystems.mockClear();
		mockGetProjectSettings.mockClear();
		mockGetAllTeams.mockClear();
		mockSaveProjectSettings.mockClear();
		mockValidateProjectSettings.mockClear();

		mockGetWorkTrackingSystems.mockResolvedValue(workTrackingSystems);
		mockGetProjectSettings.mockResolvedValue(projectSettings);
		mockGetAllTeams.mockResolvedValue(teams);
	});

	it("renders loading state initially", () => {
		renderWithProvider(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		expect(screen.getByText("Loading...")).toBeInTheDocument();
	});

	it("renders components correctly after loading", async () => {
		renderWithProvider(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		expect(screen.getByText("Modify Project Settings")).toBeInTheDocument();
		expect(screen.getByText("GeneralSettingsComponent")).toBeInTheDocument();
		expect(screen.getByText("WorkItemTypesComponent")).toBeInTheDocument();
		expect(screen.getByText("StatesList")).toBeInTheDocument();
		expect(screen.getByText("Change Work Tracking System")).toBeInTheDocument();
	});

	it("handles project settings change", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.click(screen.getByText("Change General"));
		expect(screen.getByText("GeneralSettingsComponent")).toBeInTheDocument();
	});

	it("handles adding and removing work item types", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.click(screen.getByText("Add Work Item"));
		fireEvent.click(screen.getByText("Remove Work Item"));

		expect(screen.getByText("WorkItemTypesComponent")).toBeInTheDocument();
	});

	it("handles adding and removing states", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.click(screen.getByText("Add ToDo"));
		fireEvent.click(screen.getByText("Remove ToDo"));
		fireEvent.click(screen.getByText("Add Doing"));
		fireEvent.click(screen.getByText("Remove Doing"));
		fireEvent.click(screen.getByText("Add Done"));
		fireEvent.click(screen.getByText("Remove Done"));

		expect(screen.getByText("StatesList")).toBeInTheDocument();
	});

	it("handles work tracking system change", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.click(screen.getByText("Change Work Tracking System"));
		fireEvent.click(screen.getByText("Add New Work Tracking System"));

		expect(screen.getByText("GeneralSettingsComponent")).toBeInTheDocument();
	});

	it("handles adding and removing tags", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		// Test adding a tag
		fireEvent.click(screen.getByText("Add Tag"));
		// Test removing a tag
		fireEvent.click(screen.getByText("Remove Tag"));

		expect(screen.getByText("TagsComponent")).toBeInTheDocument();
	});

	it("handles save action", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);
		mockValidateProjectSettings.mockResolvedValue(true);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);
		fireEvent.click(screen.getByText("Validate"));
		await waitFor(() => expect(mockValidateProjectSettings).toHaveBeenCalled());

		fireEvent.click(screen.getByText("Save"));
		await waitFor(() => expect(mockSaveProjectSettings).toHaveBeenCalled());
	});

	it("handles validate action", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		fireEvent.click(screen.getByText("Validate"));

		await waitFor(() => expect(mockValidateProjectSettings).toHaveBeenCalled());
	});

	it("sets formValid to true when all inputs are valid", async () => {
		render(
			<ModifyProjectSettings
				title="Modify Project Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getProjectSettings={mockGetProjectSettings}
				getAllTeams={mockGetAllTeams}
				saveProjectSettings={mockSaveProjectSettings}
				validateProjectSettings={mockValidateProjectSettings}
			/>,
		);

		await waitFor(() =>
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
		);

		expect(screen.getByText("Validate")).not.toBeDisabled();
	});

	const scenarios = [
		{ invalidValue: "Name", name: "" },
		{
			invalidValue: "Default Amount of Work Items",
			defaultAmountOfWorkItemsPerFeature: undefined,
		},
		{ invalidValue: "Work Item Types", workItemTypes: [] },
		{ invalidValue: "To Do States", toDoStates: [] },
		{ invalidValue: "Doing States", doingStates: [] },
		{ invalidValue: "Done States", doneStates: [] },
		{ invalidValue: "Work Item Query", dataRetrievalValue: "" },
		{ invalidValue: "Involved Teams", involvedTeams: [] },
		{
			invalidValue: "Missing Historical Days",
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
			defaultWorkItemPercentile: 80,
			percentileHistoryInDays: null,
		},
		{
			invalidValue: "Missing Default Work Item Percentile",
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
			defaultWorkItemPercentile: 0,
			historicalFeaturesWorkItemQuery: "My Query",
		},
	];

	for (const scenario of scenarios) {
		it(`sets formValid to false when ${scenario.invalidValue} is invalid`, async () => {
			const invalidProjectSettings = {
				...projectSettings,
				...scenario,
			};

			mockGetProjectSettings.mockResolvedValue(invalidProjectSettings);
			mockGetAllTeams.mockResolvedValue(invalidProjectSettings.involvedTeams);

			render(
				<ModifyProjectSettings
					title="Modify Project Settings"
					getWorkTrackingSystems={mockGetWorkTrackingSystems}
					getProjectSettings={mockGetProjectSettings}
					getAllTeams={mockGetAllTeams}
					saveProjectSettings={mockSaveProjectSettings}
					validateProjectSettings={mockValidateProjectSettings}
				/>,
			);

			await waitFor(() =>
				expect(screen.queryByText("Loading...")).not.toBeInTheDocument(),
			);

			expect(screen.getByText("Validate")).toBeDisabled();
		});
	}
});
