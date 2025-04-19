/**
 * Application color palette
 * Central location for all color definitions
 */

export const appColors = {
	// Primary Colors
	primary: {
		main: "#30574e",
		light: "#4a9183",
		dark: "#203c36",
		contrastText: "#ffffff",
	},
	// Secondary Colors
	secondary: {
		main: "#46232d",
		light: "#8d4b57",
		dark: "#2f171e",
		contrastText: "#ffffff",
	},
	// Theme colors
	light: {
		background: "#f5f5f5",
		paper: "#ffffff",
		text: {
			primary: "#222222",
			secondary: "#555555",
		},
	},
	dark: {
		background: "#121212",
		paper: "#1e1e1e",
		text: {
			primary: "#e0e0e0",
			secondary: "#aaaaaa",
		},
	},
};

// Common color values that need to be referenced directly
export const primaryColor = appColors.primary.main;
export const secondaryColor = appColors.secondary.main;

// Legacy color formats for compatibility
export const primaryColorRGBA = "rgba(48, 87, 78, 1)"; // #30574e in rgba
export const secondaryColorRGBA = "rgba(70, 35, 45, 1)"; // #46232d in rgba
