import type { IStateMapping } from "../models/Common/StateMapping";

// The characters .NET's string.Trim removes. JavaScript's own trim misses U+0085 and also removes U+FEFF,
// which the server keeps.
const SERVER_WHITESPACE =
	"\\u0009-\\u000D\\u0020\\u0085\\u00A0\\u1680\\u2000-\\u200A\\u2028\\u2029\\u202F\\u205F\\u3000";
const SURROUNDING_WHITESPACE = new RegExp(
	`^[${SERVER_WHITESPACE}]+|[${SERVER_WHITESPACE}]+$`,
	"g",
);

// The server's case-insensitive comparison leaves these two alone, although Unicode upper-cases them to I and S.
const KEPT_AS_IS = new Set(["ı", "ſ"]);

function upperOneToOne(character: string): string {
	if (KEPT_AS_IS.has(character)) return character;
	const upper = character.toUpperCase();
	return [...upper].length === 1 ? upper : character;
}

/**
 * Folds a name or a state the way the server compares them, so both sides agree on what is the same:
 * trimmed the way the server trims, then upper-cased one character at a time as the server does.
 */
export function foldKey(value: string): string {
	return [...value.replace(SURROUNDING_WHITESPACE, "")]
		.map(upperOneToOne)
		.join("");
}

/** True for a name of nothing but whitespace, in the server's sense of whitespace. */
export function isBlankName(value: string): boolean {
	return foldKey(value) === "";
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

function displayNameOf(rawState: string, mappings: IStateMapping[]): string {
	const key = foldKey(rawState);
	const group = mappings.find((mapping) =>
		mapping.states.some((state) => foldKey(state) === key),
	);
	return group?.name ?? rawState;
}

/**
 * A list of raw states as the charts show it: each state replaced by its mapping's name, the group at the
 * position of its first listed state, no name twice.
 */
export function displayStatesFor(
	rawStates: string[],
	mappings: IStateMapping[],
): string[] {
	const listed = withoutRepeats(
		rawStates.filter((state) => !isBlankName(state)),
	);
	return withoutRepeats(listed.map((state) => displayNameOf(state, mappings)));
}
