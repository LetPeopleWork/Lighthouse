import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementSettings } from "../../../models/Refinement/Refinement";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ModifyTeamSettings from "./ModifyTeamSettings";

/**
 * The Refinement section of a Team's settings, exercised through the settings form a Team admin
 * actually uses, so what is checked is what the form's autosave sends. States are picked the way wait
 * states are: the Team's To Do and Doing states are suggested and every chosen state shows as a chip.
 * The section says a Doing state already counts in WIP and cycle time. A chosen state stops being one
 * the moment the form no longer has it as To Do or Doing: its chip goes and the save leaves it out.
 *
 * Sections the refinement choice does not depend on are stood in for, as the form's own tests do. The
 * states list stand-in can only take a Doing state away, which is all these cases need from it.
 */

const { terms } = vi.hoisted(() => ({
	terms: { current: {} as Record<string, string> },
}));

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => terms.current[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

vi.mock("../../../pages/Common/AdvancedInputs/AdvancedInputs.tsx", () => ({
	__esModule: true,
	default: () => <div>AdvancedInputsComponent</div>,
}));

vi.mock(
	"../../../components/Common/BaseSettings/GeneralSettingsComponent.tsx",
	() => ({
		__esModule: true,
		default: () => <div>GeneralInputsComponent</div>,
	}),
);

vi.mock("../WorkItemTypes/WorkItemTypesComponent", () => ({
	__esModule: true,
	default: () => <div>WorkItemTypesComponent</div>,
}));

vi.mock("../StatesList/StatesList", () => ({
	__esModule: true,
	default: ({
		doingStates,
		onRemoveDoingState,
	}: {
		doingStates: string[];
		onRemoveDoingState: (state: string) => void;
	}) => (
		<div>
			{doingStates.map((state) => (
				<button
					key={state}
					type="button"
					onClick={() => onRemoveDoingState(state)}
				>
					{`Stop mapping ${state} as Doing`}
				</button>
			))}
		</div>
	),
}));

vi.mock("../Tags/TagsComponent", () => ({
	__esModule: true,
	default: () => <div>TagsComponent</div>,
}));

// Spelled out: the keys join the shared list together with the words the server seeds for them.
const REFINEMENT_KEY = "refinement";
const REFINEMENTS_KEY = "refinements";

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.WIP]: "WIP",
	[TERMINOLOGY_KEYS.CYCLE_TIME]: "Cycle Time",
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[REFINEMENT_KEY]: "Refinement",
	[REFINEMENTS_KEY]: "Refinements",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
};

const workTrackingSystems: IWorkTrackingSystemConnection[] = [
	{
		id: 1,
		name: "Gravity's Jira",
		options: [],
		workTrackingSystem: "Jira",
		authenticationMethodKey: "jira.cloud",
		additionalFieldDefinitions: [],
		writeBackMappingDefinitions: [],
		workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
	},
];

const gravitysSettings = (
	refinement: IRefinementSettings | null,
	doingStates = ["Next", "Analysing"],
): ITeamSettings => ({
	...createMockTeamSettings(),
	name: "Team Gravity",
	workTrackingSystemConnectionId: 1,
	toDoStates: ["Backlog"],
	doingStates,
	doneStates: ["Done"],
	refinement,
});

const saveTeamSettings = vi.fn();

const renderGravitysSettingsForm = async (settings: ITeamSettings) => {
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				teamService: {
					updateTeamData: vi.fn().mockResolvedValue(undefined),
					getForecastFilterSchema: vi.fn().mockResolvedValue(null),
				} as unknown as ITeamService,
			})}
		>
			<ModifyTeamSettings
				title="Edit Team"
				getWorkTrackingSystems={vi.fn().mockResolvedValue(workTrackingSystems)}
				getTeamSettings={vi.fn().mockResolvedValue(settings)}
				saveTeamSettings={saveTeamSettings}
				validateTeamSettings={vi.fn().mockResolvedValue(true)}
			/>
		</ApiServiceContext.Provider>,
	);

	await screen.findByText("GeneralInputsComponent");
};

const theStatesLastSaved = () => {
	const calls = saveTeamSettings.mock.calls;
	const saved = calls[calls.length - 1]?.[0] as ITeamSettings | undefined;
	return (saved?.refinement?.states ?? [])
		.map((entry) => entry.state)
		.sort((left, right) => left.localeCompare(right));
};

const theStateInput = () =>
	screen.findByRole("combobox", { name: "New Refinement State" });

const theSuggestions = async () => {
	await userEvent.click(await theStateInput());
	return within(screen.getByRole("listbox"))
		.getAllByRole("option")
		.map((option) => option.textContent);
};

const pick = async (state: string) => {
	await userEvent.click(await theStateInput());
	await userEvent.click(
		within(screen.getByRole("listbox")).getByRole("option", { name: state }),
	);
};

const theChips = () =>
	Array.from(document.querySelectorAll(".MuiChip-root")).map(
		(chip) => chip.textContent,
	);

const removeChip = async (state: string) => {
	const chip = screen.getByText(state).closest(".MuiChip-root");
	const deleteIcon = chip?.querySelector(".MuiChip-deleteIcon");
	if (!deleteIcon) throw new Error(`${state} has no delete affordance`);
	await userEvent.click(deleteIcon);
};

describe("The Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("offers only the Team's To Do and Doing states as suggestions", async () => {
		await renderGravitysSettingsForm(gravitysSettings(null));

		expect(
			await screen.findByRole("heading", { name: "Refinement" }),
		).toBeVisible();
		expect(await theSuggestions()).toEqual(["Backlog", "Next", "Analysing"]);
	});

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	// The server refuses a state that is not To Do or Doing, so typing one must never reach the autosave.
	it("adds nothing when a state that is not suggested is typed", async () => {
		await renderGravitysSettingsForm(gravitysSettings(null));

		await userEvent.type(await theStateInput(), "Done{Enter}");

		expect(theChips()).toEqual([]);
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it("notes that a Doing state already counts in WIP and Cycle Time", async () => {
		await renderGravitysSettingsForm(gravitysSettings(null));

		await theStateInput();

		expect(
			screen.getByText("A Doing state already counts in WIP and Cycle Time"),
		).toBeVisible();
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("saves the added states as the Team's refinement states", async () => {
		await renderGravitysSettingsForm(gravitysSettings(null));

		await pick("Backlog");
		await pick("Analysing");
		await userEvent.type(await theStateInput(), "next{Enter}");

		await waitFor(() =>
			expect(theStatesLastSaved()).toEqual(["Analysing", "Backlog", "Next"]),
		);
		expect(theChips()).toEqual(["Backlog", "Analysing", "Next"]);
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it("shows the states already chosen as chips", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({ states: [{ state: "Backlog" }] }),
		);

		await theStateInput();

		expect(theChips()).toEqual(["Backlog"]);
		expect(await theSuggestions()).toEqual(["Next", "Analysing"]);
	});

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it("saves no refinement states once the last chip is removed", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({ states: [{ state: "Backlog" }] }),
		);

		await theStateInput();
		await removeChip("Backlog");

		await waitFor(() => expect(saveTeamSettings).toHaveBeenCalled());
		expect(theStatesLastSaved()).toEqual([]);
		expect(theChips()).toEqual([]);
	});

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it("shows no chip and no flag for a chosen state that is no longer To Do or Doing", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings(
				{ states: [{ state: "Backlog" }, { state: "Analysing" }] },
				["Next"],
			),
		);

		await theStateInput();

		expect(theChips()).toEqual(["Backlog"]);
		expect(screen.queryByText(/no longer mapped/i)).not.toBeInTheDocument();
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("drops a chosen state from the chips and the save once it stops being a Doing state", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				states: [{ state: "Backlog" }, { state: "Analysing" }],
			}),
		);
		expect(theChips()).toEqual(["Backlog", "Analysing"]);

		await userEvent.click(
			screen.getByRole("button", { name: "Stop mapping Analysing as Doing" }),
		);

		await waitFor(() => expect(saveTeamSettings).toHaveBeenCalled());
		expect(theStatesLastSaved()).toEqual(["Backlog"]);
		expect(theChips()).toEqual(["Backlog"]);
	});

	// @us-02 @slice-01 @boundary @contract-shape:bounded-change
	it("says the Team's own word for Refinement as the section's title and in the state input", async () => {
		terms.current = {
			...defaultTerms,
			[REFINEMENT_KEY]: "Replenishment",
			[REFINEMENTS_KEY]: "Replenishments",
		};
		await renderGravitysSettingsForm(gravitysSettings(null));

		expect(
			await screen.findByRole("heading", { name: "Replenishment" }),
		).toBeVisible();
		expect(
			screen.getByRole("combobox", { name: "New Replenishment State" }),
		).toBeInTheDocument();
	});
});
