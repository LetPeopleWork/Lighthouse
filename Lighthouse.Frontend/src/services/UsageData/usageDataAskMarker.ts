/**
 * Where this browser remembers that it has already been shown the usage data dialog unprompted.
 *
 * RED scaffold (DISTILL). DELIVER replaces the bodies.
 *
 * This is the one piece of the cadence the server cannot hold. A browser that closes the dialog
 * without answering has no consent row, so there is nothing on the server to write "we asked this
 * one" against - and asking again on every visit is the nag the whole slice exists to avoid.
 *
 * Unlike the consent token, what is stored here records something Lighthouse did rather than
 * something the reader chose. That difference matters legally and is unresolved: see the open item
 * against DoR-9 in the feature delta.
 */

/**
 * Its own key, deliberately separate from the consent token beside it. The token is the only handle
 * on a consent record and can never be reissued, so anything sharing a key with it is one bug away
 * from taking somebody's ability to withdraw.
 */
const ASKED_STORAGE_KEY = "lighthouse:usagedata:asked";

export const readAskedMarker = (): string | null => {
	try {
		return localStorage.getItem(ASKED_STORAGE_KEY);
	} catch {
		// Private windows and locked-down browsers throw rather than returning null. A browser that
		// cannot remember being asked is simply a browser that gets asked again, which is a worse
		// experience and not a broken one.
		return null;
	}
};

export const writeAskedMarker = (askedAt: string): void => {
	try {
		localStorage.setItem(ASKED_STORAGE_KEY, askedAt);
	} catch {
		// Same browsers, same conclusion. Failing the interaction over a note to ourselves would put
		// an error in front of somebody who is only being shown a dialog.
	}
};
