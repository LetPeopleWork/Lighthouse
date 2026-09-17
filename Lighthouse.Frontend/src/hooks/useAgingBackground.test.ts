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
			first.result.current.chooseBackground("risk");
		});
		first.unmount();

		const second = renderHook(() => useAgingBackground());

		expect(second.result.current.background).toBe("risk");
		expect(localStorage.getItem(AGING_BACKGROUND_STORAGE_KEY)).toBe("risk");
	});

	it("switches from one background to the other without leaving both on", () => {
		const { result } = renderHook(() => useAgingBackground());

		act(() => {
			result.current.chooseBackground("pace");
		});
		act(() => {
			result.current.chooseBackground("risk");
		});

		expect(result.current.background).toBe("risk");
	});

	it("names the key the stored preference has always lived under", () => {
		// Pinned against the word itself. Every other assertion here goes through the constant, which
		// holds just as well when it is blank - and a renamed key silently forgets what every
		// existing reader had chosen.
		expect(AGING_BACKGROUND_STORAGE_KEY).toBe("workItemAgingPaceBandsEnabled");
	});

	it.each(["off", "pace", "risk"] as const)(
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
				result.current.chooseBackground("risk");
			});

			expect(result.current.background).toBe("risk");
		} finally {
			setItem.mockRestore();
		}
	});

	// --- What was stored before the control had three options ---

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

	it("ignores a stored value it cannot make sense of", () => {
		localStorage.setItem(AGING_BACKGROUND_STORAGE_KEY, "sideways");

		const { result } = renderHook(() => useAgingBackground());

		expect(result.current.background).toBe("off");
	});
});
