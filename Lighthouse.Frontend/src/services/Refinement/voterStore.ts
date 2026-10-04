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
