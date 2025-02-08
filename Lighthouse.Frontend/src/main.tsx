import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.tsx";

import {
	type ThemeOptions,
	ThemeProvider,
	createTheme,
} from "@mui/material/styles";

const themeOptions: ThemeOptions = createTheme({
	palette: {
		mode: "light",
		primary: {
			main: "#30574e",
		},
		secondary: {
			main: "#46232d",
		},
	},
});

const rootElement = document.getElementById("root");
if (rootElement) {
	ReactDOM.createRoot(rootElement).render(
		<React.StrictMode>
			<ThemeProvider theme={themeOptions}>
				<App />
			</ThemeProvider>
		</React.StrictMode>,
	);
}
