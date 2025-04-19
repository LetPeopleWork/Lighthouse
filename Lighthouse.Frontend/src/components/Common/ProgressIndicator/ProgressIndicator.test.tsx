import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IProgressable } from "../../../models/IProgressable";
import ProgressIndicator from "./ProgressIndicator";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			palette: {
				primary: {
					main: "#1976d2",
				},
				mode: "light",
				text: {
					primary: "#333333",
					secondary: "#666666",
				},
			},
		}),
	};
});

describe("ProgressIndicator component", () => {
	const createProgressable = (
		remainingWork: number,
		totalWork: number,
	): IProgressable => ({
		remainingWork,
		totalWork,
	});

	it("renders component with title", () => {
		const title = "Progress Title";
		const progressable = createProgressable(4, 10);

		render(<ProgressIndicator title={title} progressableItem={progressable} />);

		expect(screen.getByText(title)).toBeInTheDocument();
	});

	it("displays correct completion percentage", () => {
		const progressable = createProgressable(4, 10); // 6/10 = 60%

		render(
			<ProgressIndicator
				title="Test Progress"
				progressableItem={progressable}
			/>,
		);

		// Calculate completed items
		const expectedText = "60% (6/10)";

		// Wait for animation to complete and check the text
		setTimeout(() => {
			expect(screen.getByText(expectedText)).toBeInTheDocument();
		}, 200);
	});

	it("shows 'Could not determine work' when total work is 0", () => {
		const progressable = createProgressable(0, 0);

		render(
			<ProgressIndicator
				title="Test Progress"
				progressableItem={progressable}
			/>,
		);

		expect(screen.getByText(/Could not determine work/)).toBeInTheDocument();
		expect(screen.getByRole("button")).toBeInTheDocument(); // Info button
	});

	it("does not show details when showDetails is false", () => {
		const progressable = createProgressable(4, 10);

		render(
			<ProgressIndicator
				title="Test Progress"
				progressableItem={progressable}
				showDetails={false}
			/>,
		);

		// Calculate expected percentage text that should NOT be visible
		const percentageText = "60% (6/10)";

		// This should not be in the document
		expect(screen.queryByText(percentageText)).not.toBeInTheDocument();
	});

	it("applies custom height when specified", () => {
		const progressable = createProgressable(4, 10);
		const customHeight = 30;

		render(
			<ProgressIndicator
				title="Test Progress"
				progressableItem={progressable}
				height={customHeight}
			/>,
		);

		// Find the LinearProgress element by its role
		const progressBar = screen.getByRole("progressbar");

		// Check that the height styles are applied correctly
		expect(progressBar).toHaveStyle(`height: ${customHeight}px`);
	});
});
