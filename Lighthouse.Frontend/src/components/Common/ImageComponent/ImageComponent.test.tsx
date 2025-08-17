import { act, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ImageComponent from "./ImageComponent";

describe("ImageComponent", () => {
	// Mock the Image constructor and its onload event
	beforeEach(() => {
		// @ts-expect-error - mock Image constructor
		global.Image = class {
			src = "";
			width = 0;
			height = 0;
			onload: () => void = () => {};

			constructor() {
				setTimeout(() => {
					this.width = 400;
					this.height = 300;
					this.onload();
				}, 0);
			}
		};
	});

	it("renders an image with the correct src", () => {
		const src = "test-image.jpg";
		render(<ImageComponent src={src} />);

		const image = screen.getByRole("img");
		expect(image).toHaveAttribute("src", src);
	});

	it("uses provided alt text", () => {
		const alt = "Test Alt Text";
		render(<ImageComponent src="test-image.jpg" alt={alt} />);

		const image = screen.getByAltText(alt);
		expect(image).toBeInTheDocument();
	});

	it("uses default alt text when none provided", () => {
		render(<ImageComponent src="test-image.jpg" />);

		const image = screen.getByAltText("Image");
		expect(image).toBeInTheDocument();
	});

	it("sets appropriate styling properties", () => {
		render(<ImageComponent src="test-image.jpg" />);

		const image = screen.getByRole("img");
		expect(image).toHaveStyle({
			width: "100%",
			height: "auto",
			maxWidth: "100%",
			objectFit: "contain",
			borderRadius: "4px", // Equivalent to theme.shape.borderRadius * 1
		});
	});

	it("applies aspect ratio based on loaded image dimensions", async () => {
		vi.useFakeTimers();

		// Use act to properly handle state updates
		act(() => {
			render(<ImageComponent src="test-image.jpg" />);
		});

		// Allow the mock Image onload to be called
		await act(async () => {
			vi.runAllTimers();
		});

		// Get the image after state update
		const image = screen.getByRole("img");

		// Check for aspect ratio - the computed value format may vary between browsers
		// So check for either the calculated value or the actual expression
		const style = window.getComputedStyle(image);
		expect(
			style.aspectRatio === "1.3333333333333333" ||
				style.aspectRatio === "400 / 300" ||
				image.style.aspectRatio === "1.3333333333333333",
		).toBeTruthy();

		vi.useRealTimers();
	});
});
