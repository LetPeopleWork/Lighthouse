import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	AGING_BACKGROUND_STORAGE_KEY,
	useAgingBackground,
} from "./useAgingBackground";

/**
 * Breaks one of storage's own methods, and puts it back afterwards.
 *
 * **Spied on the object, not on `Storage.prototype`.** In this environment `localStorage` does not
 * inherit from it - `getItem` and `setItem` are its own properties - so a prototype spy installs
 * cleanly, reports itself as installed, and intercepts nothing at all. The two tests below were
 * written that way and passed for years without once entering the guard they name.
 *
 * Undone by hand, because `vi.restoreAllMocks()` does not put these back and a broken `setItem`
 * leaking into the next test fails it while it is arranging its own fixture - a long way from the
 * cause.
 */
const brokenStorage: { mockRestore: () => void }[] = [];

const breakStorage = (method: "getItem" | "setItem") => {
	const spy = vi.spyOn(localStorage, method).mockImplementation(() => {
		throw new Error("site data is blocked");
	});

	brokenStorage.push(spy);

	return spy;
};

describe("useAgingBackground", () => {
	beforeEach(() => {
		localStorage.clear();
	});

	afterEach(() => {
		for (const spy of brokenStorage.splice(0)) {
			spy.mockRestore();
		}
	});

	it("paints nothing when no preference is stored", () => {
		const { result } = renderHook(() => useAgingBackground());

		expect(result.current.background).toBe("off");
	});

	it("does not write to localStorage until the user picks a mode", () => {
		renderHook(() => useAgingBackground());

		expect(localStorage.getItem(AGING_BACKGROUND_STORAGE_KEY)).toBeNull();
	});

	it("keeps a choice across remounts so it becomes the default everywhere", () => {
		const first = renderHook(() => useAgingBackground());
		act(() => {
			first.result.current.chooseBackground("pace");
		});
		first.unmount();

		const second = renderHook(() => useAgingBackground());

		expect(second.result.current.background).toBe("pace");
		expect(localStorage.getItem(AGING_BACKGROUND_STORAGE_KEY)).toBe("pace");
	});

	it("switches from one background to the other without leaving both on", () => {
		const { result } = renderHook(() => useAgingBackground());

		act(() => {
			result.current.chooseBackground("pace");
		});
		act(() => {
			result.current.chooseBackground("off");
		});

		expect(result.current.background).toBe("off");
	});

	it("names the key the stored preference has always lived under", () => {
		// Pinned against the word itself. Every other assertion here goes through the constant, which
		// holds just as well when it is blank - and a renamed key silently forgets what every
		// existing reader had chosen.
		expect(AGING_BACKGROUND_STORAGE_KEY).toBe("workItemAgingPaceBandsEnabled");
	});

	it.each(["off", "pace"] as const)(
		"restores %s, the shape it writes itself",
		(mode) => {
			// The ordinary path from the day this ships: what the hook stored is what it reads back.
			localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, mode);

			const { result } = renderHook(() => useAgingBackground());

			expect(result.current.background).toBe(mode);
		},
	);

	it("still draws a chart when the browser refuses to say what was stored", () => {
		// The read side of the same private-window case as below. Without the guard the throw
		// escapes the effect and takes the whole chart with it, which is a worse outcome than
		// forgetting a preference.
		//
		// **A choice is stored before the read is broken, and that is what gives this an answer.**
		// Asserting "off" against an empty store says nothing: "off" is what no stored preference
		// produces anyway, so a guard that ran and a guard that was never reached look identical.
		// With "pace" stored, a read that got through would paint it.
		localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, "pace");
		breakStorage("getItem");

		const { result } = renderHook(() => useAgingBackground());

		expect(result.current.background).toBe("off");
	});

	it("still applies a choice the browser refuses to remember", () => {
		// A private window, or site data blocked. The chart must still change when asked; it just
		// will not outlive the tab.
		breakStorage("setItem");

		const { result } = renderHook(() => useAgingBackground());

		act(() => {
			result.current.chooseBackground("pace");
		});

		// Both halves. The chart the reader asked for, and the memory they did not get - without
		// the second, this holds just as well against a write that succeeded, which is every run
		// where the throw was never reached.
		expect(result.current.background).toBe("pace");
		expect(localStorage.getItem(AGING_BACKGROUND_STORAGE_KEY)).toBeNull();
	});

	// --- What earlier versions of this control wrote ---

	it("reads a chart that had pace bands on as still having them on", () => {
		// The value this key held when it was a boolean. A reader who upgrades must not find their
		// chart repainted, or turned off, because a third option was added beside the one they use.
		localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, "true");

		const { result } = renderHook(() => useAgingBackground());

		expect(result.current.background).toBe("pace");
	});

	it("reads a chart that had them off as painting nothing", () => {
		localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, "false");

		const { result } = renderHook(() => useAgingBackground());

		expect(result.current.background).toBe("off");
	});

	it("paints nothing for a chart left on a background that no longer exists", () => {
		// A background that painted where an item's odds of missing its target crossed fixed
		// thresholds. It was withdrawn before release, so only a developer's browser can hold it.
		//
		// This reads the same as the unrecognised-value case below today, and deliberately so: both
		// paint nothing. Keeping the two apart is a guard for later. If someone gives a new mode
		// this same word, a browser still holding it would silently opt in - and this test, not the
		// one below, is what would go red and say so.
		localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, "risk");

		const { result } = renderHook(() => useAgingBackground());

		expect(result.current.background).toBe("off");
		// Left where it was rather than rewritten, so rolling back to a build that still has the
		// mode returns the reader to the chart they left.
		expect(localStorage.getItem(AGING_BACKGROUND_STORAGE_KEY)).toBe("risk");
	});

	it("ignores a stored value it cannot make sense of", () => {
		localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, "sideways");

		const { result } = renderHook(() => useAgingBackground());

		expect(result.current.background).toBe("off");
	});
});
