import { CssBaseline } from "@mui/material";
import {
	ThemeProvider as MuiThemeProvider,
	createTheme,
} from "@mui/material/styles";
import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.tsx";
import { ThemeProvider, useTheme } from "./context/ThemeContext";
import { appColors } from "./utils/theme/colors";
import { extendTheme } from "./utils/theme/themeExtensions";
import "reflect-metadata";

// Light theme base
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
	typography: {
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
	},
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

// Apply extensions to light theme
const lightTheme = extendTheme(lightThemeBase);

// Dark theme base with optimized contrast
const darkThemeBase = createTheme({
	palette: {
		mode: "dark",
		primary: {
			main: appColors.primary.light, // Using enhanced lighter shade for dark mode for better contrast
			light: appColors.primary.light,
			dark: appColors.primary.main, // Using standard color as dark in dark mode
			contrastText: appColors.primary.contrastText,
		},
		secondary: {
			main: appColors.secondary.light, // Using enhanced lighter shade for dark mode for better contrast
			light: appColors.secondary.light,
			dark: appColors.secondary.main, // Using standard color as dark in dark mode
			contrastText: appColors.secondary.contrastText,
		},
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
	typography: {
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
	},
	components: {
		MuiCard: {
			styleOverrides: {
				root: {
					borderRadius: 12,
					boxShadow: "0 4px 12px 0 rgba(0,0,0,0.3)",
					border: "1px solid rgba(255,255,255,0.12)", // Adds subtle border for better card visibility
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
					borderWidth: "2px", // Thicker borders for better visibility in dark mode
				},
			},
		},
		MuiDivider: {
			styleOverrides: {
				root: {
					opacity: 0.6, // Make dividers more visible
				},
			},
		},
		MuiTableRow: {
			styleOverrides: {
				root: {
					"&:hover": {
						backgroundColor: "rgba(255,255,255,0.08)", // More visible hover effect
					},
				},
			},
		},
	},
});

// Apply extensions to dark theme
const darkTheme = extendTheme(darkThemeBase);

// This component will use the ThemeContext and apply the appropriate MUI theme
const MuiThemeWrapper: React.FC<{ children: React.ReactNode }> = ({
	children,
}) => {
	const { mode } = useTheme();
	const theme = mode === "light" ? lightTheme : darkTheme;

	return (
		<MuiThemeProvider theme={theme}>
			<CssBaseline />
			{children}
		</MuiThemeProvider>
	);
};

// Main app wrapper with theme providers
const AppWithTheme: React.FC = () => {
	return (
		<ThemeProvider>
			<MuiThemeWrapper>
				<App />
			</MuiThemeWrapper>
		</ThemeProvider>
	);
};

const rootElement = document.getElementById("root");
if (rootElement) {
	ReactDOM.createRoot(rootElement).render(
		<React.StrictMode>
			<AppWithTheme />
		</React.StrictMode>,
	);
}
