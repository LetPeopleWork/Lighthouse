import { act, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { useCategorySelection } from "./useCategorySelection";

describe("useCategorySelection", () => {
	afterEach(() => {
		localStorage.clear();
	});

	it("returns flow-overview as default when nothing stored", () => {
		const { result } = renderHook(() => useCategorySelection("team", 1));
		expect(result.current.selectedCategory).toBe("flow-overview");
	});

	it("reads stored category from localStorage", () => {
		localStorage.setItem(
			"lighthouse:metrics:team:42:category",
			"predictability",
		);
		const { result } = renderHook(() => useCategorySelection("team", 42));
		expect(result.current.selectedCategory).toBe("predictability");
	});

	it("persists selected category to localStorage", () => {
		const { result } = renderHook(() => useCategorySelection("team", 7));
		act(() => result.current.setSelectedCategory("portfolio"));
		expect(result.current.selectedCategory).toBe("portfolio");
		expect(localStorage.getItem("lighthouse:metrics:team:7:category")).toBe(
			"portfolio",
		);
	});

	it("ignores invalid stored values and defaults", () => {
		localStorage.setItem(
			"lighthouse:metrics:portfolio:5:category",
			"not-a-real-category",
		);
		const { result } = renderHook(() => useCategorySelection("portfolio", 5));
		expect(result.current.selectedCategory).toBe("flow-overview");
	});
});
