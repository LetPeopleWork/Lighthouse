import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import ImageComponent from "./ImageComponent";

describe("ImageComponent", () => {
	beforeEach(() => {
		// @ts-expect-error - mock Image constructor
		globalThis.Image = class {
			src = "";
			width = 0;
			height = 0;
			onload: (() => void) | null = null;

			constructor() {
				// Simulate image loading asynchronously
				setTimeout(() => {
					this.width = 400;
					this.height = 300;
					if (this.onload) {
						this.onload();
					}
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
		render(<ImageComponent src="test-image.jpg" />);

		// Wait for the image to load and the component to update with aspect ratio
		// MUI sx prop creates CSS classes, so check the rendered element's computed dimensions
		await waitFor(
			() => {
				const image = screen.getByRole("img");
				// Check if the image has been styled with maxHeight (which is set alongside aspectRatio)
				// This indicates the state update has occurred
				const computedStyle = globalThis.getComputedStyle(image);
				// maxHeight is set to imageDimensions.height when dimensions are loaded
				expect(computedStyle.maxHeight).not.toBe("none");
				expect(computedStyle.maxHeight).toBe("300px");
			},
			{ timeout: 1000 },
		);
	});
});
