import type React from "react";
import { createContext, useContext, useEffect, useState } from "react";

type ThemeMode = "light" | "dark";

interface ThemeContextType {
	mode: ThemeMode;
	toggleTheme: () => void;
}

const ThemeContext = createContext<ThemeContextType>({
	mode: "light",
	toggleTheme: () => {},
});

export const useTheme = () => useContext(ThemeContext);

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({
	children,
}) => {
	const [mode, setMode] = useState<ThemeMode>(() => {
		// Check if theme preference is saved in localStorage
		const savedTheme = localStorage.getItem("theme") as ThemeMode;
		return (
			savedTheme ||
			(window.matchMedia("(prefers-color-scheme: dark)").matches
				? "dark"
				: "light")
		);
	});

	const toggleTheme = () => {
		setMode((prevMode) => (prevMode === "light" ? "dark" : "light"));
	};

	// Save theme preference to localStorage whenever it changes
	useEffect(() => {
		localStorage.setItem("theme", mode);
	}, [mode]);

	return (
		<ThemeContext.Provider value={{ mode, toggleTheme }}>
			{children}
		</ThemeContext.Provider>
	);
};

export default ThemeContext;
