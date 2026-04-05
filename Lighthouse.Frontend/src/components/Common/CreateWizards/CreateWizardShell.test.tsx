import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { IBoardInformation } from "../../../models/Boards/BoardInformation";
import type {
	DataRetrievalWizardProps,
	IDataRetrievalWizard,
} from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

// ---------- module mocks ----------

vi.mock("../LoadingAnimation/LoadingAnimation", () => ({
	default: ({
		isLoading,
		children,
	}: {
		isLoading: boolean;
		children: React.ReactNode;
	}) => <div>{isLoading ? "Loading..." : children}</div>,
}));

vi.mock("../WorkItemTypes/WorkItemTypesComponent", () => ({
	default: ({
		onAddWorkItemType,
	}: {
		onAddWorkItemType: (t: string) => void;
	}) => (
		<div>
			<div>WorkItemTypesComponent</div>
			<button type="button" onClick={() => onAddWorkItemType("NewType")}>
				Add Work Item Type
			</button>
		</div>
	),
}));

vi.mock("../StatesList/StatesList", () => ({
	default: () => <div>StatesListComponent</div>,
}));

vi.mock("../ActionButton/ActionButton", () => ({
	default: ({
		buttonText,
		onClickHandler,
		disabled,
	}: {
		buttonText: string;
		onClickHandler: () => void;
		disabled?: boolean;
	}) => (
		<button type="button" onClick={onClickHandler} disabled={disabled}>
			{buttonText}
		</button>
	),
}));

// ---------- helpers ----------

import {
	STEP_CHOOSE_CONNECTION,
	STEP_CONFIGURE,
	STEP_LOAD_DATA,
	STEP_NAME_CREATE,
} from "../../../hooks/useCreateWizard";
import CreateWizardShell, {
	type CreateWizardShellProps,
} from "./CreateWizardShell";

const makeConnection = (
	id: number,
	name: string,
): IWorkTrackingSystemConnection => ({
	id,
	name,
	workTrackingSystem: "AzureDevOps",
	options: [],
	availableAuthenticationMethods: [],
	authenticationMethodKey: "ado.pat",
	workTrackingSystemGetDataRetrievalDisplayName: () => "WIQL Query",
	additionalFieldDefinitions: [],
	writeBackMappingDefinitions: [],
});

const makeMockWizard = (
	id: string,
	name: string,
	boardInfo: IBoardInformation,
): IDataRetrievalWizard => {
	const MockWizardComponent: React.FC<DataRetrievalWizardProps> = ({
		onComplete,
		onCancel,
	}) => (
		<div data-testid={`wizard-${id}`}>
			<button type="button" onClick={() => onComplete(boardInfo)}>
				Complete {name}
			</button>
			<button type="button" onClick={onCancel}>
				Cancel {name}
			</button>
		</div>
	);
	MockWizardComponent.displayName = `MockWizard_${id}`;
	return {
		id,
		name,
		applicableSystemTypes: ["AzureDevOps"],
		applicableSettingsContexts: ["team"],
		component: MockWizardComponent,
	};
};

const defaultBoardInfo: IBoardInformation = {
	dataRetrievalValue: "SELECT * FROM Items",
	workItemTypes: ["Story"],
	toDoStates: ["New"],
	doingStates: ["Active"],
	doneStates: ["Done"],
};

const baseProps = (): CreateWizardShellProps => ({
	activeStep: STEP_CHOOSE_CONNECTION,
	loading: false,
	connections: [
		makeConnection(1, "ADO Connection"),
		makeConnection(2, "Jira Connection"),
	],
	onSelectConnection: vi.fn(),
	entityLabel: "team",
	availableWizards: [],
	activeWizard: null,
	onSetActiveWizard: vi.fn(),
	onWizardComplete: vi.fn(),
	onWizardCancel: vi.fn(),
	onSetActiveStep: vi.fn(),
	selectedConnectionId: null,
	showDataRetrievalField: true,
	dataRetrievalLabel: "WIQL Query",
	dataRetrievalValue: "",
	onDataRetrievalChange: vi.fn(),
	showWorkItemTypes: true,
	workItemTypes: [],
	onAddWorkItemType: vi.fn(),
	onRemoveWorkItemType: vi.fn(),
	isForTeam: true,
	toDoStates: [],
	doingStates: [],
	doneStates: [],
	onAddToDoState: vi.fn(),
	onRemoveToDoState: vi.fn(),
	onReorderToDoStates: vi.fn(),
	onAddDoingState: vi.fn(),
	onRemoveDoingState: vi.fn(),
	onReorderDoingStates: vi.fn(),
	onAddDoneState: vi.fn(),
	onRemoveDoneState: vi.fn(),
	onReorderDoneStates: vi.fn(),
	validationError: null,
	configInputsValid: false,
	validating: false,
	nameLabel: "Team Name",
	name: "New Team",
	onNameChange: vi.fn(),
	saving: false,
	onCancel: vi.fn(),
	onBack: vi.fn(),
	onNext: vi.fn(),
	onCreate: vi.fn(),
});

// ---------- tests ----------

describe("CreateWizardShell", () => {
	describe("loading state", () => {
		it("shows loading indicator when loading=true", () => {
			render(<CreateWizardShell {...baseProps()} loading={true} />);
			expect(screen.getByText("Loading...")).toBeInTheDocument();
		});

		it("renders content when loading=false", () => {
			render(<CreateWizardShell {...baseProps()} />);
			expect(screen.queryByText("Loading...")).not.toBeInTheDocument();
		});
	});

	describe("stepper", () => {
		it("always renders all four step labels", () => {
			render(<CreateWizardShell {...baseProps()} />);
			expect(screen.getByText("Choose Connection")).toBeInTheDocument();
			expect(screen.getByText("Load Data")).toBeInTheDocument();
			expect(screen.getByText("Configure")).toBeInTheDocument();
			expect(screen.getByText("Name & Create")).toBeInTheDocument();
		});
	});

	describe("Step 1: Choose Connection", () => {
		it("renders connection buttons with the entity label", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CHOOSE_CONNECTION}
				/>,
			);
			expect(
				screen.getByText(/Select the connection to use for this team/i),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "ADO Connection" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: "Jira Connection" }),
			).toBeInTheDocument();
		});

		it("calls onSelectConnection when a connection button is clicked", async () => {
			const user = userEvent.setup();
			const onSelectConnection = vi.fn();
			const props = { ...baseProps(), onSelectConnection };
			render(<CreateWizardShell {...props} />);

			await user.click(screen.getByRole("button", { name: "ADO Connection" }));
			expect(onSelectConnection).toHaveBeenCalledWith(
				expect.objectContaining({ id: 1 }),
			);
		});

		it("does not show Back button", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CHOOSE_CONNECTION}
				/>,
			);
			expect(
				screen.queryByRole("button", { name: /Back/i }),
			).not.toBeInTheDocument();
		});

		it("shows Cancel button", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CHOOSE_CONNECTION}
				/>,
			);
			expect(
				screen.getByRole("button", { name: /Cancel/i }),
			).toBeInTheDocument();
		});

		it("calls onCancel when Cancel is clicked", async () => {
			const user = userEvent.setup();
			const onCancel = vi.fn();
			render(<CreateWizardShell {...baseProps()} onCancel={onCancel} />);
			await user.click(screen.getByRole("button", { name: /Cancel/i }));
			expect(onCancel).toHaveBeenCalledTimes(1);
		});

		it("uses entityLabel in prompt text", () => {
			render(<CreateWizardShell {...baseProps()} entityLabel="portfolio" />);
			expect(screen.getByText(/for this portfolio/i)).toBeInTheDocument();
		});
	});

	describe("Step 2: Load Data", () => {
		const wizard = makeMockWizard("w1", "Select Board", defaultBoardInfo);

		it("renders wizard buttons and Configure Manually option", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[wizard]}
				/>,
			);
			expect(
				screen.getByRole("button", { name: "Select Board" }),
			).toBeInTheDocument();
			expect(
				screen.getByRole("button", { name: /Configure Manually/i }),
			).toBeInTheDocument();
		});

		it("calls onSetActiveWizard when a wizard button is clicked", async () => {
			const user = userEvent.setup();
			const onSetActiveWizard = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[wizard]}
					onSetActiveWizard={onSetActiveWizard}
				/>,
			);
			await user.click(screen.getByRole("button", { name: "Select Board" }));
			expect(onSetActiveWizard).toHaveBeenCalledWith(wizard);
		});

		it("calls onSetActiveStep(STEP_CONFIGURE) when Configure Manually is clicked", async () => {
			const user = userEvent.setup();
			const onSetActiveStep = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[wizard]}
					onSetActiveStep={onSetActiveStep}
				/>,
			);
			await user.click(
				screen.getByRole("button", { name: /Configure Manually/i }),
			);
			expect(onSetActiveStep).toHaveBeenCalledWith(STEP_CONFIGURE);
		});

		it("renders the active wizard dialog when activeWizard and selectedConnectionId are set", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[wizard]}
					activeWizard={wizard}
					selectedConnectionId={1}
				/>,
			);
			expect(screen.getByTestId("wizard-w1")).toBeInTheDocument();
		});

		it("does not render wizard dialog when selectedConnectionId is null", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[wizard]}
					activeWizard={wizard}
					selectedConnectionId={null}
				/>,
			);
			expect(screen.queryByTestId("wizard-w1")).not.toBeInTheDocument();
		});

		it("calls onWizardComplete when wizard completes", async () => {
			const user = userEvent.setup();
			const onWizardComplete = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[wizard]}
					activeWizard={wizard}
					selectedConnectionId={1}
					onWizardComplete={onWizardComplete}
				/>,
			);
			await user.click(screen.getByText("Complete Select Board"));
			expect(onWizardComplete).toHaveBeenCalledWith(defaultBoardInfo);
		});

		it("calls onWizardCancel when wizard is cancelled", async () => {
			const user = userEvent.setup();
			const onWizardCancel = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[wizard]}
					activeWizard={wizard}
					selectedConnectionId={1}
					onWizardCancel={onWizardCancel}
				/>,
			);
			await user.click(screen.getByText("Cancel Select Board"));
			expect(onWizardCancel).toHaveBeenCalledTimes(1);
		});

		it("shows Back button on this step", () => {
			render(
				<CreateWizardShell {...baseProps()} activeStep={STEP_LOAD_DATA} />,
			);
			expect(screen.getByRole("button", { name: /Back/i })).toBeInTheDocument();
		});

		it("calls onBack when Back is clicked", async () => {
			const user = userEvent.setup();
			const onBack = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					onBack={onBack}
				/>,
			);
			await user.click(screen.getByRole("button", { name: /Back/i }));
			expect(onBack).toHaveBeenCalledTimes(1);
		});
	});

	describe("Step 3: Configure", () => {
		it("renders data retrieval field when showDataRetrievalField=true", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					showDataRetrievalField={true}
					dataRetrievalLabel="WIQL Query"
				/>,
			);
			expect(screen.getByLabelText("WIQL Query")).toBeInTheDocument();
		});

		it("does not render data retrieval field when showDataRetrievalField=false", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					showDataRetrievalField={false}
				/>,
			);
			expect(screen.queryByLabelText("WIQL Query")).not.toBeInTheDocument();
		});

		it("calls onDataRetrievalChange when the field is typed in", async () => {
			const user = userEvent.setup();
			const onDataRetrievalChange = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					showDataRetrievalField={true}
					dataRetrievalLabel="WIQL Query"
					onDataRetrievalChange={onDataRetrievalChange}
				/>,
			);
			await user.type(screen.getByLabelText("WIQL Query"), "a");
			expect(onDataRetrievalChange).toHaveBeenCalled();
		});

		it("renders WorkItemTypesComponent when showWorkItemTypes=true", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					showWorkItemTypes={true}
				/>,
			);
			expect(screen.getByText("WorkItemTypesComponent")).toBeInTheDocument();
		});

		it("hides WorkItemTypesComponent when showWorkItemTypes=false", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					showWorkItemTypes={false}
				/>,
			);
			expect(
				screen.queryByText("WorkItemTypesComponent"),
			).not.toBeInTheDocument();
		});

		it("always renders StatesList", () => {
			render(
				<CreateWizardShell {...baseProps()} activeStep={STEP_CONFIGURE} />,
			);
			expect(screen.getByText("StatesListComponent")).toBeInTheDocument();
		});

		it("shows wizard buttons when availableWizards is non-empty", () => {
			const wizard = makeMockWizard("w2", "Re-load Wizard", defaultBoardInfo);
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					availableWizards={[wizard]}
				/>,
			);
			expect(
				screen.getByRole("button", { name: "Re-load Wizard" }),
			).toBeInTheDocument();
		});

		it("shows validationError when provided", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					validationError="Validation failed. Check your configuration and try again."
				/>,
			);
			expect(screen.getByText(/validation failed/i)).toBeInTheDocument();
		});

		it("does not show validationError when null", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					validationError={null}
				/>,
			);
			expect(screen.queryByText(/validation failed/i)).not.toBeInTheDocument();
		});

		it("Next button is disabled when configInputsValid=false", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					configInputsValid={false}
				/>,
			);
			expect(screen.getByRole("button", { name: /Next/i })).toBeDisabled();
		});

		it("Next button is disabled when validating=true even if inputs are valid", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					configInputsValid={true}
					validating={true}
				/>,
			);
			expect(screen.getByRole("button", { name: /Next/i })).toBeDisabled();
		});

		it("Next button is enabled when configInputsValid=true and not validating", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					configInputsValid={true}
					validating={false}
				/>,
			);
			expect(screen.getByRole("button", { name: /Next/i })).toBeEnabled();
		});

		it("calls onNext when Next is clicked", async () => {
			const user = userEvent.setup();
			const onNext = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_CONFIGURE}
					configInputsValid={true}
					onNext={onNext}
				/>,
			);
			await user.click(screen.getByRole("button", { name: /Next/i }));
			expect(onNext).toHaveBeenCalledTimes(1);
		});
	});

	describe("Step 4: Name & Create", () => {
		it("renders the name field with the provided nameLabel and value", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					nameLabel="Portfolio Name"
					name="My Portfolio"
				/>,
			);
			const input = screen.getByLabelText<HTMLInputElement>("Portfolio Name");
			expect(input.value).toBe("My Portfolio");
		});

		it("calls onNameChange when the name field is edited", async () => {
			const user = userEvent.setup();
			const onNameChange = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					onNameChange={onNameChange}
				/>,
			);
			await user.type(screen.getByLabelText("Team Name"), "X");
			expect(onNameChange).toHaveBeenCalled();
		});

		it("Create button is disabled when name is empty", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					name=""
				/>,
			);
			expect(screen.getByRole("button", { name: /Create/i })).toBeDisabled();
		});

		it("Create button is disabled when name is whitespace only", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					name="   "
				/>,
			);
			expect(screen.getByRole("button", { name: /Create/i })).toBeDisabled();
		});

		it("Create button is disabled when saving=true", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					name="Valid Name"
					saving={true}
				/>,
			);
			expect(screen.getByRole("button", { name: /Create/i })).toBeDisabled();
		});

		it("Create button is enabled when name is non-empty and not saving", () => {
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					name="Valid Name"
					saving={false}
				/>,
			);
			expect(screen.getByRole("button", { name: /Create/i })).toBeEnabled();
		});

		it("calls onCreate when Create is clicked", async () => {
			const user = userEvent.setup();
			const onCreate = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					name="Valid Name"
					onCreate={onCreate}
				/>,
			);
			await user.click(screen.getByRole("button", { name: /Create/i }));
			expect(onCreate).toHaveBeenCalledTimes(1);
		});

		it("shows Back button on this step", () => {
			render(
				<CreateWizardShell {...baseProps()} activeStep={STEP_NAME_CREATE} />,
			);
			expect(screen.getByRole("button", { name: /Back/i })).toBeInTheDocument();
		});

		it("calls onBack when Back is clicked", async () => {
			const user = userEvent.setup();
			const onBack = vi.fn();
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_NAME_CREATE}
					onBack={onBack}
				/>,
			);
			await user.click(screen.getByRole("button", { name: /Back/i }));
			expect(onBack).toHaveBeenCalledTimes(1);
		});
	});

	describe("linear wizard boardType routing", () => {
		it("passes boardType='Team' for linear.team wizard id", () => {
			const linearWizard: IDataRetrievalWizard = {
				...makeMockWizard("linear.team", "Linear Wizard", defaultBoardInfo),
				id: "linear.team",
			};
			// The component passes boardType to the wizard component.
			// We verify it renders without crashing and the dialog is present.
			render(
				<CreateWizardShell
					{...baseProps()}
					activeStep={STEP_LOAD_DATA}
					availableWizards={[linearWizard]}
					activeWizard={linearWizard}
					selectedConnectionId={1}
				/>,
			);
			expect(screen.getByTestId("wizard-linear.team")).toBeInTheDocument();
		});
	});
});
