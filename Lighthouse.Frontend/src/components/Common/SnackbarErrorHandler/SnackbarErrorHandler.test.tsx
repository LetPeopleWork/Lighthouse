import {
	act,
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SnackbarErrorHandler, {
	type SnackbarAction,
	useErrorSnackbar,
} from "./SnackbarErrorHandler";

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

		it("says nothing about a resize loop the browser already recovered from", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// The browser raises this whenever a resize callback causes another resize in the same
			// frame. It defers the rest to the next frame and loses nothing — but it arrives as a
			// window error event, so without a filter a chart that measures itself puts a red
			// failure toast in front of the reader for something that did not fail.
			fireEvent(
				window,
				new ErrorEvent("error", {
					message:
						"ResizeObserver loop completed with undelivered notifications.",
				}),
			);

			await waitFor(() => {
				expect(screen.queryByRole("alert")).not.toBeInTheDocument();
			});
		});

		it("still reports a resize loop that never settled", async () => {
			render(
				<SnackbarErrorHandler>
					<div data-testid="content">Content</div>
				</SnackbarErrorHandler>,
			);

			// The harsher variant. This one means the loop hit its limit rather than deferring, so
			// it is a real runaway and must not be swallowed with its benign sibling.
			fireEvent(
				window,
				new ErrorEvent("error", {
					message: "ResizeObserver loop limit exceeded",
				}),
			);

			await waitFor(() => {
				expect(screen.getByRole("alert")).toBeInTheDocument();
			});
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

	// Bug #5732: the Snackbar used to live inside the boundary, so the error it reported
	// unmounted the very element meant to report it — leaving a silent blank area.
	describe("when a child throws during render", () => {
		it("still shows the error in the snackbar", async () => {
			render(
				<SnackbarErrorHandler>
					<TestComponent throwError={true} />
				</SnackbarErrorHandler>,
			);

			await waitFor(() => {
				expect(screen.getByText("Test component error")).toBeInTheDocument();
			});
		});

		it("replaces the failed subtree with a visible notice rather than nothing", async () => {
			render(
				<SnackbarErrorHandler>
					<TestComponent throwError={true} />
				</SnackbarErrorHandler>,
			);

			await waitFor(() => {
				expect(screen.queryByTestId("test-content")).not.toBeInTheDocument();
				expect(
					screen.getByText(/this section could not be displayed/i),
				).toBeInTheDocument();
			});
		});
	});
});

const NO_PDF_WAS_MADE =
	"Part of the page couldn't be loaded, so no PDF was made.";
const ANOTHER_MESSAGE = "Saving the Team failed.";
const RETRY = "Retry";
const SIX_SECONDS = 6000;

let theSnackbar: ReturnType<typeof useErrorSnackbar> | undefined;

const HoldsTheSnackbar = () => {
	theSnackbar = useErrorSnackbar();
	return null;
};

const renderTheSnackbar = () =>
	render(
		<SnackbarErrorHandler>
			<HoldsTheSnackbar />
		</SnackbarErrorHandler>,
	);

const show = (message: string, action?: SnackbarAction): unknown => {
	let handedBack: unknown;
	act(() => {
		handedBack = theSnackbar?.showError(
			message,
			action === undefined ? undefined : { action },
		);
	});
	return handedBack;
};

/** What showError hands back for taking its message away again; nothing at all fails the scenario. */
const aWayToTakeItBack = (handedBack: unknown): (() => void) => {
	expect(
		typeof handedBack,
		"showError hands back no way to take its message away",
	).toBe("function");
	return handedBack as () => void;
};

const theMessage = () => screen.queryByRole("alert");

const timePasses = (milliseconds: number) =>
	act(() => {
		vi.advanceTimersByTime(milliseconds);
	});

describe("a message with a way to act on it", () => {
	afterEach(() => {
		vi.useRealTimers();
		theSnackbar = undefined;
	});

	// Every caller from before the export hands over a sentence alone, and the snackbar must keep treating it so.
	it("hides a message given alone after six seconds, as it always has", () => {
		vi.useFakeTimers();
		renderTheSnackbar();

		show(ANOTHER_MESSAGE);
		timePasses(SIX_SECONDS - 100);
		expect(theMessage()).toHaveTextContent(ANOTHER_MESSAGE);
		timePasses(200);
		timePasses(2000);

		expect(theMessage()).toBeNull();
	});

	it(`shows the action beside the message, and choosing it runs the action once and closes the message`, async () => {
		const user = userEvent.setup();
		const retry = vi.fn();
		renderTheSnackbar();

		show(NO_PDF_WAS_MADE, { label: RETRY, onClick: retry });
		const message = await screen.findByRole("alert");
		expect(message).toHaveTextContent(NO_PDF_WAS_MADE);
		await user.click(within(message).getByRole("button", { name: RETRY }));

		expect(retry).toHaveBeenCalledTimes(1);
		await waitFor(() => expect(theMessage()).toBeNull());
	});

	// A reader needs time to reach Retry; a message that offers it waits for them.
	it(`keeps a message that offers an action open past six seconds, until the reader closes it`, () => {
		vi.useFakeTimers();
		renderTheSnackbar();

		show(NO_PDF_WAS_MADE, { label: RETRY, onClick: vi.fn() });
		timePasses(60_000);
		expect(theMessage(), "the message hid itself").toHaveTextContent(
			NO_PDF_WAS_MADE,
		);
		fireEvent.click(screen.getByLabelText("Close"));
		timePasses(200);
		timePasses(2000);

		expect(theMessage()).toBeNull();
	});

	// The export takes its own message back when the reader leaves the page whose Retry it offers.
	it(`takes a message away when the one who showed it asks`, async () => {
		renderTheSnackbar();

		const takeItBack = aWayToTakeItBack(
			show(NO_PDF_WAS_MADE, { label: RETRY, onClick: vi.fn() }),
		);
		act(takeItBack);

		await waitFor(() => expect(theMessage()).toBeNull());
	});

	// One message at a time: a newer one has replaced the export's, and taking the export's back must not take it.
	// A message being closed stays on screen while it fades, so the time for that passes before the newer one is read.
	it(`leaves a newer message standing when an older one is taken back`, () => {
		vi.useFakeTimers();
		renderTheSnackbar();
		const takeTheFirstBack = aWayToTakeItBack(
			show(NO_PDF_WAS_MADE, { label: RETRY, onClick: vi.fn() }),
		);
		show(ANOTHER_MESSAGE);

		act(takeTheFirstBack);
		timePasses(200);
		timePasses(2000);

		expect(
			theMessage(),
			"taking the older message back closed the newer one",
		).toHaveTextContent(ANOTHER_MESSAGE);
	});

	it(`offers no action with a message given alone after one that offered Retry`, async () => {
		renderTheSnackbar();
		show(NO_PDF_WAS_MADE, { label: RETRY, onClick: vi.fn() });

		show(ANOTHER_MESSAGE);

		const message = await screen.findByRole("alert");
		expect(message).toHaveTextContent(ANOTHER_MESSAGE);
		expect(within(message).queryByRole("button", { name: RETRY })).toBeNull();
	});
});
