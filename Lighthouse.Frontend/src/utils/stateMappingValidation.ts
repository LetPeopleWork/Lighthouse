import type { IStateMapping } from "../models/Common/StateMapping";

export function validateStateMappings(
	mappings: IStateMapping[],
	directStates: string[],
): string[] {
	const errors: string[] = [];

	for (let i = 0; i < mappings.length; i++) {
		const mapping = mappings[i];

		if (!mapping.name.trim()) {
			errors.push(`Mapping at position ${i + 1} has an empty name.`);
		}

		if (mapping.states.length === 0) {
			const label = mapping.name.trim() || `at position ${i + 1}`;
			errors.push(`Mapping "${label}" has no source states.`);
		}
	}

	// Check for duplicate names (case-insensitive)
	const seenNames = new Map<string, string>();
	for (const mapping of mappings) {
		if (!mapping.name.trim()) continue;
		const lower = mapping.name.trim().toLowerCase();
		if (seenNames.has(lower)) {
			errors.push(`Duplicate mapping name: "${mapping.name.trim()}".`);
		} else {
			seenNames.set(lower, mapping.name.trim());
		}
	}

	// Check for overlapping source states across mappings
	const seenStates = new Map<string, string>();
	for (const mapping of mappings) {
		for (const state of mapping.states) {
			const lower = state.trim().toLowerCase();
			if (seenStates.has(lower)) {
				errors.push(`Source state "${state}" is used in multiple mappings.`);
			} else {
				seenStates.set(lower, state);
			}
		}
	}

	// Check for mapping name colliding with a direct state
	const directStatesLower = new Set(
		directStates.map((s) => s.trim().toLowerCase()),
	);
	for (const mapping of mappings) {
		if (!mapping.name.trim()) continue;
		if (directStatesLower.has(mapping.name.trim().toLowerCase())) {
			errors.push(
				`Mapping name "${mapping.name.trim()}" conflicts with a directly configured state.`,
			);
		}
	}

	return errors;
}
