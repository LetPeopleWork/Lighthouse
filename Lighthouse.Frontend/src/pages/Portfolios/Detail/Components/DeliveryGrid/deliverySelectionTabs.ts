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
	/** Which entry of a source the delivery follows, or null when it follows none. */
	sourceReference: string | null;
}

export interface DeliverySelectionState extends DeliverySelectionValues {
	rulesValidated: boolean;
	matchedFeaturesLength: number;
}

export interface DeliverySelectionTerms {
	featureTerm: string;
	deliveryTerm: string;
	deliveriesTerm: string;
}

export interface DeliverySelectionPayload {
	featureIds: number[];
	rules?: IWorkItemRuleCondition[];
	mode?: DeliveryRuleMode;
	sourceKey?: string;
	sourceReference?: string;
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
	sourceReference: null,
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
	sourceReference: delivery.sourceReference ?? null,
});

/**
 * A delivery read back from the server names its selection mode — "SourceBound" — where the browser
 * sends the number, so comparing a stored delivery against the enum member alone matches nothing
 * that ever came off the wire, and every bound delivery would read as a hand-picked one.
 */
export const isStoredAs = (
	delivery: IDelivery,
	mode: DeliverySelectionMode,
): boolean =>
	delivery.selectionMode === mode ||
	(delivery.selectionMode as unknown as string) === DeliverySelectionMode[mode];

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

const ruleBasedSelectionTab = (
	terms: DeliverySelectionTerms,
): DeliverySelectionTab => ({
	key: RULE_BASED_SELECTION_TAB_KEY,
	label: "Rule-Based",
	mode: DeliverySelectionMode.RuleBased,
	premiumGate: {
		whenLocked: "lockTab",
		notice: `Rule-based ${terms.deliveryTerm.toLowerCase()} selection is a premium feature. Please upgrade your license to use this functionality.`,
		tooltipExtraInfo: `Please obtain a premium license to use rule-based ${terms.deliveriesTerm.toLowerCase()}.`,
	},
	claims: (delivery) =>
		isStoredAs(delivery, DeliverySelectionMode.RuleBased) ||
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
});

const builtInSelectionTabs = (
	terms: DeliverySelectionTerms,
): DeliverySelectionTab[] => [manualTab, ruleBasedSelectionTab(terms)];

export const defaultDeliverySelectionTab = manualTab;

/**
 * A tab for one way the connection lets a date be read out of the work tracking system. Picking an
 * entry here binds the delivery to it: from then on the name, the date and the work all come from
 * the tracker, so the only thing this tab has to be told is which entry, and the only thing that can
 * block saving is not having said. It writes down no features of its own for the same reason — the
 * server resolves those from the entry every time it syncs, and anything sent here would be ignored.
 */
const sourceSelectionTab = (
	source: IDeliverySource,
	terms: DeliverySelectionTerms,
): DeliverySelectionTab => ({
	key: `source:${source.key}`,
	label: source.displayName,
	mode: DeliverySelectionMode.SourceBound,
	source,
	premiumGate: {
		whenLocked: "explainInside",
		notice: `Taking a ${terms.deliveryTerm.toLowerCase()} date from a ${source.displayName} is a premium feature. Please upgrade your license to use this functionality.`,
	},
	claims: (delivery) =>
		isStoredAs(delivery, DeliverySelectionMode.SourceBound) &&
		delivery.sourceKey === source.key,
	hydrate: (delivery) => ({
		...emptySelectionValues(),
		sourceReference: delivery.sourceReference ?? null,
	}),
	firstBlockingError: (state) =>
		state.sourceReference === null
			? `Pick a ${source.displayName} to see the date it would set.`
			: null,
	fieldErrors: () => ({}),
	toPayload: (state) => ({
		featureIds: [],
		sourceKey: source.key,
		sourceReference: state.sourceReference ?? undefined,
	}),
});

/**
 * Every tab this Portfolio should offer. The sources come from the server, so a connection that
 * grows another one grows another tab here without a line changing.
 */
export const deliverySelectionTabsFor = (
	sources: IDeliverySource[],
	terms: DeliverySelectionTerms,
): DeliverySelectionTab[] => [
	...builtInSelectionTabs(terms),
	...sources.map((source) => sourceSelectionTab(source, terms)),
];

/**
 * Which tab a stored delivery reopens on. The tabs to search through are handed in because the ones
 * that read from the work tracking system only exist once the server has said which sources this
 * connection offers — search the built-in pair alone and a bound delivery reopens as a hand-picked
 * one, which the next save would make true.
 */
export const deliveryTabForDelivery = (
	delivery: IDelivery,
	tabs: DeliverySelectionTab[],
): DeliverySelectionTab =>
	tabs.find((tab) => tab.claims?.(delivery) === true) ??
	defaultDeliverySelectionTab;
