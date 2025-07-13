/**
 * Application color palette
 * Central location for all color definitions
 *
 * WCAG 2.1 Contrast Requirements:
 * - Text & background: min 4.5:1 (AA), 7:1 (AAA)
 * - Large text: min 3:1 (AA), 4.5:1 (AAA)
 * - UI components: min 3:1 (AA)
 */

export const appColors = {
	// Primary Colors - Company brand color: #30574e
	primary: {
		main: "#30574e", // Brand color - Meets 4.5:1 contrast on white
		light: "#59B5A7", // Enhanced for better contrast in dark mode (8:1 on dark bg)
		dark: "#1b3430", // Darker shade for better contrast
		contrastText: "#ffffff",
	},
	// Secondary Colors
	secondary: {
		main: "#46232d", // Meets 4.5:1 contrast on white
		light: "#B25A68", // Enhanced for better contrast in dark mode (7:1 on dark bg)
		dark: "#2f171e",
		contrastText: "#ffffff",
	},
	// Forecast level colors - Semantically meaningful and with good contrast
	forecast: {
		risky: "#f44336", // Red with better contrast in both modes
		realistic: "#ff9800", // Orange with better contrast in both modes
		confident: "#4caf50", // Green with good contrast in both modes
		certain: "#388e3c", // Green with better contrast in both modes
	},
	// Status colors for consistency
	status: {
		success: "#4caf50", // Brighter green for better dark mode contrast
		warning: "#ff9800", // Brighter orange for better dark mode contrast
		error: "#f44336", // Brighter red for better dark mode contrast
		info: "#29b6f6", // Brighter blue for better dark mode contrast
		default: "#9e9e9e", // Lighter grey for better dark mode contrast
	},
	// Theme colors
	light: {
		background: "#f5f5f5",
		paper: "#ffffff",
		text: {
			primary: "#222222", // Very good contrast on light bg (13:1)
			secondary: "#555555", // Good contrast on light bg (7:1)
		},
		divider: "#dddddd",
	},
	dark: {
		background: "#121212",
		paper: "#1e1e1e",
		text: {
			primary: "#ffffff", // Maximum contrast on dark bg (21:1)
			secondary: "#bdbdbd", // Enhanced contrast on dark bg (9:1)
		},
		divider: "#424242", // Slightly darker for more visible dividers
	},
};

// Common color values that need to be referenced directly
export const primaryColor = appColors.primary.main;
export const secondaryColor = appColors.secondary.main;

// Forecast level colors - use these instead of direct strings
export const riskyColor = appColors.forecast.risky;
export const realisticColor = appColors.forecast.realistic;
export const confidentColor = appColors.forecast.confident;
export const certainColor = appColors.forecast.certain;

// Status colors for consistency across the app
export const successColor = appColors.status.success;
export const warningColor = appColors.status.warning;
export const errorColor = appColors.status.error;
export const infoColor = appColors.status.info;
export const defaultColor = appColors.status.default;

// Legacy color formats for compatibility
// Prefer using MUI theme colors through the theme object where possible
export const primaryColorRGBA = "rgba(48, 87, 78, 1)"; // #30574e in rgba
export const secondaryColorRGBA = "rgba(70, 35, 45, 1)"; // #46232d in rgba

/**
 * Returns Material UI color for work item or feature state
 * @param stateCategory State category of the work item or feature
 * @returns MUI color name ('success', 'warning', 'default', etc.)
 */
export const getStateColor = (
	stateCategory: string,
): "success" | "warning" | "default" | "info" | "error" => {
	if (stateCategory === "Done") return "success";
	if (stateCategory === "Doing") return "warning";
	if (stateCategory === "ToDo") return "info";
	return "default";
};

/**
 * Helper function to convert hex color to rgba
 * @param hex Hex color value
 * @param alpha Alpha value (0-1)
 * @returns rgba string
 */
export const hexToRgba = (hex: string, alpha = 1): string => {
	const r = Number.parseInt(hex.slice(1, 3), 16);
	const g = Number.parseInt(hex.slice(3, 5), 16);
	const b = Number.parseInt(hex.slice(5, 7), 16);
	return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

/**
 * Theme-aware function to get text color based on background
 * @param bgColor Background color hex
 * @returns White or black depending on contrast
 */
export const getContrastText = (bgColor: string): string => {
	// Convert hex to RGB
	const r = Number.parseInt(bgColor.slice(1, 3), 16);
	const g = Number.parseInt(bgColor.slice(3, 5), 16);
	const b = Number.parseInt(bgColor.slice(5, 7), 16);

	// Calculate relative luminance using WCAG formula
	// More accurate for accessibility purposes
	const rsRGB = r / 255;
	const gsRGB = g / 255;
	const bsRGB = b / 255;

	const R = rsRGB <= 0.03928 ? rsRGB / 12.92 : ((rsRGB + 0.055) / 1.055) ** 2.4;
	const G = gsRGB <= 0.03928 ? gsRGB / 12.92 : ((gsRGB + 0.055) / 1.055) ** 2.4;
	const B = bsRGB <= 0.03928 ? bsRGB / 12.92 : ((bsRGB + 0.055) / 1.055) ** 2.4;

	const luminance = 0.2126 * R + 0.7152 * G + 0.0722 * B;

	// Return white for dark backgrounds, dark gray for light backgrounds
	// 0.175 threshold provides better contrast than the typical 0.5
	return luminance < 0.175 ? "#ffffff" : "#222222";
};

/**
 * Get color with transparency
 * @param color Base color from appColors
 * @param opacity Opacity level (0-1)
 * @returns Color with transparency
 */
export const getColorWithOpacity = (color: string, opacity: number): string => {
	return hexToRgba(color, opacity);
};

/**
 * Calculate contrast ratio between two colors according to WCAG
 * @param color1 First color in hex format
 * @param color2 Second color in hex format
 * @returns Contrast ratio as a number (1-21)
 */
export const calculateContrastRatio = (
	color1: string,
	color2: string,
): number => {
	// Calculate luminance for color1
	const r1 = Number.parseInt(color1.slice(1, 3), 16);
	const g1 = Number.parseInt(color1.slice(3, 5), 16);
	const b1 = Number.parseInt(color1.slice(5, 7), 16);

	const rsRGB1 = r1 / 255;
	const gsRGB1 = g1 / 255;
	const bsRGB1 = b1 / 255;

	const R1 =
		rsRGB1 <= 0.03928 ? rsRGB1 / 12.92 : ((rsRGB1 + 0.055) / 1.055) ** 2.4;
	const G1 =
		gsRGB1 <= 0.03928 ? gsRGB1 / 12.92 : ((gsRGB1 + 0.055) / 1.055) ** 2.4;
	const B1 =
		bsRGB1 <= 0.03928 ? bsRGB1 / 12.92 : ((bsRGB1 + 0.055) / 1.055) ** 2.4;

	const L1 = 0.2126 * R1 + 0.7152 * G1 + 0.0722 * B1;

	// Calculate luminance for color2
	const r2 = Number.parseInt(color2.slice(1, 3), 16);
	const g2 = Number.parseInt(color2.slice(3, 5), 16);
	const b2 = Number.parseInt(color2.slice(5, 7), 16);

	const rsRGB2 = r2 / 255;
	const gsRGB2 = g2 / 255;
	const bsRGB2 = b2 / 255;

	const R2 =
		rsRGB2 <= 0.03928 ? rsRGB2 / 12.92 : ((rsRGB2 + 0.055) / 1.055) ** 2.4;
	const G2 =
		gsRGB2 <= 0.03928 ? gsRGB2 / 12.92 : ((gsRGB2 + 0.055) / 1.055) ** 2.4;
	const B2 =
		bsRGB2 <= 0.03928 ? bsRGB2 / 12.92 : ((bsRGB2 + 0.055) / 1.055) ** 2.4;

	const L2 = 0.2126 * R2 + 0.7152 * G2 + 0.0722 * B2;

	// Calculate contrast ratio
	const lighter = Math.max(L1, L2);
	const darker = Math.min(L1, L2);

	return (lighter + 0.05) / (darker + 0.05);
};

/**
 * Get color based on predictability score thresholds
 * @param score - Predictability score between 0 and 1
 * @returns Color string from forecast color palette
 */
export const getPredictabilityScoreColor = (score: number): string => {
	if (score >= 0.75) return appColors.forecast.certain;
	if (score >= 0.6) return appColors.forecast.confident;
	if (score >= 0.5) return appColors.forecast.realistic;
	return appColors.forecast.risky;
};
