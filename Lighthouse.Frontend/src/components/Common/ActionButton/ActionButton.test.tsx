import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ActionButton from "./ActionButton";

describe("ActionButton", () => {
	it("renders correctly", () => {
		render(<ActionButton buttonText="Click Me" onClickHandler={vi.fn()} />);
		expect(screen.getByText("Click Me")).toBeInTheDocument();
	});

	it("shows spinner when executing action", () => {
		const longRunningAction = () => new Promise<void>(() => {});

		render(
			<ActionButton buttonText="Click Me" onClickHandler={longRunningAction} />,
		);
		fireEvent.click(screen.getByText("Click Me"));

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
	});

	it("does not show spinner when action is not running", () => {
		render(<ActionButton buttonText="Click Me" onClickHandler={vi.fn()} />);
		expect(screen.queryByRole("progressbar")).toBeNull();
	});

	it("calls onClickHandler when button is clicked", () => {
		const onClickHandler = vi.fn();
		render(
			<ActionButton buttonText="Click Me" onClickHandler={onClickHandler} />,
		);
		fireEvent.click(screen.getByText("Click Me"));
		expect(onClickHandler).toHaveBeenCalled();
	});

	it("disables the button while action is running", () => {
		const longRunningAction = () => new Promise<void>(() => {});

		render(
			<ActionButton buttonText="Click Me" onClickHandler={longRunningAction} />,
		);
		fireEvent.click(screen.getByText("Click Me"));

		expect(screen.getByText("Click Me")).toBeDisabled();
	});

	it("does not disable the button when action is not running", () => {
		render(<ActionButton buttonText="Click Me" onClickHandler={vi.fn()} />);
		expect(screen.getByText("Click Me")).not.toBeDisabled();
	});

	it("displays the correct button text", () => {
		render(<ActionButton buttonText="Submit" onClickHandler={vi.fn()} />);
		expect(screen.getByText("Submit")).toBeInTheDocument();
	});

	it("sets internalIsWaiting state correctly", async () => {
		const mockOnClickHandler = vi.fn(
			(): Promise<void> => new Promise((resolve) => setTimeout(resolve, 500)),
		);

		render(
			<ActionButton
				buttonText="Click Me"
				onClickHandler={mockOnClickHandler}
			/>,
		);

		const button = screen.getByText("Click Me");
		fireEvent.click(button);

		// Check if CircularProgress is rendered (indicating internalIsWaiting is true)
		expect(screen.getByRole("progressbar")).toBeInTheDocument();

		// Wait for the internalIsWaiting state to be set to false
		await waitFor(() =>
			expect(screen.queryByRole("progressbar")).not.toBeInTheDocument(),
		);
	});
});
