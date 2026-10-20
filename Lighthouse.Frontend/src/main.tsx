import "@fontsource-variable/quicksand";
import { CssBaseline } from "@mui/material";
import { ThemeProvider as MuiThemeProvider } from "@mui/material/styles";
import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.tsx";
import AppErrorBoundary from "./components/Common/ErrorBoundary/AppErrorBoundary";
import { ThemeProvider, useTheme } from "./context/ThemeContext";
import { darkTheme, lightTheme } from "./utils/theme/appThemes";

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

const AppWithTheme: React.FC = () => {
	return (
		<ThemeProvider>
			<MuiThemeWrapper>
				<AppErrorBoundary>
					<App />
				</AppErrorBoundary>
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
