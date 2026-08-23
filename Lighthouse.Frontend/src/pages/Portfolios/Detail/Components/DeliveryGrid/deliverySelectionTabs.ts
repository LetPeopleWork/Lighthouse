import type { IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
} from "../../../../../models/WorkItemRules";

export type DeliveryRuleMode = "and" | "or";

/** The part of the create/edit form that differs from one selection tab to the next. */
export interface DeliverySelectionValues {
	selectedFeatureIds: number[];
	rules: IWorkItemRuleCondition[];
	mode: DeliveryRuleMode;
}

export interface DeliverySelectionState extends DeliverySelectionValues {
	rulesValidated: boolean;
	matchedFeaturesLength: number;
}

export interface DeliverySelectionTerms {
	featureTerm: string;
}

export interface DeliverySelectionPayload {
	featureIds: number[];
	rules?: IWorkItemRuleCondition[];
	mode?: DeliveryRuleMode;
}

export interface DeliverySelectionFieldErrors {
	features?: string;
	rules?: string;
}

/**
 * How a tab behaves under a licence that does not cover it. Locking is right where opening the tab
 * would start work the licence cannot pay for; explaining is right where the tab has something
 * worth seeing and a reader shut out of it would never learn what they are missing.
 */
export interface DeliveryPremiumGate {
	whenLocked: "lockTab" | "explainInside";
	/** Shown in place of the tab body. */
	notice: string;
	/** Set only for tabs whose button is wrapped in a licence tooltip. */
	tooltipExtraInfo?: string;
}

export interface DeliverySelectionTab {
	key: string;
	label: string;
	/** Absent on tabs that cannot be saved, so nothing they show is ever written down. */
	mode?: DeliverySelectionMode;
	/** Set on tabs that read their choices from the work tracking system. */
	source?: IDeliverySource;
	/** Absent on tabs every licence covers. */
	premiumGate?: DeliveryPremiumGate;
	/** Recognises a stored delivery as belonging to this tab when the form opens for editing. */
	claims?: (delivery: IDelivery) => boolean;
	hydrate: (delivery: IDelivery) => DeliverySelectionValues;
	firstBlockingError: (
		state: DeliverySelectionState,
		terms: DeliverySelectionTerms,
	) => string | null;
	fieldErrors: (
		state: DeliverySelectionState,
		terms: DeliverySelectionTerms,
	) => DeliverySelectionFieldErrors;
	toPayload: (state: DeliverySelectionState) => DeliverySelectionPayload;
}

export const MANUAL_SELECTION_TAB_KEY = "manual";
export const RULE_BASED_SELECTION_TAB_KEY = "rules";

export const emptySelectionValues = (): DeliverySelectionValues => ({
	selectedFeatureIds: [],
	rules: [],
	mode: "and",
});

const valuesFromDelivery = (delivery: IDelivery): DeliverySelectionValues => ({
	selectedFeatureIds: delivery.features ?? [],
	rules:
		delivery.rules?.map((rule) => ({
			fieldKey: rule.fieldKey,
			operator: rule.operator,
			value: rule.value,
		})) ?? [],
	mode: delivery.mode === "or" ? "or" : "and",
});

export const isIncompleteRule = (rule: IWorkItemRuleCondition): boolean =>
	!rule.fieldKey.trim() || !rule.operator.trim() || !rule.value.trim();

/** What is wrong with the rules as typed, before anyone asks the backend to match them. */
export const ruleInputError = (
	rules: IWorkItemRuleCondition[],
): string | null => {
	if (rules.length === 0) {
		return "At least one rule must be defined";
	}

	if (rules.some(isIncompleteRule)) {
		return "All rule fields must be completed";
	}

	return null;
};

const manualBlockingError = (
	state: DeliverySelectionState,
	terms: DeliverySelectionTerms,
): string | null =>
	state.selectedFeatureIds.length === 0
		? `At least one ${terms.featureTerm.toLowerCase()} must be selected`
		: null;

const ruleBasedBlockingError = (
	state: DeliverySelectionState,
): string | null => {
	if (!state.rulesValidated) {
		return "Rules must be validated before saving";
	}

	if (state.matchedFeaturesLength === 0) {
		return "No features match the rules";
	}

	return null;
};

const manualTab: DeliverySelectionTab = {
	key: MANUAL_SELECTION_TAB_KEY,
	label: "Manual",
	mode: DeliverySelectionMode.Manual,
	hydrate: valuesFromDelivery,
	firstBlockingError: manualBlockingError,
	fieldErrors: (state, terms) => {
		const features = manualBlockingError(state, terms);
		return features === null ? {} : { features };
	},
	toPayload: (state) => ({ featureIds: state.selectedFeatureIds }),
};

const ruleBasedTab: DeliverySelectionTab = {
	key: RULE_BASED_SELECTION_TAB_KEY,
	label: "Rule-Based",
	mode: DeliverySelectionMode.RuleBased,
	premiumGate: {
		whenLocked: "lockTab",
		notice:
			"Rule-based delivery selection is a premium feature. Please upgrade your license to use this functionality.",
		tooltipExtraInfo:
			"Please obtain a premium license to use rule-based deliveries.",
	},
	claims: (delivery) =>
		delivery.selectionMode === DeliverySelectionMode.RuleBased ||
		(delivery.rules?.length ?? 0) > 0,
	hydrate: valuesFromDelivery,
	firstBlockingError: ruleBasedBlockingError,
	fieldErrors: (state) => {
		const rules = ruleInputError(state.rules) ?? ruleBasedBlockingError(state);
		return rules === null ? {} : { rules };
	},
	toPayload: (state) => ({
		featureIds: state.selectedFeatureIds,
		rules: state.rules,
		mode: state.mode,
	}),
};

const builtInSelectionTabs: DeliverySelectionTab[] = [manualTab, ruleBasedTab];

export const defaultDeliverySelectionTab = manualTab;

/**
 * A tab for one way the connection lets a date be read out of the work tracking system. It carries
 * no mode and no payload because looking at a source changes nothing yet; the blocking error says
 * so rather than letting a user press Save and wonder why nothing stuck.
 */
const sourceSelectionTab = (source: IDeliverySource): DeliverySelectionTab => ({
	key: `source:${source.key}`,
	label: source.displayName,
	source,
	premiumGate: {
		whenLocked: "explainInside",
		notice: `Taking a delivery date from a ${source.displayName} is a premium feature. Please upgrade your license to use this functionality.`,
	},
	hydrate: () => emptySelectionValues(),
	firstBlockingError: () =>
		`Picking a ${source.displayName} only previews it. Switch to Manual or Rule-Based to save.`,
	fieldErrors: () => ({}),
	toPayload: () => ({ featureIds: [] }),
});

/**
 * Every tab this Portfolio should offer. The sources come from the server, so a connection that
 * grows another one grows another tab here without a line changing.
 */
export const deliverySelectionTabsFor = (
	sources: IDeliverySource[],
): DeliverySelectionTab[] => [
	...builtInSelectionTabs,
	...sources.map(sourceSelectionTab),
];

export const deliveryTabForDelivery = (
	delivery: IDelivery,
): DeliverySelectionTab =>
	builtInSelectionTabs.find((tab) => tab.claims?.(delivery) === true) ??
	defaultDeliverySelectionTab;
