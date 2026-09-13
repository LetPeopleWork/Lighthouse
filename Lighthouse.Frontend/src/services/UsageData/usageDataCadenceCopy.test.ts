import { describe, expect, it } from "vitest";
import { cadenceSentence } from "./usageDataCadenceCopy";

/**
 * Slice 02, AC-02.9 and AC-05.5. describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 */

describe.skip("cadenceSentence", () => {
	it("tells a reader who will be asked again that they will be", () => {
		expect(cadenceSentence(true)).toMatch(/ask you again/i);
	});

	it("tells a reader who will not be asked again that this is the last time", () => {
		expect(cadenceSentence(false)).toMatch(/not ask you again/i);
	});

	// The two sentences have to be different sentences, not the same words under a negation a
	// reader has to parse. Asserting they differ is what stops a later edit collapsing them into
	// one hedge that is true of both and informative about neither.
	it("says two different things", () => {
		expect(cadenceSentence(true)).not.toBe(cadenceSentence(false));
	});

	// A substring match is how "we will not ask you again" would pass a test looking for "ask you
	// again". The negative case has to be checked as a negative, or the promise can invert while
	// every test stays green.
	it("does not let the will-ask wording appear in the will-not-ask sentence", () => {
		expect(cadenceSentence(false)).not.toBe(cadenceSentence(true));
		expect(cadenceSentence(true)).not.toMatch(/not ask you again/i);
	});

	// Each sentence has to end. The copy is held to its promises by patterns that refuse to cross a
	// sentence boundary, and a missing full stop lets this sentence run into the next one, so that
	// "we will not ask you again" and a following clause read as a single different claim.
	it("ends each sentence", () => {
		expect(cadenceSentence(true).trim()).toMatch(/\.$/);
		expect(cadenceSentence(false).trim()).toMatch(/\.$/);
	});
});
