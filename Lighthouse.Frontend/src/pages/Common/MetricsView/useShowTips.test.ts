import { act, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { useShowTips } from "./useShowTips";

describe("useShowTips", () => {
	afterEach(() => {
		localStorage.clear();
	});

	it("defaults to true when nothing stored", () => {
		const { result } = renderHook(() => useShowTips("team", 1));
		expect(result.current.showTips).toBe(true);
	});

	it("reads false from localStorage", () => {
		localStorage.setItem("lighthouse:metrics:team:42:showTips", "false");
		const { result } = renderHook(() => useShowTips("team", 42));
		expect(result.current.showTips).toBe(false);
	});

	it("persists toggle to localStorage", () => {
		const { result } = renderHook(() => useShowTips("team", 7));
		expect(result.current.showTips).toBe(true);
		act(() => result.current.toggleShowTips());
		expect(result.current.showTips).toBe(false);
		expect(localStorage.getItem("lighthouse:metrics:team:7:showTips")).toBe(
			"false",
		);
	});

	it("toggles back to true", () => {
		localStorage.setItem("lighthouse:metrics:portfolio:3:showTips", "false");
		const { result } = renderHook(() => useShowTips("portfolio", 3));
		expect(result.current.showTips).toBe(false);
		act(() => result.current.toggleShowTips());
		expect(result.current.showTips).toBe(true);
		expect(
			localStorage.getItem("lighthouse:metrics:portfolio:3:showTips"),
		).toBe("true");
	});
});
