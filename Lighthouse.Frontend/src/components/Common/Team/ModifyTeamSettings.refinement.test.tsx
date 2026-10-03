import { render, screen, waitFor } from "@testing-library/react";
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
 * actually uses, so what is checked is what the form's autosave sends. It offers only the Team's To Do
 * and Doing states, each labelled with its category; a Doing state says it already counts in WIP and
 * cycle time; a chosen state the Team stopped mapping is flagged and kept, never quietly dropped.
 *
 * Sections the refinement choice does not depend on are stood in for, as the form's own tests do.
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
	default: () => <div>StatesList</div>,
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

describe("The Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("offers only the Team's To Do and Doing states, each labelled with its category", async () => {
		await renderGravitysSettingsForm(gravitysSettings(null));

		expect(
			await screen.findByRole("heading", { name: "Refinement" }),
		).toBeVisible();
		expect(
			screen.getByRole("checkbox", { name: "Backlog (To Do)" }),
		).toBeVisible();
		expect(
			screen.getByRole("checkbox", { name: "Next (Doing)" }),
		).toBeVisible();
		expect(
			screen.getByRole("checkbox", { name: "Analysing (Doing)" }),
		).toBeVisible();
		expect(screen.queryByRole("checkbox", { name: /^Done/ })).toBeNull();
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it("notes that a Doing state already counts in WIP and Cycle Time", async () => {
		await renderGravitysSettingsForm(gravitysSettings(null));

		await screen.findByRole("checkbox", { name: "Backlog (To Do)" });

		expect(
			screen.getAllByText(/already counts in WIP and Cycle Time/),
		).toHaveLength(2);
	});

	// @us-01 @slice-01 @driving_port @contract-shape:bounded-change
	it("saves the ticked states as the Team's refinement states", async () => {
		await renderGravitysSettingsForm(gravitysSettings(null));

		await userEvent.click(
			await screen.findByRole("checkbox", { name: "Backlog (To Do)" }),
		);
		await userEvent.click(
			screen.getByRole("checkbox", { name: "Analysing (Doing)" }),
		);
		await userEvent.click(
			screen.getByRole("checkbox", { name: "Next (Doing)" }),
		);

		await waitFor(() =>
			expect(theStatesLastSaved()).toEqual(["Analysing", "Backlog", "Next"]),
		);
	});

	// @us-01 @slice-01 @contract-shape:bounded-change
	it("shows the states already chosen as ticked", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({ states: [{ state: "Backlog", isMapped: true }] }),
		);

		expect(
			await screen.findByRole("checkbox", { name: "Backlog (To Do)" }),
		).toBeChecked();
		expect(
			screen.getByRole("checkbox", { name: "Next (Doing)" }),
		).not.toBeChecked();
	});

	// @us-01 @slice-01 @boundary @contract-shape:bounded-change
	it("saves no refinement states once the last one is unticked", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({ states: [{ state: "Backlog", isMapped: true }] }),
		);

		await userEvent.click(
			await screen.findByRole("checkbox", { name: "Backlog (To Do)" }),
		);

		await waitFor(() => expect(saveTeamSettings).toHaveBeenCalled());
		expect(theStatesLastSaved()).toEqual([]);
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	it.skip("flags a chosen state the Team no longer maps", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings(
				{
					states: [
						{ state: "Backlog", isMapped: true },
						{ state: "Analysing", isMapped: false },
					],
				},
				["Next"],
			),
		);

		expect(
			await screen.findByText(
				"Analysing is no longer mapped; its Work Items cannot appear",
			),
		).toBeVisible();
	});

	// @us-01 @slice-01 @error @contract-shape:bounded-change
	// Never silently dropped: the next edit to the section still carries the flagged state.
	it.skip("keeps a flagged state chosen when another state is ticked", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings(
				{
					states: [
						{ state: "Backlog", isMapped: true },
						{ state: "Analysing", isMapped: false },
					],
				},
				["Next"],
			),
		);

		await userEvent.click(
			await screen.findByRole("checkbox", { name: "Next (Doing)" }),
		);

		await waitFor(() =>
			expect(theStatesLastSaved()).toEqual(["Analysing", "Backlog", "Next"]),
		);
	});

	// @us-02 @slice-01 @boundary @contract-shape:bounded-change
	it.skip("says the Team's own word for Refinement as the section's title and in the flag", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
			[REFINEMENT_KEY]: "Replenishment",
			[REFINEMENTS_KEY]: "Replenishments",
		};
		await renderGravitysSettingsForm(
			gravitysSettings({ states: [{ state: "Analysing", isMapped: false }] }, [
				"Next",
			]),
		);

		expect(
			await screen.findByRole("heading", { name: "Replenishment" }),
		).toBeVisible();
		expect(
			screen.getByText(
				"Analysing is no longer mapped; its Tickets cannot appear",
			),
		).toBeVisible();
	});
});
