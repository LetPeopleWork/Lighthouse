import type { IStateMapping } from "../models/Common/StateMapping";

export function normalise(s: string): string {
	return s.trim().toLowerCase();
}

export function listedAs(states: string[], name: string): string | undefined {
	return states.find((s) => normalise(s) === normalise(name));
}

/**
 * The other mapping that groups a state equal to this mapping's name, if any.
 * Such a name is never taken into Doing: it would stand for the other
 * mapping's state and hide the state it replaces.
 */
export function groupingMappingOf(
	mapping: IStateMapping,
	mappings: IStateMapping[],
): IStateMapping | undefined {
	return mappings.find(
		(other) => other !== mapping && listedAs(other.states, mapping.name),
	);
}

function indexByName(mappings: IStateMapping[]): Map<string, IStateMapping> {
	const map = new Map<string, IStateMapping>();
	for (const m of mappings) {
		const key = normalise(m.name);
		if (key) map.set(key, m);
	}
	return map;
}

/**
 * True when the mapping's name is already a Doing state the mapping does not
 * itself contain. The backend expands a Doing entry equal to a mapping name
 * into that mapping's states, so such a state would be silently dropped.
 */
export function wouldShadow(m: IStateMapping, doing: string[]): boolean {
	const key = normalise(m.name);
	if (!key) return false;
	const inDoing = doing.some((s) => normalise(s) === key);
	return inDoing && !m.states.some((s) => normalise(s) === key);
}

/**
 * True when another mapping in the list groups one of this mapping's states.
 * Neither mapping can then be taken into Doing: both would claim the state.
 */
export function sharesStateWithAnother(
	m: IStateMapping,
	mappings: IStateMapping[],
): boolean {
	return mappings.some(
		(other) =>
			other !== m &&
			m.states.some((s) => listedAs(other.states, s) !== undefined),
	);
}

/**
 * True when the mapping, judged within its mapping list, has been taken into
 * the Doing list: it shares no state with another mapping, has a name and
 * states, its name is listed there, and none of its states (other than one
 * equal to its own name) still is.
 */
export function isApplied(
	m: IStateMapping,
	doing: string[],
	mappings: IStateMapping[],
): boolean {
	const key = normalise(m.name);
	if (!key || m.states.length === 0) return false;
	if (sharesStateWithAnother(m, mappings)) return false;
	const doingKeys = new Set(doing.map(normalise));
	if (!doingKeys.has(key)) return false;
	return m.states.every((s) => {
		const stateKey = normalise(s);
		return stateKey === key || !doingKeys.has(stateKey);
	});
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

	for (const [key, prev] of prevByName) {
		const next = nextByName.get(key);
		if (
			(!next || statesChanged(prev, next)) &&
			isApplied(prev, doingStates, prevMappings)
		) {
			result = result.filter((s) => normalise(s) !== key);
			result = [...result, ...prev.states];
		}
	}

	for (const [key, next] of nextByName) {
		const prev = prevByName.get(key);
		if (
			next.states.length === 0 ||
			wouldShadow(next, result) ||
			groupingMappingOf(next, nextMappings) ||
			sharesStateWithAnother(next, nextMappings)
		)
			continue;
		// A mapping that was left out because it shared a state is taken in
		// once the sharing ends, although its own definition did not change.
		if (
			!prev ||
			statesChanged(prev, next) ||
			sharesStateWithAnother(prev, prevMappings)
		) {
			const sourceKeys = new Set(next.states.map(normalise));
			result = result.filter((s) => !sourceKeys.has(normalise(s)));
			if (!result.some((s) => normalise(s) === key)) {
				result = [...result, next.name.trim()];
			}
		}
	}

	return result;
}
