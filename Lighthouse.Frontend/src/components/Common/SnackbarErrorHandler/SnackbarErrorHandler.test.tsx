import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler, { useErrorSnackbar } from "./SnackbarErrorHandler";

// Mock console.error to avoid noise in test output
const originalError = console.error;
beforeEach(() => {
	console.error = vi.fn();
});

afterEach(() => {
	console.error = originalError;
});

// Test component that uses the error context
const TestComponent = ({ throwError }: { throwError?: boolean }) => {
	const { showError } = useErrorSnackbar();

	if (throwError) {
		throw new Error("Test component error");
	}

	return (
		<div>
			<span data-testid="test-content">Test Content</span>
			<button
				data-testid="show-error-button"
				type="button"
				onClick={() => showError("Test error message")}
			>
				Show Error
			</button>
		</div>
	);
};

describe("SnackbarErrorHandler", () => {
	describe("Component Rendering", () => {
		it("renders children correctly", () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="child">Child Content</div>
				</SnackbarErrorHandler>,
			);

			expect(screen.getByTestId("child")).toBeInTheDocument();
			expect(screen.getByTestId("child")).toHaveTextContent("Child Content");
		});

		it("renders without crashing when no children provided", () => {
			render(<SnackbarErrorHandler>{null}</SnackbarErrorHandler>);
			// Should not crash
		});

		it("renders snackbar but not visible initially", () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="child">Child</div>
				</SnackbarErrorHandler>,
			);

			// Snackbar should exist but not be visible
			const snackbar = screen.queryByRole("alert");
			expect(snackbar).not.toBeInTheDocument();
		});
	});

	describe("Error Context", () => {
		it("provides error context to children", () => {
			render(
				<SnackbarErrorHandler>
					<TestComponent />
				</SnackbarErrorHandler>,
			);

			expect(screen.getByTestId("test-content")).toBeInTheDocument();
			expect(screen.getByTestId("show-error-button")).toBeInTheDocument();
		});

		it("throws error when useErrorSnackbar is used outside of provider", () => {
			// This test verifies that the hook throws when used outside the provider
			// We expect this to throw during rendering
			expect(() => {
				render(
					<TestComponent />, // TestComponent calls useErrorSnackbar
				);
			}).toThrow("useErrorSnackbar must be used within a SnackbarErrorHandler");
		});
	});

	describe("Manual Error Display", () => {
		it("shows error snackbar when showError is called", async () => {
			const user = userEvent.setup();

			render(
				<SnackbarErrorHandler>
					<TestComponent />
				</SnackbarErrorHandler>,
			);

			await user.click(screen.getByTestId("show-error-button"));

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent("Test error message");
		});

		it("shows multiple errors sequentially", async () => {
			const user = userEvent.setup();

			render(
				<SnackbarErrorHandler>
					<TestComponent />
				</SnackbarErrorHandler>,
			);

			// Show first error
			await user.click(screen.getByTestId("show-error-button"));

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent("Test error message");

			// Close first error
			const closeButton = screen.getByLabelText("Close");
			await user.click(closeButton);

			await waitFor(() => {
				expect(screen.queryByRole("alert")).not.toBeInTheDocument();
			});

			// Show second error
			await user.click(screen.getByTestId("show-error-button"));

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent("Test error message");
		});
	});

	describe("Window Error Handling", () => {
		it("handles window error events", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// Simulate a window error
			const errorEvent = new ErrorEvent("error", {
				error: new Error("Window error message"),
				message: "Window error message",
			});

			fireEvent(window, errorEvent);

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent(
				"Window error message",
			);
		});

		it("handles window error events without error object", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// Simulate a window error without error object
			const errorEvent = new ErrorEvent("error", {
				message: "Generic error message",
			});

			fireEvent(window, errorEvent);

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent(
				"Generic error message",
			);
		});
	});

	describe("Unhandled Rejection Handling", () => {
		it("handles unhandled promise rejections with Error objects", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// Create a custom event that mimics PromiseRejectionEvent
			const rejectionEvent = new CustomEvent(
				"unhandledrejection",
			) as unknown as PromiseRejectionEvent;
			Object.defineProperty(rejectionEvent, "reason", {
				value: new Error("Promise rejection error"),
			});

			fireEvent(window, rejectionEvent);

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent(
				"Promise rejection error",
			);
		});

		it("handles unhandled promise rejections with string reasons", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// Create a custom event that mimics PromiseRejectionEvent
			const rejectionEvent = new CustomEvent(
				"unhandledrejection",
			) as unknown as PromiseRejectionEvent;
			Object.defineProperty(rejectionEvent, "reason", {
				value: "String rejection reason",
			});

			fireEvent(window, rejectionEvent);

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent(
				"String rejection reason",
			);
		});

		it("handles unhandled promise rejections with no reason", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// Create a custom event that mimics PromiseRejectionEvent with undefined reason
			const rejectionEvent = new CustomEvent(
				"unhandledrejection",
			) as unknown as PromiseRejectionEvent;
			Object.defineProperty(rejectionEvent, "reason", {
				value: undefined,
			});

			fireEvent(window, rejectionEvent);

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent(
				"Unhandled promise rejection",
			);
		});

		it("handles unhandled promise rejections with detail fallback", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// Create a custom event with detail property
			const rejectionEvent = new CustomEvent("unhandledrejection", {
				detail: "Detail error message",
			}) as unknown as PromiseRejectionEvent;

			// Override reason to be undefined to trigger detail fallback
			Object.defineProperty(rejectionEvent, "reason", {
				value: undefined,
			});

			fireEvent(window, rejectionEvent);

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			expect(screen.getByRole("alert")).toHaveTextContent(
				"Detail error message",
			);
		});
	});

	describe("Snackbar Interaction", () => {
		it("closes snackbar when close button is clicked", async () => {
			const user = userEvent.setup();

			render(
				<SnackbarErrorHandler>
					<TestComponent />
				</SnackbarErrorHandler>,
			);

			// Show error
			await user.click(screen.getByTestId("show-error-button"));

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			// Close error
			const closeButton = screen.getByLabelText("Close");
			await user.click(closeButton);

			await waitFor(() => {
				expect(screen.queryByRole("alert")).not.toBeInTheDocument();
			});
		});

		it("does not close snackbar on clickaway", async () => {
			const user = userEvent.setup();

			render(
				<SnackbarErrorHandler>
					<TestComponent />
				</SnackbarErrorHandler>,
			);

			// Show error
			await user.click(screen.getByTestId("show-error-button"));

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});

			// Click away (simulate clickaway)
			await user.click(document.body);

			// Snackbar should still be visible
			expect(screen.getByRole("alert")).toBeInTheDocument();
		});
	});

	describe("Event Listener Management", () => {
		it("removes event listeners on unmount", () => {
			const addEventListenerSpy = vi.spyOn(window, "addEventListener");
			const removeEventListenerSpy = vi.spyOn(window, "removeEventListener");

			const { unmount } = render(
				<SnackbarErrorHandler>
					<div>Content</div>
				</SnackbarErrorHandler>,
			);

			// Verify listeners were added
			expect(addEventListenerSpy).toHaveBeenCalledWith(
				"error",
				expect.any(Function),
			);
			expect(addEventListenerSpy).toHaveBeenCalledWith(
				"unhandledrejection",
				expect.any(Function),
			);

			unmount();

			// Verify listeners were removed
			expect(removeEventListenerSpy).toHaveBeenCalledWith(
				"error",
				expect.any(Function),
			);
			expect(removeEventListenerSpy).toHaveBeenCalledWith(
				"unhandledrejection",
				expect.any(Function),
			);

			addEventListenerSpy.mockRestore();
			removeEventListenerSpy.mockRestore();
		});
	});
});
