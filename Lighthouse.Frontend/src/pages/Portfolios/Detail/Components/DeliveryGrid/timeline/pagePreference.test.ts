import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { pagePreference } from "./pagePreference";

// One preference, on its own. What it does with a value it does not recognise, with storage that
// refuses it, and with a reader who has stopped listening are questions about the store rather
// than about any chart drawn from it - and a component's own tests cannot reach most of them,
// because by the time it has rendered the answer has already been decided.

const A_KEY = "lighthouse:test:aPreference";
const ANOTHER_KEY = "lighthouse:test:anotherPreference";

/**
 * Breaks one of storage's own methods, and puts it back afterwards.
 *
 * Two things about this are the reason it exists rather than being written inline at each site.
 *
 * **It spies on the object, not on `Storage.prototype`.** In this environment `localStorage` does
 * not inherit from it - `getItem` and `setItem` are its own properties - so a prototype spy
 * installs cleanly, reports itself as installed, and intercepts nothing at all. Every
 * blocked-storage test in this folder was written that way once and passed without ever reaching
 * the guard it was named for.
 *
 * **And it is undone by hand.** `vi.restoreAllMocks()` does not put these back, so a broken
 * `setItem` left standing makes the *next* test's fixture throw while it is arranging itself -
 * which surfaces as a failure in a test that has nothing to do with storage.
 */
const brokenStorage: { mockRestore: () => void }[] = [];

const breakStorage = (method: "getItem" | "setItem") => {
	const spy = vi.spyOn(localStorage, method).mockImplementation(() => {
		throw new Error("site data is blocked");
	});

	brokenStorage.push(spy);

	return spy;
};

afterEach(() => {
	for (const spy of brokenStorage.splice(0)) {
		spy.mockRestore();
	}
});

beforeEach(() => {
	localStorage.clear();
	vi.restoreAllMocks();
});

describe("what the page is showing", () => {
	it("shows nothing to a reader who has never answered", () => {
		expect(pagePreference(A_KEY).read()).toBe(false);
	});

	it("shows what the reader last chose", () => {
		localStorage.setItem(A_KEY, "true");

		expect(pagePreference(A_KEY).read()).toBe(true);
	});

	it("reads the stored word rather than trusting that anything is there", () => {
		// `localStorage` hands back text, and the text "false" is truthy. Coerced rather than
		// compared, the preference turns itself on and can never be turned off again.
		localStorage.setItem(A_KEY, "false");

		expect(pagePreference(A_KEY).read()).toBe(false);
	});

	it("shows nothing for a stored value it does not recognise", () => {
		localStorage.setItem(A_KEY, "yes");

		expect(pagePreference(A_KEY).read()).toBe(false);
	});

	it("keeps the reader's answer for this visit when storage will not take it", () => {
		// Private browsing and blocked site data make the write throw. The reader loses the memory
		// of the choice, not the view they asked for.
		const preference = pagePreference(A_KEY);

		breakStorage("setItem");

		preference.set(true);

		// Both halves: the view the reader asked for, and the memory they did not get. Without the
		// second, this says nothing a store that wrote successfully would not also satisfy.
		expect(preference.read()).toBe(true);
		expect(localStorage.getItem(A_KEY)).toBeNull();
	});

	it("shows nothing, rather than throwing, when storage cannot be read at all", () => {
		// Unguarded, this throw comes out through the hook and takes the whole Portfolio
		// accordion down with it.
		//
		// **The choice is stored as on before the read is broken, and that is the whole test.**
		// Asserting `false` against an *absent* key cannot tell a caught throw from nothing being
		// there, because both answer `false` - which is how two tests named for this guard passed
		// while the guard was never entered at all. With `"true"` stored, a read that got through
		// answers `true` and a read that was caught answers `false`, so the two outcomes are
		// distinguishable and the assertion has something to say.
		localStorage.setItem(A_KEY, "true");

		const preference = pagePreference(A_KEY);

		// The fixture proving itself: without the throw, this answers the other way.
		expect(preference.read()).toBe(true);
		preference.forget();

		breakStorage("getItem");

		expect(preference.read()).toBe(false);
	});

	it("remembers the answer under the key it was given", () => {
		// Pinned against the key itself, once. A renamed key silently forgets every reader's
		// choice while every round trip through the store keeps passing.
		pagePreference(A_KEY).set(true);

		expect(localStorage.getItem(A_KEY)).toBe("true");
	});
});

describe("one preference is not another", () => {
	it("leaves the others where they were", () => {
		// The reason this is a factory rather than a module holding one remembered value: three
		// switches sharing one variable move together, which reads as a decision rather than as a
		// fault. A closure per call is what keeps them apart, and nothing else does.
		const one = pagePreference(A_KEY);
		const other = pagePreference(ANOTHER_KEY);

		one.set(true);

		expect(one.read()).toBe(true);
		expect(other.read()).toBe(false);
		expect(localStorage.getItem(ANOTHER_KEY)).toBeNull();
	});

	it("tells only its own readers", () => {
		const mine = vi.fn();
		const theirs = vi.fn();

		const one = pagePreference(A_KEY);
		const other = pagePreference(ANOTHER_KEY);

		const stopMine = one.subscribe(mine);
		const stopTheirs = other.subscribe(theirs);

		one.set(true);

		// Paired, so this cannot pass against a store that tells nobody anything.
		expect(mine).toHaveBeenCalledTimes(1);
		expect(theirs).not.toHaveBeenCalled();

		stopMine();
		stopTheirs();
	});
});

describe("telling the readers", () => {
	it("tells everyone still listening when the answer moves", () => {
		const first = vi.fn();
		const second = vi.fn();
		const preference = pagePreference(A_KEY);

		const stopFirst = preference.subscribe(first);
		const stopSecond = preference.subscribe(second);

		preference.set(true);

		expect(first).toHaveBeenCalledTimes(1);
		expect(second).toHaveBeenCalledTimes(1);

		stopFirst();
		stopSecond();
	});

	it("stops telling a reader who has gone", () => {
		// The Delivery accordions open and close all day. An unsubscribe that does not remove the
		// listener leaves one behind on every close, and nothing ever notices.
		const gone = vi.fn();
		const staying = vi.fn();
		const preference = pagePreference(A_KEY);

		const stopListening = preference.subscribe(gone);
		const stopStaying = preference.subscribe(staying);

		stopListening();
		preference.set(true);

		expect(gone).not.toHaveBeenCalled();
		// Paired, so this cannot pass against a store that tells nobody anything.
		expect(staying).toHaveBeenCalledTimes(1);

		stopStaying();
	});
});
