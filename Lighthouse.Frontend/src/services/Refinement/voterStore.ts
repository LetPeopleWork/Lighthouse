const VOTER_STORAGE_KEY = "lighthouse:refinement:voter";

/**
 * Who votes from this browser when nobody signs in: the name they declared and the key that tells the
 * server their votes apart from everyone else's. Kept in one storage entry that this module alone reads.
 */
export interface IStoredVoter {
	name: string;
	key: string;
}

const isStoredVoter = (value: unknown): value is IStoredVoter => {
	if (typeof value !== "object" || value === null) {
		return false;
	}

	const { name, key } = value as Record<string, unknown>;
	return typeof name === "string" && typeof key === "string" && key !== "";
};

/**
 * Read fresh on every call, so a name declared in another tab is picked up. Anything unreadable -
 * no entry, storage refused, not JSON, a different shape - counts as no voter.
 */
export const readStoredVoter = (): IStoredVoter | null => {
	try {
		const stored = globalThis.localStorage.getItem(VOTER_STORAGE_KEY);
		if (stored === null) {
			return null;
		}

		const parsed: unknown = JSON.parse(stored);
		return isStoredVoter(parsed) ? parsed : null;
	} catch {
		return null;
	}
};

export const readVoterKey = (): string | null => readStoredVoter()?.key ?? null;

const VOTER_KEY_BYTES = 32;

// getRandomValues, unlike randomUUID, also exists on a page served over plain HTTP, as a LAN install is.
const aRandomVoterKey = (): string =>
	Array.from(
		globalThis.crypto.getRandomValues(new Uint8Array(VOTER_KEY_BYTES)),
		(byte) => byte.toString(16).padStart(2, "0"),
	).join("");

/**
 * Keeps the declared name, and the key this browser already holds - minting one the first time - so a
 * renamed voter's earlier votes stay theirs. A browser that refuses to store it still gets the voter
 * back, so the vote goes ahead and the caller can hold on to it for as long as the page is open.
 */
export const rememberVoter = (
	name: string,
	keyHeldByThePage: string | null = null,
): IStoredVoter => {
	const voter = {
		name,
		key: readVoterKey() ?? keyHeldByThePage ?? aRandomVoterKey(),
	};
	try {
		globalThis.localStorage.setItem(VOTER_STORAGE_KEY, JSON.stringify(voter));
	} catch {
		// Private browsing or a full storage: the name lasts only as long as this page.
	}
	return voter;
};
