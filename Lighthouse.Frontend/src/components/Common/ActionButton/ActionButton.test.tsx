import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ExpectedTestFailure } from "../../../tests/ExpectedTestFailure";
import { testTheme } from "../../../tests/testTheme";
import ActionButton from "./ActionButton";

// Mock the useTheme hook
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			...testTheme,
			palette: {
				...testTheme.palette,
				common: {
					white: "#ffffff",
					black: "#000000",
				},
			},
			shadows: [
				"none",
				"0px 2px 1px -1px rgba(0,0,0,0.2),0px 1px 1px 0px rgba(0,0,0,0.14),0px 1px 3px 0px rgba(0,0,0,0.12)",
				"0px 3px 1px -2px rgba(0,0,0,0.2),0px 2px 2px 0px rgba(0,0,0,0.14),0px 1px 5px 0px rgba(0,0,0,0.12)",
				"0px 3px 3px -2px rgba(0,0,0,0.2),0px 3px 4px 0px rgba(0,0,0,0.14),0px 1px 8px 0px rgba(0,0,0,0.12)",
				"0px 2px 4px -1px rgba(0,0,0,0.2),0px 4px 5px 0px rgba(0,0,0,0.14),0px 1px 10px 0px rgba(0,0,0,0.12)",
			],
		}),
	};
});

describe("ActionButton component", () => {
	it("renders button with correct text", () => {
		const buttonText = "Test Button";
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		render(
			<ActionButton buttonText={buttonText} onClickHandler={mockHandler} />,
		);

		expect(
			screen.getByRole("button", { name: buttonText }),
		).toBeInTheDocument();
	});

	it("renders as disabled when disabled prop is true", () => {
		const buttonText = "Test Button";
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		render(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				disabled={true}
			/>,
		);

		expect(screen.getByRole("button", { name: buttonText })).toBeDisabled();
	});

	it("shows loading indicator when clicked", async () => {
		const user = userEvent.setup();
		const buttonText = "Test Button";
		// Create a mock that doesn't resolve immediately
		const mockHandler = vi.fn().mockImplementation(
			() =>
				new Promise((resolve) => {
					setTimeout(() => resolve(undefined), 100);
				}),
		);

		render(
			<ActionButton buttonText={buttonText} onClickHandler={mockHandler} />,
		);

		const button = screen.getByRole("button", { name: buttonText });
		await user.click(button);

		// Button should now show loading indicator
		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(button).toBeDisabled();

		// The button stays busy for at least 300ms; letting the test end before
		// that would update state after the test environment is torn down.
		await waitFor(() => expect(button).toBeEnabled(), { timeout: 1000 });
		expect(mockHandler).toHaveBeenCalledTimes(1);
	});

	it("applies the correct button variant", () => {
		const buttonText = "Test Button";
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		const { rerender } = render(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				buttonVariant="outlined"
			/>,
		);

		expect(screen.getByRole("button")).toHaveClass("MuiButton-outlined");

		rerender(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				buttonVariant="contained"
			/>,
		);

		expect(screen.getByRole("button")).toHaveClass("MuiButton-contained");
	});

	it("applies full width when specified", () => {
		const buttonText = "Test Button";
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		render(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				fullWidth={true}
			/>,
		);

		expect(screen.getByRole("button")).toHaveClass("MuiButton-fullWidth");
	});

	it("applies the correct color", () => {
		const buttonText = "Test Button";
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		const { rerender } = render(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				color="secondary"
			/>,
		);

		expect(screen.getByRole("button")).toHaveClass("MuiButton-colorSecondary");

		rerender(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				color="error"
			/>,
		);

		expect(screen.getByRole("button")).toHaveClass("MuiButton-colorError");
	});

	it("renders with start icon when provided", () => {
		const buttonText = "Test Button";
		const mockHandler = vi.fn().mockResolvedValue(undefined);
		const startIcon = <span data-testid="test-icon">Icon</span>;

		render(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				startIcon={startIcon}
			/>,
		);

		expect(screen.getByTestId("test-icon")).toBeInTheDocument();
	});

	it("is ready to be pressed again after its handler fails", async () => {
		const user = userEvent.setup();
		const buttonText = "Test Button";
		const mockHandler = vi
			.fn()
			.mockRejectedValue(new ExpectedTestFailure("The save failed"));

		render(
			<ActionButton buttonText={buttonText} onClickHandler={mockHandler} />,
		);

		const button = screen.getByRole("button", { name: buttonText });
		await user.click(button);

		await waitFor(() => expect(button).toBeEnabled(), { timeout: 1000 });
		expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
		expect(mockHandler).toHaveBeenCalledTimes(1);
	});

	it("takes the focus when asked to", () => {
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		render(
			<>
				<ActionButton buttonText="First" onClickHandler={mockHandler} />
				<ActionButton
					buttonText="Second"
					onClickHandler={mockHandler}
					autoFocus
				/>
			</>,
		);

		expect(screen.getByRole("button", { name: "Second" })).toHaveFocus();
	});

	it("does not take the focus unless asked to", () => {
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		render(<ActionButton buttonText="First" onClickHandler={mockHandler} />);

		expect(screen.getByRole("button", { name: "First" })).not.toHaveFocus();
	});

	it("shows loading indicator when externalIsWaiting is true", () => {
		const buttonText = "Test Button";
		const mockHandler = vi.fn().mockResolvedValue(undefined);

		render(
			<ActionButton
				buttonText={buttonText}
				onClickHandler={mockHandler}
				externalIsWaiting={true}
			/>,
		);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByText(buttonText)).toBeInTheDocument();
	});
});
