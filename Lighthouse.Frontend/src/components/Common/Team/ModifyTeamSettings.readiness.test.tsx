import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IReadinessSetting,
	IRefinementSettings,
} from "../../../models/Refinement/Refinement";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ModifyTeamSettings from "./ModifyTeamSettings";
import {
	MIN_VOTERS_ERROR,
	MIN_YES_ERROR,
	readinessErrors,
} from "./RefinementSettingsSection";

/**
 * Readiness in the Refinement section of a Team's settings: how many Yes votes, from how many voters,
 * make a Work Item Ready, and whether some No - or No and "Yes, if…" - votes send it to discussion
 * instead. Exercised through the settings form a Team admin uses, so what is checked is what the
 * form's autosave sends. At least one Yes is always needed, and never fewer voters than Yes votes.
 *
 * Sections readiness does not depend on are stood in for, as the form's own tests do.
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
	default: () => <div>StatesListComponent</div>,
}));

vi.mock("../Tags/TagsComponent", () => ({
	__esModule: true,
	default: () => <div>TagsComponent</div>,
}));

const YES_VOTES_NEEDED = "Yes votes needed";
const VOTERS_NEEDED = "Voters needed";
const SEND_TO_DISCUSSION = "Send to discussion";

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	refinement: "Refinement",
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

const THE_DEFAULTS: IReadinessSetting = { minYes: 3, minVoters: 3, veto: null };

const gravitysSettings = (
	readiness: IReadinessSetting = THE_DEFAULTS,
): ITeamSettings => {
	const refinement: IRefinementSettings = {
		states: [{ state: "Backlog" }, { state: "Analysing" }],
		readiness,
	};

	return {
		...createMockTeamSettings(),
		name: "Team Gravity",
		workTrackingSystemConnectionId: 1,
		toDoStates: ["Backlog"],
		doingStates: ["Next", "Analysing"],
		doneStates: ["Done"],
		refinement,
	};
};

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

const theReadinessLastSaved = () => {
	const calls = saveTeamSettings.mock.calls;
	const saved = calls[calls.length - 1]?.[0] as ITeamSettings | undefined;
	return saved?.refinement?.readiness;
};

const replaceTheNumberIn = async (label: string, value: string) => {
	const field = await screen.findByRole("spinbutton", { name: label });
	await userEvent.clear(field);
	await userEvent.type(field, value);
};

// Opening the form is not an edit, so nothing may be saved even once the autosave would have fired.
const AUTOSAVE_DELAY_AND_MARGIN_MS = 600;

const pastTheAutosaveDelay = () =>
	new Promise((resolve) => setTimeout(resolve, AUTOSAVE_DELAY_AND_MARGIN_MS));

describe("Readiness in the Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	// @us-13 @slice-13 @driving_port @contract-shape:pure-function
	it("shows three Yes votes from three voters and no veto until the admin changes them", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		expect(
			await screen.findByRole("spinbutton", { name: YES_VOTES_NEEDED }),
		).toHaveValue(3);
		expect(screen.getByRole("spinbutton", { name: VOTERS_NEEDED })).toHaveValue(
			3,
		);
		expect(
			screen.getByRole("checkbox", { name: SEND_TO_DISCUSSION }),
		).not.toBeChecked();
	});

	// @us-13 @slice-13 @driving_port @contract-shape:bounded-change
	it("saves a changed number of Yes votes with the rest of readiness as it was", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(YES_VOTES_NEEDED, "2");

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 2,
				minVoters: 3,
				veto: null,
			}),
		);
	});

	// @us-13 @slice-13 @error @contract-shape:unbounded-preservation
	it("refuses fewer than one Yes vote, says why and saves nothing", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(YES_VOTES_NEEDED, "0");
		await pastTheAutosaveDelay();

		expect(
			await screen.findByText("At least one Yes vote is needed"),
		).toBeVisible();
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-13 @slice-13 @error @contract-shape:unbounded-preservation
	it("refuses fewer voters than Yes votes, says why and saves nothing", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(VOTERS_NEEDED, "2");
		await pastTheAutosaveDelay();

		expect(
			await screen.findByText(
				"Voters needed cannot be fewer than Yes votes needed",
			),
		).toBeVisible();
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-13 @slice-13 @contract-shape:bounded-change
	it("saves a veto that sends a Work Item to discussion after one No", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await userEvent.click(
			await screen.findByRole("checkbox", { name: SEND_TO_DISCUSSION }),
		);

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 3,
				minVoters: 3,
				veto: { threshold: 1, counts: "No" },
			}),
		);
	});

	// @us-13 @slice-13 @contract-shape:bounded-change
	it("saves a veto that counts Yes, if… votes as well", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				minYes: 3,
				minVoters: 3,
				veto: { threshold: 2, counts: "No" },
			}),
		);

		const counting = await screen.findByRole("radiogroup", {
			name: "Counting",
		});
		await userEvent.click(
			within(counting).getByRole("radio", { name: "No or Yes, if…" }),
		);

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 3,
				minVoters: 3,
				veto: { threshold: 2, counts: "NoOrYesBut" },
			}),
		);
	});

	// @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
	it("saves nothing just because the form was opened", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await screen.findByRole("spinbutton", { name: YES_VOTES_NEEDED });
		await pastTheAutosaveDelay();

		expect(saveTeamSettings).not.toHaveBeenCalled();
	});
});

describe("the readiness rule the form shares with the server", () => {
	it.each([
		{ minYes: 3, minVoters: 3, minYesError: null, minVotersError: null },
		{ minYes: 1, minVoters: 1, minYesError: null, minVotersError: null },
		{ minYes: 2, minVoters: 5, minYesError: null, minVotersError: null },
		{
			minYes: 0,
			minVoters: 3,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: -1,
			minVoters: 0,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: Number.NaN,
			minVoters: 3,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: 3,
			minVoters: 2,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
		{
			minYes: 1,
			minVoters: 0,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
		{
			minYes: 3,
			minVoters: Number.NaN,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
	])(
		"$minYes Yes from $minVoters voters → $minYesError / $minVotersError",
		({ minYes, minVoters, minYesError, minVotersError }) => {
			expect(readinessErrors({ minYes, minVoters, veto: null })).toEqual({
				minYes: minYesError,
				minVoters: minVotersError,
			});
		},
	);
});
