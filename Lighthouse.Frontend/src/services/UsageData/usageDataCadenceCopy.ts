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
	throw new Error(
		`cadenceSentence is not implemented — RED scaffold. Asked for willAskAgain=${willAskAgain}.`,
	);
};
