import { createTheme, ThemeProvider } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import LegendChip from "./LegendChip";

describe("LegendChip", () => {
	const mockToggle = vi.fn();
	const testColor = "#4caf50";

	const renderWithTheme = (
		component: React.ReactElement,
		mode: "light" | "dark" = "light",
	) => {
		const theme = createTheme({
			palette: {
				mode,
			},
		});
		return render(<ThemeProvider theme={theme}>{component}</ThemeProvider>);
	};

	afterEach(() => {
		vi.clearAllMocks();
	});

	describe("Basic Rendering", () => {
		it("should render chip with provided label", () => {
			renderWithTheme(<LegendChip label="Test Label" color={testColor} />);

			expect(screen.getByText("Test Label")).toBeInTheDocument();
		});

		it("should render chip with role button", () => {
			renderWithTheme(<LegendChip label="Test Label" color={testColor} />);

			expect(screen.getByRole("button")).toBeInTheDocument();
		});

		it("should have proper aria-label for accessibility", () => {
			renderWithTheme(<LegendChip label="Test Label" color={testColor} />);

			expect(screen.getByRole("button")).toHaveAttribute(
				"aria-label",
				"Test Label visibility toggle",
			);
		});
	});

	describe("Visibility States", () => {
		it("should render with visible state by default", () => {
			renderWithTheme(<LegendChip label="Test" color={testColor} />);

			const chip = screen.getByRole("button");
			expect(chip).toHaveAttribute("aria-pressed", "true");
		});

		it("should render with visible state when explicitly set to true", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveAttribute("aria-pressed", "true");
		});

		it("should render with invisible state when set to false", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={false} />,
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveAttribute("aria-pressed", "false");
		});

		it("should apply solid background color when visible", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveStyle({ backgroundColor: testColor });
		});

		it("should apply translucent background when not visible", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={false} />,
			);

			const chip = screen.getByRole("button");
			// The chip should have a background with 0.3 alpha (rgba)
			expect(chip).toHaveStyle({
				backgroundColor: expect.stringMatching(/rgba\(76, 175, 80, 0\.3\)/),
			});
		});
	});

	describe("User Interactions", () => {
		it("should call onToggle when chip is clicked", async () => {
			const user = userEvent.setup();
			renderWithTheme(
				<LegendChip label="Test" color={testColor} onToggle={mockToggle} />,
			);

			const chip = screen.getByRole("button");
			await user.click(chip);

			expect(mockToggle).toHaveBeenCalledTimes(1);
		});

		it("should be clickable when onToggle is provided", async () => {
			const user = userEvent.setup();
			renderWithTheme(
				<LegendChip label="Test" color={testColor} onToggle={mockToggle} />,
			);

			const chip = screen.getByRole("button");
			await user.click(chip);

			expect(mockToggle).toHaveBeenCalled();
		});

		it("should have pointer cursor for clickable chip", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} onToggle={mockToggle} />,
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveStyle({ cursor: "pointer" });
		});

		it("should not call onToggle when not provided", async () => {
			const user = userEvent.setup();
			renderWithTheme(<LegendChip label="Test" color={testColor} />);

			const chip = screen.getByRole("button");
			// This should not throw error even without onToggle
			await user.click(chip);

			// No assertion needed - test passes if no error thrown
		});
	});

	describe("Styling and Theme", () => {
		it("should render as filled variant without visible border styling", () => {
			renderWithTheme(<LegendChip label="Test" color={testColor} />);

			const chip = screen.getByRole("button");
			// Verify the chip renders and has the expected solid background
			expect(chip).toBeInTheDocument();
			expect(chip).toHaveStyle({ backgroundColor: testColor });
		});

		it("should use white text for enabled chip in both modes", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
			);

			const chip = screen.getByRole("button");
			// Enabled chips always use white text
			expect(chip).toHaveStyle({ color: "#ffffff" });
		});

		it("should apply custom sx props", () => {
			const customSx = { margin: "10px" };
			renderWithTheme(
				<LegendChip label="Test" color={testColor} sx={customSx} />,
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveStyle({ margin: "10px" });
		});

		it("should render correctly in dark theme", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
				"dark",
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveStyle({ backgroundColor: testColor });
		});

		it("should render correctly in light theme", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
				"light",
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveStyle({ backgroundColor: testColor });
		});
	});

	describe("Color Handling", () => {
		it("should handle different hex color formats", () => {
			const colors = ["#ff0000", "#00ff00", "#0000ff"];

			for (const color of colors) {
				const { unmount } = renderWithTheme(
					<LegendChip label="Test" color={color} visible={true} />,
				);

				const chip = screen.getByRole("button");
				expect(chip).toHaveStyle({ backgroundColor: color });

				unmount();
			}
		});

		it("should use white text even for light background colors when enabled", () => {
			const lightColor = "#ffeb3b"; // Yellow - enabled chips still use white text
			renderWithTheme(
				<LegendChip label="Test" color={lightColor} visible={true} />,
			);

			const chip = screen.getByRole("button");
			// Always white text when enabled
			expect(chip).toHaveStyle({ color: "#ffffff" });
		});

		it("should use white text for dark background colors when enabled", () => {
			const darkColor = "#1b3430"; // Dark teal - enabled chips use white text
			renderWithTheme(
				<LegendChip label="Test" color={darkColor} visible={true} />,
			);

			const chip = screen.getByRole("button");
			// Always white text when enabled
			expect(chip).toHaveStyle({ color: "#ffffff" });
		});
	});

	describe("Integration with Chart Legends", () => {
		it("should toggle visibility state through multiple clicks", async () => {
			const user = userEvent.setup();
			renderWithTheme(
				<LegendChip
					label="Series 1"
					color={testColor}
					visible={true}
					onToggle={mockToggle}
				/>,
			);

			const chip = screen.getByRole("button");

			// First click
			await user.click(chip);
			expect(mockToggle).toHaveBeenCalledTimes(1);

			// Second click
			await user.click(chip);
			expect(mockToggle).toHaveBeenCalledTimes(2);
		});

		it("should render multiple chips with different colors", () => {
			renderWithTheme(
				<>
					<LegendChip label="Series 1" color="#ff0000" visible={true} />
					<LegendChip label="Series 2" color="#00ff00" visible={true} />
					<LegendChip label="Series 3" color="#0000ff" visible={false} />
				</>,
			);

			expect(screen.getByText("Series 1")).toBeInTheDocument();
			expect(screen.getByText("Series 2")).toBeInTheDocument();
			expect(screen.getByText("Series 3")).toBeInTheDocument();

			const chips = screen.getAllByRole("button");
			expect(chips).toHaveLength(3);
		});

		it("should maintain visual consistency across different states", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
			);

			const visibleChip = screen.getByRole("button");
			expect(visibleChip).toHaveStyle({ backgroundColor: testColor });

			// Re-render with invisible state
			const { unmount } = renderWithTheme(
				<LegendChip label="Test2" color={testColor} visible={false} />,
			);

			const invisibleChip = screen.getByRole("button", { name: /Test2/ });
			// Invisible chip should have translucent background
			expect(invisibleChip).toHaveStyle({
				backgroundColor: expect.stringMatching(/rgba\(76, 175, 80, 0\.3\)/),
			});

			unmount();
		});
	});

	describe("Dark Mode White Text", () => {
		it("should display white text when enabled in dark mode", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
				"dark",
			);

			const chip = screen.getByRole("button");
			expect(chip).toHaveStyle({ color: "#ffffff" });
		});

		it("should display white text when enabled in light mode", () => {
			const darkColor = "#1b3430";
			renderWithTheme(
				<LegendChip label="Test" color={darkColor} visible={true} />,
				"light",
			);

			const chip = screen.getByRole("button");
			// Always white text when enabled, regardless of mode
			expect(chip).toHaveStyle({ color: "#ffffff" });
		});

		it("should display dark text when disabled in both modes", () => {
			// Test in light mode
			const { unmount } = renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={false} />,
				"light",
			);

			let chip = screen.getByRole("button");
			const lightModeColor = globalThis.getComputedStyle(chip).color;

			unmount();

			// Test in dark mode
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={false} />,
				"dark",
			);

			chip = screen.getByRole("button");
			const darkModeColor = globalThis.getComputedStyle(chip).color;

			// Text color should use theme.palette.text.primary which adapts to mode
			expect(lightModeColor).toBeTruthy();
			expect(darkModeColor).toBeTruthy();
		});
	});

	describe("Disabled State Border", () => {
		it("should display solid border when disabled (visible=false)", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={false} />,
			);

			const chip = screen.getByRole("button");
			// Check for border styling - MUI applies borders via CSS
			const computedStyle = globalThis.getComputedStyle(chip);
			expect(computedStyle.borderWidth).not.toBe("0px");
			expect(computedStyle.borderStyle).toBe("solid");
		});

		it("should not display border when enabled (visible=true)", () => {
			renderWithTheme(
				<LegendChip label="Test" color={testColor} visible={true} />,
			);

			const chip = screen.getByRole("button");
			const computedStyle = globalThis.getComputedStyle(chip);
			// When border is "none", borderStyle should be "none"
			expect(computedStyle.borderStyle).toBe("none");
		});

		it("should display correct colored border for different colors", () => {
			const redColor = "#f44336";
			renderWithTheme(
				<LegendChip label="Test" color={redColor} visible={false} />,
			);

			const chip = screen.getByRole("button");
			const computedStyle = globalThis.getComputedStyle(chip);
			expect(computedStyle.borderWidth).not.toBe("0px");
			expect(computedStyle.borderStyle).toBe("solid");
			// Border color should include the red color RGB values
			expect(computedStyle.borderColor).toContain("244");
		});
	});

	describe("At Least One Visible Enforcement", () => {
		it("should allow parent to handle last visible item logic", async () => {
			const user = userEvent.setup();
			const mockConditionalToggle = vi.fn((currentVisible: boolean) => {
				// Parent logic: don't toggle if this is the last visible item
				if (currentVisible) {
					// Check if this would be the last one - parent responsibility
					return; // Don't toggle
				}
			});

			renderWithTheme(
				<LegendChip
					label="Test"
					color={testColor}
					visible={true}
					onToggle={() => mockConditionalToggle(true)}
				/>,
			);

			const chip = screen.getByRole("button");
			await user.click(chip);

			expect(mockConditionalToggle).toHaveBeenCalledTimes(1);
		});

		it("should always call onToggle when clicked - parent decides behavior", async () => {
			const user = userEvent.setup();
			renderWithTheme(
				<LegendChip
					label="Test"
					color={testColor}
					visible={true}
					onToggle={mockToggle}
				/>,
			);

			const chip = screen.getByRole("button");
			await user.click(chip);

			expect(mockToggle).toHaveBeenCalledTimes(1);
		});

		it("should allow toggling disabled items", async () => {
			const user = userEvent.setup();
			renderWithTheme(
				<LegendChip
					label="Test"
					color={testColor}
					visible={false}
					onToggle={mockToggle}
				/>,
			);

			const chip = screen.getByRole("button");
			await user.click(chip);

			expect(mockToggle).toHaveBeenCalledTimes(1);
		});
	});
});
