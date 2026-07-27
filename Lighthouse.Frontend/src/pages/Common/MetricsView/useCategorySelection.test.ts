import type { RenderHookResult } from "@testing-library/react";
import { act, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import type { CategoryKey } from "./categoryMetadata";
import {
	useCategorySelection,
	useVisitedCategories,
} from "./useCategorySelection";

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

	it("maps retired cycle-time key to flow-metrics", () => {
		localStorage.setItem("lighthouse:metrics:team:10:category", "cycle-time");
		const { result } = renderHook(() => useCategorySelection("team", 10));
		expect(result.current.selectedCategory).toBe("flow-metrics");
	});

	it("maps retired throughput key to flow-metrics", () => {
		localStorage.setItem("lighthouse:metrics:team:11:category", "throughput");
		const { result } = renderHook(() => useCategorySelection("team", 11));
		expect(result.current.selectedCategory).toBe("flow-metrics");
	});

	it("maps retired wip-aging key to flow-metrics", () => {
		localStorage.setItem(
			"lighthouse:metrics:portfolio:12:category",
			"wip-aging",
		);
		const { result } = renderHook(() => useCategorySelection("portfolio", 12));
		expect(result.current.selectedCategory).toBe("flow-metrics");
	});
});

describe("useVisitedCategories", () => {
	const renderVisited = (
		category: CategoryKey,
		resetToken: string,
	): RenderHookResult<
		readonly CategoryKey[],
		{ category: CategoryKey; resetToken: string }
	> =>
		renderHook(({ category: c, resetToken: t }) => useVisitedCategories(c, t), {
			initialProps: { category, resetToken },
		});

	it("starts with the initially selected category", () => {
		const { result } = renderVisited("flow-metrics", "window-a");
		expect(result.current).toEqual(["flow-metrics"]);
	});

	it("grows in visit order as new categories are selected", () => {
		const { result, rerender } = renderVisited("flow-overview", "window-a");

		rerender({ category: "flow-metrics", resetToken: "window-a" });
		rerender({ category: "predictability", resetToken: "window-a" });

		expect(result.current).toEqual([
			"flow-overview",
			"flow-metrics",
			"predictability",
		]);
	});

	it("keeps the identical array when an already-visited category is revisited", () => {
		const { result, rerender } = renderVisited("flow-overview", "window-a");
		rerender({ category: "flow-metrics", resetToken: "window-a" });
		const afterSecondVisit = result.current;

		rerender({ category: "flow-overview", resetToken: "window-a" });

		expect(result.current).toBe(afterSecondVisit);
		expect(result.current).toEqual(["flow-overview", "flow-metrics"]);
	});

	it("keeps the identical array when nothing changed between renders", () => {
		const { result, rerender } = renderVisited("predictability", "window-a");
		const initial = result.current;

		rerender({ category: "predictability", resetToken: "window-a" });

		expect(result.current).toBe(initial);
	});

	it("collapses to the current selection when the reset token changes", () => {
		const { result, rerender } = renderVisited("flow-overview", "window-a");
		rerender({ category: "flow-metrics", resetToken: "window-a" });

		rerender({ category: "flow-metrics", resetToken: "window-b" });

		expect(result.current).toEqual(["flow-metrics"]);
	});

	it("does not resurrect an earlier set when a previous reset token returns", () => {
		const { result, rerender } = renderVisited("flow-overview", "window-a");
		rerender({ category: "flow-metrics", resetToken: "window-a" });
		rerender({ category: "predictability", resetToken: "window-b" });

		rerender({ category: "predictability", resetToken: "window-a" });

		expect(result.current).toEqual(["predictability"]);
	});
});
