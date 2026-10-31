import type { IStateMapping } from "../models/Common/StateMapping";
import {
	groupingMappingOf,
	isApplied,
	listedAs,
	normalise,
	wouldShadow,
} from "./stateMappingReconciliation";

export interface IStateLists {
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
}

function validateMappingFields(mappings: IStateMapping[]): string[] {
	return mappings.flatMap((mapping, i) => {
		const errors: string[] = [];
		if (!mapping.name.trim()) {
			errors.push(`Mapping at position ${i + 1} has an empty name.`);
		}
		if (mapping.states.length === 0) {
			const label = mapping.name.trim() || `at position ${i + 1}`;
			errors.push(`Mapping "${label}" has no source states.`);
		}
		return errors;
	});
}

function validateUniqueNames(mappings: IStateMapping[]): string[] {
	const seen = new Map<string, string>();
	const errors: string[] = [];
	for (const mapping of mappings) {
		const name = mapping.name.trim();
		if (!name) continue;
		const lower = name.toLowerCase();
		if (seen.has(lower)) {
			errors.push(`Duplicate mapping name: "${name}".`);
		} else {
			seen.set(lower, name);
		}
	}
	return errors;
}

function validateUniqueStates(mappings: IStateMapping[]): string[] {
	const seen = new Map<string, string>();
	const errors: string[] = [];
	for (const mapping of mappings) {
		for (const state of mapping.states) {
			const lower = state.trim().toLowerCase();
			if (seen.has(lower)) {
				errors.push(`Source state "${state}" is used in multiple mappings.`);
			} else {
				seen.set(lower, state);
			}
		}
	}
	return errors;
}

function namesAStateOfAnotherMapping(
	mapping: IStateMapping,
	mappings: IStateMapping[],
): boolean {
	return mappings.some(
		(other) => other !== mapping && listedAs(mapping.states, other.name),
	);
}

// A mapping already taken into Doing legitimately appears there under its own
// name, so only a mapping whose source states are still listed is a clash. A
// mapping that itself groups another mapping's name is not reported here: the
// other mapping's own error already says to rename it.
function doingClashError(
	mapping: IStateMapping,
	mappings: IStateMapping[],
	doingStates: string[],
): string[] {
	if (
		!wouldShadow(mapping, doingStates) ||
		isApplied(mapping, doingStates) ||
		namesAStateOfAnotherMapping(mapping, mappings)
	) {
		return [];
	}
	const name = mapping.name.trim();
	return [
		`Mapping "${name}" has the same name as the Doing state "${listedAs(doingStates, name)}". Add "${name}" to this mapping to group it, or choose another name.`,
	];
}

function listClashError(
	mapping: IStateMapping,
	listLabel: string,
	states: string[],
): string[] {
	const listed = listedAs(states, mapping.name);
	return listed === undefined
		? []
		: [
				`Mapping "${mapping.name.trim()}" has the same name as the ${listLabel} state "${listed}". Choose another name.`,
			];
}

function groupedElsewhereError(
	mapping: IStateMapping,
	mappings: IStateMapping[],
): string[] {
	const grouping = groupingMappingOf(mapping, mappings);
	return grouping
		? [
				`Mapping "${mapping.name.trim()}" has the same name as a state grouped by mapping "${grouping.name.trim()}". Choose another name.`,
			]
		: [];
}

export function stateNameClashErrors(
	mappings: IStateMapping[],
	{ toDoStates, doingStates, doneStates }: IStateLists,
): string[] {
	return mappings
		.filter((m) => m.states.length > 0 && normalise(m.name) !== "")
		.flatMap((m) => [
			...doingClashError(m, mappings, doingStates),
			...listClashError(m, "To Do", toDoStates),
			...listClashError(m, "Done", doneStates),
			...groupedElsewhereError(m, mappings),
		]);
}

export function validateStateMappings(
	mappings: IStateMapping[],
	lists: IStateLists,
): string[] {
	return [
		...validateMappingFields(mappings),
		...validateUniqueNames(mappings),
		...validateUniqueStates(mappings),
		...stateNameClashErrors(mappings, lists),
	];
}
