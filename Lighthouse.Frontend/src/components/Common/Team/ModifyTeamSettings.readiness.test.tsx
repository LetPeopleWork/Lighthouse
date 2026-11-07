import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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
import { pastTheAutosaveDelay } from "../../../tests/pastTheAutosaveDelay";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ModifyTeamSettings from "./ModifyTeamSettings";

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
const NO_THRESHOLD = "No votes that send to discussion";

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

beforeEach(() => {
	vi.useFakeTimers({ shouldAdvanceTime: true });
});

afterEach(() => {
	vi.useRealTimers();
});

describe("Readiness in the Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

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

	it("saves nothing just because the form was opened", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await screen.findByRole("spinbutton", { name: YES_VOTES_NEEDED });
		await pastTheAutosaveDelay();

		expect(saveTeamSettings).not.toHaveBeenCalled();
	});
});
