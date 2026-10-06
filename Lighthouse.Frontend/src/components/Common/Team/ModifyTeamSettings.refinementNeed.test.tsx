import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementSettings } from "../../../models/Refinement/Refinement";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type {
	IWorkItemRuleCondition,
	IWorkItemRuleSchema,
	IWorkItemRuleSet,
} from "../../../models/WorkItemRules";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import ModifyTeamSettings from "./ModifyTeamSettings";
import RefinementCadenceSettings from "./RefinementCadenceSettings";
import RefinementSettingsSection, {
	refinementSettingsBlockers,
} from "./RefinementSettingsSection";
import StageRulesSettings, {
	hasIncompleteStageRule,
	NO_STAGE_RULES,
	stageRuleOf,
} from "./StageRulesSettings";

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

// Opening the form is not an edit, so nothing may be saved even once the autosave would have fired.
const AUTOSAVE_DELAY_AND_MARGIN_MS = 600;

const pastTheAutosaveDelay = () =>
	new Promise((resolve) => setTimeout(resolve, AUTOSAVE_DELAY_AND_MARGIN_MS));

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

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it("offers optional stages, a Ready rule and a Being refined rule, each with nothing set", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		expect(await screen.findByText("Stages (optional)")).toBeVisible();
		for (const stage of [READY_WHEN, BEING_REFINED_WHEN]) {
			const group = await theStageGroup(stage);
			expect(
				within(group).getByRole("button", { name: /^Add Rule$/i }),
			).toBeEnabled();
			expect(within(group).queryByLabelText("Value")).not.toBeInTheDocument();
		}

		await pastTheAutosaveDelay();
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it("shows the Ready rule the Team stored", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				stageRules: { ready: tagsContain("ready"), beingRefined: null },
			}),
		);

		const ready = await theStageGroup(READY_WHEN);
		expect(within(ready).getByLabelText("Value")).toHaveValue("ready");
	});

	// @us-03 @slice-03 @driving_port @contract-shape:bounded-change
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

	// @us-03 @slice-03 @boundary @contract-shape:bounded-change
	it("switches the Ready rule off when its last condition is removed", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				stageRules: {
					ready: tagsContain("ready"),
					beingRefined: tagsContain("analysing"),
				},
			}),
		);

		await userEvent.click(
			within(await theStageGroup(READY_WHEN)).getByRole("button", {
				name: "Remove rule",
			}),
		);

		await waitFor(() =>
			expect(theRefinementLastSaved()?.stageRules).toEqual({
				ready: null,
				beingRefined: tagsContain("analysing"),
			}),
		);
	});

	// @us-03 @slice-03 @boundary @contract-shape:bounded-change
	it("keeps an edited Ready rule when the Yes votes needed change before it is saved", async () => {
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

		await waitFor(() =>
			expect(theRefinementLastSaved()).toEqual({
				states: [
					{ state: "Backlog" },
					{ state: "Analysing" },
					{ state: "Next" },
				],
				readiness: {
					minYes: 2,
					minVoters: 3,
					discussWhen: { no: 1, yesIf: 2 },
				},
				stageRules: {
					ready: tagsContain("groomed"),
					beingRefined: tagsContain("analysing"),
				},
			}),
		);
	});

	// @us-03 @slice-03 @error @contract-shape:unbounded-preservation
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

describe("What a stage rule saves as", () => {
	const incomplete = {
		fieldKey: "workitem.tags",
		operator: "contains",
		value: " ",
	};
	const valueless = {
		fieldKey: "workitem.tags",
		operator: "isEmpty",
		value: "",
	};

	it.each<[string, IWorkItemRuleCondition[], IWorkItemRuleSet | null, boolean]>(
		[
			["no conditions", [], null, false],
			[
				"a condition without a value",
				[incomplete],
				{ version: 1, mode: "or", conditions: [incomplete] },
				true,
			],
			[
				"a condition that needs no value",
				[valueless],
				{ version: 1, mode: "or", conditions: [valueless] },
				false,
			],
			[
				"a finished condition beside an unfinished one",
				[...tagsContain("ready").conditions, incomplete],
				{
					version: 1,
					mode: "or",
					conditions: [...tagsContain("ready").conditions, incomplete],
				},
				true,
			],
			[
				"finished conditions only",
				tagsContain("ready").conditions,
				{ ...tagsContain("ready"), mode: "or" },
				false,
			],
		],
	)(
		"%s saves as the rule shown and blocks the save only while unfinished",
		(_, conditions, rule, blocks) => {
			expect(stageRuleOf(conditions, "or")).toEqual(rule);
			expect(hasIncompleteStageRule({ ready: null, beingRefined: rule })).toBe(
				blocks,
			);
			expect(hasIncompleteStageRule({ ready: rule, beingRefined: null })).toBe(
				blocks,
			);
		},
	);

	it("holds nothing back for a Team without stage rules", () => {
		expect(hasIncompleteStageRule(undefined)).toBe(false);
		expect(hasIncompleteStageRule(null)).toBe(false);
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

	// @us-04 @slice-04 @driving_port @contract-shape:pure-function
	it("offers the weekdays Monday to Sunday and every week, with no starting week asked for", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		expect(await screen.findByText("Refinement cadence")).toBeVisible();
		const weekdays = [
			"Monday",
			"Tuesday",
			"Wednesday",
			"Thursday",
			"Friday",
			"Saturday",
			"Sunday",
		];
		expect(
			screen
				.getAllByRole("checkbox")
				.map((box) => box.closest("label")?.textContent ?? "")
				.filter((label) => weekdays.includes(label)),
		).toEqual(weekdays);
		expect(screen.getByRole("spinbutton", { name: REPEAT_EVERY })).toHaveValue(
			1,
		);
		expect(screen.queryByLabelText(STARTING_WEEK)).not.toBeInTheDocument();
	});

	// @us-04 @slice-04 @driving_port @contract-shape:bounded-change
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

	// @us-04 @slice-04 @driving_port @contract-shape:bounded-change
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

	// @us-04 @slice-04 @error @contract-shape:unbounded-preservation
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

	// @us-04 @slice-04 @error @contract-shape:unbounded-preservation
	it("saves nothing for fewer than one week between Refinements", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				cadence: { weekdays: ["Thursday"], intervalWeeks: 1, anchorWeek: null },
			}),
		);

		await replaceTheNumberIn(REPEAT_EVERY, "0");
		await pastTheAutosaveDelay();

		expect(
			screen.getByRole("spinbutton", { name: REPEAT_EVERY }),
		).toHaveAttribute("aria-invalid", "true");
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-04 @slice-04 @error @contract-shape:unbounded-preservation
	// The server looks a year ahead for the next Refinement, so a longer gap could leave it none.
	it("saves nothing for more than 52 weeks between Refinements, and says why", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				cadence: { weekdays: ["Thursday"], intervalWeeks: 1, anchorWeek: null },
			}),
		);

		await replaceTheNumberIn(REPEAT_EVERY, "53");
		await pastTheAutosaveDelay();

		expect(
			screen.getByRole("spinbutton", { name: REPEAT_EVERY }),
		).toHaveAttribute("aria-invalid", "true");
		expect(
			screen.getByText(/^Refinements are at most 52 weeks apart\.$/),
		).toBeVisible();
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-04 @slice-04 @boundary @contract-shape:bounded-change
	it("leaves the Team without a cadence when the last weekday is unticked", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({
				cadence: { weekdays: ["Thursday"], intervalWeeks: 1, anchorWeek: null },
			}),
		);

		await userEvent.click(
			await screen.findByRole("checkbox", { name: "Thursday" }),
		);

		await waitFor(() =>
			expect(theRefinementLastSaved()?.cadence?.weekdays).toEqual([]),
		);
	});
});

const LOW_END = /^Low end likelihood/;
const HIGH_END = /^High end likelihood/;

const replaceTheLikelihood = async (label: RegExp, value: string) => {
	const field = await screen.findByRole("spinbutton", { name: label });
	await userEvent.clear(field);
	await userEvent.type(field, value);
};

// What the info icon beside the band's heading says when the admin points at it.
const theBandExplanation = async () => {
	await userEvent.hover(
		await screen.findByRole("button", { name: /^Based on the Team's / }),
	);
	return (await screen.findByRole("tooltip")).textContent ?? "";
};

describe("The band in the Refinement section of a Team's settings", () => {
	beforeEach(() => {
		terms.current = { ...defaultTerms };
		saveTeamSettings.mockReset();
		saveTeamSettings.mockResolvedValue(undefined);
	});

	// @us-07 @slice-07 @driving_port @contract-shape:pure-function
	it("shows the range read at 50% and 85% until the admin changes it", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		expect(
			await screen.findByRole("spinbutton", { name: LOW_END }),
		).toHaveValue(50);
		expect(screen.getByRole("spinbutton", { name: HIGH_END })).toHaveValue(85);

		await pastTheAutosaveDelay();
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-07 @slice-07 @driving_port @contract-shape:bounded-change
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

	// @us-07 @slice-07 @error @contract-shape:unbounded-preservation
	it.each([
		["90", "85"],
		["85", "85"],
	])(
		"refuses a low end of %s against a high end of %s, naming both, and saves nothing",
		async (low, high) => {
			await renderGravitysSettingsForm(
				gravitysSettings({
					band: { lowPercentile: 50, highPercentile: Number(high) },
				}),
			);

			await replaceTheLikelihood(LOW_END, low);
			await pastTheAutosaveDelay();

			expect(
				await screen.findByText(
					`The low end (${low}%) must be below the high end (${high}%).`,
				),
			).toBeVisible();
			expect(saveTeamSettings).not.toHaveBeenCalled();
		},
	);

	// @us-07 @slice-07 @error @boundary @contract-shape:unbounded-preservation
	it.each([
		["Low end likelihood", "49"],
		["High end likelihood", "96"],
	])(
		"refuses %s at %s, outside 50 to 95, and saves nothing",
		async (fieldName, value) => {
			const label = new RegExp(`^${fieldName}`);
			await renderGravitysSettingsForm(gravitysSettings());

			await replaceTheLikelihood(label, value);
			await pastTheAutosaveDelay();

			const field = screen.getByRole("spinbutton", { name: label });
			expect(field).toHaveAttribute("aria-invalid", "true");
			expect(field).toHaveAccessibleDescription("Between 50% and 95%.");
			expect(saveTeamSettings).not.toHaveBeenCalled();
		},
	);

	// @us-07 @slice-07 @error @contract-shape:unbounded-preservation
	it("refuses a decimal likelihood instead of saving it rounded, and saves nothing", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheLikelihood(HIGH_END, "85.5");
		await pastTheAutosaveDelay();

		const field = screen.getByRole("spinbutton", { name: HIGH_END });
		expect(field).toHaveAttribute("aria-invalid", "true");
		expect(field).toHaveAccessibleDescription("Between 50% and 95%.");
		expect(saveTeamSettings).not.toHaveBeenCalled();
	});

	// @us-07 @slice-07 @error @a11y @contract-shape:unbounded-preservation
	it("marks the high end being typed in when it falls below the low end, and explains why there", async () => {
		await renderGravitysSettingsForm(
			gravitysSettings({ band: { lowPercentile: 60, highPercentile: 85 } }),
		);

		await replaceTheLikelihood(HIGH_END, "55");

		const inverted = "The low end (60%) must be below the high end (55%).";
		const high = screen.getByRole("spinbutton", { name: HIGH_END });
		expect(high).toHaveAttribute("aria-invalid", "true");
		expect(high).toHaveAccessibleDescription(inverted);
		const low = screen.getByRole("spinbutton", { name: LOW_END });
		expect(low).toHaveAttribute("aria-invalid", "true");
		expect(low).toHaveAccessibleDescription(inverted);
		expect(screen.getAllByText(inverted)).toHaveLength(1);
	});

	// @us-07 @slice-07 @driving_port @contract-shape:pure-function
	it("heads the band with the Team's words for work items and Refinement", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.WORK_ITEMS]: "Tickets",
			[TERMINOLOGY_KEYS.REFINEMENT]: "Grooming",
		};
		await renderGravitysSettingsForm(gravitysSettings());

		expect(
			await screen.findByText("Tickets needed before the next Grooming"),
		).toBeVisible();
	});

	// @us-07 @slice-07 @driving_port @contract-shape:pure-function
	it("explains the high end at 95 as reached with only 5% likelihood", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheLikelihood(HIGH_END, "95");

		expect(await theBandExplanation()).toContain(
			"The Team pulls at least the low end with 50% likelihood, " +
				"and more than the high end with only 5% likelihood.",
		);
	});

	// @us-07 @slice-07 @error @contract-shape:unbounded-preservation
	it("keeps explaining the last valid band while the low end is cleared", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await userEvent.clear(
			await screen.findByRole("spinbutton", { name: LOW_END }),
		);

		const explanation = await theBandExplanation();
		expect(explanation).toContain(
			"low end with 50% likelihood, and more than the high end with only 15% likelihood.",
		);
		expect(explanation).not.toContain("NaN");
	});

	// @us-07 @slice-07 @driving_port @contract-shape:bounded-change
	it("explains the band as last edited, not as it was when the form opened", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheLikelihood(HIGH_END, "90");
		const afterTheEdit = "more than the high end with only 10% likelihood.";
		expect(await theBandExplanation()).toContain(afterTheEdit);

		await userEvent.clear(screen.getByRole("spinbutton", { name: LOW_END }));
		expect(await theBandExplanation()).toContain(afterTheEdit);
	});

	// @us-07 @slice-07 @driving_port @contract-shape:bounded-change
	it("explains the band as last edited when only the low end changed", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheLikelihood(LOW_END, "60");
		const afterTheEdit = "pulls at least the low end with 40% likelihood";
		expect(await theBandExplanation()).toContain(afterTheEdit);

		await userEvent.clear(screen.getByRole("spinbutton", { name: HIGH_END }));
		expect(await theBandExplanation()).toContain(afterTheEdit);
	});

	// @us-07 @need-over-one-cycle @slice-07 @driving_port @contract-shape:pure-function
	// The band is read over one Refinement cycle, so the explanation names that cycle, not the days until the next.
	it("explains the range as a How Many forecast between the next Refinement and the one after, in the Team's word", async () => {
		terms.current = {
			...defaultTerms,
			[TERMINOLOGY_KEYS.REFINEMENT]: "Grooming",
		};
		await renderGravitysSettingsForm(gravitysSettings());

		const explanation = await theBandExplanation();
		expect(explanation).toContain(
			"a How Many forecast for the working days between the next Grooming and the one after. ",
		);
		expect(explanation).not.toContain("until the next");
	});

	// @us-07 @slice-07 @error @contract-shape:unbounded-preservation
	it("shows a cleared low end as empty, not as 0, and says what it takes", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		const low = await screen.findByRole("spinbutton", { name: LOW_END });
		await userEvent.clear(low);

		expect(low).toHaveValue(null);
		expect(low).toHaveAccessibleDescription("Between 50% and 95%.");
	});

	// @us-07 @slice-07 @driving_port @a11y @contract-shape:pure-function
	it("marks neither end while the band is valid, and shows each end in percent", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		for (const label of [LOW_END, HIGH_END]) {
			const field = await screen.findByRole("spinbutton", { name: label });
			expect(field).not.toHaveAttribute("aria-invalid", "true");
			expect(
				within(field.parentElement as HTMLElement).getByText("%"),
			).toBeVisible();
		}
	});

	// @us-07 @slice-07 @error @a11y @contract-shape:unbounded-preservation
	it("marks only the low end when it is out of range above the high end, since that is not an inverted band", async () => {
		await renderGravitysSettingsForm(gravitysSettings());

		await replaceTheLikelihood(LOW_END, "96");

		const low = screen.getByRole("spinbutton", { name: LOW_END });
		expect(low).toHaveAttribute("aria-invalid", "true");
		expect(low).toHaveAccessibleDescription("Between 50% and 95%.");
		expect(
			screen.getByRole("spinbutton", { name: HIGH_END }),
		).not.toHaveAttribute("aria-invalid", "true");
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

		await act(async () => {
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
