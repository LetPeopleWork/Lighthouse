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

export const readAskedMarker = (): string | null => {
	throw new Error("readAskedMarker is not implemented — RED scaffold.");
};

export const writeAskedMarker = (askedAt: string): void => {
	throw new Error(
		`writeAskedMarker is not implemented — RED scaffold. Asked to record ${askedAt}.`,
	);
};
