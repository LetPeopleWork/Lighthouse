import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ThemeProvider, useTheme } from "./ThemeContext";

// Mock localStorage
const localStorageMock = (() => {
	let store: Record<string, string> = {};
	return {
		getItem: (key: string) => store[key] || null,
		setItem: (key: string, value: string) => {
			store[key] = value;
		},
		clear: () => {
			store = {};
		},
	};
})();

// Mock matchMedia
const mockMatchMedia = (matches: boolean) => {
	Object.defineProperty(window, "matchMedia", {
		writable: true,
		value: vi.fn().mockImplementation((query) => ({
			matches,
			media: query,
			onchange: null,
			addListener: vi.fn(),
			removeListener: vi.fn(),
			addEventListener: vi.fn(),
			removeEventListener: vi.fn(),
			dispatchEvent: vi.fn(),
		})),
	});
};

// Set up localStorage mock before each test
beforeEach(() => {
	Object.defineProperty(window, "localStorage", { value: localStorageMock });
	localStorageMock.clear();
});

// TestComponent to access the theme context in tests
const TestComponent = () => {
	const { mode, toggleTheme } = useTheme();
	return (
		<div>
			<span data-testid="theme-mode">{mode}</span>
			<button type="button" data-testid="toggle-button" onClick={toggleTheme}>
				Toggle Theme
			</button>
		</div>
	);
};

describe("ThemeContext", () => {
	test("uses light theme by default when no preferences exist", () => {
		// Mock matchMedia to return false for dark mode preference
		mockMatchMedia(false);

		render(
			<ThemeProvider>
				<TestComponent />
			</ThemeProvider>,
		);

		expect(screen.getByTestId("theme-mode")).toHaveTextContent("light");
	});

	test("uses dark theme when system prefers dark mode", () => {
		// Mock matchMedia to return true for dark mode preference
		mockMatchMedia(true);

		render(
			<ThemeProvider>
				<TestComponent />
			</ThemeProvider>,
		);

		expect(screen.getByTestId("theme-mode")).toHaveTextContent("dark");
	});

	test("uses theme from localStorage when available", () => {
		// Set localStorage theme preference
		localStorageMock.setItem("theme", "dark");

		render(
			<ThemeProvider>
				<TestComponent />
			</ThemeProvider>,
		);

		expect(screen.getByTestId("theme-mode")).toHaveTextContent("dark");
	});

	test("toggles theme when toggle function is called", async () => {
		// Start with light theme
		mockMatchMedia(false);

		const user = userEvent.setup();

		render(
			<ThemeProvider>
				<TestComponent />
			</ThemeProvider>,
		);

		// Initial state is light
		expect(screen.getByTestId("theme-mode")).toHaveTextContent("light");

		// Toggle theme to dark
		await user.click(screen.getByTestId("toggle-button"));
		expect(screen.getByTestId("theme-mode")).toHaveTextContent("dark");

		// Toggle theme back to light
		await user.click(screen.getByTestId("toggle-button"));
		expect(screen.getByTestId("theme-mode")).toHaveTextContent("light");
	});

	test("saves theme preference to localStorage when theme changes", async () => {
		// Start with light theme
		mockMatchMedia(false);

		const user = userEvent.setup();

		render(
			<ThemeProvider>
				<TestComponent />
			</ThemeProvider>,
		);

		// Toggle theme to dark
		await user.click(screen.getByTestId("toggle-button"));

		// Check if localStorage was updated
		expect(localStorageMock.getItem("theme")).toBe("dark");

		// Toggle theme back to light
		await user.click(screen.getByTestId("toggle-button"));

		// Check if localStorage was updated
		expect(localStorageMock.getItem("theme")).toBe("light");
	});
});
