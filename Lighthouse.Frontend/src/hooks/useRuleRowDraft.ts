import { useState } from "react";
import type { DeliveryRuleGroupMode } from "../components/Common/DeliveryRuleBuilder/types";
import type { IWorkItemRuleCondition } from "../models/WorkItemRules";

/**
 * A rule row is born empty — a field and an operator, no value yet — and only the
 * finished rows are worth storing. Holding the rows being edited here lets a
 * half-typed one stay on screen without ever reaching the settings payload, which
 * would otherwise autosave a rule nobody finished writing.
 *
 * The match mode is held alongside them because it has nowhere else to live while no
 * rule is complete: an empty rule set is stored as null, which reads back as AND, so a
 * chosen OR would be forgotten between clearing the last value and typing the next one.
 */
export const useRuleRowDraft = (
	storedRules: IWorkItemRuleCondition[],
	storedMode: DeliveryRuleGroupMode,
) => {
	const [draft, setDraft] = useState<IWorkItemRuleCondition[] | null>(null);
	const [draftMode, setDraftMode] = useState<DeliveryRuleGroupMode | null>(
		null,
	);

	return {
		rules: draft ?? storedRules,
		mode: draftMode ?? storedMode,
		trackRules: setDraft,
		trackMode: setDraftMode,
		discardDraft: () => {
			setDraft(null);
			setDraftMode(null);
		},
	};
};
