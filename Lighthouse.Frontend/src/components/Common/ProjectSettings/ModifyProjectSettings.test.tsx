import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
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
								target: { value: "System 2" },
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
								writeBackMappingDefinitions: [],
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

vi.mock("../StateMappings/StateMappingsEditor", () => ({
	__esModule: true,
	default: ({
		onChange,
		refreshFailed,
		onReloadDependentData,
	}: {
		onChange: (mappings: { name: string; states: string[] }[]) => void;
		refreshFailed?: boolean;
		onReloadDependentData?: () => void;
	}) => (
		<div>
			<div>StateMappingsEditor</div>
			<button
				type="button"
				onClick={() => onChange([{ name: "Group", states: ["Active"] }])}
			>
				Change State Mapping
			</button>
			{refreshFailed && (
				<button type="button" onClick={() => onReloadDependentData?.()}>
					Reload now
				</button>
			)}
		</div>
	),
}));

const mockCanUpdatePortfolioData = vi.fn(() => true);

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: mockCanUpdatePortfolioData(),
		licenseStatus: {
			hasLicense: true,
			isValid: true,
			canUsePremiumFeatures: true,
		},
		maxTeamsWithoutPremium: 1,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

const mockIsPortfolioAdmin = vi.fn(() => true);

vi.mock("../../../hooks/useRbac", () => ({
	useRbac: () => ({
		isLoading: false,
		isRbacEnabled: true,
		isSystemAdmin: false,
		canCreateTeam: true,
		canCreatePortfolio: true,
		isTeamAdmin: () => true,
		isPortfolioAdmin: mockIsPortfolioAdmin,
		summary: {
			isRbacEnabled: true,
			isSystemAdmin: false,
			canCreateTeam: true,
			canCreatePortfolio: true,
			adminTeamIds: [],
			adminPortfolioIds: [],
		},
	}),
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
			writeBackMappingDefinitions: [],
			workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
		},
		{
			id: 2,
			name: "System 2",
			options: [],
			workTrackingSystem: "AzureDevOps",
			authenticationMethodKey: "ado.pat",
			additionalFieldDefinitions: [],
			writeBackMappingDefinitions: [],
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
			portfolios: [],
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
			hasThroughputBlackoutOverlap: false,
			hasForecastFilter: false,
			refinementConfigured: false,
		},
		{
			id: 2,
			name: "Team 2",
			features: [],
			featureWip: 1,
			lastUpdated: new Date(),
			portfolios: [],
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
			hasThroughputBlackoutOverlap: false,
			hasForecastFilter: false,
			refinementConfigured: false,
		},
	];

	const projectSettings = createMockProjectSettings();

	beforeEach(() => {
		mockGetWorkTrackingSystems.mockClear();
		mockGetProjectSettings.mockClear();
		mockGetAllTeams.mockClear();
		mockSaveProjectSettings.mockClear();
		mockValidateProjectSettings.mockClear();

		mockGetWorkTrackingSystems.mockResolvedValue(workTrackingSystems);
		mockGetProjectSettings.mockResolvedValue(projectSettings);
		mockGetAllTeams.mockResolvedValue(teams);
		mockCanUpdatePortfolioData.mockReturnValue(true);
		mockSaveProjectSettings.mockResolvedValue(undefined);
		mockPortfolioService.refreshFeaturesForPortfolio = vi
			.fn()
			.mockResolvedValue(undefined);
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
		expect(screen.getByText("Cycle Times")).toBeInTheDocument();
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
		await waitFor(() =>
			expect(mockSaveProjectSettings).toHaveBeenCalledWith(
				expect.objectContaining({ workTrackingSystemConnectionId: 2 }),
			),
		);
	});

	it("auto-saves a valid portfolio field change after the debounce window", async () => {
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

		await waitFor(() => expect(mockSaveProjectSettings).toHaveBeenCalled());
	});

	it("does not render a Save button", async () => {
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

		expect(screen.queryByText("Validate")).not.toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Save" }),
		).not.toBeInTheDocument();
	});

	it("suppresses auto-save when portfolio data updates are not permitted", async () => {
		mockCanUpdatePortfolioData.mockReturnValue(false);

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
		await new Promise((resolve) => setTimeout(resolve, 500));

		expect(mockSaveProjectSettings).not.toHaveBeenCalled();
	});

	it("refreshes dependent portfolio data after a valid state-mapping change", async () => {
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

		fireEvent.click(screen.getByText("Change State Mapping"));

		await waitFor(() => expect(mockSaveProjectSettings).toHaveBeenCalled());
		await waitFor(() =>
			expect(
				mockPortfolioService.refreshFeaturesForPortfolio,
			).toHaveBeenCalledWith(projectSettings.id),
		);
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
		it(`does not auto-save when ${scenario.invalidValue} is invalid`, async () => {
			const invalidProjectSettings = {
				...projectSettings,
				...scenario,
			};

			mockGetProjectSettings.mockResolvedValue(invalidProjectSettings);
			mockGetAllTeams.mockResolvedValue(teams);

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

			fireEvent.click(screen.getByText("Remove ToDo"));
			await new Promise((resolve) => setTimeout(resolve, 500));

			expect(mockSaveProjectSettings).not.toHaveBeenCalled();
		});
	}

	describe("Linear schema-driven behavior", () => {
		it("hides WorkItemTypesComponent when schema says isWorkItemTypesRequired is false", async () => {
			const linearPortfolioSettings = {
				...projectSettings,
				dataRetrievalSchema: {
					key: "linear.projects",
					displayLabel: "Linear Projects",
					inputKind: "none" as const,
					isRequired: false,
					isWorkItemTypesRequired: false,
					wizardHint: null,
				},
			};

			mockGetProjectSettings.mockResolvedValue(linearPortfolioSettings);

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

			expect(
				screen.queryByText("WorkItemTypesComponent"),
			).not.toBeInTheDocument();
		});

		it("validates successfully without work item types and data retrieval value for Linear portfolio", async () => {
			const linearPortfolioSettings = {
				...projectSettings,
				workItemTypes: [],
				dataRetrievalValue: "",
				dataRetrievalSchema: {
					key: "linear.projects",
					displayLabel: "Linear Projects",
					inputKind: "none" as const,
					isRequired: false,
					isWorkItemTypesRequired: false,
					wizardHint: null,
				},
			};

			mockGetProjectSettings.mockResolvedValue(linearPortfolioSettings);
			mockValidateProjectSettings.mockResolvedValue(true);

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

			fireEvent.click(screen.getByText("Change General"));

			await waitFor(() => expect(mockSaveProjectSettings).toHaveBeenCalled());
		});
	});

	describe("staleness relocated to Flow Metrics", () => {
		it("surfaces the staleness threshold in Flow Metrics Configuration, not a standalone Flow Signals group", async () => {
			mockGetProjectSettings.mockResolvedValue({
				...projectSettings,
				stalenessThresholdDays: 14,
			});

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

			expect(screen.queryByText("Flow Signals")).not.toBeInTheDocument();
			expect(
				screen.getByText("Flow Metrics Configuration"),
			).toBeInTheDocument();
			expect(screen.getByLabelText("Staleness Threshold (days)")).toHaveValue(
				14,
			);
		});

		it("hides the staleness field when the threshold is 0 (opt-in off)", async () => {
			mockGetProjectSettings.mockResolvedValue({
				...projectSettings,
				stalenessThresholdDays: 0,
			});

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

			expect(
				screen.queryByLabelText("Staleness Threshold (days)"),
			).not.toBeInTheDocument();
		});

		// The seed value is owner-specific wiring: ModifyProjectSettings passes 14,
		// ModifyTeamSettings passes 5. FlowMetricsConfigurationComponent's own test
		// only proves it seeds with whatever it is handed, so the per-owner number
		// is only pinned here.
		it("seeds the revealed field with the portfolio default of 14 on opt-in", async () => {
			mockGetProjectSettings.mockResolvedValue({
				...projectSettings,
				stalenessThresholdDays: 0,
			});

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

			fireEvent.click(screen.getByLabelText("Set Staleness Threshold"));

			await waitFor(() =>
				expect(screen.getByLabelText("Staleness Threshold (days)")).toHaveValue(
					14,
				),
			);
		});
	});

	describe("blocked autosave feedback", () => {
		it("names the To Do States field when no To Do state is configured", async () => {
			mockGetProjectSettings.mockResolvedValue({
				...projectSettings,
				toDoStates: [],
			});

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

			const warning = await screen.findByTestId("settings-blocking-warning");
			expect(warning).toHaveTextContent("Add at least one To Do State");
		});

		it("renders no warning while the form can be saved", async () => {
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

			expect(
				screen.queryByTestId("settings-blocking-warning"),
			).not.toBeInTheDocument();
		});
	});

	describe("a Portfolio that follows the connection's Parent Override Field", () => {
		const givenThePortfolioFollowsTheDefault = async (
			theOtherConnection: Partial<IWorkTrackingSystemConnection> = {},
		) => {
			mockGetWorkTrackingSystems.mockResolvedValue([
				{
					...workTrackingSystems[0],
					additionalFieldDefinitions: [
						{ id: 21, displayName: "Parent Link", reference: "customfield_1" },
						{ id: 22, displayName: "Caused by", reference: "customfield_2" },
					],
				},
				{ ...workTrackingSystems[1], ...theOtherConnection },
			]);
			mockGetProjectSettings.mockResolvedValue({
				...createMockProjectSettings(),
				workTrackingSystem: "Jira",
				parentOverrideSource: "ConnectionDefault",
				parentOverrideDefaultName: "Parent Link",
				parentOverrideAdditionalFieldDefinitionId: 21,
			});

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

			const advanced = screen
				.getByText("Advanced Configuration")
				.closest(".MuiCard-root") as HTMLElement;
			fireEvent.click(within(advanced).getByLabelText("toggle"));
			return screen.getByRole("combobox", { name: "Parent Override Field" });
		};

		// @driving_port @us-07 @ac-7.5 @contract-shape:pure-function
		it("shows the followed field as the selected first entry", async () => {
			const dropdown = await givenThePortfolioFollowsTheDefault();

			expect(dropdown).toHaveTextContent("Connection default (Parent Link)");
			expect(
				screen.getByText("Set on the Jira connection."),
			).toBeInTheDocument();
			fireEvent.mouseDown(dropdown);
			expect(
				within(screen.getByRole("listbox"))
					.getAllByRole("option")
					.map((entry) => entry.textContent),
			).toEqual([
				"Connection default (Parent Link)",
				"None",
				"Parent Link",
				"Caused by",
			]);
		});

		// @us-07 @ac-7.5 @contract-shape:bounded-change
		it("picking another entry makes the value the Portfolio's own", async () => {
			const dropdown = await givenThePortfolioFollowsTheDefault();

			fireEvent.mouseDown(dropdown);
			fireEvent.click(
				within(screen.getByRole("listbox")).getByRole("option", {
					name: "Caused by",
				}),
			);

			await waitFor(() =>
				expect(mockSaveProjectSettings).toHaveBeenCalledWith(
					expect.objectContaining({
						parentOverrideSource: "Own",
						parentOverrideAdditionalFieldDefinitionId: 22,
					}),
				),
			);
		});

		// @us-07 @ac-7.5 @contract-shape:bounded-change
		it("switching the connection shows the default of the connection now chosen", async () => {
			const dropdown = await givenThePortfolioFollowsTheDefault({
				additionalFieldDefinitions: [
					{ id: 31, displayName: "Epic Link", reference: "System.Parent" },
				],
				defaults: {
					parentOverrideForTeams: null,
					parentOverrideForPortfolios: 31,
				},
			});

			fireEvent.click(screen.getByText("Change Work Tracking System"));

			await waitFor(() =>
				expect(dropdown).toHaveTextContent(
					/^Connection default \(Epic Link\)$/,
				),
			);
			expect(
				screen.getByText(/^Set on the Azure DevOps connection\.$/),
			).toBeInTheDocument();
		});

		// @us-07 @ac-7.5 @contract-shape:bounded-change
		it("switching to a connection whose default names nothing shows None", async () => {
			const dropdown = await givenThePortfolioFollowsTheDefault({
				defaults: { parentOverrideForTeams: 31 },
			});

			fireEvent.click(screen.getByText("Change Work Tracking System"));

			await waitFor(() =>
				expect(dropdown).toHaveTextContent(/^Connection default \(None\)$/),
			);
		});

		// @us-07 @ac-7.5 @contract-shape:bounded-change
		it("switching to a connection whose tracker holds no default offers no connection default", async () => {
			const dropdown = await givenThePortfolioFollowsTheDefault({
				workTrackingSystem: "Linear",
			});

			fireEvent.click(screen.getByText("Change Work Tracking System"));

			await waitFor(() =>
				expect(dropdown).not.toHaveTextContent(/^Connection default/),
			);
			fireEvent.mouseDown(dropdown);
			expect(
				within(screen.getByRole("listbox"))
					.getAllByRole("option")
					.map((entry) => entry.textContent),
			).not.toContainEqual(expect.stringMatching(/^Connection default/));
			expect(screen.queryByText(/^Set on the /)).not.toBeInTheDocument();
		});
	});

	describe.each([
		{
			group: "Default Feature Size",
			field: "Size Estimate Field",
			theDefault: "sizeEstimateFieldForPortfolios",
			following: {
				sizeEstimateSource: "ConnectionDefault",
				sizeEstimateDefaultName: "T-Shirt Size",
				sizeEstimateAdditionalFieldDefinitionId: 21,
			},
		},
		{
			group: "Dependency Settings",
			field: "Dependency Field",
			theDefault: "dependencyFieldForPortfolios",
			following: {
				dependencyFieldSource: "ConnectionDefault",
				dependencyFieldDefaultName: "T-Shirt Size",
				dependencyOverrideAdditionalFieldDefinitionId: 21,
			},
		},
		{
			group: "Ownership Settings",
			field: "Feature Owner Field",
			theDefault: "featureOwnerFieldForPortfolios",
			following: {
				featureOwnerSource: "ConnectionDefault",
				featureOwnerDefaultName: "T-Shirt Size",
				featureOwnerAdditionalFieldDefinitionId: 21,
			},
		},
	] as const)(
		"a Portfolio that follows the connection's $field",
		({ group, field, theDefault, following }) => {
			// @us-09 @ac-9.1 @contract-shape:bounded-change
			it("switching the connection shows the default of the connection now chosen", async () => {
				mockGetWorkTrackingSystems.mockResolvedValue([
					{
						...workTrackingSystems[0],
						additionalFieldDefinitions: [
							{
								id: 21,
								displayName: "T-Shirt Size",
								reference: "customfield_1",
							},
						],
					},
					{
						...workTrackingSystems[1],
						additionalFieldDefinitions: [
							{ id: 31, displayName: "Effort", reference: "System.Effort" },
						],
						defaults: { [theDefault]: 31 },
					},
				]);
				mockGetProjectSettings.mockResolvedValue({
					...createMockProjectSettings(),
					workTrackingSystem: "Jira",
					...following,
				});
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
				const section = screen
					.getByText(group)
					.closest(".MuiCard-root") as HTMLElement;
				fireEvent.click(within(section).getByLabelText("toggle"));
				const dropdown = screen.getByRole("combobox", { name: field });
				expect(dropdown).toHaveTextContent(
					/^Connection default \(T-Shirt Size\)$/,
				);

				fireEvent.click(screen.getByText("Change Work Tracking System"));

				await waitFor(() =>
					expect(dropdown).toHaveTextContent(/^Connection default \(Effort\)$/),
				);
			});
		},
	);
});
