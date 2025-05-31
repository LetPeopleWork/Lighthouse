import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ModifyTeamSettings from "./ModifyTeamSettings";

vi.mock("../../../pages/Teams/Edit/AdvancedInputs", () => ({
	__esModule: true,
	default: ({
		onTeamSettingsChange,
	}: {
		onTeamSettingsChange: (key: keyof ITeamSettings, value: string) => void;
	}) => (
		<div>
			<div>AdvancedInputsComponent</div>
			<button
				type="button"
				onClick={() => onTeamSettingsChange("name", "value")}
			>
				Change Advanced
			</button>
		</div>
	),
}));

vi.mock(
	"../../../components/Common/BaseSettings/GeneralSettingsComponent.tsx",
	() => ({
		__esModule: true,
		default: ({
			onSettingsChange,
		}: {
			onSettingsChange: (key: keyof ITeamSettings, value: string) => void;
		}) => (
			<div>
				<div>GeneralInputsComponent</div>
				<button type="button" onClick={() => onSettingsChange("name", "value")}>
					Change General
				</button>
			</div>
		),
	}),
);

vi.mock("../LoadingAnimation/LoadingAnimation", () => ({
	__esModule: true,
	default: ({
		isLoading,
		children,
	}: { isLoading: boolean; children: React.ReactNode }) => (
		<div>{isLoading ? "Loading..." : children}</div>
	),
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

vi.mock("../WorkTrackingSystems/WorkTrackingSystemComponent", () => ({
	__esModule: true,
	default: ({
		onWorkTrackingSystemChange,
		onNewWorkTrackingSystemConnectionAdded,
	}: {
		onWorkTrackingSystemChange: (
			event: React.ChangeEvent<{ value: unknown }>,
		) => void;
		onNewWorkTrackingSystemConnectionAdded: (
			connection: IWorkTrackingSystemConnection,
		) => void;
	}) => (
		<div>
			<div>WorkTrackingSystemComponent</div>
			<button
				type="button"
				onClick={() =>
					onWorkTrackingSystemChange({
						target: { value: "System 1" },
					} as React.ChangeEvent<{ value: unknown }>)
				}
			>
				Change Work Tracking System
			</button>
			<button
				type="button"
				onClick={() =>
					onNewWorkTrackingSystemConnectionAdded({
						id: 1,
						name: "New System",
						options: [],
						workTrackingSystem: "Linear",
					})
				}
			>
				Add New Work Tracking System
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

describe("ModifyTeamSettings", () => {
	const mockGetWorkTrackingSystems = vi.fn();
	const mockGetTeamSettings = vi.fn();
	const mockSaveTeamSettings = vi.fn();
	const mockValidateTeamSettings = vi.fn();

	const teamSettings: ITeamSettings = {
		name: "Team 1",
		throughputHistory: 10,
		useFixedDatesForThroughput: false,
		throughputHistoryStartDate: new Date(),
		throughputHistoryEndDate: new Date(),
		featureWIP: 5,
		toDoStates: ["ToDo"],
		doingStates: ["Doing"],
		doneStates: ["Done"],
		workItemTypes: ["Bug"],
		tags: [],
		workItemQuery: "Query",
		workTrackingSystemConnectionId: 1,
		automaticallyAdjustFeatureWIP: false,
		id: 12,
		relationCustomField: "",
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
	};

	const workTrackingSystems: IWorkTrackingSystemConnection[] = [
		{ id: 1, name: "System 1", options: [], workTrackingSystem: "Jira" },
		{ id: 2, name: "System 2", options: [], workTrackingSystem: "AzureDevOps" },
	];

	const renderModifyTeamSettings = async () => {
		render(
			<ModifyTeamSettings
				title="Modify Team Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getTeamSettings={mockGetTeamSettings}
				saveTeamSettings={mockSaveTeamSettings}
				validateTeamSettings={mockValidateTeamSettings}
			/>,
		);

		// Wait for all async operations to complete
		await waitFor(
			() => {
				expect(screen.queryByText("Loading...")).not.toBeInTheDocument();
				expect(mockGetTeamSettings).toHaveBeenCalled();
				expect(mockGetWorkTrackingSystems).toHaveBeenCalled();
			},
			{ timeout: 3000 },
		);

		// Add a small delay to ensure state updates are processed
		await new Promise((resolve) => setTimeout(resolve, 0));
	};

	beforeEach(() => {
		mockGetWorkTrackingSystems.mockClear();
		mockGetTeamSettings.mockClear();
		mockSaveTeamSettings.mockClear();
		mockValidateTeamSettings.mockClear();

		mockGetWorkTrackingSystems.mockResolvedValue(workTrackingSystems);
		mockGetTeamSettings.mockResolvedValue(teamSettings);
	});

	it("renders loading state initially", () => {
		render(
			<ModifyTeamSettings
				title="Modify Team Settings"
				getWorkTrackingSystems={mockGetWorkTrackingSystems}
				getTeamSettings={mockGetTeamSettings}
				saveTeamSettings={mockSaveTeamSettings}
				validateTeamSettings={mockValidateTeamSettings}
			/>,
		);

		expect(screen.getByText("Loading...")).toBeInTheDocument();
	});

	it("renders components correctly after loading", async () => {
		await renderModifyTeamSettings();

		expect(screen.getByText("Modify Team Settings")).toBeInTheDocument();
		expect(screen.getByText("GeneralInputsComponent")).toBeInTheDocument();
		expect(screen.getByText("WorkItemTypesComponent")).toBeInTheDocument();
		expect(screen.getByText("StatesList")).toBeInTheDocument();
		expect(screen.getByText("WorkTrackingSystemComponent")).toBeInTheDocument();
		expect(screen.getByText("AdvancedInputsComponent")).toBeInTheDocument();
	});

	it("handles team settings change", async () => {
		await renderModifyTeamSettings();

		fireEvent.click(screen.getByText("Change General"));
		fireEvent.click(screen.getByText("Change Advanced"));

		expect(screen.getByText("GeneralInputsComponent")).toBeInTheDocument();
		expect(screen.getByText("AdvancedInputsComponent")).toBeInTheDocument();
	});

	it("handles adding and removing work item types", async () => {
		await renderModifyTeamSettings();

		fireEvent.click(screen.getByText("Add Work Item"));
		fireEvent.click(screen.getByText("Remove Work Item"));

		expect(screen.getByText("WorkItemTypesComponent")).toBeInTheDocument();
	});

	it("handles adding and removing states", async () => {
		await renderModifyTeamSettings();

		fireEvent.click(screen.getByText("Add ToDo"));
		fireEvent.click(screen.getByText("Remove ToDo"));
		fireEvent.click(screen.getByText("Add Doing"));
		fireEvent.click(screen.getByText("Remove Doing"));
		fireEvent.click(screen.getByText("Add Done"));
		fireEvent.click(screen.getByText("Remove Done"));

		expect(screen.getByText("StatesList")).toBeInTheDocument();
	});

	it("handles work tracking system change", async () => {
		await renderModifyTeamSettings();

		fireEvent.click(screen.getByText("Change Work Tracking System"));
		fireEvent.click(screen.getByText("Add New Work Tracking System"));

		expect(screen.getByText("WorkTrackingSystemComponent")).toBeInTheDocument();
	});

	it("handles adding and removing tags", async () => {
		await renderModifyTeamSettings();

		// Test adding a tag
		fireEvent.click(screen.getByText("Add Tag"));
		// Test removing a tag
		fireEvent.click(screen.getByText("Remove Tag"));

		expect(screen.getByText("TagsComponent")).toBeInTheDocument();
	});

	it("handles save action", async () => {
		await renderModifyTeamSettings();
		mockValidateTeamSettings.mockResolvedValue(true);

		fireEvent.click(screen.getByText("Validate"));
		await waitFor(() => expect(mockValidateTeamSettings).toHaveBeenCalled());

		fireEvent.click(screen.getByText("Save"));

		await waitFor(() => expect(mockSaveTeamSettings).toHaveBeenCalled());
	});

	it("handles validate action", async () => {
		await renderModifyTeamSettings();

		fireEvent.click(screen.getByText("Validate"));

		await waitFor(() => expect(mockValidateTeamSettings).toHaveBeenCalled());
	});

	it("sets inputsValid to true when all inputs are valid", async () => {
		const validTeamSettings: ITeamSettings = {
			id: 1,
			name: "Valid Team",
			throughputHistory: 10,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 5,
			workItemTypes: ["Bug"],
			toDoStates: ["ToDo"],
			doingStates: ["Doing"],
			doneStates: ["Done"],
			tags: [],
			workItemQuery: "Query",
			workTrackingSystemConnectionId: 1,
			automaticallyAdjustFeatureWIP: false,
			relationCustomField: "custom",
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
		};

		mockGetTeamSettings.mockResolvedValueOnce(validTeamSettings);
		mockValidateTeamSettings.mockResolvedValueOnce(true);

		await renderModifyTeamSettings();

		await waitFor(() => {
			const validateButton = screen.getByText("Validate");
			expect(validateButton).not.toBeDisabled();
		});
	});

	const scenarios = [
		{ invalidValue: "Name", name: "" },
		{ invalidValue: "Throughput History", throughputHistory: 0 },
		{ invalidValue: "Feature WIP", featureWIP: undefined },
		{ invalidValue: "To Do States", toDoStates: [] },
		{ invalidValue: "Doing States", doingStates: [], doneStates: ["Done"] },
		{ invalidValue: "Done States", doneStates: [] },
		{ invalidValue: "Work Item Types", workItemTypes: [] },
	];

	for (const scenario of scenarios) {
		it(`sets inputValid to false when ${scenario.invalidValue} is invalid`, async () => {
			const invalidTeamSettings = {
				...teamSettings,
				...scenario,
			};

			mockGetTeamSettings.mockResolvedValueOnce(invalidTeamSettings);

			await renderModifyTeamSettings();

			expect(screen.getByText("Validate")).toBeDisabled();
		});
	}
});
