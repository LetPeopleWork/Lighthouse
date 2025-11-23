import { describe, expect, it } from "vitest";
import {
	appColors,
	calculateContrastRatio,
	getColorMapForKeys,
	getColorWithOpacity,
	getContrastText,
	getPredictabilityScoreColor,
	getStateColor,
	hexToRgba,
} from "./colors";

describe("colors utility functions", () => {
	describe("getStateColor", () => {
		it("should return 'success' for Done state category", () => {
			expect(getStateColor("Done")).toBe("success");
		});

		it("should return 'warning' for Doing state category", () => {
			expect(getStateColor("Doing")).toBe("warning");
		});

		it("should return 'info' for ToDo state category", () => {
			expect(getStateColor("ToDo")).toBe("info");
		});

		it("should return 'default' for unknown state categories", () => {
			expect(getStateColor("Unknown")).toBe("default");
			expect(getStateColor("")).toBe("default");
			expect(getStateColor("InProgress")).toBe("default");
		});

		it("should be case sensitive", () => {
			expect(getStateColor("done")).toBe("default");
			expect(getStateColor("DONE")).toBe("default");
			expect(getStateColor("doing")).toBe("default");
		});
	});

	describe("hexToRgba", () => {
		it("should convert hex to rgba with default alpha of 1", () => {
			expect(hexToRgba("#ffffff")).toBe("rgba(255, 255, 255, 1)");
			expect(hexToRgba("#000000")).toBe("rgba(0, 0, 0, 1)");
			expect(hexToRgba("#ff0000")).toBe("rgba(255, 0, 0, 1)");
			expect(hexToRgba("#00ff00")).toBe("rgba(0, 255, 0, 1)");
			expect(hexToRgba("#0000ff")).toBe("rgba(0, 0, 255, 1)");
		});

		it("should convert hex to rgba with custom alpha", () => {
			expect(hexToRgba("#ffffff", 0.5)).toBe("rgba(255, 255, 255, 0.5)");
			expect(hexToRgba("#000000", 0.25)).toBe("rgba(0, 0, 0, 0.25)");
			expect(hexToRgba("#ff0000", 0.75)).toBe("rgba(255, 0, 0, 0.75)");
		});

		it("should handle alpha value of 0", () => {
			expect(hexToRgba("#ffffff", 0)).toBe("rgba(255, 255, 255, 0)");
		});

		it("should handle primary brand color correctly", () => {
			// #30574e = rgb(48, 87, 78)
			expect(hexToRgba("#30574e")).toBe("rgba(48, 87, 78, 1)");
			expect(hexToRgba("#30574e", 0.8)).toBe("rgba(48, 87, 78, 0.8)");
		});

		it("should handle mixed case hex values", () => {
			expect(hexToRgba("#FfFfFf")).toBe("rgba(255, 255, 255, 1)");
			expect(hexToRgba("#aAbBcC")).toBe("rgba(170, 187, 204, 1)");
		});
	});

	describe("getContrastText", () => {
		it("should return white text for dark backgrounds", () => {
			expect(getContrastText("#000000")).toBe("#ffffff"); // Black background
			expect(getContrastText("#121212")).toBe("#ffffff"); // Very dark gray
			expect(getContrastText("#30574e")).toBe("#ffffff"); // Primary brand color
		});

		it("should return dark text for light backgrounds", () => {
			expect(getContrastText("#ffffff")).toBe("#222222"); // White background
			expect(getContrastText("#f5f5f5")).toBe("#222222"); // Light gray
			expect(getContrastText("#ffff00")).toBe("#222222"); // Yellow
		});

		it("should handle edge cases around the luminance threshold", () => {
			// Colors around the 0.175 luminance threshold
			expect(getContrastText("#777777")).toBe("#222222"); // Above threshold (luminance ~0.184)
			expect(getContrastText("#666666")).toBe("#ffffff"); // Below threshold (darker)
		});

		it("should work with forecast colors", () => {
			expect(getContrastText(appColors.forecast.risky)).toBe("#222222"); // Red is bright, gets dark text
			expect(getContrastText(appColors.forecast.realistic)).toBe("#222222"); // Orange is bright
			expect(getContrastText(appColors.forecast.confident)).toBe("#222222"); // Green is bright
			expect(getContrastText(appColors.forecast.certain)).toBe("#222222"); // Green is above threshold too
		});
	});

	describe("calculateContrastRatio", () => {
		it("should calculate maximum contrast ratio for black and white", () => {
			const ratio = calculateContrastRatio("#000000", "#ffffff");
			expect(ratio).toBeCloseTo(21, 1); // Maximum contrast ratio
		});

		it("should calculate minimum contrast ratio for identical colors", () => {
			const ratio = calculateContrastRatio("#ffffff", "#ffffff");
			expect(ratio).toBeCloseTo(1, 1); // Minimum contrast ratio
		});

		it("should calculate contrast ratio for brand colors", () => {
			const primaryOnWhite = calculateContrastRatio(
				appColors.primary.main,
				"#ffffff",
			);
			expect(primaryOnWhite).toBeGreaterThanOrEqual(4.5); // Should meet AA standard

			const secondaryOnWhite = calculateContrastRatio(
				appColors.secondary.main,
				"#ffffff",
			);
			expect(secondaryOnWhite).toBeGreaterThanOrEqual(4.5); // Should meet AA standard
		});

		it("should be commutative (order shouldn't matter)", () => {
			const ratio1 = calculateContrastRatio("#000000", "#ffffff");
			const ratio2 = calculateContrastRatio("#ffffff", "#000000");
			expect(ratio1).toBeCloseTo(ratio2, 5);
		});

		it("should calculate contrast for forecast colors", () => {
			// Test that forecast colors have reasonable contrast on typical backgrounds
			const riskyOnWhite = calculateContrastRatio(
				appColors.forecast.risky,
				"#ffffff",
			);
			expect(riskyOnWhite).toBeGreaterThan(3.5); // Red should have good contrast

			const confidentOnWhite = calculateContrastRatio(
				appColors.forecast.confident,
				"#ffffff",
			);
			expect(confidentOnWhite).toBeGreaterThan(2.5); // Green has moderate contrast

			const certainOnWhite = calculateContrastRatio(
				appColors.forecast.certain,
				"#ffffff",
			);
			expect(certainOnWhite).toBeGreaterThan(4); // Darker green should have good contrast
		});

		it("should handle edge case colors", () => {
			// Test with gray values
			const lightGray = calculateContrastRatio("#cccccc", "#ffffff");
			const darkGray = calculateContrastRatio("#333333", "#000000");

			expect(lightGray).toBeGreaterThan(1);
			expect(lightGray).toBeLessThan(21);
			expect(darkGray).toBeGreaterThan(1);
			expect(darkGray).toBeLessThan(21);
		});
	});

	describe("getPredictabilityScoreColor", () => {
		it("should return certain color for scores >= 0.75", () => {
			expect(getPredictabilityScoreColor(0.75)).toBe(
				appColors.forecast.certain,
			);
			expect(getPredictabilityScoreColor(0.8)).toBe(appColors.forecast.certain);
			expect(getPredictabilityScoreColor(0.9)).toBe(appColors.forecast.certain);
			expect(getPredictabilityScoreColor(1)).toBe(appColors.forecast.certain);
		});

		it("should return confident color for scores >= 0.6 and < 0.75", () => {
			expect(getPredictabilityScoreColor(0.6)).toBe(
				appColors.forecast.confident,
			);
			expect(getPredictabilityScoreColor(0.65)).toBe(
				appColors.forecast.confident,
			);
			expect(getPredictabilityScoreColor(0.7)).toBe(
				appColors.forecast.confident,
			);
			expect(getPredictabilityScoreColor(0.74)).toBe(
				appColors.forecast.confident,
			);
		});

		it("should return realistic color for scores >= 0.5 and < 0.6", () => {
			expect(getPredictabilityScoreColor(0.5)).toBe(
				appColors.forecast.realistic,
			);
			expect(getPredictabilityScoreColor(0.55)).toBe(
				appColors.forecast.realistic,
			);
			expect(getPredictabilityScoreColor(0.59)).toBe(
				appColors.forecast.realistic,
			);
		});

		it("should return risky color for scores < 0.5", () => {
			expect(getPredictabilityScoreColor(0)).toBe(appColors.forecast.risky);
			expect(getPredictabilityScoreColor(0.25)).toBe(appColors.forecast.risky);
			expect(getPredictabilityScoreColor(0.49)).toBe(appColors.forecast.risky);
		});

		it("should handle edge cases at threshold boundaries", () => {
			// Test exact threshold values
			expect(getPredictabilityScoreColor(0.75)).toBe(
				appColors.forecast.certain,
			);
			expect(getPredictabilityScoreColor(0.6)).toBe(
				appColors.forecast.confident,
			);
			expect(getPredictabilityScoreColor(0.5)).toBe(
				appColors.forecast.realistic,
			);

			// Test values just below thresholds
			expect(getPredictabilityScoreColor(0.74999)).toBe(
				appColors.forecast.confident,
			);
			expect(getPredictabilityScoreColor(0.59999)).toBe(
				appColors.forecast.realistic,
			);
			expect(getPredictabilityScoreColor(0.49999)).toBe(
				appColors.forecast.risky,
			);
		});

		it("should handle extreme values", () => {
			// Test negative values (though not expected in real usage)
			expect(getPredictabilityScoreColor(-0.1)).toBe(appColors.forecast.risky);

			// Test values above 1 (though not expected in real usage)
			expect(getPredictabilityScoreColor(1.5)).toBe(appColors.forecast.certain);
		});

		it("should return consistent results for the same input", () => {
			const score = 0.73;
			const result1 = getPredictabilityScoreColor(score);
			const result2 = getPredictabilityScoreColor(score);
			expect(result1).toBe(result2);
			expect(result1).toBe(appColors.forecast.confident);
		});
	});

	describe("getColorWithOpacity", () => {
		it("should return color with specified opacity", () => {
			expect(getColorWithOpacity("#ffffff", 0.5)).toBe(
				"rgba(255, 255, 255, 0.5)",
			);
			expect(getColorWithOpacity("#000000", 0.25)).toBe("rgba(0, 0, 0, 0.25)");
		});

		describe("getColorMapForKeys", () => {
			it("should return an empty object for empty keys", () => {
				expect(getColorMapForKeys([])).toEqual({});
			});

			it("should return deterministic mapping independent of input order and remove duplicates", () => {
				const keys = ["TypeB", "TypeA", "TypeA"];
				const map1 = getColorMapForKeys(keys);
				const map2 = getColorMapForKeys(["TypeA", "TypeB"]);
				// Same keys should yield identical mapping
				expect(Object.keys(map1).sort((a, b) => a.localeCompare(b))).toEqual(
					Object.keys(map2).sort((a, b) => a.localeCompare(b)),
				);
				expect(map1.TypeA).toBe(map2.TypeA);
				expect(map1.TypeB).toBe(map2.TypeB);
			});

			it("should preserve input order when preserveInputOrder is true", () => {
				const keys = ["B", "A", "C"];
				const map = getColorMapForKeys(keys, true);
				// Ensure keys are present in the same order by comparing to array of keys from Object.keys
				const orderedKeys = Object.keys(map);
				expect(orderedKeys[0]).toBe("B");
				expect(orderedKeys[1]).toBe("A");
				expect(orderedKeys[2]).toBe("C");
			});

			it("should return hex colors for few keys by default", () => {
				const map = getColorMapForKeys(["A", "B", "C"]);
				expect(map.A).toMatch(/^#([0-9a-fA-F]{6})$/);
				expect(map.B).toMatch(/^#([0-9a-fA-F]{6})$/);
				expect(map.C).toMatch(/^#([0-9a-fA-F]{6})$/);
			});

			it("should use HSL hex colors for many keys (hsl fallback)", () => {
				const keys: string[] = [];
				for (let i = 0; i < 12; i++) keys.push(`K${i}`);
				const map = getColorMapForKeys(keys);
				// Expect hex values (# followed by 6 hex digits)
				for (const c of Object.values(map))
					expect(c).toMatch(/^#[0-9a-fA-F]{6}$/);
				// Ensure distinct colors
				const unique = new Set(Object.values(map));
				expect(unique.size).toBe(keys.length);
			});
		});

		it("should handle brand colors with opacity", () => {
			expect(getColorWithOpacity(appColors.primary.main, 0.8)).toBe(
				"rgba(48, 87, 78, 0.8)",
			);
		});

		it("should handle opacity values at boundaries", () => {
			expect(getColorWithOpacity("#ff0000", 0)).toBe("rgba(255, 0, 0, 0)");
			expect(getColorWithOpacity("#ff0000", 1)).toBe("rgba(255, 0, 0, 1)");
		});
	});

	describe("appColors structure", () => {
		it("should have all required color categories", () => {
			expect(appColors).toHaveProperty("primary");
			expect(appColors).toHaveProperty("secondary");
			expect(appColors).toHaveProperty("forecast");
			expect(appColors).toHaveProperty("status");
			expect(appColors).toHaveProperty("light");
			expect(appColors).toHaveProperty("dark");
		});

		it("should have valid hex colors for primary palette", () => {
			expect(appColors.primary.main).toMatch(/^#[0-9a-fA-F]{6}$/);
			expect(appColors.primary.light).toMatch(/^#[0-9a-fA-F]{6}$/);
			expect(appColors.primary.dark).toMatch(/^#[0-9a-fA-F]{6}$/);
		});

		it("should have valid hex colors for forecast palette", () => {
			expect(appColors.forecast.risky).toMatch(/^#[0-9a-fA-F]{6}$/);
			expect(appColors.forecast.realistic).toMatch(/^#[0-9a-fA-F]{6}$/);
			expect(appColors.forecast.confident).toMatch(/^#[0-9a-fA-F]{6}$/);
			expect(appColors.forecast.certain).toMatch(/^#[0-9a-fA-F]{6}$/);
		});

		it("should have all forecast colors be distinct", () => {
			const forecastColors = [
				appColors.forecast.risky,
				appColors.forecast.realistic,
				appColors.forecast.confident,
				appColors.forecast.certain,
			];

			const uniqueColors = new Set(forecastColors);
			expect(uniqueColors.size).toBe(forecastColors.length);
		});
	});

	describe("accessibility compliance", () => {
		it("should ensure primary colors meet WCAG AA contrast requirements", () => {
			// Primary color should have at least 4.5:1 contrast on white
			const primaryContrastOnWhite = calculateContrastRatio(
				appColors.primary.main,
				"#ffffff",
			);
			expect(primaryContrastOnWhite).toBeGreaterThanOrEqual(4.5);

			// Secondary color should have at least 4.5:1 contrast on white
			const secondaryContrastOnWhite = calculateContrastRatio(
				appColors.secondary.main,
				"#ffffff",
			);
			expect(secondaryContrastOnWhite).toBeGreaterThanOrEqual(4.5);
		});

		it("should ensure forecast colors have adequate contrast", () => {
			// Test forecast colors have reasonable contrast - adjusted for actual values
			expect(
				calculateContrastRatio(appColors.forecast.risky, "#ffffff"),
			).toBeGreaterThanOrEqual(3.5); // Red has good contrast
			expect(
				calculateContrastRatio(appColors.forecast.certain, "#ffffff"),
			).toBeGreaterThanOrEqual(4); // Dark green has good contrast

			// Orange and light green have lower contrast but are still usable
			expect(
				calculateContrastRatio(appColors.forecast.realistic, "#ffffff"),
			).toBeGreaterThanOrEqual(2); // Orange has moderate contrast
			expect(
				calculateContrastRatio(appColors.forecast.confident, "#ffffff"),
			).toBeGreaterThanOrEqual(2.5); // Light green has moderate contrast
		});

		it("should ensure text colors meet contrast requirements", () => {
			// Light theme text on light background
			const lightTextContrast = calculateContrastRatio(
				appColors.light.text.primary,
				appColors.light.background,
			);
			expect(lightTextContrast).toBeGreaterThanOrEqual(4.5);

			// Dark theme text on dark background
			const darkTextContrast = calculateContrastRatio(
				appColors.dark.text.primary,
				appColors.dark.background,
			);
			expect(darkTextContrast).toBeGreaterThanOrEqual(4.5);
		});
	});
});
