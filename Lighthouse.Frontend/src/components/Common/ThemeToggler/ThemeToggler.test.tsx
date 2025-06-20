import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { useTheme as mockedUseTheme } from "../../../context/ThemeContext";
import ThemeToggler from "./ThemeToggler";

const useThemeMock = vi.mocked(mockedUseTheme);

// Mock the ThemeContext hook
vi.mock("../../../context/ThemeContext", () => ({
	useTheme: vi.fn(),
}));

// Mock Material UI theme hook
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			palette: {
				mode: "light",
				primary: {
					main: "#1976d2",
				},
			},
		}),
	};
});

describe("ThemeToggler component", () => {
	const mockToggleTheme = vi.fn();

	it("renders light mode icon when theme is dark", () => {
		// Mock the useTheme hook to return dark mode
		useThemeMock.mockReturnValue({
			mode: "dark",
			toggleTheme: mockToggleTheme,
		});

		render(<ThemeToggler />);

		// LightMode icon should be present
		expect(screen.getByTestId("LightModeIcon")).toBeInTheDocument();
	});

	it("renders dark mode icon when theme is light", () => {
		// Mock the useTheme hook to return light mode
		useThemeMock.mockReturnValue({
			mode: "light",
			toggleTheme: mockToggleTheme,
		});

		render(<ThemeToggler />);

		// DarkMode icon should be present
		expect(screen.getByTestId("DarkModeIcon")).toBeInTheDocument();
	});

	it("calls toggleTheme when clicked", async () => {
		const user = userEvent.setup();

		// Mock the useTheme hook
		useThemeMock.mockReturnValue({
			mode: "light",
			toggleTheme: mockToggleTheme,
		});

		render(<ThemeToggler />);

		// Find and click the button
		const button = screen.getByRole("button");
		await user.click(button);

		// Check if toggleTheme was called
		expect(mockToggleTheme).toHaveBeenCalledTimes(1);
	});

	it("has the correct tooltip text for light mode", () => {
		// Mock the useTheme hook to return light mode
		useThemeMock.mockReturnValue({
			mode: "light",
			toggleTheme: mockToggleTheme,
		});

		render(<ThemeToggler />);

		// Check tooltip text
		expect(screen.getByRole("button")).toHaveAttribute(
			"aria-label",
			"Switch to dark mode",
		);
	});

	it("has the correct tooltip text for dark mode", () => {
		// Mock the useTheme hook to return dark mode
		useThemeMock.mockReturnValue({
			mode: "dark",
			toggleTheme: mockToggleTheme,
		});

		render(<ThemeToggler />);

		// Check tooltip text
		expect(screen.getByRole("button")).toHaveAttribute(
			"aria-label",
			"Switch to light mode",
		);
	});
});
