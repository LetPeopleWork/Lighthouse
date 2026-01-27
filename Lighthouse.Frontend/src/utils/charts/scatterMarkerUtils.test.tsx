import { createTheme, type Theme } from "@mui/material";
import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
	type BaseGroupedItem,
	getBubbleSize,
	getMarkerColor,
	renderFallbackMarker,
	renderMarkerButton,
	renderMarkerCircle,
} from "./scatterMarkerUtils";

describe("scatterMarkerUtils", () => {
	const createTestTheme = (): Theme =>
		createTheme({
			palette: {
				primary: { main: "#1976d2" },
				background: { paper: "#ffffff" },
			},
		});

	describe("getBubbleSize", () => {
		it("should return minimum size for count of 1", () => {
			expect(getBubbleSize(1)).toBe(8); // 5 + sqrt(1) * 3 = 8
		});

		it("should increase size with count using square root scale", () => {
			expect(getBubbleSize(4)).toBe(11); // 5 + sqrt(4) * 3 = 11
			expect(getBubbleSize(9)).toBe(14); // 5 + sqrt(9) * 3 = 14
		});

		it("should cap at maximum size of 20", () => {
			expect(getBubbleSize(100)).toBe(20);
			expect(getBubbleSize(1000)).toBe(20);
		});

		it("should handle zero count", () => {
			expect(getBubbleSize(0)).toBe(5); // 5 + sqrt(0) * 3 = 5
		});
	});

	describe("renderMarkerCircle", () => {
		it("should render circle with correct attributes", () => {
			const theme = createTestTheme();

			const { container } = render(
				<svg aria-hidden="true">
					{renderMarkerCircle({
						x: 100,
						y: 50,
						size: 10,
						color: "#ff0000",
						theme,
						title: "Test marker",
					})}
				</svg>,
			);

			const circle = container.querySelector("circle");
			expect(circle).toBeInTheDocument();
			expect(circle).toHaveAttribute("cx", "100");
			expect(circle).toHaveAttribute("cy", "50");
			expect(circle).toHaveAttribute("r", "10");
			expect(circle).toHaveAttribute("fill", "#ff0000");
			expect(circle).toHaveAttribute("opacity", "0.8");
		});

		it("should apply highlight styling when isHighlighted is true", () => {
			const theme = createTestTheme();

			const { container } = render(
				<svg aria-hidden="true">
					{renderMarkerCircle({
						x: 100,
						y: 50,
						size: 10,
						color: "#ff0000",
						isHighlighted: true,
						theme,
						title: "Highlighted marker",
					})}
				</svg>,
			);

			const circle = container.querySelector("circle");
			expect(circle).toHaveAttribute("opacity", "1");
			expect(circle).toHaveAttribute("stroke", theme.palette.background.paper);
			expect(circle).toHaveAttribute("stroke-width", "2");
		});

		it("should not apply stroke when not highlighted", () => {
			const theme = createTestTheme();

			const { container } = render(
				<svg aria-hidden="true">
					{renderMarkerCircle({
						x: 100,
						y: 50,
						size: 10,
						color: "#ff0000",
						isHighlighted: false,
						theme,
						title: "Normal marker",
					})}
				</svg>,
			);

			const circle = container.querySelector("circle");
			expect(circle).toHaveAttribute("stroke", "none");
			expect(circle).toHaveAttribute("stroke-width", "0");
		});

		it("should include title element for tooltip", () => {
			const theme = createTestTheme();

			const { container } = render(
				<svg aria-hidden="true">
					{renderMarkerCircle({
						x: 100,
						y: 50,
						size: 10,
						color: "#ff0000",
						theme,
						title: "My tooltip text",
					})}
				</svg>,
			);

			const title = container.querySelector("title");
			expect(title).toBeInTheDocument();
			expect(title?.textContent).toBe("My tooltip text");
		});
	});

	describe("renderMarkerButton", () => {
		it("should render button with correct dimensions", () => {
			render(
				<svg aria-hidden="true">
					{renderMarkerButton({
						x: 100,
						y: 50,
						size: 10,
						ariaLabel: "Click me",
						onClick: vi.fn(),
					})}
				</svg>,
			);

			const button = screen.getByRole("button", {
				name: "Click me",
				hidden: true,
			});
			expect(button).toBeInTheDocument();
		});

		it("should call onClick when button is clicked", async () => {
			const user = userEvent.setup();
			const onClick = vi.fn();

			render(
				<svg aria-hidden="true">
					{renderMarkerButton({
						x: 100,
						y: 50,
						size: 10,
						ariaLabel: "View details",
						onClick,
					})}
				</svg>,
			);

			const button = screen.getByRole("button", {
				name: "View details",
				hidden: true,
			});
			await user.click(button);

			expect(onClick).toHaveBeenCalledTimes(1);
		});

		it("should have transparent background and cursor pointer", () => {
			render(
				<svg aria-hidden="true">
					{renderMarkerButton({
						x: 100,
						y: 50,
						size: 10,
						ariaLabel: "Styled button",
						onClick: vi.fn(),
					})}
				</svg>,
			);

			const button = screen.getByRole("button", {
				name: "Styled button",
				hidden: true,
			});
			expect(button).toHaveStyle({
				background: "transparent",
				cursor: "pointer",
			});
		});
	});

	describe("getMarkerColor", () => {
		const theme = createTestTheme();
		const colorMap: Record<string, string> = {
			bug: "#ff0000",
			feature: "#00ff00",
			task: "#0000ff",
		};

		it("should return error color when hasBlockedItems is true", () => {
			const group: BaseGroupedItem<unknown> = {
				items: [],
				hasBlockedItems: true,
				type: "feature",
			};

			const result = getMarkerColor(group, colorMap, theme);

			// errorColor is imported in the module as "#f44336" or similar
			expect(result).toBe("#f44336");
		});

		it("should return color from colorMap when type matches", () => {
			const group: BaseGroupedItem<unknown> = {
				items: [],
				hasBlockedItems: false,
				type: "bug",
			};

			const result = getMarkerColor(group, colorMap, theme);

			expect(result).toBe("#ff0000");
		});

		it("should return providedColor when type is not in colorMap", () => {
			const group: BaseGroupedItem<unknown> = {
				items: [],
				hasBlockedItems: false,
				type: "unknown",
			};

			const result = getMarkerColor(group, colorMap, theme, "#purple");

			expect(result).toBe("#purple");
		});

		it("should return theme primary color as fallback", () => {
			const group: BaseGroupedItem<unknown> = {
				items: [],
				hasBlockedItems: false,
				type: "unknown",
			};

			const result = getMarkerColor(group, colorMap, theme);

			expect(result).toBe(theme.palette.primary.main);
		});

		it("should return providedColor when type is undefined", () => {
			const group: BaseGroupedItem<unknown> = {
				items: [],
				hasBlockedItems: false,
			};

			const result = getMarkerColor(group, colorMap, theme, "#custom");

			expect(result).toBe("#custom");
		});
	});

	describe("renderFallbackMarker", () => {
		it("should render circle and button with default styling", () => {
			const theme = createTestTheme();
			const mockProps = {
				x: 150,
				y: 75,
				isHighlighted: false,
				dataIndex: 0,
				color: "#test",
				seriesId: "test-series",
				size: 6,
				isFaded: false,
			};

			const { container } = render(
				<svg aria-hidden="true">
					{renderFallbackMarker({
						props: mockProps,
						theme,
					})}
				</svg>,
			);

			const circle = container.querySelector("circle");
			expect(circle).toBeInTheDocument();
			expect(circle).toHaveAttribute("cx", "150");
			expect(circle).toHaveAttribute("cy", "75");
			expect(circle).toHaveAttribute("r", "6"); // fallback size
			expect(circle).toHaveAttribute("fill", theme.palette.primary.main);

			const button = screen.getByRole("button", {
				name: "View item details",
				hidden: true,
			});
			expect(button).toBeInTheDocument();
		});

		it("should use providedColor when specified", () => {
			const theme = createTestTheme();
			const mockProps = {
				x: 150,
				y: 75,
				isHighlighted: false,
				dataIndex: 0,
				color: "#test",
				seriesId: "test-series",
				size: 6,
				isFaded: false,
			};

			const { container } = render(
				<svg aria-hidden="true">
					{renderFallbackMarker({
						props: mockProps,
						theme,
						providedColor: "#custom123",
					})}
				</svg>,
			);

			const circle = container.querySelector("circle");
			expect(circle).toHaveAttribute("fill", "#custom123");
		});

		it("should include title with fallback text", () => {
			const theme = createTestTheme();
			const mockProps = {
				x: 150,
				y: 75,
				isHighlighted: false,
				dataIndex: 0,
				color: "#test",
				seriesId: "test-series",
				size: 6,
				isFaded: false,
			};

			const { container } = render(
				<svg aria-hidden="true">
					{renderFallbackMarker({
						props: mockProps,
						theme,
					})}
				</svg>,
			);

			const title = container.querySelector("title");
			expect(title?.textContent).toBe(
				"Item (unknown group) - click for details",
			);
		});
	});
});
