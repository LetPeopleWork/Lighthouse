import { useState } from "react";
import type { IWorkItemRuleCondition } from "../models/WorkItemRules";

/**
 * A rule row is born empty — a field and an operator, no value yet — and only the
 * finished rows are worth storing. Holding the rows being edited here lets a
 * half-typed one stay on screen without ever reaching the settings payload, which
 * would otherwise autosave a rule nobody finished writing.
 */
export const useRuleRowDraft = (storedRules: IWorkItemRuleCondition[]) => {
	const [draft, setDraft] = useState<IWorkItemRuleCondition[] | null>(null);

	return {
		rules: draft ?? storedRules,
		trackRules: setDraft,
		discardDraft: () => setDraft(null),
	};
};
