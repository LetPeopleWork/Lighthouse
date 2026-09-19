import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	AGING_BACKGROUND_STORAGE_KEY,
	useAgingBackground,
} from "./useAgingBackground";

describe("useAgingBackground", () => {
	beforeEach(() => {
		localStorage.clear();
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

	it("still applies a choice the browser refuses to remember", () => {
		// A private window, or site data blocked. The chart must still change when asked; it just
		// will not outlive the tab.
		const setItem = vi
			.spyOn(Storage.prototype, "setItem")
			.mockImplementation(() => {
				throw new Error("storage is full");
			});

		try {
			const { result } = renderHook(() => useAgingBackground());

			act(() => {
				result.current.chooseBackground("pace");
			});

			expect(result.current.background).toBe("pace");
		} finally {
			setItem.mockRestore();
		}
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
