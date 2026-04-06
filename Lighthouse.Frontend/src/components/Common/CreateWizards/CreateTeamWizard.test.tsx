import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBoardInformation } from "../../../models/Boards/BoardInformation";
import type {
	DataRetrievalWizardProps,
	IDataRetrievalWizard,
} from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockTerminologyService,
} from "../../../tests/MockApiServiceProvider";
// ---------- mock wizard registry ----------
import { getWizardsForSystem } from "../../DataRetrievalWizards";
import CreateTeamWizard from "./CreateTeamWizard";

vi.mock("../../DataRetrievalWizards", () => ({
	getWizardsForSystem: vi.fn().mockReturnValue([]),
}));
const mockGetWizardsForSystem = vi.mocked(getWizardsForSystem);

// A mock wizard component that completes immediately on click
const createMockWizardComponent = (boardInfo: IBoardInformation) => {
	const MockWizard: React.FC<DataRetrievalWizardProps> = ({
		onComplete,
		onCancel,
	}) => (
		<div data-testid="mock-wizard-dialog">
			<button type="button" onClick={() => onComplete(boardInfo)}>
				Complete Wizard
			</button>
			<button type="button" onClick={onCancel}>
				Cancel Wizard
			</button>
		</div>
	);
	MockWizard.displayName = "MockWizard";
	return MockWizard;
};

const fullBoardInfo: IBoardInformation = {
	dataRetrievalValue: "SELECT * FROM WorkItems",
	workItemTypes: ["User Story", "Bug"],
	toDoStates: ["New"],
	doingStates: ["Active"],
	doneStates: ["Closed"],
};

const createMockWizard = (
	name: string,
	boardInfo: IBoardInformation,
): IDataRetrievalWizard => ({
	id: `mock.${name}`,
	name,
	applicableSystemTypes: ["AzureDevOps"],
	applicableSettingsContexts: ["team"],
	component: createMockWizardComponent(boardInfo),
});

// ---------- mock child components ----------
vi.mock("../WorkItemTypes/WorkItemTypesComponent", () => ({
	default: ({
		workItemTypes,
		onAddWorkItemType,
		onRemoveWorkItemType,
	}: {
		workItemTypes: string[];
		onAddWorkItemType: (type: string) => void;
		onRemoveWorkItemType: (type: string) => void;
	}) => (
		<div>
			<div>WorkItemTypesComponent</div>
			{workItemTypes.map((type) => (
				<span key={type}>{type}</span>
			))}
			<button type="button" onClick={() => onAddWorkItemType("NewType")}>
				Add Work Item Type
			</button>
			<button
				type="button"
				onClick={() => onRemoveWorkItemType(workItemTypes[0] ?? "")}
			>
				Remove Work Item Type
			</button>
		</div>
	),
}));

vi.mock("../StatesList/StatesList", () => ({
	default: ({
		toDoStates,
		doingStates,
		doneStates,
		onAddToDoState,
		onAddDoingState,
		onAddDoneState,
	}: {
		toDoStates: string[];
		doingStates: string[];
		doneStates: string[];
		onAddToDoState: (state: string) => void;
		onAddDoingState: (state: string) => void;
		onAddDoneState: (state: string) => void;
	}) => (
		<div>
			<div>StatesListComponent</div>
			<div data-testid="todo-states">
				{toDoStates.map((s) => (
					<span key={s}>{s}</span>
				))}
			</div>
			<div data-testid="doing-states">
				{doingStates.map((s) => (
					<span key={s}>{s}</span>
				))}
			</div>
			<div data-testid="done-states">
				{doneStates.map((s) => (
					<span key={s}>{s}</span>
				))}
			</div>
			<button type="button" onClick={() => onAddToDoState("New")}>
				Add ToDo State
			</button>
			<button type="button" onClick={() => onAddDoingState("InProgress")}>
				Add Doing State
			</button>
			<button type="button" onClick={() => onAddDoneState("Done")}>
				Add Done State
			</button>
		</div>
	),
}));

// ---------- test fixtures ----------
const mockConnections: IWorkTrackingSystemConnection[] = [
	{
		id: 1,
		name: "My ADO Connection",
		workTrackingSystem: "AzureDevOps",
		options: [],
		availableAuthenticationMethods: [],
		authenticationMethodKey: "ado.pat",
		workTrackingSystemGetDataRetrievalDisplayName: () => "Board/Query",
		additionalFieldDefinitions: [],
		writeBackMappingDefinitions: [],
	},
	{
		id: 2,
		name: "My Jira Connection",
		workTrackingSystem: "Jira",
		options: [],
		availableAuthenticationMethods: [],
		authenticationMethodKey: "jira.cloud",
		workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
		additionalFieldDefinitions: [],
		writeBackMappingDefinitions: [],
	},
	{
		id: 3,
		name: "My Linear Connection",
		workTrackingSystem: "Linear",
		options: [],
		availableAuthenticationMethods: [],
		authenticationMethodKey: "linear.apikey",
		workTrackingSystemGetDataRetrievalDisplayName: () => "Team",
		additionalFieldDefinitions: [],
		writeBackMappingDefinitions: [],
	},
];

const createQueryClient = () =>
	new QueryClient({
		defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
	});

interface RenderOptions {
	connections?: IWorkTrackingSystemConnection[];
	validateTeamSettings?: (settings: ITeamSettings) => Promise<boolean>;
	saveTeamSettings?: (settings: ITeamSettings) => Promise<void>;
	onCancel?: () => void;
}

const renderWizard = (options: RenderOptions = {}) => {
	const {
		connections = mockConnections,
		validateTeamSettings = vi.fn().mockResolvedValue(true),
		saveTeamSettings = vi.fn().mockResolvedValue(undefined),
		onCancel = vi.fn(),
	} = options;

	const getConnections = vi.fn().mockResolvedValue(connections);

	const mockTerminologyService = createMockTerminologyService();
	vi.mocked(mockTerminologyService.getAllTerminology).mockResolvedValue([
		{
			id: 1,
			key: "workTrackingSystem",
			defaultValue: "Work Tracking System",
			description: "",
			value: "Work Tracking System",
		},
	]);

	const mockApiServiceContext = createMockApiServiceContext({
		terminologyService: mockTerminologyService,
	});

	render(
		<QueryClientProvider client={createQueryClient()}>
			<MemoryRouter>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<TerminologyProvider>
						<CreateTeamWizard
							getConnections={getConnections}
							validateTeamSettings={validateTeamSettings}
							saveTeamSettings={saveTeamSettings}
							onCancel={onCancel}
						/>
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</MemoryRouter>
		</QueryClientProvider>,
	);

	return {
		getConnections,
		validateTeamSettings,
		saveTeamSettings,
		onCancel,
	};
};

// ---------- tests ----------
describe("CreateTeamWizard", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockGetWizardsForSystem.mockReturnValue([]);
	});

	describe("Step 1: Choose Connection", () => {
		it("renders a stepper with four steps", async () => {
			renderWizard();
			await waitFor(() => {
				expect(screen.getByText("Choose Connection")).toBeInTheDocument();
				expect(screen.getByText("Load Data")).toBeInTheDocument();
				expect(screen.getByText("Configure")).toBeInTheDocument();
				expect(screen.getByText("Name & Create")).toBeInTheDocument();
			});
		});

		it("shows available connections as selectable options", async () => {
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("button", { name: /My Jira Connection/i }),
				).toBeInTheDocument();
				expect(
					screen.getByRole("button", { name: /My Linear Connection/i }),
				).toBeInTheDocument();
			});
		});

		it("does not show a Back button on step 1", async () => {
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			expect(
				screen.queryByRole("button", { name: /Back/i }),
			).not.toBeInTheDocument();
		});

		it("advances to Load Data step when a connection is selected", async () => {
			const user = userEvent.setup();
			mockGetWizardsForSystem.mockReturnValue([
				createMockWizard("Select Board", fullBoardInfo),
			]);
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Select Board/i }),
				).toBeInTheDocument();
			});
		});

		it("skips Load Data to Configure when no wizards available", async () => {
			const user = userEvent.setup();
			mockGetWizardsForSystem.mockReturnValue([]);
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			// Should skip Load Data and go straight to Configure (step 2)
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});
		});
	});

	describe("Step 2: Load Data", () => {
		const goToLoadData = async () => {
			const user = userEvent.setup();
			const wizard = createMockWizard("Select Board", fullBoardInfo);
			mockGetWizardsForSystem.mockReturnValue([wizard]);
			const validateTeamSettings = vi.fn().mockResolvedValue(true);
			const saveTeamSettings = vi.fn().mockResolvedValue(undefined);
			renderWizard({ validateTeamSettings, saveTeamSettings });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Select Board/i }),
				).toBeInTheDocument();
			});
			return { user, validateTeamSettings, saveTeamSettings };
		};

		it("shows wizard buttons for the selected connection type", async () => {
			await goToLoadData();
			expect(
				screen.getByRole("button", { name: /Select Board/i }),
			).toBeInTheDocument();
		});

		it("shows a Configure Manually button to skip", async () => {
			await goToLoadData();
			expect(
				screen.getByRole("button", { name: /Configure Manually/i }),
			).toBeInTheDocument();
		});

		it("Configure Manually advances to Configure step", async () => {
			const { user } = await goToLoadData();
			await user.click(
				screen.getByRole("button", { name: /Configure Manually/i }),
			);
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});
		});

		it("clicking a wizard opens the wizard dialog", async () => {
			const { user } = await goToLoadData();
			await user.click(screen.getByRole("button", { name: /Select Board/i }));
			await waitFor(() => {
				expect(screen.getByTestId("mock-wizard-dialog")).toBeInTheDocument();
			});
		});

		it("wizard complete + validation pass → jumps to Name & Create", async () => {
			const { user, validateTeamSettings } = await goToLoadData();
			await user.click(screen.getByRole("button", { name: /Select Board/i }));
			await waitFor(() => {
				expect(screen.getByTestId("mock-wizard-dialog")).toBeInTheDocument();
			});
			await user.click(screen.getByText("Complete Wizard"));
			// Should auto-validate and jump to Name & Create
			await waitFor(() => {
				expect(validateTeamSettings).toHaveBeenCalledTimes(1);
			});
			await waitFor(() => {
				expect(screen.getByLabelText("Team Name")).toBeInTheDocument();
			});
		});

		it("wizard complete + validation fail → lands on Configure step with data", async () => {
			const user = userEvent.setup();
			const wizard = createMockWizard("Select Board", fullBoardInfo);
			mockGetWizardsForSystem.mockReturnValue([wizard]);
			const validateTeamSettings = vi.fn().mockResolvedValue(false);
			renderWizard({ validateTeamSettings });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Select Board/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Select Board/i }));
			await waitFor(() => {
				expect(screen.getByTestId("mock-wizard-dialog")).toBeInTheDocument();
			});
			await user.click(screen.getByText("Complete Wizard"));
			// Should land on Configure step with pre-filled data
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});
			// Work item types from wizard should be present
			expect(screen.getByText("User Story")).toBeInTheDocument();
			expect(screen.getByText("Bug")).toBeInTheDocument();
		});

		it("wizard cancel returns to Load Data step", async () => {
			const { user } = await goToLoadData();
			await user.click(screen.getByRole("button", { name: /Select Board/i }));
			await waitFor(() => {
				expect(screen.getByTestId("mock-wizard-dialog")).toBeInTheDocument();
			});
			await user.click(screen.getByText("Cancel Wizard"));
			await waitFor(() => {
				expect(
					screen.queryByTestId("mock-wizard-dialog"),
				).not.toBeInTheDocument();
			});
			// Still on Load Data step
			expect(
				screen.getByRole("button", { name: /Select Board/i }),
			).toBeInTheDocument();
		});

		it("shows Back button that returns to Choose Connection", async () => {
			const { user } = await goToLoadData();
			await user.click(screen.getByRole("button", { name: /Back/i }));
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
		});
	});

	describe("Step 3: Configure (manual)", () => {
		const goToConfigure = async () => {
			const user = userEvent.setup();
			const wizard = createMockWizard("Select Board", fullBoardInfo);
			mockGetWizardsForSystem.mockReturnValue([wizard]);
			const validateTeamSettings = vi.fn().mockResolvedValue(true);
			const saveTeamSettings = vi.fn().mockResolvedValue(undefined);
			renderWizard({ validateTeamSettings, saveTeamSettings });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Configure Manually/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /Configure Manually/i }),
			);
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});
			return { user, validateTeamSettings, saveTeamSettings };
		};

		it("renders data retrieval field, work item types, and states", async () => {
			await goToConfigure();
			expect(screen.getByLabelText("WIQL Query")).toBeInTheDocument();
			expect(screen.getByText("WorkItemTypesComponent")).toBeInTheDocument();
			expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
		});

		it("also renders wizard buttons for re-loading data", async () => {
			await goToConfigure();
			expect(
				screen.getByRole("button", { name: /Select Board/i }),
			).toBeInTheDocument();
		});

		it("disables Next when required fields are empty", async () => {
			await goToConfigure();
			const nextButton = screen.getByRole("button", { name: /Next/i });
			expect(nextButton).toBeDisabled();
		});

		it("validates on Next and advances to Name & Create on success", async () => {
			const { user, validateTeamSettings } = await goToConfigure();

			const dataRetrievalInput = screen.getByLabelText("WIQL Query");
			await user.type(dataRetrievalInput, String.raw`MyProject\MyBoard`);
			fireEvent.click(screen.getByText("Add ToDo State"));
			fireEvent.click(screen.getByText("Add Doing State"));
			fireEvent.click(screen.getByText("Add Done State"));
			fireEvent.click(screen.getByText("Add Work Item Type"));

			const nextButton = screen.getByRole("button", { name: /Next/i });
			await waitFor(() => {
				expect(nextButton).toBeEnabled();
			});
			await user.click(nextButton);

			await waitFor(() => {
				expect(validateTeamSettings).toHaveBeenCalledTimes(1);
			});
			await waitFor(() => {
				expect(screen.getByLabelText("Team Name")).toBeInTheDocument();
			});
		});

		it("shows validation error and stays on Configure when validation fails", async () => {
			const user = userEvent.setup();
			mockGetWizardsForSystem.mockReturnValue([]);
			const validateTeamSettings = vi.fn().mockResolvedValue(false);
			renderWizard({ validateTeamSettings });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});

			const dataRetrievalInput = screen.getByLabelText("WIQL Query");
			await user.type(dataRetrievalInput, String.raw`MyProject\MyBoard`);
			fireEvent.click(screen.getByText("Add ToDo State"));
			fireEvent.click(screen.getByText("Add Doing State"));
			fireEvent.click(screen.getByText("Add Done State"));
			fireEvent.click(screen.getByText("Add Work Item Type"));

			const nextButton = screen.getByRole("button", { name: /Next/i });
			await waitFor(() => {
				expect(nextButton).toBeEnabled();
			});
			await user.click(nextButton);

			await waitFor(() => {
				expect(screen.getByText(/validation failed/i)).toBeInTheDocument();
			});
			expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
		});

		it("shows validation details when validation throws ApiError", async () => {
			const user = userEvent.setup();
			mockGetWizardsForSystem.mockReturnValue([]);
			const validateTeamSettings = vi
				.fn()
				.mockRejectedValue(
					new ApiError(400, "No work items were found.", "Check your query."),
				);
			renderWizard({ validateTeamSettings });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});

			const dataRetrievalInput = screen.getByLabelText("WIQL Query");
			await user.type(dataRetrievalInput, String.raw`MyProject\MyBoard`);
			fireEvent.click(screen.getByText("Add ToDo State"));
			fireEvent.click(screen.getByText("Add Doing State"));
			fireEvent.click(screen.getByText("Add Done State"));
			fireEvent.click(screen.getByText("Add Work Item Type"));

			await user.click(screen.getByRole("button", { name: /Next/i }));

			await waitFor(() => {
				expect(
					screen.getByText("No work items were found."),
				).toBeInTheDocument();
				expect(screen.getByText("Check your query.")).toBeInTheDocument();
			});
		});

		it("does not show WorkItemTypes for Linear (isWorkItemTypesRequired=false)", async () => {
			const user = userEvent.setup();
			mockGetWizardsForSystem.mockReturnValue([]);
			renderWizard();
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My Linear Connection/i }),
				).toBeInTheDocument();
			});
			await user.click(
				screen.getByRole("button", { name: /My Linear Connection/i }),
			);
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});
			expect(
				screen.queryByText("WorkItemTypesComponent"),
			).not.toBeInTheDocument();
		});
	});

	describe("Step 4: Name & Create", () => {
		const goToNameCreate = async () => {
			const user = userEvent.setup();
			const wizard = createMockWizard("Select Board", fullBoardInfo);
			mockGetWizardsForSystem.mockReturnValue([wizard]);
			const validateTeamSettings = vi.fn().mockResolvedValue(true);
			const saveTeamSettings = vi.fn().mockResolvedValue(undefined);
			renderWizard({ validateTeamSettings, saveTeamSettings });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			// Step 1: select connection
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			// Step 2: use wizard
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /Select Board/i }),
				).toBeInTheDocument();
			});
			await user.click(screen.getByRole("button", { name: /Select Board/i }));
			await waitFor(() => {
				expect(screen.getByTestId("mock-wizard-dialog")).toBeInTheDocument();
			});
			await user.click(screen.getByText("Complete Wizard"));
			// Should auto-validate and jump to Name & Create
			await waitFor(() => {
				expect(screen.getByLabelText("Team Name")).toBeInTheDocument();
			});
			return { user, validateTeamSettings, saveTeamSettings };
		};

		it("renders name input with default name", async () => {
			await goToNameCreate();
			const nameInput = screen.getByLabelText<HTMLInputElement>("Team Name");
			expect(nameInput.value).toBe("New Team");
		});

		it("disables Create button when name is empty", async () => {
			const { user } = await goToNameCreate();
			const nameInput = screen.getByLabelText("Team Name");
			await user.clear(nameInput);
			const createButton = screen.getByRole("button", { name: /Create/i });
			expect(createButton).toBeDisabled();
		});

		it("enables Create button when name is non-empty", async () => {
			await goToNameCreate();
			const createButton = screen.getByRole("button", { name: /Create/i });
			expect(createButton).toBeEnabled();
		});

		it("calls saveTeamSettings with assembled DTO on Create click", async () => {
			const { user, saveTeamSettings } = await goToNameCreate();
			await user.click(screen.getByRole("button", { name: /Create/i }));
			await waitFor(() => {
				expect(saveTeamSettings).toHaveBeenCalledTimes(1);
			});
			const savedSettings = saveTeamSettings.mock.calls[0][0];
			expect(savedSettings.name).toBe("New Team");
			expect(savedSettings.workTrackingSystemConnectionId).toBe(1);
			expect(savedSettings.throughputHistory).toBe(90);
			expect(savedSettings.featureWIP).toBe(0);
		});

		it("shows Back button that returns to Configure step", async () => {
			const { user } = await goToNameCreate();
			await user.click(screen.getByRole("button", { name: /Back/i }));
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});
		});
	});

	describe("Cancel", () => {
		it("calls onCancel when Cancel button is clicked", async () => {
			const onCancel = vi.fn();
			renderWizard({ onCancel });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			await userEvent.click(screen.getByRole("button", { name: /Cancel/i }));
			expect(onCancel).toHaveBeenCalledTimes(1);
		});
	});

	describe("No standalone Validate button", () => {
		it("does not render a standalone Validate button at any step", async () => {
			const user = userEvent.setup();
			mockGetWizardsForSystem.mockReturnValue([]);
			const validateTeamSettings = vi.fn().mockResolvedValue(true);
			renderWizard({ validateTeamSettings });
			await waitFor(() => {
				expect(
					screen.getByRole("button", { name: /My ADO Connection/i }),
				).toBeInTheDocument();
			});
			// Step 1
			expect(screen.queryByText("Validate")).not.toBeInTheDocument();

			// Step 2 (no wizards → goes to Configure)
			await user.click(
				screen.getByRole("button", { name: /My ADO Connection/i }),
			);
			await waitFor(() => {
				expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
			});
			expect(screen.queryByText("Validate")).not.toBeInTheDocument();

			// Fill and go to Name & Create
			const dataRetrievalInput = screen.getByLabelText("WIQL Query");
			await user.type(dataRetrievalInput, String.raw`MyProject\MyBoard`);
			fireEvent.click(screen.getByText("Add ToDo State"));
			fireEvent.click(screen.getByText("Add Doing State"));
			fireEvent.click(screen.getByText("Add Done State"));
			fireEvent.click(screen.getByText("Add Work Item Type"));

			const nextButton = screen.getByRole("button", { name: /Next/i });
			await waitFor(() => {
				expect(nextButton).toBeEnabled();
			});
			await user.click(nextButton);
			await waitFor(() => {
				expect(screen.getByLabelText("Team Name")).toBeInTheDocument();
			});
			expect(screen.queryByText("Validate")).not.toBeInTheDocument();
		});
	});
});
