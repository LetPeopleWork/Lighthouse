/**
 * Theme extension utilities
 *
 * This file contains theme extensions and custom properties to replace
 * conditional theme.palette.mode checks with more maintainable properties.
 */
import type { Theme as MuiTheme } from "@mui/material/styles";
import { hexToRgba } from "./colors";

// Extend the Theme type with our custom properties
declare module "@mui/material/styles" {
	interface Theme {
		customShadows: {
			subtle: string;
			medium: string;
			strong: string;
			text: string;
			none: string;
		};
		emphasis: {
			high: number;
			medium: number;
			low: number;
			disabled: number;
		};
		effects: {
			hover: {
				opacity: number;
				background: string;
				backgroundOpacity: number;
			};
		};
		opacity: {
			subtle: number;
			medium: number;
			high: number;
			opaque: number;
		};
		assets: {
			logoVariant: "light" | "dark";
			isDark: boolean;
		};
	}
	// Allow configuration using `createTheme`
	interface ThemeOptions {
		customShadows?: {
			subtle?: string;
			medium?: string;
			strong?: string;
			text?: string;
			none?: string;
		};
		emphasis?: {
			high?: number;
			medium?: number;
			low?: number;
			disabled?: number;
		};
		effects?: {
			hover?: {
				opacity?: number;
				background?: string;
				backgroundOpacity?: number;
			};
		};
		opacity?: {
			subtle?: number;
			medium?: number;
			high?: number;
			opaque?: number;
		};
		assets?: {
			logoVariant?: "light" | "dark";
			isDark?: boolean;
		};
	}
}

export const getThemeExtensions = (theme: MuiTheme): Partial<MuiTheme> => {
	const isDark = theme.palette.mode === "dark";

	return {
		// Custom shadows based on theme mode
		customShadows: {
			subtle: isDark
				? "0 1px 3px rgba(0,0,0,0.2)"
				: "0 1px 3px rgba(0,0,0,0.08)",
			medium: isDark
				? "0 4px 8px rgba(0,0,0,0.4)"
				: "0 2px 6px rgba(0,0,0,0.12)",
			strong: isDark
				? "0 8px 16px rgba(0,0,0,0.6)"
				: "0 4px 10px rgba(0,0,0,0.16)",
			text: isDark ? "0 1px 2px rgba(0,0,0,0.4)" : "none",
			none: "none",
		},

		// Font weight and visual emphasis based on theme mode
		emphasis: {
			high: isDark ? 600 : 500,
			medium: isDark ? 500 : 400,
			low: isDark ? 400 : 300,
			disabled: isDark ? 400 : 300,
		},

		// Hover and interaction effects
		effects: {
			hover: {
				opacity: isDark ? 0.9 : 0.8,
				background: isDark
					? hexToRgba(theme.palette.common.white, 0.15)
					: hexToRgba(theme.palette.common.black, 0.06),
				backgroundOpacity: isDark ? 0.2 : 0.06,
			},
		},

		// General opacity values based on theme
		opacity: {
			subtle: isDark ? 0.08 : 0.04,
			medium: isDark ? 0.15 : 0.08,
			high: isDark ? 0.3 : 0.12,
			opaque: 1,
		},
		// Asset selection based on theme
		assets: {
			logoVariant: isDark ? "dark" : "light",
			isDark,
		},
	};
};

/**
 * Applies theme extensions to an existing theme object
 * @param theme MUI Theme to extend
 * @returns Theme with extensions applied
 */
export const extendTheme = (theme: MuiTheme): MuiTheme => {
	const extensions = getThemeExtensions(theme);

	return {
		...theme,
		...extensions,
	};
};
