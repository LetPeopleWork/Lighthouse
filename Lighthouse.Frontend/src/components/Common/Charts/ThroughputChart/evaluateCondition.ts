// Operator semantics here are the frontend parity-guard for the backend RuleEvaluator;
// backend tests assert JSON-shape parity but not operator semantics — change both sides together.
export type EvaluableWorkItem = {
	type: string;
	state: string;
	name: string;
	referenceId: string;
	parentReferenceId: string;
	tags: readonly string[];
	additionalFieldValues: Readonly<Record<number, string | null | undefined>>;
};

export type EvaluatorCondition = {
	fieldKey: string;
	operator: string;
	value: string;
};

const TAGS_FIELD_KEY = "workitem.tags";
const ADDITIONAL_FIELD_PREFIX = "additionalfield.";

const FIXED_FIELDS: Record<string, (item: EvaluableWorkItem) => string> = {
	"workitem.type": (item) => item.type,
	"workitem.state": (item) => item.state,
	"workitem.name": (item) => item.name,
	"workitem.referenceid": (item) => item.referenceId,
	"workitem.parentreferenceid": (item) => item.parentReferenceId,
};

const resolveAdditionalField = (
	item: EvaluableWorkItem,
	fieldKey: string,
): string | null => {
	const idPart = fieldKey.slice(ADDITIONAL_FIELD_PREFIX.length);
	const id = Number(idPart);
	if (!Number.isInteger(id) || idPart.trim() === "") {
		return null;
	}
	return item.additionalFieldValues[id] ?? "";
};

const resolveFieldValue = (
	item: EvaluableWorkItem,
	fieldKey: string,
): string | null => {
	const normalized = fieldKey.toLowerCase();
	if (normalized.startsWith(ADDITIONAL_FIELD_PREFIX)) {
		return resolveAdditionalField(item, normalized);
	}
	const resolver = FIXED_FIELDS[normalized];
	return resolver ? resolver(item) : null;
};

const equalsIgnoreCase = (left: string, right: string): boolean =>
	left.toLowerCase() === right.toLowerCase();

const containsIgnoreCase = (haystack: string, needle: string): boolean =>
	haystack.toLowerCase().includes(needle.toLowerCase());

const evaluateScalar = (
	fieldValue: string,
	operator: string,
	value: string,
): boolean => {
	switch (operator) {
		case "equals":
			return equalsIgnoreCase(fieldValue, value);
		case "notequals":
			return !equalsIgnoreCase(fieldValue, value);
		case "contains":
			return containsIgnoreCase(fieldValue, value);
		default:
			return false;
	}
};

const evaluateTags = (
	tags: readonly string[],
	operator: string,
	value: string,
): boolean => {
	switch (operator) {
		case "equals":
			return tags.some((tag) => equalsIgnoreCase(tag, value));
		case "notequals":
			return !tags.some((tag) => equalsIgnoreCase(tag, value));
		case "contains":
			return tags.some((tag) => containsIgnoreCase(tag, value));
		default:
			return false;
	}
};

export const evaluateCondition = (
	item: EvaluableWorkItem,
	condition: EvaluatorCondition,
): boolean => {
	const operator = condition.operator.toLowerCase();
	const fieldKey = condition.fieldKey.toLowerCase();
	if (fieldKey === TAGS_FIELD_KEY) {
		return evaluateTags(item.tags, operator, condition.value);
	}
	const fieldValue = resolveFieldValue(item, fieldKey);
	if (fieldValue === null) {
		return false;
	}
	return evaluateScalar(fieldValue, operator, condition.value);
};

export const matchesAllConditions = (
	item: EvaluableWorkItem,
	conditions: readonly EvaluatorCondition[],
): boolean =>
	conditions.every((condition) => evaluateCondition(item, condition));
