import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type {
	IRefinementSettings,
	IStageRulesSetting,
} from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItemRuleSchema } from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { ITeamService } from "../../../services/Api/TeamService";
import { inSequence } from "../../../tests/inSequence";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { renderRefinementSection } from "../../../tests/renderRefinementSection";
import RefinementSettingsSection from "./RefinementSettingsSection";
import StageRulesSettings, { NO_STAGE_RULES } from "./StageRulesSettings";

/**
 * What the stage rules part of a Team's Refinement settings shows for a value and what it hands back
 * when the admin edits it: an optional Ready rule and Being refined rule, each switched off until a
 * condition is added. The form's own tests prove that the form hands the section its value, saves
 * what the section hands back, and saves nothing while a new rule is incomplete.
 */

const TERMS: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.REFINEMENT]: "Refinement",
	[TERMINOLOGY_KEYS.REFINEMENTS]: "Refinements",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
};

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => TERMS[key] ?? key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

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

const READY_WHEN = "Ready when";
const BEING_REFINED_WHEN = "Being refined when";

const withTheTeamsFields = (ui: React.ReactElement) => (
	<QueryClientProvider client={new QueryClient()}>
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({
				teamService: {
					getForecastFilterSchema: vi
						.fn()
						.mockResolvedValue(workItemRuleSchema),
				} as unknown as ITeamService,
			})}
		>
			{ui}
		</ApiServiceContext.Provider>
	</QueryClientProvider>
);

const renderStageRules = (stageRules: IStageRulesSetting = NO_STAGE_RULES) =>
	renderRefinementSection(stageRules, (value, onChange) =>
		withTheTeamsFields(
			<StageRulesSettings teamId={1} stageRules={value} onChange={onChange} />,
		),
	);

// The Refinement section as a whole, for what spans two of its parts.
const renderTheWholeSection = (refinement: IRefinementSettings) =>
	renderRefinementSection(refinement, (value, onChange) =>
		withTheTeamsFields(
			<RefinementSettingsSection
				teamId={1}
				toDoStates={["Backlog"]}
				doingStates={["Next", "Analysing"]}
				stateMappings={[]}
				refinement={value}
				onChange={(change) => onChange({ ...value, ...change })}
			/>,
		),
	);

const theStageGroup = async (name: string) =>
	await screen.findByRole("group", { name });

describe("Stage rules in the Refinement section of a Team's settings", () => {
	it("offers optional stages, a Ready rule and a Being refined rule, each with nothing set", async () => {
		renderStageRules();

		expect(await screen.findByText("Stages (optional)")).toBeVisible();
		await inSequence([READY_WHEN, BEING_REFINED_WHEN], async (stage) => {
			const group = await theStageGroup(stage);
			expect(
				within(group).getByRole("button", { name: /^Add Rule$/i }),
			).toBeEnabled();
			expect(within(group).queryByLabelText("Value")).not.toBeInTheDocument();
		});
	});

	it("shows the Ready rule the Team stored", async () => {
		renderStageRules({ ready: tagsContain("ready"), beingRefined: null });

		const ready = await theStageGroup(READY_WHEN);
		expect(within(ready).getByLabelText("Value")).toHaveValue("ready");
	});

	it("switches the Ready rule off when its last condition is removed", async () => {
		const { user, latest } = renderStageRules({
			ready: tagsContain("ready"),
			beingRefined: tagsContain("analysing"),
		});

		await user.click(
			within(await theStageGroup(READY_WHEN)).getByRole("button", {
				name: "Remove rule",
			}),
		);

		expect(latest()).toEqual({
			ready: null,
			beingRefined: tagsContain("analysing"),
		});
	});

	it("keeps an edited Ready rule when the Yes votes needed change before it is saved", async () => {
		const { user, latest } = renderTheWholeSection({
			states: [{ state: "Backlog" }, { state: "Analysing" }, { state: "Next" }],
			readiness: { minYes: 3, minVoters: 3, discussWhen: { no: 1, yesIf: 2 } },
			stageRules: {
				ready: tagsContain("ready"),
				beingRefined: tagsContain("analysing"),
			},
		});

		const value = within(await theStageGroup(READY_WHEN)).getByLabelText(
			"Value",
		);
		await user.clear(value);
		await user.type(value, "groomed");
		const yesVotes = screen.getByRole("spinbutton", {
			name: "Yes votes needed",
		});
		await user.clear(yesVotes);
		await user.type(yesVotes, "2");

		expect(latest()).toEqual({
			states: [{ state: "Backlog" }, { state: "Analysing" }, { state: "Next" }],
			readiness: {
				minYes: 2,
				minVoters: 3,
				discussWhen: { no: 1, yesIf: 2 },
			},
			stageRules: {
				ready: tagsContain("groomed"),
				beingRefined: tagsContain("analysing"),
			},
		});
	});
});
