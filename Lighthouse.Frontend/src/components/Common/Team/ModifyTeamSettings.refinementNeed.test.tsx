import {
	act,
	fireEvent,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementSettings } from "../../../models/Refinement/Refinement";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItemRuleSchema } from "../../../models/WorkItemRules";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import { actAndSettle } from "../../../tests/actAndSettle";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { pastTheAutosaveDelay } from "../../../tests/pastTheAutosaveDelay";
import { renderWithQueryClient as render } from "../../../tests/renderWithQueryClient";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ModifyTeamSettings from "./ModifyTeamSettings";
import RefinementCadenceSettings from "./RefinementCadenceSettings";
import RefinementSettingsSection, {
	refinementSettingsBlockers,
} from "./RefinementSettingsSection";
import StageRulesSettings, { NO_STAGE_RULES } from "./StageRulesSettings";

/**
 * What a Team admin sets in the Refinement section so the tab can say how much to refine: the optional
 * stage rules, the Refinement cadence, and the likelihoods the range is read at. Exercised through the
 * settings form a Team admin uses, so what is checked is what the form's autosave sends. The stage rules
 * use the rule editor the Team's other rules use; the cadence copies the recurring-blackout form.
 *
 * Sections these settings do not depend on are stood in for, as the form's own tests do.
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

const defaultTerms: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.REFINEMENT]: "Refinement",
	[TERMINOLOGY_KEYS.REFINEMENTS]: "Refinements",
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

// The fields a rule on Gravity's Work Items can look at, as the server describes them.
const workItemRuleSchema: IWorkItemRuleSchema = {
	fields: [
		{ fieldKey: "workitem.tags", displayName: "Tags", isMultiValue: true },
		{ fieldKey: "workitem.type", displayName: "Type", isMultiValue: false },
	],
	operators: ["equals", "notEquals", "contains"],
	maxRules: 20,
	maxValueLength: 500,
};

const tagsContain = (tag: string) => ({
	version: 1,
	mode: "and" as const,
	conditions: [{ fieldKey: "workitem.tags", operator: "contains", value: tag }],
});

const gravitysSettings = (
	refinement: Partial<IRefinementSettings> = {},
): ITeamSettings => ({
	...createMockTeamSettings(),
	name: "Team Gravity",
	workTrackingSystemConnectionId: 1,
	toDoStates: ["Backlog"],
	doingStates: ["Next", "Analysing"],
	doneStates: ["Done"],
	refinement: {
		states: [{ state: "Backlog" }, { state: "Analysing" }, { state: "Next" }],
		readiness: { minYes: 3, minVoters: 3, discussWhen: { no: 1, yesIf: 2 } },
		...refinement,
	},
});

const saveTeamSettings = vi.fn();

const renderGravitysSettingsForm = async (settings: ITeamSettings) => {
	render(
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				teamService: {
					updateTeamData: vi.fn().mockResolvedValue(undefined),
					getForecastFilterSchema: vi
						.fn()
						.mockResolvedValue(workItemRuleSchema),
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

const theRefinementLastSaved = () => {
	const calls = saveTeamSettings.mock.calls;
	const saved = calls[calls.length - 1]?.[0] as ITeamSettings | undefined;
	return saved?.refinement;
};

beforeEach(() => {
	vi.useFakeTimers({ shouldAdvanceTime: true });
});

afterEach(() => {
	vi.useRealTimers();
});

const replaceTheNumberIn = async (label: string, value: string) => {
	const field = await screen.findByRole("spinbutton", { name: label });
	await userEvent.clear(field);
	await userEvent.type(field, value);
};

const READY_WHEN = "Ready when";
const BEING_REFINED_WHEN = "Being refined when";

const theStageGroup = async (name: string) =>
	await screen.findByRole("group", { name });

describe("Stage rules in the Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	it("saves a changed Ready rule and leaves the Being refined rule as it was", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				stageRules: {
					ready: tagsContain("ready"),
					beingRefined: tagsContain("analysing"),
				},
			}),
		);

		const value = within(await theStageGroup(READY_WHEN)).getByLabelText(
			"Value",
		);
		await userEvent.clear(value);
		await userEvent.type(value, "groomed");

		await waitFor(() =>
			expect(theRefinementLastSaved()?.stageRules).toEqual({
				ready: tagsContain("groomed"),
				beingRefined: tagsContain("analysing"),
			}),
		);
	});

	it("saves an edited Ready rule together with Yes votes needed changed before the first save", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				stageRules: {
					ready: tagsContain("ready"),
					beingRefined: tagsContain("analysing"),
				},
			}),
		);

		const value = within(await theStageGroup(READY_WHEN)).getByLabelText(
			"Value",
		);
		await userEvent.clear(value);
		await userEvent.type(value, "groomed");
		await replaceTheNumberIn("Yes votes needed", "2");

		const bothEdits = {
			states: [{ state: "Backlog" }, { state: "Analysing" }, { state: "Next" }],
			readiness: { minYes: 2, minVoters: 3, discussWhen: { no: 1, yesIf: 2 } },
			stageRules: {
				ready: tagsContain("groomed"),
				beingRefined: tagsContain("analysing"),
			},
		};
		await waitFor(() => expect(theRefinementLastSaved()).toEqual(bothEdits));
	});

	it("saves nothing while a new rule is still incomplete", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await userEvent.click(
			within(await theStageGroup(BEING_REFINED_WHEN)).getByRole("button", {
				name: /^Add Rule$/i,
			}),
		);
		await pastTheAutosaveDelay();

		expect(saveTeamSettings).not.toHaveBeenCalled();
	});
});

const REPEAT_EVERY = "Repeat every (weeks)";
const STARTING_WEEK = "Starting week";

describe("The Refinement cadence in the Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	it("saves Thursdays every week", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await userEvent.click(
			await screen.findByRole("checkbox", { name: "Thursday" }),
		);

		await waitFor(() =>
			expect(theRefinementLastSaved()?.cadence).toEqual({
				weekdays: ["Thursday"],
				intervalWeeks: 1,
				anchorWeek: null,
			}),
		);
	});

	it("asks for the starting week once Refinements are more than a week apart, and saves it", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				cadence: { weekdays: ["Tuesday"], intervalWeeks: 1, anchorWeek: null },
			}),
		);

		await replaceTheNumberIn(REPEAT_EVERY, "2");
		fireEvent.change(await screen.findByLabelText(STARTING_WEEK), {
			target: { value: "2026-10-05" },
		});

		await waitFor(() =>
			expect(theRefinementLastSaved()?.cadence).toEqual({
				weekdays: ["Tuesday"],
				intervalWeeks: 2,
				anchorWeek: "2026-10-05",
			}),
		);
	});

	it("saves nothing while every second week names no starting week, and marks the field", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				cadence: { weekdays: ["Tuesday"], intervalWeeks: 1, anchorWeek: null },
			}),
		);

		await replaceTheNumberIn(REPEAT_EVERY, "2");
		await pastTheAutosaveDelay();

		expect(await screen.findByLabelText(STARTING_WEEK)).toHaveAttribute(
			"aria-invalid",
			"true",
		);
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});
});

const HIGH_END = /^High end likelihood/;

const replaceTheLikelihood = async (label: RegExp, value: string) => {
	const field = await screen.findByRole("spinbutton", { name: label });
	await userEvent.clear(field);
	await userEvent.type(field, value);
};

describe("The band in the Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	it("saves a changed high end with the low end as it was", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheLikelihood(HIGH_END, "95");

		await waitFor(() =>
			expect(theRefinementLastSaved()?.band).toEqual({
				lowPercentile: 50,
				highPercentile: 95,
			}),
		);
	});

	it("keeps the stage rules, cadence and readiness as they were when the band changes", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				stageRules: { ready: tagsContain("ready"), beingRefined: null },
				cadence: { weekdays: ["Thursday"], intervalWeeks: 1, anchorWeek: null },
			}),
		);

		await replaceTheLikelihood(HIGH_END, "95");

		await waitFor(() =>
			expect(theRefinementLastSaved()).toEqual({
				states: [
					{ state: "Backlog" },
					{ state: "Analysing" },
					{ state: "Next" },
				],
				readiness: {
					minYes: 3,
					minVoters: 3,
					discussWhen: { no: 1, yesIf: 2 },
				},
				stageRules: { ready: tagsContain("ready"), beingRefined: null },
				cadence: { weekdays: ["Thursday"], intervalWeeks: 1, anchorWeek: null },
				band: { lowPercentile: 50, highPercentile: 95 },
			}),
		);
	});

	it("refuses a decimal likelihood instead of saving it rounded, and saves nothing", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheLikelihood(HIGH_END, "85.5");
		await pastTheAutosaveDelay();

		const field = screen.getByRole("spinbutton", { name: HIGH_END });
		expect(field).toHaveAttribute("aria-invalid", "true");
		expect(field).toHaveAccessibleDescription("Between 50% and 95%.");
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});
});

describe("What holds back the save of the Refinement settings", () => {
	it("names an unfinished stage rule and a cadence with a mistake, in the Team's word", () => {
		expect(
			refinementSettingsBlockers(
				{
					states: [],
					stageRules: {
						ready: {
							version: 1,
							mode: "and",
							conditions: [
								{ fieldKey: "workitem.tags", operator: "contains", value: "" },
							],
						},
						beingRefined: null,
					},
					cadence: {
						weekdays: ["Tuesday"],
						intervalWeeks: 0,
						anchorWeek: null,
					},
				},
				"Grooming",
			),
		).toEqual(["Complete the stage rules", "Complete the Grooming cadence"]);
	});

	it("names likelihoods with the low end not below the high end", () => {
		expect(
			refinementSettingsBlockers(
				{ states: [], band: { lowPercentile: 90, highPercentile: 85 } },
				"Grooming",
			),
		).toEqual(["Correct the low and high end likelihoods"]);
	});
});

const withTeamService = (
	getForecastFilterSchema: ITeamService["getForecastFilterSchema"],
) => {
	const context = createMockApiServiceContext({
		teamService: { getForecastFilterSchema } as unknown as ITeamService,
	});
	return (ui: React.ReactElement) => (
		<ApiServiceContext.Provider value={context}>
			{ui}
		</ApiServiceContext.Provider>
	);
};

const STAGES_HEADING = "Stages (optional)";
const READY_EMPTY = "Add a rule to mark work items as Ready.";
const BEING_REFINED_EMPTY = "Add a rule to mark work items as being refined.";

describe("The stage rules of a single Team", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
	});

	it("says what each empty stage would do, in the Team's word for work items", async () => {
		const wrap = withTeamService(vi.fn().mockResolvedValue(workItemRuleSchema));
		render(
			wrap(
				<StageRulesSettings
					teamId={1}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);

		expect(await screen.findByText(READY_EMPTY)).toBeVisible();
		expect(screen.getByText(BEING_REFINED_EMPTY)).toBeVisible();
	});

	it("asks for no fields and offers no stages for a Team that is not saved yet", async () => {
		const getSchema = vi.fn().mockResolvedValue(workItemRuleSchema);
		render(
			withTeamService(getSchema)(
				<StageRulesSettings
					teamId={0}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);

		await act(async () => {
			await Promise.resolve();
		});

		expect(getSchema).not.toHaveBeenCalled();
		expect(screen.queryByText(STAGES_HEADING)).not.toBeInTheDocument();
	});

	it("stops offering stages once the form is for a Team that is not saved yet", async () => {
		const wrap = withTeamService(vi.fn().mockResolvedValue(workItemRuleSchema));
		const { rerender } = render(
			wrap(
				<StageRulesSettings
					teamId={1}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);
		await screen.findByText(STAGES_HEADING);

		rerender(
			wrap(
				<StageRulesSettings
					teamId={0}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);

		expect(screen.queryByText(STAGES_HEADING)).not.toBeInTheDocument();
	});

	it("offers no stages when the fields of the Team cannot be fetched", async () => {
		const wrap = withTeamService(
			vi.fn((teamId: number) =>
				teamId === 1
					? Promise.resolve(workItemRuleSchema)
					: Promise.reject(new Error("unreachable")),
			),
		);
		const { rerender } = render(
			wrap(
				<StageRulesSettings
					teamId={1}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);
		await screen.findByText(STAGES_HEADING);

		rerender(
			wrap(
				<StageRulesSettings
					teamId={2}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);

		await waitFor(() =>
			expect(screen.queryByText(STAGES_HEADING)).not.toBeInTheDocument(),
		);
	});

	it("ignores the fields of a Team the form has already moved away from", async () => {
		let answerTheFirstTeam: (schema: IWorkItemRuleSchema) => void = () => {};
		const wrap = withTeamService(
			vi.fn((teamId: number) =>
				teamId === 1
					? new Promise<IWorkItemRuleSchema>((resolve) => {
							answerTheFirstTeam = resolve;
						})
					: Promise.resolve(workItemRuleSchema),
			),
		);
		const { rerender } = render(
			wrap(
				<StageRulesSettings
					teamId={1}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);
		rerender(
			wrap(
				<StageRulesSettings
					teamId={2}
					stageRules={NO_STAGE_RULES}
					onChange={vi.fn()}
				/>,
			),
		);
		await screen.findByText(READY_EMPTY);

		await actAndSettle(() => {
			answerTheFirstTeam({ ...workItemRuleSchema, fields: [] });
		});

		expect(screen.getByText(READY_EMPTY)).toBeVisible();
		expect(screen.queryByText(/^No fields available/)).not.toBeInTheDocument();
	});

	it("saves the first condition of an empty stage as matching all conditions", async () => {
		const onChange = vi.fn();
		render(
			withTeamService(vi.fn().mockResolvedValue(workItemRuleSchema))(
				<StageRulesSettings
					teamId={1}
					stageRules={NO_STAGE_RULES}
					onChange={onChange}
				/>,
			),
		);

		await userEvent.click(
			within(await theStageGroup(READY_WHEN)).getByRole("button", {
				name: /^Add Rule$/i,
			}),
		);

		expect(onChange).toHaveBeenLastCalledWith({
			ready: {
				version: 1,
				mode: "and",
				conditions: [
					{ fieldKey: "workitem.tags", operator: "equals", value: "" },
				],
			},
			beingRefined: null,
		});
	});

	it("saves a stage rule switched to match any condition", async () => {
		const onChange = vi.fn();
		const twoTags = {
			version: 1,
			mode: "and" as const,
			conditions: [
				...tagsContain("ready").conditions,
				...tagsContain("groomed").conditions,
			],
		};
		render(
			withTeamService(vi.fn().mockResolvedValue(workItemRuleSchema))(
				<StageRulesSettings
					teamId={1}
					stageRules={{ ready: twoTags, beingRefined: null }}
					onChange={onChange}
				/>,
			),
		);

		await userEvent.click(
			within(await theStageGroup(READY_WHEN)).getByRole("button", {
				name: "Match any rule (OR)",
			}),
		);

		expect(onChange).toHaveBeenLastCalledWith({
			ready: { ...twoTags, mode: "or" },
			beingRefined: null,
		});
	});
});

describe("The Refinement states a Team admin can choose from", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	it("the refinement picker offers a mapping's name, not its states", async () => {
		await renderGravitysSettingsForm({
			...gravitysSettings({ states: [] }),
			stateMappings: [{ name: "Analysis", states: ["Next", "Analysing"] }],
		});

		await userEvent.click(
			await screen.findByRole("combobox", { name: /refinement state/i }),
		);

		const offered = within(await screen.findByRole("listbox"))
			.getAllByRole("option")
			.map((option) => option.textContent);
		expect(offered).toEqual(["Backlog", "Analysis"]);
	});
});

describe("The Refinement states when the To Do and Doing states change", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
	});

	it("leaves the chosen states alone while every one of them is still offered", () => {
		const onChange = vi.fn();
		const refinement: IRefinementSettings = {
			states: [{ state: "Backlog" }, { state: "Next" }],
		};
		const wrap = withTeamService(vi.fn());
		const { rerender } = render(
			wrap(
				<RefinementSettingsSection
					teamId={0}
					toDoStates={["Backlog"]}
					doingStates={["Next"]}
					stateMappings={[]}
					refinement={refinement}
					onChange={onChange}
				/>,
			),
		);

		rerender(
			wrap(
				<RefinementSettingsSection
					teamId={0}
					toDoStates={["Backlog"]}
					doingStates={["Next", "Analysing"]}
					stateMappings={[]}
					refinement={refinement}
					onChange={onChange}
				/>,
			),
		);

		expect(onChange).not.toHaveBeenCalled();
	});
});

describe("The Refinement cadence fields on their own", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
	});

	it("saves just the ticked weekday for a Team without a cadence", async () => {
		const onChange = vi.fn();
		render(<RefinementCadenceSettings cadence={null} onChange={onChange} />);

		await userEvent.click(screen.getByRole("checkbox", { name: "Thursday" }));

		expect(onChange).toHaveBeenLastCalledWith({
			weekdays: ["Thursday"],
			intervalWeeks: 1,
			anchorWeek: null,
		});
	});

	it("keeps the starting week when a weekday is ticked", async () => {
		const onChange = vi.fn();
		render(
			<RefinementCadenceSettings
				cadence={{
					weekdays: ["Tuesday"],
					intervalWeeks: 2,
					anchorWeek: "2026-10-05",
				}}
				onChange={onChange}
			/>,
		);

		await userEvent.click(screen.getByRole("checkbox", { name: "Thursday" }));

		expect(onChange).toHaveBeenLastCalledWith({
			weekdays: ["Tuesday", "Thursday"],
			intervalWeeks: 2,
			anchorWeek: "2026-10-05",
		});
	});

	it("drops a ticked weekday when it is ticked again and keeps the others", async () => {
		const onChange = vi.fn();
		render(
			<RefinementCadenceSettings
				cadence={{
					weekdays: ["Tuesday", "Thursday"],
					intervalWeeks: 1,
					anchorWeek: null,
				}}
				onChange={onChange}
			/>,
		);

		await userEvent.click(screen.getByRole("checkbox", { name: "Tuesday" }));

		expect(onChange).toHaveBeenLastCalledWith({
			weekdays: ["Thursday"],
			intervalWeeks: 1,
			anchorWeek: null,
		});
	});

	it("says Refinements are at least one week apart for fewer than one week", () => {
		render(
			<RefinementCadenceSettings
				cadence={{ weekdays: ["Tuesday"], intervalWeeks: 0, anchorWeek: null }}
				onChange={vi.fn()}
			/>,
		);

		expect(
			screen.getByText(/^Refinements are at least one week apart\.$/),
		).toBeVisible();
	});

	it("asks for a day in the week of a Refinement when every second week has no starting week", () => {
		render(
			<RefinementCadenceSettings
				cadence={{ weekdays: ["Tuesday"], intervalWeeks: 2, anchorWeek: null }}
				onChange={vi.fn()}
			/>,
		);

		expect(
			screen.getByText(
				/^Pick a day in the week of a Refinement to count the weeks from\.$/,
			),
		).toBeVisible();
	});

	it("does not mark a valid number of weeks as invalid", () => {
		render(
			<RefinementCadenceSettings
				cadence={{ weekdays: ["Tuesday"], intervalWeeks: 1, anchorWeek: null }}
				onChange={vi.fn()}
			/>,
		);

		expect(
			screen.getByRole("spinbutton", { name: REPEAT_EVERY }),
		).toHaveAttribute("aria-invalid", "false");
	});
});
