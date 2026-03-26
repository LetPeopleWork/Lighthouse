import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import AuthPageLayout from "./AuthPageLayout";

describe("AuthPageLayout", () => {
	it("should render the Lighthouse logo", () => {
		render(<AuthPageLayout>Content</AuthPageLayout>);
		const logo = screen.getByAltText("Lighthouse logo");
		expect(logo).toBeInTheDocument();
		expect(logo).toHaveAttribute("src", "/icons/icon-512x512.png");
	});

	it("should render the branded Lighthouse name", () => {
		render(<AuthPageLayout>Content</AuthPageLayout>);
		expect(screen.getByText("Light")).toBeInTheDocument();
		expect(screen.getByText("house")).toBeInTheDocument();
	});

	it("should render children", () => {
		render(
			<AuthPageLayout>
				<span data-testid="child">Hello</span>
			</AuthPageLayout>,
		);
		expect(screen.getByTestId("child")).toBeInTheDocument();
	});

	it("should apply the testId prop", () => {
		render(<AuthPageLayout testId="my-page">Content</AuthPageLayout>);
		expect(screen.getByTestId("my-page")).toBeInTheDocument();
	});
});
