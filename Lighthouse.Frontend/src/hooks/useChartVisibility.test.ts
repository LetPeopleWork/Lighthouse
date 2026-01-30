import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IPercentileValue } from "../models/PercentileValue";
import {
	useChartVisibility,
	usePercentileVisibility,
	useServiceLevelExpectationVisibility,
	useTypesVisibility,
} from "./useChartVisibility";

describe("usePercentileVisibility", () => {
	describe("initial visibility with unique values", () => {
		it("should show all percentiles when each has a unique value", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 50, value: 5 },
				{ percentile: 70, value: 10 },
				{ percentile: 85, value: 15 },
				{ percentile: 95, value: 20 },
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[50]).toBe(true);
			expect(result.current.visiblePercentiles[70]).toBe(true);
			expect(result.current.visiblePercentiles[85]).toBe(true);
			expect(result.current.visiblePercentiles[95]).toBe(true);
		});
	});

	describe("initial visibility with overlapping values", () => {
		it("should hide lower percentiles when two percentiles have the same value", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 50, value: 5 },
				{ percentile: 70, value: 12 },
				{ percentile: 85, value: 12 }, // Same value as 70%
				{ percentile: 95, value: 20 },
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[50]).toBe(true);
			expect(result.current.visiblePercentiles[70]).toBe(false); // Hidden - same value as 85%
			expect(result.current.visiblePercentiles[85]).toBe(true); // Shown - highest in group
			expect(result.current.visiblePercentiles[95]).toBe(true);
		});

		it("should hide all but the highest when multiple percentiles share the same value", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 50, value: 10 },
				{ percentile: 70, value: 10 },
				{ percentile: 85, value: 10 },
				{ percentile: 95, value: 10 },
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[50]).toBe(false);
			expect(result.current.visiblePercentiles[70]).toBe(false);
			expect(result.current.visiblePercentiles[85]).toBe(false);
			expect(result.current.visiblePercentiles[95]).toBe(true); // Only highest shown
		});

		it("should handle multiple groups of overlapping values", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 50, value: 5 },
				{ percentile: 70, value: 5 }, // Same as 50%
				{ percentile: 85, value: 15 },
				{ percentile: 95, value: 15 }, // Same as 85%
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[50]).toBe(false); // Hidden - 70 is higher
			expect(result.current.visiblePercentiles[70]).toBe(true); // Shown - highest in group
			expect(result.current.visiblePercentiles[85]).toBe(false); // Hidden - 95 is higher
			expect(result.current.visiblePercentiles[95]).toBe(true); // Shown - highest in group
		});

		it("should use rounded values for grouping (rounding up)", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 70, value: 11.6 }, // Rounds to 12
				{ percentile: 85, value: 12.4 }, // Rounds to 12
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[70]).toBe(false); // Hidden - same rounded value
			expect(result.current.visiblePercentiles[85]).toBe(true); // Shown - highest
		});

		it("should use rounded values for grouping (rounding down)", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 70, value: 11.4 }, // Rounds to 11
				{ percentile: 85, value: 11.2 }, // Rounds to 11
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[70]).toBe(false);
			expect(result.current.visiblePercentiles[85]).toBe(true);
		});

		it("should not group values that round to different integers", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 70, value: 11.4 }, // Rounds to 11
				{ percentile: 85, value: 11.6 }, // Rounds to 12
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[70]).toBe(true); // Different rounded values
			expect(result.current.visiblePercentiles[85]).toBe(true);
		});
	});

	describe("toggle visibility", () => {
		it("should toggle a hidden percentile to visible", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 70, value: 12 },
				{ percentile: 85, value: 12 },
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[70]).toBe(false);

			act(() => {
				result.current.togglePercentileVisibility(70);
			});

			expect(result.current.visiblePercentiles[70]).toBe(true);
		});

		it("should toggle a visible percentile to hidden", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 70, value: 12 },
				{ percentile: 85, value: 12 },
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[85]).toBe(true);

			act(() => {
				result.current.togglePercentileVisibility(85);
			});

			expect(result.current.visiblePercentiles[85]).toBe(false);
		});
	});

	describe("percentile list changes", () => {
		it("should recompute visibility when percentile list changes", () => {
			const initialPercentiles: IPercentileValue[] = [
				{ percentile: 50, value: 5 },
				{ percentile: 70, value: 10 },
			];

			const { result, rerender } = renderHook(
				({ percentiles }) => usePercentileVisibility(percentiles),
				{ initialProps: { percentiles: initialPercentiles } },
			);

			expect(result.current.visiblePercentiles[50]).toBe(true);
			expect(result.current.visiblePercentiles[70]).toBe(true);

			// New percentiles with overlapping values
			const newPercentiles: IPercentileValue[] = [
				{ percentile: 50, value: 10 },
				{ percentile: 70, value: 10 },
				{ percentile: 85, value: 10 },
			];

			rerender({ percentiles: newPercentiles });

			expect(result.current.visiblePercentiles[50]).toBe(false);
			expect(result.current.visiblePercentiles[70]).toBe(false);
			expect(result.current.visiblePercentiles[85]).toBe(true);
		});

		it("should preserve visibility when only values change but percentile list stays same", () => {
			const initialPercentiles: IPercentileValue[] = [
				{ percentile: 70, value: 10 },
				{ percentile: 85, value: 15 },
			];

			const { result, rerender } = renderHook(
				({ percentiles }) => usePercentileVisibility(percentiles),
				{ initialProps: { percentiles: initialPercentiles } },
			);

			// Toggle 85 off
			act(() => {
				result.current.togglePercentileVisibility(85);
			});
			expect(result.current.visiblePercentiles[85]).toBe(false);

			// Change values but keep same percentile numbers
			const newPercentiles: IPercentileValue[] = [
				{ percentile: 70, value: 12 },
				{ percentile: 85, value: 12 },
			];

			rerender({ percentiles: newPercentiles });

			// User's toggle should be preserved (percentile list didn't change)
			expect(result.current.visiblePercentiles[85]).toBe(false);
		});
	});

	describe("edge cases", () => {
		it("should handle empty percentiles array", () => {
			const percentiles: IPercentileValue[] = [];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles).toEqual({});
		});

		it("should handle single percentile", () => {
			const percentiles: IPercentileValue[] = [{ percentile: 85, value: 10 }];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[85]).toBe(true);
		});

		it("should handle zero values", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 50, value: 0 },
				{ percentile: 70, value: 0 },
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[50]).toBe(false);
			expect(result.current.visiblePercentiles[70]).toBe(true);
		});

		it("should handle negative values", () => {
			const percentiles: IPercentileValue[] = [
				{ percentile: 50, value: -5 },
				{ percentile: 70, value: -5 },
			];

			const { result } = renderHook(() => usePercentileVisibility(percentiles));

			expect(result.current.visiblePercentiles[50]).toBe(false);
			expect(result.current.visiblePercentiles[70]).toBe(true);
		});
	});
});

describe("useServiceLevelExpectationVisibility", () => {
	it("should start with SLE hidden", () => {
		const { result } = renderHook(() => useServiceLevelExpectationVisibility());

		expect(result.current.sleVisible).toBe(false);
	});

	it("should toggle SLE visibility", () => {
		const { result } = renderHook(() => useServiceLevelExpectationVisibility());

		act(() => {
			result.current.toggleSleVisibility();
		});

		expect(result.current.sleVisible).toBe(true);

		act(() => {
			result.current.toggleSleVisibility();
		});

		expect(result.current.sleVisible).toBe(false);
	});
});

describe("useTypesVisibility", () => {
	it("should show all types by default", () => {
		const types = ["Bug", "Story", "Task"];

		const { result } = renderHook(() => useTypesVisibility(types));

		expect(result.current.visibleTypes.Bug).toBe(true);
		expect(result.current.visibleTypes.Story).toBe(true);
		expect(result.current.visibleTypes.Task).toBe(true);
	});

	it("should use initial visibility when provided", () => {
		const types = ["Bug", "Story", "Task"];
		const initialVisibility = { Bug: false, Story: true, Task: false };

		const { result } = renderHook(() =>
			useTypesVisibility(types, initialVisibility),
		);

		expect(result.current.visibleTypes.Bug).toBe(false);
		expect(result.current.visibleTypes.Story).toBe(true);
		expect(result.current.visibleTypes.Task).toBe(false);
	});

	it("should toggle type visibility", () => {
		const types = ["Bug", "Story"];

		const { result } = renderHook(() => useTypesVisibility(types));

		act(() => {
			result.current.toggleTypeVisibility("Bug");
		});

		expect(result.current.visibleTypes.Bug).toBe(false);
		expect(result.current.visibleTypes.Story).toBe(true);
	});
});

describe("useChartVisibility", () => {
	it("should combine all visibility hooks with overlapping percentile handling", () => {
		const percentiles: IPercentileValue[] = [
			{ percentile: 70, value: 10 },
			{ percentile: 85, value: 10 }, // Same value as 70%
		];
		const types = ["Bug", "Story"];

		const { result } = renderHook(() =>
			useChartVisibility({ percentiles, types }),
		);

		// Percentile visibility with overlap handling
		expect(result.current.visiblePercentiles[70]).toBe(false);
		expect(result.current.visiblePercentiles[85]).toBe(true);

		// SLE visibility
		expect(result.current.sleVisible).toBe(false);

		// Types visibility
		expect(result.current.visibleTypes.Bug).toBe(true);
		expect(result.current.visibleTypes.Story).toBe(true);
	});

	it("should provide toggle functions for all visibility types", () => {
		const percentiles: IPercentileValue[] = [{ percentile: 85, value: 10 }];
		const types = ["Bug"];

		const { result } = renderHook(() =>
			useChartVisibility({ percentiles, types }),
		);

		expect(typeof result.current.togglePercentileVisibility).toBe("function");
		expect(typeof result.current.toggleSleVisibility).toBe("function");
		expect(typeof result.current.toggleTypeVisibility).toBe("function");
	});
});
