import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useHideCompletedFeatures } from "./useHideCompletedFeatures";

describe("useHideCompletedFeatures", () => {
	const storageKey = "test_hide_completed_key";

	beforeEach(() => {
		localStorage.clear();
	});

	it("should default to true when no localStorage entry exists", () => {
		const { result } = renderHook(() => useHideCompletedFeatures(storageKey));

		expect(result.current.hideCompleted).toBe(true);
	});

	it("should write default value to localStorage on first mount", () => {
		renderHook(() => useHideCompletedFeatures(storageKey));

		expect(localStorage.getItem(storageKey)).toBe("true");
	});

	it("should read existing localStorage value on mount", () => {
		localStorage.setItem(storageKey, "false");

		const { result } = renderHook(() => useHideCompletedFeatures(storageKey));

		expect(result.current.hideCompleted).toBe(false);
	});

	it("should update state and localStorage when toggled", () => {
		const { result } = renderHook(() => useHideCompletedFeatures(storageKey));

		act(() => {
			result.current.handleToggleChange({
				target: { checked: false },
			} as React.ChangeEvent<HTMLInputElement>);
		});

		expect(result.current.hideCompleted).toBe(false);
		expect(localStorage.getItem(storageKey)).toBe("false");
	});

	it("should toggle back to true after being set to false", () => {
		const { result } = renderHook(() => useHideCompletedFeatures(storageKey));

		act(() => {
			result.current.handleToggleChange({
				target: { checked: false },
			} as React.ChangeEvent<HTMLInputElement>);
		});

		act(() => {
			result.current.handleToggleChange({
				target: { checked: true },
			} as React.ChangeEvent<HTMLInputElement>);
		});

		expect(result.current.hideCompleted).toBe(true);
		expect(localStorage.getItem(storageKey)).toBe("true");
	});
});
