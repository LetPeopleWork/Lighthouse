import type { IStateMapping } from "../models/Common/StateMapping";
import { isApplied, wouldShadow } from "./stateMappingReconciliation";

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

// A mapping already taken into Doing legitimately appears there under its own
// name, so only a mapping whose source states are still listed is a clash.
export function shadowedDoingStateErrors(
	mappings: IStateMapping[],
	doingStates: string[],
): string[] {
	return mappings
		.filter(
			(m) =>
				m.states.length > 0 &&
				wouldShadow(m, doingStates) &&
				!isApplied(m, doingStates),
		)
		.map((m) => {
			const name = m.name.trim();
			const doingState = doingStates.find(
				(s) => s.trim().toLowerCase() === name.toLowerCase(),
			);
			return `Mapping "${name}" has the same name as the Doing state "${doingState}". Add "${name}" to this mapping to group it, or choose another name.`;
		});
}

export function validateStateMappings(
	mappings: IStateMapping[],
	doingStates: string[],
): string[] {
	return [
		...validateMappingFields(mappings),
		...validateUniqueNames(mappings),
		...validateUniqueStates(mappings),
		...shadowedDoingStateErrors(mappings, doingStates),
	];
}
