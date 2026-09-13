/**
 * What the dialog tells a reader about whether the question will come back.
 *
 * RED scaffold (DISTILL). DELIVER replaces the body.
 *
 * The sentence and the behaviour are one promise, which is why the same boolean the server uses to
 * decide whether this browser gets asked again is the one that picks these words. A dialog that
 * says "we will not ask again" and reappears in ninety days has not been slightly untidy - it has
 * broken a promise on the one screen where promises are the entire product.
 *
 * It is a boolean rather than a licence tier on purpose: the state endpoint answers anonymously,
 * and the tier it derives this from is not something an unauthenticated caller may learn.
 */

export const cadenceSentence = (willAskAgain: boolean): string => {
	// Each sentence ends in a full stop, and that is load-bearing rather than tidy: the dialog's
	// copy is held to its promises by patterns that refuse to cross a sentence boundary, so a
	// missing stop lets this run into whatever follows and the two read as one different claim.
	return willAskAgain
		? "If you say no, we will ask you again in a few months."
		: "If you say no, we will not ask you again.";
};
