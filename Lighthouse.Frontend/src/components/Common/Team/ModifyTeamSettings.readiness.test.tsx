import { render, screen, waitFor } from "@testing-library/react";
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
	DISCUSSION_RULE_ERROR,
	MIN_VOTERS_ERROR,
	MIN_YES_ERROR,
	readinessErrors,
} from "./RefinementSettingsSection";

/**
 * Readiness in the Refinement section of a Team's settings: how many Yes votes, from how many voters,
 * make a Work Item Ready, and how many No votes, or how many "Yes, if…" votes, send it to discussion
 * instead. Each of the two discussion rules can be switched off on its own. Exercised through the settings form a Team admin uses, so what is checked is what the
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
const NO_RULE = "Send to discussion on No votes";
const YES_IF_RULE = "Send to discussion on “Yes, if…” votes";
const NO_THRESHOLD = "No votes that send to discussion";
const YES_IF_THRESHOLD = "“Yes, if…” votes that send to discussion";

// The server binds each number as a 32-bit int and refuses anything larger with a bare 400.
const LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES = 2147483647;

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

const THE_DEFAULTS: IReadinessSetting = {
	minYes: 3,
	minVoters: 3,
	discussWhen: { no: 1, yesIf: 2 },
};

// Readiness stored before the discussion rules existed carries none.
const STORED_WITHOUT_DISCUSSION_RULES: IReadinessSetting = {
	minYes: 3,
	minVoters: 3,
};

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
	it("shows three Yes votes from three voters, and discussion at one No or two “Yes, if…”, until the admin changes them", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings(STORED_WITHOUT_DISCUSSION_RULES),
		);

		expect(
			await screen.findByRole("spinbutton", { name: YES_VOTES_NEEDED }),
		).toHaveValue(3);
		expect(screen.getByRole("spinbutton", { name: VOTERS_NEEDED })).toHaveValue(
			3,
		);
		expect(screen.getByText("Send to discussion when")).toBeVisible();
		expect(screen.getByRole("checkbox", { name: NO_RULE })).toBeChecked();
		expect(screen.getByRole("spinbutton", { name: NO_THRESHOLD })).toHaveValue(
			1,
		);
		expect(screen.getByText("or more No votes")).toBeVisible();
		expect(screen.getByRole("checkbox", { name: YES_IF_RULE })).toBeChecked();
		expect(
			screen.getByRole("spinbutton", { name: YES_IF_THRESHOLD }),
		).toHaveValue(2);
		expect(screen.getByText("or more “Yes, if…” votes")).toBeVisible();

		await pastTheAutosaveDelay();
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-13 @slice-13 @driving_port @contract-shape:bounded-change
	it("saves a changed number of Yes votes with the rest of readiness as it was", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(YES_VOTES_NEEDED, "2");

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 2,
				minVoters: 3,
				discussWhen: { no: 1, yesIf: 2 },
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
	it("switches off the No rule on its own, keeping the “Yes, if…” rule", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await userEvent.click(
			await screen.findByRole("checkbox", { name: NO_RULE }),
		);

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: null, yesIf: 2 },
			}),
		);
	});

	// @us-13 @slice-13 @contract-shape:bounded-change
	it("saves a changed number of “Yes, if…” votes that send to discussion", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(YES_IF_THRESHOLD, "3");

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: 1, yesIf: 3 },
			}),
		);
	});

	// @us-13 @slice-13 @contract-shape:bounded-change
	it("switches a rule back on at the threshold it had before", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: 4, yesIf: 2 },
			}),
		);

		const noRule = await screen.findByRole("checkbox", { name: NO_RULE });
		await userEvent.click(noRule);
		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: null,
				yesIf: 2,
			}),
		);
		await userEvent.click(noRule);

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: 4, yesIf: 2 },
			}),
		);
	});

	// @us-13 @slice-13 @contract-shape:bounded-change
	it("switches on a rule stored as off at its default threshold", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: 1, yesIf: null },
			}),
		);

		await userEvent.click(
			await screen.findByRole("checkbox", { name: YES_IF_RULE }),
		);

		await waitFor(() =>
			expect(theReadinessLastSaved()).toEqual({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: 1, yesIf: 2 },
			}),
		);
	});

	// @us-13 @slice-13 @error @contract-shape:unbounded-preservation
	it.each([
		{ field: NO_THRESHOLD, typed: "0" },
		{ field: NO_THRESHOLD, typed: "" },
		{ field: YES_IF_THRESHOLD, typed: "0" },
		{ field: YES_IF_THRESHOLD, typed: "" },
	])(
		"refuses '$typed' in $field, says why and saves nothing",
		async ({ field, typed }) => {
			await renderGravitysSettingsForm(gravitysSettings());

			const threshold = await screen.findByRole("spinbutton", { name: field });
			await userEvent.clear(threshold);
			if (typed !== "") {
				await userEvent.type(threshold, typed);
			}
			await pastTheAutosaveDelay();

			expect(
				await screen.findByText("A discussion rule needs at least 1 vote"),
			).toBeVisible();
			expect(saveTeamSettings).not.toHaveBeenCalled();
		},
	);

	it("starts a Team that has never chosen refinement at the default readiness, and saves it with no states", async () => {
		await renderGravitysSettingsForm({
			...gravitysSettings(),
			refinement: undefined,
		});

		expect(
			await screen.findByRole("spinbutton", { name: YES_VOTES_NEEDED }),
		).toHaveValue(3);
		expect(screen.getByRole("spinbutton", { name: VOTERS_NEEDED })).toHaveValue(
			3,
		);

		await replaceTheNumberIn(YES_VOTES_NEEDED, "2");

		await waitFor(() =>
			expect(saveTeamSettings.mock.lastCall?.[0]?.refinement).toEqual({
				states: [],
				readiness: {
					minYes: 2,
					minVoters: 3,
					discussWhen: { no: 1, yesIf: 2 },
				},
			}),
		);
	});

	it("keeps the chosen refinement states when readiness changes", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(VOTERS_NEEDED, "4");

		await waitFor(() =>
			expect(saveTeamSettings.mock.lastCall?.[0]?.refinement).toEqual({
				states: [{ state: "Backlog" }, { state: "Analysing" }],
				readiness: {
					minYes: 3,
					minVoters: 4,
					discussWhen: { no: 1, yesIf: 2 },
				},
			}),
		);
	});

	it("names readiness as what keeps the settings from being saved", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(YES_VOTES_NEEDED, "0");

		expect(
			await screen.findByTestId("settings-blocking-warning"),
		).toHaveTextContent("Correct Readiness by votes");
	});

	it.each([
		{ refused: YES_VOTES_NEEDED, typed: "0", accepted: VOTERS_NEEDED },
		{ refused: VOTERS_NEEDED, typed: "2", accepted: YES_VOTES_NEEDED },
	])(
		"marks $refused as refused and leaves $accepted unmarked",
		async ({ refused, typed, accepted }) => {
			await renderGravitysSettingsForm(gravitysSettings());

			await replaceTheNumberIn(refused, typed);

			await waitFor(() =>
				expect(
					screen.getByRole("spinbutton", { name: refused }),
				).toHaveAttribute("aria-invalid", "true"),
			);
			expect(
				screen.getByRole("spinbutton", { name: accepted }),
			).toHaveAttribute("aria-invalid", "false");
		},
	);

	it.each([
		{ refused: NO_THRESHOLD, accepted: YES_IF_THRESHOLD },
		{ refused: YES_IF_THRESHOLD, accepted: NO_THRESHOLD },
	])(
		"marks $refused as refused, describes it by why, and leaves $accepted unmarked",
		async ({ refused, accepted }) => {
			await renderGravitysSettingsForm(gravitysSettings());

			await replaceTheNumberIn(refused, "0");

			const refusedField = screen.getByRole("spinbutton", { name: refused });
			await waitFor(() =>
				expect(refusedField).toHaveAttribute("aria-invalid", "true"),
			);
			expect(refusedField).toHaveAccessibleDescription(DISCUSSION_RULE_ERROR);
			const acceptedField = screen.getByRole("spinbutton", { name: accepted });
			expect(acceptedField).toHaveAttribute("aria-invalid", "false");
			expect(acceptedField).not.toHaveAttribute("aria-describedby");
		},
	);

	it("does not let any of its numbers step below one vote", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await screen.findByRole("spinbutton", { name: YES_VOTES_NEEDED });
		for (const name of [
			YES_VOTES_NEEDED,
			VOTERS_NEEDED,
			NO_THRESHOLD,
			YES_IF_THRESHOLD,
		]) {
			const field = screen.getByRole("spinbutton", { name });
			expect(field).toHaveAttribute("min", "1");
			expect(field).toHaveAttribute("step", "1");
		}
	});

	it("switches a rule back on at the threshold it was changed to before it was switched off", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheNumberIn(NO_THRESHOLD, "3");
		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: 3,
				yesIf: 2,
			}),
		);
		const noRule = screen.getByRole("checkbox", { name: NO_RULE });
		await userEvent.click(noRule);
		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: null,
				yesIf: 2,
			}),
		);
		await userEvent.click(noRule);

		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: 3,
				yesIf: 2,
			}),
		);
	});

	it("switches a rule that was refused when switched off back on at its last allowed threshold", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: 4, yesIf: 2 },
			}),
		);
		const noRule = await screen.findByRole("checkbox", { name: NO_RULE });
		await userEvent.click(noRule);
		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: null,
				yesIf: 2,
			}),
		);
		await userEvent.click(noRule);
		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: 4,
				yesIf: 2,
			}),
		);

		await replaceTheNumberIn(NO_THRESHOLD, "0");
		await userEvent.click(noRule);
		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: null,
				yesIf: 2,
			}),
		);
		await userEvent.click(noRule);

		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: 4,
				yesIf: 2,
			}),
		);
	});

	it("switches on a No rule stored as off at its default threshold", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				minYes: 3,
				minVoters: 3,
				discussWhen: { no: null, yesIf: 2 },
			}),
		);

		await userEvent.click(
			await screen.findByRole("checkbox", { name: NO_RULE }),
		);

		await waitFor(() =>
			expect(theReadinessLastSaved()?.discussWhen).toEqual({
				no: 1,
				yesIf: 2,
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
		{
			minYes: 1e11,
			minVoters: 1e11,
			minYesError: MIN_YES_ERROR,
			minVotersError: null,
		},
		{
			minYes: 3,
			minVoters: 1e11,
			minYesError: null,
			minVotersError: MIN_VOTERS_ERROR,
		},
		{
			minYes: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			minVoters: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			minYesError: null,
			minVotersError: null,
		},
	])(
		"$minYes Yes from $minVoters voters → $minYesError / $minVotersError",
		({ minYes, minVoters, minYesError, minVotersError }) => {
			expect(
				readinessErrors({
					minYes,
					minVoters,
					discussWhen: { no: 1, yesIf: 2 },
				}),
			).toEqual({
				minYes: minYesError,
				minVoters: minVotersError,
				discussWhenNo: null,
				discussWhenYesIf: null,
			});
		},
	);
});

describe("the discussion rules the form shares with the server", () => {
	it.each([
		{ no: 1, yesIf: 2, noError: null, yesIfError: null },
		{ no: 5, yesIf: 1, noError: null, yesIfError: null },
		{ no: null, yesIf: null, noError: null, yesIfError: null },
		{ no: 0, yesIf: 2, noError: DISCUSSION_RULE_ERROR, yesIfError: null },
		{ no: 1, yesIf: -1, noError: null, yesIfError: DISCUSSION_RULE_ERROR },
		{
			no: Number.NaN,
			yesIf: Number.NaN,
			noError: DISCUSSION_RULE_ERROR,
			yesIfError: DISCUSSION_RULE_ERROR,
		},
		{ no: null, yesIf: 0, noError: null, yesIfError: DISCUSSION_RULE_ERROR },
		{ no: 1e11, yesIf: 2, noError: DISCUSSION_RULE_ERROR, yesIfError: null },
		{ no: 1, yesIf: 1e11, noError: null, yesIfError: DISCUSSION_RULE_ERROR },
		{
			no: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			yesIf: LARGEST_WHOLE_NUMBER_THE_SERVER_TAKES,
			noError: null,
			yesIfError: null,
		},
	])(
		"No at $no, “Yes, if…” at $yesIf → $noError / $yesIfError",
		({ no, yesIf, noError, yesIfError }) => {
			expect(
				readinessErrors({
					minYes: 3,
					minVoters: 3,
					discussWhen: { no, yesIf },
				}),
			).toEqual({
				minYes: null,
				minVoters: null,
				discussWhenNo: noError,
				discussWhenYesIf: yesIfError,
			});
		},
	);

	it("checks a readiness stored without discussion rules as if it had the defaults", () => {
		expect(readinessErrors({ minYes: 3, minVoters: 3 })).toEqual({
			minYes: null,
			minVoters: null,
			discussWhenNo: null,
			discussWhenYesIf: null,
		});
	});
});
