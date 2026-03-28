import type { IStateMapping } from "../models/Common/StateMapping";

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

function validateNoDirectStateConflicts(
	mappings: IStateMapping[],
	directStates: string[],
): string[] {
	const directLower = new Set(directStates.map((s) => s.trim().toLowerCase()));
	return mappings
		.map((m) => m.name.trim())
		.filter((name) => name && directLower.has(name.toLowerCase()))
		.map(
			(name) =>
				`Mapping name "${name}" conflicts with a directly configured state.`,
		);
}

export function validateStateMappings(
	mappings: IStateMapping[],
	directStates: string[],
): string[] {
	return [
		...validateMappingFields(mappings),
		...validateUniqueNames(mappings),
		...validateUniqueStates(mappings),
		...validateNoDirectStateConflicts(mappings, directStates),
	];
}
