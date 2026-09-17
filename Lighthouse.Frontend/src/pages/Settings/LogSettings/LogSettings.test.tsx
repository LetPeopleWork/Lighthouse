import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

vi.mock("@melloware/react-logviewer", () => ({
	LazyLog: ({ text }: { text: string }) => (
		<div data-testid="log-viewer">{text}</div>
	),
}));

import type { ILogService } from "../../../services/Api/LogService";
import {
	createMockApiServiceContext,
	createMockLogService,
} from "../../../tests/MockApiServiceProvider";
import LogSettings from "./LogSettings";

const mockGetLogs = vi.fn();
const mockGetLogLevel = vi.fn();
const mockGetSupportedLogLevels = vi.fn();
const mockSetLogLevel = vi.fn();

/** How big a tail the page last asked for. */
const tailAskedFor = () => {
	const { calls } = mockGetLogs.mock;
	return Number(calls[calls.length - 1]?.[0]);
};

const mockLogService: ILogService = createMockLogService();
mockLogService.getLogs = mockGetLogs;
mockLogService.getLogLevel = mockGetLogLevel;
mockLogService.getSupportedLogLevels = mockGetSupportedLogLevels;
mockLogService.setLogLevel = mockSetLogLevel;

const MockApiServiceProvider = ({
	children,
}: {
	children: React.ReactNode;
}) => {
	const mockContext = createMockApiServiceContext({
		logService: mockLogService,
	});

	return (
		<ApiServiceContext.Provider value={mockContext}>
			{children}
		</ApiServiceContext.Provider>
	);
};

describe("LogSettings", () => {
	let originalCreateObjectURL: typeof URL.createObjectURL;
	let originalAppendChild: typeof document.body.appendChild;
	let originalRemoveChild: typeof document.body.removeChild;

	beforeEach(() => {
		originalCreateObjectURL = URL.createObjectURL;
		originalAppendChild = document.body.appendChild;
		originalRemoveChild = document.body.removeChild;

		mockGetLogs.mockResolvedValue("Sample log data");
		mockGetLogLevel.mockResolvedValue("info");
		mockGetSupportedLogLevels.mockResolvedValue(["info", "warn", "error"]);
		mockSetLogLevel.mockResolvedValue(undefined);
	});

	afterEach(() => {
		URL.createObjectURL = originalCreateObjectURL;
		document.body.appendChild = originalAppendChild;
		document.body.removeChild = originalRemoveChild;

		vi.resetAllMocks();
		vi.restoreAllMocks();
	});

	it("renders correctly and loads data", async () => {
		render(
			<MockApiServiceProvider>
				<LogSettings />
			</MockApiServiceProvider>,
		);

		await waitFor(() => {
			expect(mockGetLogs).toHaveBeenCalled();
			expect(mockGetLogLevel).toHaveBeenCalled();
			expect(mockGetSupportedLogLevels).toHaveBeenCalled();
		});

		expect(screen.getByText("Logs")).toBeInTheDocument();
	});

	it("updates log level when changed", async () => {
		render(
			<MockApiServiceProvider>
				<LogSettings />
			</MockApiServiceProvider>,
		);

		// Wait for initial data to load
		await waitFor(() => {
			expect(mockGetLogs).toHaveBeenCalled();
			expect(mockGetLogLevel).toHaveBeenCalled();
			expect(mockGetSupportedLogLevels).toHaveBeenCalled();
		});

		// Fire event to change the select value
		const select = screen.getByTestId("select-id");
		fireEvent.change(select, { target: { value: "warn" } });

		// Wait for the mockSetLogLevel to be called
		await waitFor(() => {
			expect(mockSetLogLevel).toHaveBeenCalledWith("warn");
		});
	});

	it("refreshes logs when refresh button is clicked", async () => {
		render(
			<MockApiServiceProvider>
				<LogSettings />
			</MockApiServiceProvider>,
		);

		// Cleared first: the page fetches once on mount, so asserting a bare "was called" would hold
		// just as well for a button that does nothing at all.
		await waitFor(() => {
			expect(mockGetLogs).toHaveBeenCalled();
		});
		mockGetLogs.mockClear();

		fireEvent.click(screen.getByRole("button", { name: /Refresh/i }));

		await waitFor(() => {
			expect(mockGetLogs).toHaveBeenCalledWith(undefined);
		});
	});

	// Following is something the operator switches on. Until then the page asks once and stays put.
	it("does not follow until it is asked to", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			render(
				<MockApiServiceProvider>
					<LogSettings />
				</MockApiServiceProvider>,
			);

			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalled();
			});
			mockGetLogs.mockClear();

			await vi.advanceTimersByTimeAsync(20_000);

			expect(mockGetLogs).not.toHaveBeenCalled();
		} finally {
			vi.useRealTimers();
		}
	});

	// Bug #6020. Following the log means asking again every few seconds. Asking for the whole file
	// each time is what makes that unaffordable on an instance logging at Debug, so a follower asks
	// for the end of it.
	it("asks only for the end of the log while it is following", async () => {
		render(
			<MockApiServiceProvider>
				<LogSettings />
			</MockApiServiceProvider>,
		);

		await waitFor(() => {
			expect(mockGetLogs).toHaveBeenCalled();
		});
		mockGetLogs.mockClear();

		fireEvent.click(screen.getByRole("switch", { name: /Live/i }));

		await waitFor(() => {
			expect(mockGetLogs).toHaveBeenCalledWith(expect.any(Number));
		});

		// A tail too small to hold a line of the log is not a tail worth asking for.
		expect(tailAskedFor()).toBeGreaterThanOrEqual(1024);
	});

	it("keeps asking for as long as it follows", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			render(
				<MockApiServiceProvider>
					<LogSettings />
				</MockApiServiceProvider>,
			);

			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalled();
			});

			fireEvent.click(screen.getByRole("switch", { name: /Live/i }));
			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalledWith(expect.any(Number));
			});
			mockGetLogs.mockClear();

			await vi.advanceTimersByTimeAsync(20_000);

			expect(mockGetLogs.mock.calls.length).toBeGreaterThanOrEqual(2);
		} finally {
			vi.useRealTimers();
		}
	});

	// Nobody is reading a log they cannot see, and an instance should not be answering for one.
	it("does not ask while the tab is hidden", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		const hidden = vi.spyOn(document, "hidden", "get").mockReturnValue(true);

		try {
			render(
				<MockApiServiceProvider>
					<LogSettings />
				</MockApiServiceProvider>,
			);

			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalled();
			});

			fireEvent.click(screen.getByRole("switch", { name: /Live/i }));
			mockGetLogs.mockClear();

			await vi.advanceTimersByTimeAsync(20_000);

			expect(mockGetLogs).not.toHaveBeenCalled();
		} finally {
			hidden.mockRestore();
			vi.useRealTimers();
		}
	});

	/**
	 * The leak this guards against: stopping while an ask is still out. The ask comes back after the
	 * cleanup has run, and if it schedules the next one anyway there is a poller left behind that
	 * nothing can now switch off — and it survives for as long as the page is open.
	 */
	it("leaves no poller behind when it stops mid-request", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			let answer: (logs: string) => void = () => undefined;
			mockGetLogs.mockImplementation(
				() =>
					new Promise<string>((resolve) => {
						answer = resolve;
					}),
			);

			render(
				<MockApiServiceProvider>
					<LogSettings />
				</MockApiServiceProvider>,
			);

			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalled();
			});

			const live = screen.getByRole("switch", { name: /Live/i });
			fireEvent.click(live);
			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalledWith(expect.any(Number));
			});

			fireEvent.click(live);
			mockGetLogs.mockClear();
			answer("whatever was still in flight");

			await vi.advanceTimersByTimeAsync(30_000);

			expect(mockGetLogs).not.toHaveBeenCalled();
		} finally {
			vi.useRealTimers();
		}
	});

	it("stops asking once it is no longer following", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			render(
				<MockApiServiceProvider>
					<LogSettings />
				</MockApiServiceProvider>,
			);

			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalled();
			});

			const live = screen.getByRole("switch", { name: /Live/i });
			fireEvent.click(live);
			await waitFor(() => {
				expect(mockGetLogs).toHaveBeenCalledWith(expect.any(Number));
			});

			fireEvent.click(live);
			mockGetLogs.mockClear();

			await vi.advanceTimersByTimeAsync(30_000);

			expect(mockGetLogs).not.toHaveBeenCalled();
		} finally {
			vi.useRealTimers();
		}
	});

	it("downloads logs when download button is clicked", async () => {
		render(
			<MockApiServiceProvider>
				<LogSettings />
			</MockApiServiceProvider>,
		);

		const downloadButton = screen.getByRole("button", { name: /Download/i });

		fireEvent.click(downloadButton);

		await waitFor(() => {
			expect(mockGetLogs).toHaveBeenCalled();
		});
	});
});
