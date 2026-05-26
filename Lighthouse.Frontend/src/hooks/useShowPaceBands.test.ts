import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { PACE_BANDS_STORAGE_KEY, useShowPaceBands } from "./useShowPaceBands";

describe("useShowPaceBands", () => {
	beforeEach(() => {
		localStorage.clear();
	});

	it("defaults to off when no preference is stored", () => {
		const { result } = renderHook(() => useShowPaceBands());

		expect(result.current.showPaceBands).toBe(false);
	});

	it("does not write to localStorage until the user toggles", () => {
		renderHook(() => useShowPaceBands());

		expect(localStorage.getItem(PACE_BANDS_STORAGE_KEY)).toBeNull();
	});

	it("restores the enabled preference from localStorage on mount", () => {
		localStorage.setItem(PACE_BANDS_STORAGE_KEY, "true");

		const { result } = renderHook(() => useShowPaceBands());

		expect(result.current.showPaceBands).toBe(true);
	});

	it("treats any stored value other than the literal true as disabled", () => {
		localStorage.setItem(PACE_BANDS_STORAGE_KEY, "false");

		const { result } = renderHook(() => useShowPaceBands());

		expect(result.current.showPaceBands).toBe(false);
	});

	it("persists the preference globally when toggled on", () => {
		const { result } = renderHook(() => useShowPaceBands());

		act(() => {
			result.current.togglePaceBands();
		});

		expect(result.current.showPaceBands).toBe(true);
		expect(localStorage.getItem(PACE_BANDS_STORAGE_KEY)).toBe("true");
	});

	it("keeps the enabled preference across remounts so it becomes the default everywhere", () => {
		const first = renderHook(() => useShowPaceBands());
		act(() => {
			first.result.current.togglePaceBands();
		});
		first.unmount();

		const second = renderHook(() => useShowPaceBands());

		expect(second.result.current.showPaceBands).toBe(true);
	});

	it("toggles back off and persists the disabled preference", () => {
		localStorage.setItem(PACE_BANDS_STORAGE_KEY, "true");
		const { result } = renderHook(() => useShowPaceBands());

		act(() => {
			result.current.togglePaceBands();
		});

		expect(result.current.showPaceBands).toBe(false);
		expect(localStorage.getItem(PACE_BANDS_STORAGE_KEY)).toBe("false");
	});
});
