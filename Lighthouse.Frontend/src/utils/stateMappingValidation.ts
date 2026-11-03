import type { IStateMapping } from "../models/Common/StateMapping";
import { foldKey, isBlankName, trimAsServer } from "./stateMappingView";

export interface IStateLists {
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
}

type CategoryLabel = "To Do" | "Doing" | "Done";

type ListEntryKind = "state" | "ownState" | "shadowing" | "collapsed";

const ONE_CATEGORY_ADVICE = "Keep a mapping's states in one category.";

function categoriesOf({
	toDoStates,
	doingStates,
	doneStates,
}: IStateLists): { label: CategoryLabel; states: string[] }[] {
	return [
		{ label: "To Do", states: toDoStates },
		{ label: "Doing", states: doingStates },
		{ label: "Done", states: doneStates },
	];
}

function isSameState(left: string, right: string): boolean {
	return foldKey(left) === foldKey(right);
}

function listedAs(states: string[], name: string): string | undefined {
	return states.find((state) => isSameState(state, name));
}

function membersOf(mapping: IStateMapping): string[] {
	return mapping.states.filter((state) => !isBlankName(state));
}

function nameOf(mapping: IStateMapping): string {
	return trimAsServer(mapping.name);
}

function withoutRepeats(states: string[]): string[] {
	const seen = new Set<string>();
	return states.filter((state) => {
		const key = foldKey(state);
		if (seen.has(key)) return false;
		seen.add(key);
		return true;
	});
}

function validateMappingFields(mappings: IStateMapping[]): string[] {
	return mappings.flatMap((mapping, i) => {
		const errors: string[] = [];
		if (isBlankName(mapping.name)) {
			errors.push(`Mapping at position ${i + 1} has an empty name.`);
		}
		if (mapping.states.length === 0) {
			const label = nameOf(mapping) || `at position ${i + 1}`;
			errors.push(`Mapping "${label}" has no source states.`);
		}
		return errors;
	});
}

function validateUniqueNames(mappings: IStateMapping[]): string[] {
	const seen = new Set<string>();
	const errors: string[] = [];
	for (const mapping of mappings) {
		const key = foldKey(mapping.name);
		if (!key) continue;
		if (seen.has(key)) {
			errors.push(`Duplicate mapping name: "${nameOf(mapping)}".`);
		} else {
			seen.add(key);
		}
	}
	return errors;
}

function validateUniqueStates(mappings: IStateMapping[]): string[] {
	const seen = new Set<string>();
	const errors: string[] = [];
	for (const mapping of mappings) {
		for (const state of mapping.states) {
			const key = foldKey(state);
			if (seen.has(key)) {
				errors.push(`Source state "${state}" is used in multiple mappings.`);
			} else {
				seen.add(key);
			}
		}
	}
	return errors;
}

/** The other mapping that groups a state equal to this mapping's name, if any. */
function groupingMappingOf(
	mapping: IStateMapping,
	mappings: IStateMapping[],
): IStateMapping | undefined {
	return mappings.find(
		(other) =>
			other !== mapping &&
			listedAs(membersOf(other), mapping.name) !== undefined,
	);
}

function groupNamed(
	entry: string,
	mappings: IStateMapping[],
): IStateMapping | undefined {
	return mappings.find(
		(mapping) =>
			isSameState(mapping.name, entry) && membersOf(mapping).length > 0,
	);
}

// How the server reads a list entry: a name that is one of its mapping's own states, or that another mapping
// groups, is a state; any other mapping name stands for the mapping's states, and hides them when one of them
// is listed beside it.
function classifyListEntry(
	entry: string,
	states: string[],
	mappings: IStateMapping[],
): ListEntryKind {
	const group = groupNamed(entry, mappings);
	if (!group) return "state";
	const members = membersOf(group);
	if (listedAs(members, entry) !== undefined) return "ownState";
	if (groupingMappingOf(group, mappings)) return "state";
	return members.some((state) => listedAs(states, state) !== undefined)
		? "shadowing"
		: "collapsed";
}

function expandListEntry(
	entry: string,
	states: string[],
	mappings: IStateMapping[],
): string[] {
	const group = groupNamed(entry, mappings);
	const kind = classifyListEntry(entry, states, mappings);
	if (!group || kind === "state") return [entry];
	const members = membersOf(group);
	return kind === "ownState"
		? [
				entry,
				...members.filter((state) => listedAs(states, state) === undefined),
			]
		: members;
}

/** A list with every mapping name replaced by the states it stands for, as the server reads it. */
function normalisedStatesFor(
	states: string[],
	mappings: IStateMapping[],
): string[] {
	const entries = states.filter((entry) => !isBlankName(entry));
	return withoutRepeats(
		entries.flatMap((entry) => expandListEntry(entry, entries, mappings)),
	);
}

function listClashError(
	mapping: IStateMapping,
	mappings: IStateMapping[],
	label: CategoryLabel,
	states: string[],
): string[] {
	const listed = listedAs(states, mapping.name);
	return listed !== undefined &&
		classifyListEntry(listed, states, mappings) === "shadowing"
		? [shadowingSentence(nameOf(mapping), label, trimAsServer(listed))]
		: [];
}

// In Doing the user may have meant to group the state too, so the sentence offers that fix there.
function shadowingSentence(
	name: string,
	label: CategoryLabel,
	state: string,
): string {
	return label === "Doing"
		? `Mapping "${name}" has the same name as the Doing state "${state}". Add "${name}" to this mapping to group it, or choose another name.`
		: `Mapping "${name}" has the same name as the ${label} state "${state}". Choose another name.`;
}

function groupedElsewhereError(
	mapping: IStateMapping,
	mappings: IStateMapping[],
): string[] {
	const grouping = groupingMappingOf(mapping, mappings);
	return grouping
		? [
				`Mapping "${nameOf(mapping)}" has the same name as a state grouped by mapping "${nameOf(grouping)}". Choose another name.`,
			]
		: [];
}

function listedCategories(labels: CategoryLabel[]): string {
	return labels.length === 2
		? `both ${labels[0]} and ${labels[1]}`
		: `${labels.slice(0, -1).join(", ")} and ${labels[labels.length - 1]}`;
}

function categoryErrors(
	mapping: IStateMapping,
	mappings: IStateMapping[],
	lists: IStateLists,
): string[] {
	const categories = categoriesOf(lists).map(({ label, states }) => ({
		label,
		states: normalisedStatesFor(states, mappings),
	}));
	const placed = membersOf(mapping)
		.map((state) => ({
			state: trimAsServer(state),
			labels: categories
				.filter(({ states }) => listedAs(states, state) !== undefined)
				.map(({ label }) => label),
		}))
		.filter(({ labels }) => labels.length > 0);

	const name = nameOf(mapping);
	const inSeveral = placed.find(({ labels }) => labels.length > 1);
	if (inSeveral) {
		return [
			`Mapping "${name}" groups "${inSeveral.state}", which is in ${listedCategories(inSeveral.labels)}. ${ONE_CATEGORY_ADVICE}`,
		];
	}

	const [first] = placed;
	const other = placed.find(({ labels }) => labels[0] !== first?.labels[0]);
	if (!first || !other) return [];

	return [
		`Mapping "${name}" groups states from different categories: "${first.state}" is in ${first.labels[0]} and "${other.state}" is in ${other.labels[0]}. ${ONE_CATEGORY_ADVICE}`,
	];
}

function stateNameClashErrors(
	mappings: IStateMapping[],
	lists: IStateLists,
): string[] {
	return mappings
		.filter((m) => membersOf(m).length > 0 && !isBlankName(m.name))
		.flatMap((m) => [
			...listClashError(m, mappings, "Doing", lists.doingStates),
			...listClashError(m, mappings, "To Do", lists.toDoStates),
			...listClashError(m, mappings, "Done", lists.doneStates),
			...groupedElsewhereError(m, mappings),
			...categoryErrors(m, mappings, lists),
		]);
}

function mappingRuleErrors(
	mappings: IStateMapping[],
	lists: IStateLists,
): string[] {
	return [
		...validateUniqueNames(mappings),
		...validateUniqueStates(mappings),
		...stateNameClashErrors(mappings, lists),
	];
}

export function validateStateMappings(
	mappings: IStateMapping[],
	lists: IStateLists,
): string[] {
	return [
		...validateMappingFields(mappings),
		...mappingRuleErrors(mappings, lists),
	];
}

// Rows still being filled in are never saved, so only complete mappings can
// make the server reject a save.
export function mappingAutoSaveBlockers(
	mappings: IStateMapping[],
	lists: IStateLists,
): string[] {
	return mappingRuleErrors(withoutIncompleteMappings(mappings), lists);
}

export function withoutIncompleteMappings(
	mappings: IStateMapping[],
): IStateMapping[] {
	return mappings.filter((m) => !isBlankName(m.name) && m.states.length > 0);
}
