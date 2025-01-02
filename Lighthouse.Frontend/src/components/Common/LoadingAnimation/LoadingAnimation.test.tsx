import { render, screen } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";
import LoadingAnimation from "./LoadingAnimation";

vi.mock("@mui/material", () => {
	return {
		CircularProgress: () => <div data-testid="sync-loader">Loading...</div>,
	};
});

describe("LoadingAnimation", () => {
	test("displays loading indicator when isLoading is true", async () => {
		render(
			<LoadingAnimation isLoading={true} hasError={false}>
				<div>Content</div>
			</LoadingAnimation>,
		);

		expect(screen.getByTestId("sync-loader")).toBeInTheDocument();
	});

	test("displays error message when hasError is true", async () => {
		render(
			<LoadingAnimation isLoading={false} hasError={true}>
				<div>Content</div>
			</LoadingAnimation>,
		);

		expect(
			screen.getByText("Error loading data. Please try again later."),
		).toBeInTheDocument();
	});

	test("displays children when not loading and no error", async () => {
		render(
			<LoadingAnimation isLoading={false} hasError={false}>
				<div>Content</div>
			</LoadingAnimation>,
		);

		expect(screen.getByText("Content")).toBeInTheDocument();
	});
});
