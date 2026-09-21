import { beforeEach, describe, expect, it, vi } from "vitest";
import { showTeamsStore } from "./useShowTeams";

// The choice, on its own. What it does with a value it does not recognise, with storage that
// refuses it, and with a reader who has stopped listening are questions about the store rather
// than about any chart drawn from it - and the tab's own tests cannot reach most of them, because
// by the time a component has rendered the answer has already been decided.

const SHOW_TEAMS_KEY = "lighthouse:deliveryTimeline:showTeams";

beforeEach(() => {
	localStorage.clear();
	showTeamsStore.forget();
	vi.restoreAllMocks();
});

describe("what the page is showing", () => {
	it("shows nothing to a reader who has never answered", () => {
		expect(showTeamsStore.read()).toBe(false);
	});

	it("shows what the reader last chose", () => {
		localStorage.setItem(SHOW_TEAMS_KEY, "true");

		expect(showTeamsStore.read()).toBe(true);
	});

	it("reads the stored word rather than trusting that anything is there", () => {
		// `localStorage` hands back text, and the text "false" is truthy. Coerced rather than
		// compared, the preference turns itself on and can never be turned off again.
		localStorage.setItem(SHOW_TEAMS_KEY, "false");

		expect(showTeamsStore.read()).toBe(false);
	});

	it("shows nothing for a stored value it does not recognise", () => {
		localStorage.setItem(SHOW_TEAMS_KEY, "yes");

		expect(showTeamsStore.read()).toBe(false);
	});

	it("keeps the reader's answer for this visit when storage will not take it", () => {
		// Private browsing and blocked site data make the write throw. The reader loses the memory
		// of the choice, not the view they asked for.
		vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
			throw new Error("site data is blocked");
		});

		showTeamsStore.set(true);

		expect(showTeamsStore.read()).toBe(true);
	});

	it("shows nothing, rather than throwing, when storage cannot be read at all", () => {
		// Unguarded, this throw comes out through the hook and takes the whole Portfolio
		// accordion down with it.
		vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
			throw new Error("site data is blocked");
		});

		expect(showTeamsStore.read()).toBe(false);
	});

	it("remembers the answer for the next visit", () => {
		showTeamsStore.set(true);

		// Pinned against the key itself, once. A renamed key silently forgets every reader's
		// choice while every round trip through this store keeps passing.
		expect(localStorage.getItem(SHOW_TEAMS_KEY)).toBe("true");
	});
});

describe("telling the readers", () => {
	it("tells everyone still listening when the answer moves", () => {
		const first = vi.fn();
		const second = vi.fn();

		const stopFirst = showTeamsStore.subscribe(first);
		const stopSecond = showTeamsStore.subscribe(second);

		showTeamsStore.set(true);

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

		const stopListening = showTeamsStore.subscribe(gone);
		const stopStaying = showTeamsStore.subscribe(staying);

		stopListening();
		showTeamsStore.set(true);

		expect(gone).not.toHaveBeenCalled();
		// Paired, so this cannot pass against a store that tells nobody anything.
		expect(staying).toHaveBeenCalledTimes(1);

		stopStaying();
	});
});
