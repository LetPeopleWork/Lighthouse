/**
 * Standard theme object for testing
 * Includes all theme extensions to make tests consistent
 */

export const testTheme = {
	palette: {
		primary: {
			main: "#1976d2",
			light: "#42a5f5",
			dark: "#1565c0",
			contrastText: "#ffffff",
		},
		secondary: {
			main: "#dc004e",
			light: "#ff5c8d",
			dark: "#9a0036",
			contrastText: "#ffffff",
		},
		success: {
			main: "#4caf50",
			light: "#81c784",
			dark: "#388e3c",
		},
		warning: {
			main: "#ff9800",
			light: "#ffb74d",
			dark: "#f57c00",
		},
		error: {
			main: "#f44336",
			light: "#e57373",
			dark: "#d32f2f",
		},
		common: {
			black: "#000",
			white: "#fff",
		},
		text: {
			primary: "#000000",
			secondary: "#555555",
			disabled: "#00000061",
		},
		background: {
			paper: "#ffffff",
			default: "#f5f5f5",
		},
		grey: {
			50: "#fafafa",
			100: "#f5f5f5",
			200: "#eeeeee",
			300: "#e0e0e0",
			400: "#bdbdbd",
			500: "#9e9e9e",
			600: "#757575",
			700: "#616161",
			800: "#424242",
			900: "#212121",
		},
		action: {
			active: "rgba(0, 0, 0, 0.54)",
			hover: "rgba(0, 0, 0, 0.08)",
			selected: "rgba(0, 0, 0, 0.14)",
			disabled: "rgba(0, 0, 0, 0.26)",
			disabledBackground: "rgba(0, 0, 0, 0.12)",
		},
		divider: "#dddddd",
		mode: "light",
	},
	spacing: (factor: number) => `${factor * 8}px`,
	shape: {
		borderRadius: 4,
	},
	shadows: [
		"none",
		"0px 2px 1px -1px rgba(0,0,0,0.2),0px 1px 1px 0px rgba(0,0,0,0.14),0px 1px 3px 0px rgba(0,0,0,0.12)",
		// ...more shadows, just using index 1 as a representative example
	],
	typography: {
		fontFamily: "'Roboto', 'Helvetica', 'Arial', sans-serif",
	},
	transitions: {
		create: () => "color 0.2s ease",
	},
	zIndex: {
		modal: 1300,
	},
	// Our custom theme extensions
	customShadows: {
		subtle: "0 1px 3px rgba(0,0,0,0.08)",
		medium: "0 2px 6px rgba(0,0,0,0.12)",
		strong: "0 4px 10px rgba(0,0,0,0.16)",
		text: "none",
		none: "none",
	},
	emphasis: {
		high: 500,
		medium: 400,
		low: 300,
		disabled: 300,
	},
	effects: {
		hover: {
			opacity: 0.8,
			background: "rgba(0,0,0,0.06)",
			backgroundOpacity: 0.06,
		},
	},
	opacity: {
		subtle: 0.04,
		medium: 0.08,
		high: 0.12,
		opaque: 1,
	},
	assets: {
		logoVariant: "light" as "light" | "dark",
		isDark: false,
	},
};
