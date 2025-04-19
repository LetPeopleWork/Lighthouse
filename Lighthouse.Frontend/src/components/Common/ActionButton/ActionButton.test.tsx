import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ActionButton from "./ActionButton";

// Mock the useTheme hook
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => ({
			palette: {
				primary: {
					main: "rgba(48, 87, 78, 1)",
				},
				secondary: {
					main: "rgba(156, 39, 176, 1)",
				},
				success: {
					main: "rgba(76, 175, 80, 1)",
				},
				error: {
					main: "rgba(244, 67, 54, 1)",
				},
				mode: "light",
			},
			// Properly define the shadows array
			shadows: [
				"none",
				"0px 2px 1px -1px rgba(0,0,0,0.2),0px 1px 1px 0px rgba(0,0,0,0.14),0px 1px 3px 0px rgba(0,0,0,0.12)",
				"0px 3px 1px -2px rgba(0,0,0,0.2),0px 2px 2px 0px rgba(0,0,0,0.14),0px 1px 5px 0px rgba(0,0,0,0.12)",
				"0px 3px 3px -2px rgba(0,0,0,0.2),0px 3px 4px 0px rgba(0,0,0,0.14),0px 1px 8px 0px rgba(0,0,0,0.12)",
				"0px 2px 4px -1px rgba(0,0,0,0.2),0px 4px 5px 0px rgba(0,0,0,0.14),0px 1px 10px 0px rgba(0,0,0,0.12)",
				"0px 3px 5px -1px rgba(0,0,0,0.2),0px 5px 8px 0px rgba(0,0,0,0.14),0px 1px 14px 0px rgba(0,0,0,0.12)",
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

		// Wait for the handler to complete
		await waitFor(
			() => {
				expect(mockHandler).toHaveBeenCalledTimes(1);
			},
			{ timeout: 200 },
		);
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
		expect(screen.queryByText(buttonText)).not.toBeInTheDocument();
	});
});
