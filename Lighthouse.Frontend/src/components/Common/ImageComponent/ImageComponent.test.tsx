import { act, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
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

		// Allow the mock Image onload to be called and state to update
		await act(async () => {
			await vi.runAllTimersAsync();
		});

		vi.useRealTimers();

		// Wait for the component to re-render with the updated dimensions
		// Since JSDOM doesn't fully support aspect-ratio CSS or Emotion styles,
		// we verify the dimensions were loaded by checking maxHeight which is set
		// when dimensions are available
		await waitFor(() => {
			const image = screen.getByRole("img");
			const computedStyle = globalThis.getComputedStyle(image);

			// The maxHeight should be set to 300px (the mock image height)
			// This confirms the image loaded and dimensions were applied
			expect(
				computedStyle.maxHeight === "300px" ||
					image.style.maxHeight === "300px",
			).toBeTruthy();
		});
	});
});
