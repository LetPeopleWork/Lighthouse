import type { IStateMapping } from "../models/Common/StateMapping";

export const __SCAFFOLD__ = true;

/**
 * Folds a name or a state the way the server compares them, so both sides agree on what is the same:
 * trimmed the way the server trims, then upper-cased one character at a time as the server does.
 */
export function foldKey(value: string): string {
	throw new Error(`Not yet implemented -- RED scaffold (${value})`);
}

/** True for a name of nothing but whitespace, in the server's sense of whitespace. */
export function isBlankName(value: string): boolean {
	throw new Error(`Not yet implemented -- RED scaffold (${value})`);
}

/**
 * A list of raw states as the charts show it: each state replaced by its mapping's name, the group at the
 * position of its first listed state, no name twice.
 */
export function displayStatesFor(
	rawStates: string[],
	mappings: IStateMapping[],
): string[] {
	throw new Error(
		`Not yet implemented -- RED scaffold (${rawStates.length}, ${mappings.length})`,
	);
}
