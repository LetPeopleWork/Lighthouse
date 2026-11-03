import { describe, expect, it } from "vitest";
import type { IRefinementSettings } from "../../../models/Refinement/Refinement";
import type {
	IWorkItemRuleCondition,
	IWorkItemRuleSet,
} from "../../../models/WorkItemRules";
import { refinementSettingsBlockers } from "./RefinementSettingsSection";
import { hasIncompleteStageRule, stageRuleOf } from "./StageRulesSettings";

const tagsContain = (tag: string) => ({
	version: 1,
	mode: "and" as const,
	conditions: [{ fieldKey: "workitem.tags", operator: "contains", value: tag }],
});

const aRefinement = (
	change: Partial<IRefinementSettings> = {},
): IRefinementSettings => ({
	states: [{ state: "Next" }],
	readiness: { minYes: 3, minVoters: 3, discussWhen: { no: 1, yesIf: 2 } },
	stageRules: { ready: null, beingRefined: null },
	...change,
});

describe("what keeps the Refinement settings from being saved", () => {
	it("names nothing for settings that are complete", () => {
		expect(refinementSettingsBlockers(aRefinement(), "Refinement")).toEqual([]);
	});

	it("names nothing for a Team that has never chosen refinement", () => {
		expect(refinementSettingsBlockers(undefined, "Refinement")).toEqual([]);
		expect(refinementSettingsBlockers(null, "Refinement")).toEqual([]);
	});

	it("names readiness when the voters needed are fewer than the Yes votes", () => {
		const refinement = aRefinement({
			readiness: { minYes: 3, minVoters: 2, discussWhen: { no: 1, yesIf: 2 } },
		});

		expect(refinementSettingsBlockers(refinement, "Refinement")).toEqual([
			"Correct Readiness by votes",
		]);
	});

	it("names the stage rules when one is still being written", () => {
		const unfinished = {
			...tagsContain(" "),
			conditions: [
				{ fieldKey: "workitem.tags", operator: "contains", value: " " },
			],
		};

		expect(
			refinementSettingsBlockers(
				aRefinement({ stageRules: { ready: unfinished, beingRefined: null } }),
				"Refinement",
			),
		).toEqual(["Complete the stage rules"]);
	});

	it("names the cadence in the Team's own word for refinement", () => {
		const cadence = { weekdays: [], intervalWeeks: 0 };

		expect(
			refinementSettingsBlockers(aRefinement({ cadence }), "Grooming"),
		).toEqual(["Complete the Grooming cadence"]);
	});

	it("names the band when the low end is above the high end", () => {
		const band = { lowPercentile: 95, highPercentile: 50 };

		expect(
			refinementSettingsBlockers(aRefinement({ band }), "Refinement"),
		).toEqual(["Correct the low and high end likelihoods"]);
	});

	it("names every part that blocks, in the order the form shows them", () => {
		const refinement = aRefinement({
			readiness: { minYes: 0, minVoters: 3, discussWhen: { no: 1, yesIf: 2 } },
			band: { lowPercentile: 95, highPercentile: 50 },
		});

		expect(refinementSettingsBlockers(refinement, "Refinement")).toEqual([
			"Correct Readiness by votes",
			"Correct the low and high end likelihoods",
		]);
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
