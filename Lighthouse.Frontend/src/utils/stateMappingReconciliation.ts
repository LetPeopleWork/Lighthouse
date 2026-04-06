import type { IStateMapping } from "../models/Common/StateMapping";

function normalise(s: string): string {
	return s.trim().toLowerCase();
}

function indexByName(mappings: IStateMapping[]): Map<string, IStateMapping> {
	const map = new Map<string, IStateMapping>();
	for (const m of mappings) {
		const key = normalise(m.name);
		if (key) map.set(key, m);
	}
	return map;
}

function statesChanged(a: IStateMapping, b: IStateMapping): boolean {
	const ak = [...a.states].map(normalise).sort((a, b) => a.localeCompare(b));
	const bk = [...b.states].map(normalise).sort((a, b) => a.localeCompare(b));
	return ak.length !== bk.length || ak.some((v, i) => v !== bk[i]);
}

/**
 * Reconciles the Doing states list when state mappings change.
 *
 * - Added mappings: source states are removed from Doing and the group name is appended.
 * - Removed mappings: the group name is removed from Doing and source states are restored.
 * - Edited mappings (same name, different source states): old effects are undone, new effects applied.
 *
 * Comparisons are case-insensitive; original casing is preserved in the output.
 */
export function reconcileDoingStates(
	prevMappings: IStateMapping[],
	nextMappings: IStateMapping[],
	doingStates: string[],
): string[] {
	const prevByName = indexByName(prevMappings);
	const nextByName = indexByName(nextMappings);

	let result = [...doingStates];

	// Undo effects of removed or changed mappings
	for (const [key, prev] of prevByName) {
		const next = nextByName.get(key);
		if (!next || statesChanged(prev, next)) {
			// Remove mapping name from Doing
			result = result.filter((s) => normalise(s) !== key);
			// Restore source states (append in their original order)
			result = [...result, ...prev.states];
		}
	}

	// Apply effects of added or changed mappings
	for (const [key, next] of nextByName) {
		const prev = prevByName.get(key);
		if (!prev || statesChanged(prev, next)) {
			// Remove source states from Doing
			const sourceKeys = new Set(next.states.map(normalise));
			result = result.filter((s) => !sourceKeys.has(normalise(s)));
			// Append mapping name if not already present
			if (!result.some((s) => normalise(s) === key)) {
				result = [...result, next.name];
			}
		}
	}

	return result;
}
