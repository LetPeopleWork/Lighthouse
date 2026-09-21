import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { pageChoice } from "./pageChoice";

// One choice, on its own. What it does with a value it does not recognise, with storage that
// refuses it, and with a reader who has stopped listening are questions about the store rather
// than about any chart drawn from it - and a component's own tests cannot reach most of them,
// because by the time it has rendered the answer has already been decided.

const A_KEY = "lighthouse:test:aChoice";
const ANOTHER_KEY = "lighthouse:test:anotherChoice";

const OFFERED = ["none", "teams", "status"] as const;
type Offered = (typeof OFFERED)[number];

const aChoice = (key = A_KEY) => pageChoice<Offered>(key, OFFERED, "status");

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
	it("starts a reader who has never answered where the caller said to", () => {
		expect(aChoice().read()).toBe("status");
	});

	it("shows what the reader last chose", () => {
		localStorage.setItem(A_KEY, "teams");

		expect(aChoice().read()).toBe("teams");
	});

	it("falls back for a stored value this version does not offer", () => {
		// Storage outlives the code that wrote it. A choice that is renamed or withdrawn leaves
		// readers holding a word this version does not know, and handing it back selects nothing
		// at all - a control with no button pressed, on a chart showing something.
		localStorage.setItem(A_KEY, "sub-lanes");

		expect(aChoice().read()).toBe("status");
	});

	it("keeps the reader's answer for this visit when storage will not take it", () => {
		// Private browsing and blocked site data make the write throw. The reader loses the memory
		// of the choice, not the view they asked for.
		const choice = aChoice();

		breakStorage("setItem");

		choice.set("teams");

		// Both halves: the view the reader asked for, and the memory they did not get. Without the
		// second, this says nothing a store that wrote successfully would not also satisfy.
		expect(choice.read()).toBe("teams");
		expect(localStorage.getItem(A_KEY)).toBeNull();
	});

	it("falls back, rather than throwing, when storage cannot be read at all", () => {
		// Unguarded, this throw comes out through the hook and takes the whole Portfolio
		// accordion down with it.
		//
		// **A value the fallback is not is stored before the read is broken, and that is the whole
		// test.** Asserting the fallback against an *absent* key cannot tell a caught throw from
		// nothing being there, because both answer the same - which is how two tests named for
		// this guard once passed while the guard was never entered.
		localStorage.setItem(A_KEY, "teams");

		const choice = aChoice();

		// The fixture proving itself: without the throw, this answers the other way.
		expect(choice.read()).toBe("teams");
		choice.forget();

		breakStorage("getItem");

		expect(choice.read()).toBe("status");
	});

	it("remembers the answer under the key it was given", () => {
		// Pinned against the key itself, once. A renamed key silently forgets every reader's
		// choice while every round trip through the store keeps passing.
		aChoice().set("teams");

		expect(localStorage.getItem(A_KEY)).toBe("teams");
	});
});

describe("one choice is not another", () => {
	it("leaves the others where they were", () => {
		// The reason this is a factory rather than a module holding one remembered value: two
		// choices sharing a variable move together, which reads as a decision rather than as a
		// fault.
		const one = aChoice();
		const other = aChoice(ANOTHER_KEY);

		one.set("teams");

		expect(one.read()).toBe("teams");
		expect(other.read()).toBe("status");
		expect(localStorage.getItem(ANOTHER_KEY)).toBeNull();
	});
});

describe("telling the readers", () => {
	it("tells everyone still listening when the answer moves", () => {
		const first = vi.fn();
		const second = vi.fn();
		const choice = aChoice();

		const stopFirst = choice.subscribe(first);
		const stopSecond = choice.subscribe(second);

		choice.set("teams");

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
		const choice = aChoice();

		const stopListening = choice.subscribe(gone);
		const stopStaying = choice.subscribe(staying);

		stopListening();
		choice.set("teams");

		expect(gone).not.toHaveBeenCalled();
		// Paired, so this cannot pass against a store that tells nobody anything.
		expect(staying).toHaveBeenCalledTimes(1);

		stopStaying();
	});
});
