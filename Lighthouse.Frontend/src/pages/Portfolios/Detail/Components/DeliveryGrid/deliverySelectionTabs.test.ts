import { describe, expect, it } from "vitest";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
} from "../../../../../models/WorkItemRules";
import {
	type DeliverySelectionState,
	type DeliverySelectionTab,
	type DeliverySelectionTerms,
	deliverySelectionTabsFor,
	deliveryTabForDelivery,
	emptySelectionValues,
	isIncompleteRule,
	MANUAL_SELECTION_TAB_KEY,
	RULE_BASED_SELECTION_TAB_KEY,
	ruleInputError,
} from "./deliverySelectionTabs";

// Both terms are renamed away from the seeded defaults on purpose. A tenant who calls a Feature a
// Deliverable must never be shown the word "Feature", and terms that match the defaults would let a
// hardcoded one through unnoticed.
const terms: DeliverySelectionTerms = {
	featureTerm: "Deliverable",
	deliveryTerm: "Launch",
	deliveriesTerm: "Launches",
};

const JIRA_RELEASE: IDeliverySource = {
	key: "jira-release",
	displayName: "Jira Release",
};

const JIRA_FIX_VERSION: IDeliverySource = {
	key: "jira-fix-version",
	displayName: "Jira Fix Version",
};

const filledInRule = (
	overrides: Partial<IWorkItemRuleCondition> = {},
): IWorkItemRuleCondition => ({
	fieldKey: "fixVersion",
	operator: "equals",
	value: "Release 44",
	...overrides,
});

const formState = (
	overrides: Partial<DeliverySelectionState> = {},
): DeliverySelectionState => ({
	selectedFeatureIds: [],
	rules: [],
	mode: "and",
	sourceReference: null,
	publishForecastToSource: false,
	rulesValidated: false,
	matchedFeaturesLength: 0,
	...overrides,
});

const storedDelivery = (overrides: Partial<IDelivery> = {}): IDelivery =>
	({
		id: 7,
		name: "Autumn launch",
		date: "2026-09-30T00:00:00Z",
		portfolioId: 1,
		features: [],
		selectionMode: DeliverySelectionMode.Manual,
		...overrides,
	}) as IDelivery;

const tabsFor = (sources: IDeliverySource[] = []): DeliverySelectionTab[] =>
	deliverySelectionTabsFor(sources, terms);

const tabWithKey = (key: string): DeliverySelectionTab =>
	tabsFor().find((tab) => tab.key === key) as DeliverySelectionTab;

const manualTab = (): DeliverySelectionTab =>
	tabWithKey(MANUAL_SELECTION_TAB_KEY);

const ruleBasedTab = (): DeliverySelectionTab =>
	tabWithKey(RULE_BASED_SELECTION_TAB_KEY);

const jiraReleaseTab = (): DeliverySelectionTab => tabsFor([JIRA_RELEASE])[2];

describe("a selection nobody has made yet", () => {
	it("starts with nothing chosen, no rules, rules that would all have to match, and nothing followed", () => {
		expect(emptySelectionValues()).toEqual({
			selectedFeatureIds: [],
			rules: [],
			mode: "and",
			sourceReference: null,
			publishForecastToSource: false,
		});
	});
});

describe("reopening the form on a stored delivery", () => {
	it("reads back what was chosen, the rules, and how they were combined", () => {
		const values = ruleBasedTab().hydrate(
			storedDelivery({
				features: [1, 2],
				rules: [filledInRule()],
				mode: "or",
			}),
		);

		expect(values.selectedFeatureIds).toEqual([1, 2]);
		expect(values.rules).toEqual([filledInRule()]);
		expect(values.mode).toBe("or");
	});

	it("reads a delivery stored before rules existed as one with nothing chosen and no rules", () => {
		const values = manualTab().hydrate(
			storedDelivery({ features: undefined, rules: undefined }),
		);

		expect(values.selectedFeatureIds).toEqual([]);
		expect(values.rules).toEqual([]);
	});

	it("treats any stored combination other than 'or' as all-must-match", () => {
		expect(manualTab().hydrate(storedDelivery({ mode: "and" })).mode).toBe(
			"and",
		);
		expect(manualTab().hydrate(storedDelivery({ mode: undefined })).mode).toBe(
			"and",
		);
	});
});

describe("a rule the reader has only half typed", () => {
	it("counts as complete when the field, the operator and the value all carry something", () => {
		expect(isIncompleteRule(filledInRule())).toBe(false);
	});

	it("counts as incomplete when any one of the three is empty", () => {
		expect(isIncompleteRule(filledInRule({ fieldKey: "" }))).toBe(true);
		expect(isIncompleteRule(filledInRule({ operator: "" }))).toBe(true);
		expect(isIncompleteRule(filledInRule({ value: "" }))).toBe(true);
	});

	it("counts as incomplete when any one of the three holds nothing but spaces", () => {
		expect(isIncompleteRule(filledInRule({ fieldKey: "   " }))).toBe(true);
		expect(isIncompleteRule(filledInRule({ operator: "   " }))).toBe(true);
		expect(isIncompleteRule(filledInRule({ value: "   " }))).toBe(true);
	});
});

describe("what is wrong with the rules before the backend is asked to match them", () => {
	it("asks for a rule when there is not one yet", () => {
		expect(ruleInputError([])).toBe("At least one rule must be defined");
	});

	it("asks for the gaps to be filled when one rule of several is half typed", () => {
		expect(ruleInputError([filledInRule(), filledInRule({ value: "" })])).toBe(
			"All rule fields must be completed",
		);
	});

	it("finds nothing wrong with rules that are all filled in", () => {
		expect(ruleInputError([filledInRule()])).toBeNull();
	});
});

describe("the manual tab", () => {
	it("blocks saving until something is chosen, in the tenant's own word for it", () => {
		expect(manualTab().firstBlockingError(formState(), terms)).toBe(
			"At least one deliverable must be selected",
		);
	});

	it("stops blocking once something is chosen", () => {
		expect(
			manualTab().firstBlockingError(
				formState({ selectedFeatureIds: [1] }),
				terms,
			),
		).toBeNull();
	});

	it("puts that same complaint on the field it is about, and takes it away again", () => {
		expect(manualTab().fieldErrors(formState(), terms)).toEqual({
			features: "At least one deliverable must be selected",
		});
		expect(
			manualTab().fieldErrors(formState({ selectedFeatureIds: [1] }), terms),
		).toEqual({});
	});

	it("writes down what was chosen and nothing else", () => {
		expect(
			manualTab().toPayload(formState({ selectedFeatureIds: [3, 4] })),
		).toEqual({ featureIds: [3, 4] });
	});

	// The three descriptor shapes are shared with the tab that reads from the work tracking system, so
	// every slot that tab needed had to be cut into them. None of it may reach this tab's behaviour.
	it("is untouched by the slots the source tab needed: it neither reads nor writes a binding", () => {
		const state = formState({
			selectedFeatureIds: [3],
			sourceReference: "10144",
		});

		expect(manualTab().mode).toBe(DeliverySelectionMode.Manual);
		expect(manualTab().firstBlockingError(state, terms)).toBeNull();
		expect(manualTab().toPayload(state)).toEqual({ featureIds: [3] });
		expect(manualTab().claims).toBeUndefined();
	});
});

describe("the rule-based tab", () => {
	it("blocks saving until the rules have been matched", () => {
		expect(ruleBasedTab().firstBlockingError(formState(), terms)).toBe(
			"Rules must be validated before saving",
		);
	});

	it("blocks saving when the rules were matched and matched nothing", () => {
		expect(
			ruleBasedTab().firstBlockingError(
				formState({ rulesValidated: true, matchedFeaturesLength: 0 }),
				terms,
			),
		).toBe("No features match the rules");
	});

	it("stops blocking once the rules have matched something", () => {
		expect(
			ruleBasedTab().firstBlockingError(
				formState({ rulesValidated: true, matchedFeaturesLength: 2 }),
				terms,
			),
		).toBeNull();
	});

	it("complains about what was typed before it complains about what was matched", () => {
		expect(ruleBasedTab().fieldErrors(formState(), terms)).toEqual({
			rules: "At least one rule must be defined",
		});
	});

	it("moves on to the matching once the rules are filled in", () => {
		expect(
			ruleBasedTab().fieldErrors(formState({ rules: [filledInRule()] }), terms),
		).toEqual({ rules: "Rules must be validated before saving" });
	});

	it("leaves the field clean when the rules are filled in and have matched", () => {
		expect(
			ruleBasedTab().fieldErrors(
				formState({
					rules: [filledInRule()],
					rulesValidated: true,
					matchedFeaturesLength: 1,
				}),
				terms,
			),
		).toEqual({});
	});

	it("writes down the rules and how they combine, not only what was matched", () => {
		expect(
			ruleBasedTab().toPayload(
				formState({
					selectedFeatureIds: [5],
					rules: [filledInRule()],
					mode: "or",
				}),
			),
		).toEqual({
			featureIds: [5],
			rules: [filledInRule()],
			mode: "or",
		});
	});

	it("is untouched by the slots the source tab needed: it neither reads nor writes a binding", () => {
		const state = formState({
			selectedFeatureIds: [5],
			rules: [filledInRule()],
			rulesValidated: true,
			matchedFeaturesLength: 1,
			sourceReference: "10144",
		});

		expect(ruleBasedTab().mode).toBe(DeliverySelectionMode.RuleBased);
		expect(ruleBasedTab().firstBlockingError(state, terms)).toBeNull();
		expect(ruleBasedTab().toPayload(state)).toEqual({
			featureIds: [5],
			rules: [filledInRule()],
			mode: "and",
		});
	});
});

describe("what a licence that does not cover a tab says", () => {
	it("locks the rule-based tab, and the tooltip on the locked button says how to unlock it", () => {
		const gate = ruleBasedTab().premiumGate;

		expect(gate?.whenLocked).toBe("lockTab");
		expect(gate?.notice).toBe(
			"Rule-based launch selection is a premium feature. Please upgrade your license to use this functionality.",
		);
		expect(gate?.tooltipExtraInfo).toBe(
			"Please obtain a premium license to use rule-based launches.",
		);
	});

	it("lets a source tab be opened and explains inside it, in the tenant's own word for a delivery", () => {
		const gate = jiraReleaseTab().premiumGate;

		expect(gate?.whenLocked).toBe("explainInside");
		expect(gate?.notice).toBe(
			"Taking a launch date from a Jira Release is a premium feature. Please upgrade your license to use this functionality.",
		);
		expect(gate?.tooltipExtraInfo).toBeUndefined();
	});
});

describe("which tab a stored delivery reopens on", () => {
	it("opens rule-based for one saved as rule-based, even with no rules stored against it", () => {
		expect(
			deliveryTabForDelivery(
				storedDelivery({
					selectionMode: DeliverySelectionMode.RuleBased,
					rules: [],
				}),
				tabsFor(),
			).key,
		).toBe(RULE_BASED_SELECTION_TAB_KEY);
	});

	it("opens rule-based for one carrying rules but stored before the mode was written down", () => {
		expect(
			deliveryTabForDelivery(
				storedDelivery({
					selectionMode: DeliverySelectionMode.Manual,
					rules: [filledInRule()],
				}),
				tabsFor(),
			).key,
		).toBe(RULE_BASED_SELECTION_TAB_KEY);
	});

	it("opens manual for one with neither", () => {
		expect(deliveryTabForDelivery(storedDelivery(), tabsFor()).key).toBe(
			MANUAL_SELECTION_TAB_KEY,
		);
	});
});

describe("a tab that reads a date out of the work tracking system", () => {
	it("is offered once per source the server reports, after the two built-in tabs", () => {
		expect(
			tabsFor([JIRA_RELEASE, JIRA_FIX_VERSION]).map((tab) => tab.label),
		).toEqual(["Manual", "Rule-Based", "Jira Release", "Jira Fix Version"]);
	});

	it("gets a tab of its own per source, so two sources are never the same tab", () => {
		const keys = tabsFor([JIRA_RELEASE, JIRA_FIX_VERSION]).map(
			(tab) => tab.key,
		);

		expect(new Set(keys).size).toBe(keys.length);
	});

	it("carries the source it was built from, so it knows what to ask the server for", () => {
		expect(jiraReleaseTab().source).toEqual(JIRA_RELEASE);
	});

	it("saves as one that follows the work tracking system", () => {
		expect(jiraReleaseTab().mode).toBe(DeliverySelectionMode.SourceBound);
	});

	it("reopens on the entry the delivery already follows, and on nothing else", () => {
		expect(
			jiraReleaseTab().hydrate(
				storedDelivery({
					features: [1, 2],
					rules: [filledInRule()],
					mode: "or",
					sourceReference: "10144",
				}),
			),
		).toEqual({
			selectedFeatureIds: [],
			rules: [],
			mode: "and",
			sourceReference: "10144",
			publishForecastToSource: false,
		});
	});

	it("asks for something to be picked, and stops asking once something is", () => {
		expect(jiraReleaseTab().firstBlockingError(formState(), terms)).toBe(
			"Pick a Jira Release to see the date it would set.",
		);
		expect(
			jiraReleaseTab().firstBlockingError(
				formState({ sourceReference: "10144" }),
				terms,
			),
		).toBeNull();
	});

	it("never puts a complaint on a field the reader cannot type into", () => {
		expect(
			jiraReleaseTab().fieldErrors(
				formState({ sourceReference: "10144" }),
				terms,
			),
		).toEqual({});
	});

	// The server resolves the work from the entry on every sync, so anything chosen or typed on the
	// way past is not merely unnecessary — sending it would claim a selection the server then ignores.
	it("writes down which entry it follows, and nothing anyone chose by hand", () => {
		expect(
			jiraReleaseTab().toPayload(
				formState({
					selectedFeatureIds: [1, 2],
					rules: [filledInRule()],
					mode: "or",
					sourceReference: "10144",
				}),
			),
		).toEqual({
			featureIds: [],
			sourceKey: "jira-release",
			sourceReference: "10144",
			publishForecastToSource: false,
		});
	});

	// The switch belongs to the binding, so it travels with it rather than being read off the delivery
	// somewhere else. A payload that left it out would read as "switch it off" at the server, which is
	// what every other field this request omits means.
	it("carries whether the forecast is broadcast, both ways", () => {
		expect(
			jiraReleaseTab().hydrate(
				storedDelivery({
					sourceReference: "10144",
					publishForecastToSource: true,
				}),
			).publishForecastToSource,
		).toBe(true);

		expect(
			jiraReleaseTab().toPayload(
				formState({ sourceReference: "10144", publishForecastToSource: true }),
			).publishForecastToSource,
		).toBe(true);
	});

	// The three descriptor shapes are shared, so the slot exists on the tabs that have nothing to
	// broadcast to. None of it may reach what they write down.
	it("is the only tab that writes down whether the forecast is broadcast", () => {
		const state = formState({
			selectedFeatureIds: [3],
			rules: [filledInRule()],
			publishForecastToSource: true,
		});

		expect(
			manualTab().toPayload(state).publishForecastToSource,
		).toBeUndefined();
		expect(
			ruleBasedTab().toPayload(state).publishForecastToSource,
		).toBeUndefined();
	});

	it("is offered by no connection that reports no source, whatever a delivery claims to follow", () => {
		expect(tabsFor().map((tab) => tab.key)).toEqual([
			MANUAL_SELECTION_TAB_KEY,
			RULE_BASED_SELECTION_TAB_KEY,
		]);
		expect(
			deliveryTabForDelivery(
				storedDelivery({
					selectionMode: DeliverySelectionMode.SourceBound,
					sourceKey: JIRA_RELEASE.key,
				}),
				tabsFor(),
			).key,
		).toBe(MANUAL_SELECTION_TAB_KEY);
	});
});

describe("which tab a delivery that follows a source reopens on", () => {
	const bound = (overrides: Partial<IDelivery> = {}): IDelivery =>
		storedDelivery({
			selectionMode: DeliverySelectionMode.SourceBound,
			sourceKey: JIRA_RELEASE.key,
			sourceReference: "10144",
			...overrides,
		});

	const bothSources = (): DeliverySelectionTab[] =>
		tabsFor([JIRA_RELEASE, JIRA_FIX_VERSION]);

	// The mode arrives as a name on everything read back from the server and as a number on everything
	// the browser has in hand, so both forms have to name the same tab or half the cases open wrong.
	it.each([
		{ form: "the name the server sends", selectionMode: "SourceBound" },
		{ form: "the number the browser sends", selectionMode: 2 },
	])("recognises it from $form", ({ selectionMode }) => {
		expect(
			deliveryTabForDelivery(
				bound({
					selectionMode: selectionMode as unknown as IDelivery["selectionMode"],
				}),
				bothSources(),
			).key,
		).toBe(`source:${JIRA_RELEASE.key}`);
	});

	it("opens on the source it follows, not on the first source the connection offers", () => {
		expect(
			deliveryTabForDelivery(
				bound({ sourceKey: JIRA_FIX_VERSION.key }),
				bothSources(),
			).key,
		).toBe(`source:${JIRA_FIX_VERSION.key}`);
	});

	it("leaves a hand-picked and a rule-based delivery on the tabs they were already on", () => {
		expect(deliveryTabForDelivery(storedDelivery(), bothSources()).key).toBe(
			MANUAL_SELECTION_TAB_KEY,
		);
		expect(
			deliveryTabForDelivery(
				storedDelivery({
					selectionMode: "RuleBased" as unknown as IDelivery["selectionMode"],
				}),
				bothSources(),
			).key,
		).toBe(RULE_BASED_SELECTION_TAB_KEY);
	});
});
