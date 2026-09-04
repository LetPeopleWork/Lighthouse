import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";

const TOKEN = /\{\{(\w+)\}\}/g;

const CONFIGURABLE_TERMS: ReadonlySet<string> = new Set(
	Object.values(TERMINOLOGY_KEYS),
);

export const resolveTerms = (
	text: string,
	getTerm: (key: string) => string,
): string =>
	text.replace(TOKEN, (token: string, key: string) =>
		// A token naming nothing the product has a word for is left on screen exactly as written, so a
		// typo is something a reader trips over. Handing it to the lookup instead would print the
		// misspelling as ordinary prose, and removing it would delete a word from the sentence - both
		// read as bad copy rather than as a broken key.
		CONFIGURABLE_TERMS.has(key) ? getTerm(key) : token,
	);
