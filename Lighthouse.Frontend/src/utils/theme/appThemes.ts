import { createTheme, type ThemeOptions } from "@mui/material/styles";
import { appColors } from "./colors";
import { extendTheme } from "./themeExtensions";

const STATUS_PALETTE = {
	success: {
		main: appColors.status.success,
	},
	warning: {
		main: appColors.status.warning,
	},
	error: {
		main: appColors.status.error,
	},
	info: {
		main: appColors.status.info,
	},
};

const TYPOGRAPHY: ThemeOptions["typography"] = {
	fontFamily: "'Quicksand', 'Roboto', 'Arial', sans-serif",
	h1: {
		fontWeight: 600,
	},
	h2: {
		fontWeight: 600,
	},
	h3: {
		fontWeight: 600,
	},
};

const lightThemeBase = createTheme({
	palette: {
		mode: "light",
		primary: {
			main: appColors.primary.main,
			light: appColors.primary.light,
			dark: appColors.primary.dark,
			contrastText: appColors.primary.contrastText,
		},
		secondary: {
			main: appColors.secondary.main,
			light: appColors.secondary.light,
			dark: appColors.secondary.dark,
			contrastText: appColors.secondary.contrastText,
		},
		...STATUS_PALETTE,
		background: {
			default: appColors.light.background,
			paper: appColors.light.paper,
		},
		text: {
			primary: appColors.light.text.primary,
			secondary: appColors.light.text.secondary,
		},
		divider: appColors.light.divider,
	},
	typography: TYPOGRAPHY,
	components: {
		MuiCard: {
			styleOverrides: {
				root: {
					borderRadius: 12,
					boxShadow: "0 4px 12px 0 rgba(0,0,0,0.05)",
				},
			},
		},
		MuiButton: {
			styleOverrides: {
				root: {
					borderRadius: 8,
					textTransform: "none",
				},
			},
		},
	},
});

export const lightTheme = extendTheme(lightThemeBase);

/**
 * On a dark ground the brand's standard shades lose contrast, so the dark theme leads with the lighter ones, and
 * gives cards, outlined buttons, dividers and row hovers firmer edges than the light theme needs.
 */
const darkThemeBase = createTheme({
	palette: {
		mode: "dark",
		primary: {
			main: appColors.primary.light,
			light: appColors.primary.light,
			dark: appColors.primary.main,
			contrastText: appColors.primary.contrastText,
		},
		secondary: {
			main: appColors.secondary.light,
			light: appColors.secondary.light,
			dark: appColors.secondary.main,
			contrastText: appColors.secondary.contrastText,
		},
		...STATUS_PALETTE,
		background: {
			default: appColors.dark.background,
			paper: appColors.dark.paper,
		},
		text: {
			primary: appColors.dark.text.primary,
			secondary: appColors.dark.text.secondary,
		},
		divider: appColors.dark.divider,
	},
	typography: TYPOGRAPHY,
	components: {
		MuiCard: {
			styleOverrides: {
				root: {
					borderRadius: 12,
					boxShadow: "0 4px 12px 0 rgba(0,0,0,0.3)",
					border: "1px solid rgba(255,255,255,0.12)",
				},
			},
		},
		MuiButton: {
			styleOverrides: {
				root: {
					borderRadius: 8,
					textTransform: "none",
				},
				outlined: {
					borderWidth: "2px",
				},
			},
		},
		MuiDivider: {
			styleOverrides: {
				root: {
					opacity: 0.6,
				},
			},
		},
		MuiTableRow: {
			styleOverrides: {
				root: {
					"&:hover": {
						backgroundColor: "rgba(255,255,255,0.08)",
					},
				},
			},
		},
	},
});

export const darkTheme = extendTheme(darkThemeBase);
