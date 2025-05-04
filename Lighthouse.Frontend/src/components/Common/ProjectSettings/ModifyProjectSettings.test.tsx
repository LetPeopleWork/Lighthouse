import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IProjectSettings } from "../../../models/Project/ProjectSettings";
import type { ITeam } from "../../../models/Team/Team";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ModifyProjectSettings from "./ModifyProjectSettings";

vi.mock("../../../pages/Projects/Edit/AdvancedInputs", () => ({
	__esModule: true,
	default: ({
		onProjectSettingsChange,
	}: {
		onProjectSettingsChange: (
			key: keyof IProjectSettings,
			value: string,
		) => void;
	}) => (
		<div>
			<div>AdvancedInputsComponent</div>
			<button
				type="button"
				onClick={() => onProjectSettingsChange("name", "value")}
			>
				Change Advanced
			</button>
		</div>
	),
}));

vi.mock("../../../pages/Projects/Edit/GeneralInputs", () => ({
	__esModule: true,
	default: ({
		onProjectSettingsChange,
	}: {
		onProjectSettingsChange: (
			key: keyof IProjectSettings,
			value: string,
		) => void;
	}) => (
		<div>
			<div>GeneralInputsComponent</div>
			<button
				type="button"
				onClick={() => onProjectSettingsChange("name", "value")}
			>
				Change General
			</button>
		</div>
	),
}));

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
						workTrackingSystem: "",
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

describe("ModifyProjectSettings", () => {
	const mockGetWorkTrackingSystems = vi.fn();
	const mockGetProjectSettings = vi.fn();
	const mockGetAllTeams = vi.fn();
	const mockSaveProjectSettings = vi.fn();
	const mockValidateProjectSettings = vi.fn();

	const workTrackingSystems: IWorkTrackingSystemConnection[] = [
		{ id: 1, name: "System 1", options: [], workTrackingSystem: "" },
		{ id: 2, name: "System 2", options: [], workTrackingSystem: "" },
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
			remainingWork: 1,
			totalWork: 12,
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			),
			throughputEndDate: new Date(),
			tags: [],
		},
		{
			id: 2,
			name: "Team 2",
			features: [],
			featureWip: 1,
			lastUpdated: new Date(),
			projects: [],
			remainingFeatures: 1,
			remainingWork: 1,
			totalWork: 12,
			useFixedDatesForThroughput: false,
			throughputStartDate: new Date(
				new Date().setDate(new Date().getDate() - [1].length),
			),
			throughputEndDate: new Date(),
			tags: [],
		},
	];

	const projectSettings: IProjectSettings = {
		name: "Project 1",
		defaultAmountOfWorkItemsPerFeature: 10,
		workItemTypes: ["Bug"],
		workItemQuery: "Query",
		workTrackingSystemConnectionId: 1,
		toDoStates: ["ToDo"],
		doingStates: ["Doing"],
		doneStates: ["Done"],
		tags: [],
		milestones: [],
		involvedTeams: teams,
		id: 1,
		owningTeam: undefined,
		usePercentileToCalculateDefaultAmountOfWorkItems: false,
		defaultWorkItemPercentile: 80,
		historicalFeaturesWorkItemQuery: "",
		unparentedItemsQuery: "",
		overrideRealChildCountStates: [],
		featureOwnerField: "",
		sizeEstimateField: "",
	};

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

		expect(screen.getByText("Loading...")).toBeInTheDocument();
	});

	it("renders components correctly after loading", async () => {
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

		expect(screen.getByText("Modify Project Settings")).toBeInTheDocument();
		expect(screen.getByText("GeneralInputsComponent")).toBeInTheDocument();
		expect(screen.getByText("WorkItemTypesComponent")).toBeInTheDocument();
		expect(screen.getByText("StatesList")).toBeInTheDocument();
		expect(screen.getByText("WorkTrackingSystemComponent")).toBeInTheDocument();
		expect(screen.getByText("AdvancedInputsComponent")).toBeInTheDocument();
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
		fireEvent.click(screen.getByText("Change Advanced"));

		expect(screen.getByText("GeneralInputsComponent")).toBeInTheDocument();
		expect(screen.getByText("AdvancedInputsComponent")).toBeInTheDocument();
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

		expect(screen.getByText("WorkTrackingSystemComponent")).toBeInTheDocument();
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
		{ invalidValue: "Work Item Query", workItemQuery: "" },
		{ invalidValue: "Involved Teams", involvedTeams: [] },
		{
			invalidValue: "Missing Historical Features Work Item Query",
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
			defaultWorkItemPercentile: 80,
			historicalFeaturesWorkItemQuery: "",
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
