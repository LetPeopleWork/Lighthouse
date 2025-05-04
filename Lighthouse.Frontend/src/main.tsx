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
import "reflect-metadata";

// Light theme
const lightTheme = createTheme({
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
		background: {
			default: appColors.light.background,
			paper: appColors.light.paper,
		},
		text: {
			primary: appColors.light.text.primary,
			secondary: appColors.light.text.secondary,
		},
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

// Dark theme
const darkTheme = createTheme({
	palette: {
		mode: "dark",
		primary: {
			main: appColors.primary.light, // Using lighter shade for dark mode
			light: appColors.primary.main,
			dark: appColors.primary.dark,
			contrastText: appColors.primary.contrastText,
		},
		secondary: {
			main: appColors.secondary.light, // Using lighter shade for dark mode
			light: appColors.secondary.main,
			dark: appColors.secondary.dark,
			contrastText: appColors.secondary.contrastText,
		},
		background: {
			default: appColors.dark.background,
			paper: appColors.dark.paper,
		},
		text: {
			primary: appColors.dark.text.primary,
			secondary: appColors.dark.text.secondary,
		},
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
					boxShadow: "0 4px 12px 0 rgba(0,0,0,0.15)",
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
