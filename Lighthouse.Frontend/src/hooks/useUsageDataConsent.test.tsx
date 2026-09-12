import { act, renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { IUsageDataState } from "../models/UsageData/UsageData";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IUsageDataService } from "../services/Api/UsageDataService";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { useUsageDataConsent } from "./useUsageDataConsent";

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

const renderConsent = (
	state: IUsageDataState,
	overrides: Partial<IUsageDataService> = {},
) => {
	const usageDataService: IUsageDataService = {
		getState: vi.fn().mockResolvedValue(state),
		recordDecision: vi.fn().mockResolvedValue("freshly-minted-token"),
		revoke: vi.fn().mockResolvedValue(undefined),
		...overrides,
	};

	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<ApiServiceContext.Provider
			value={createMockApiServiceContext({ usageDataService })}
		>
			{children}
		</ApiServiceContext.Provider>
	);

	const rendered = renderHook(() => useUsageDataConsent(), { wrapper });
	return { ...rendered, usageDataService };
};

const undecided: IUsageDataState = {
	sending: false,
	decision: null,
	willAskAgain: true,
};

const granted: IUsageDataState = {
	sending: true,
	decision: "Granted",
	willAskAgain: false,
};

afterEach(() => {
	localStorage.clear();
	vi.restoreAllMocks();
});

describe("useUsageDataConsent", () => {
	// The defect this guards against shipped once and every component test passed over it: saying no
	// to something already agreed to was recorded as a brand-new refusal, which left the original
	// grant untouched and still counting. The instance kept sending for the rest of the liveness
	// window while this browser showed the opposite.
	it("withdraws an existing grant instead of recording a second, unrelated refusal", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const { result, usageDataService } = renderConsent(granted);

		await waitFor(() => expect(result.current.indicatorState).toBe("sending"));
		await act(async () => {
			await result.current.decide("declined");
		});

		expect(usageDataService.revoke).toHaveBeenCalledWith("this-browsers-token");
		expect(usageDataService.recordDecision).not.toHaveBeenCalled();
	});

	it("records a refusal from a browser that has not decided, because there is nothing to withdraw", async () => {
		const { result, usageDataService } = renderConsent(undecided);

		await waitFor(() =>
			expect(result.current.indicatorState).toBe("not-sending"),
		);
		await act(async () => {
			await result.current.decide("declined");
		});

		expect(usageDataService.recordDecision).toHaveBeenCalledWith("declined");
		expect(usageDataService.revoke).not.toHaveBeenCalled();
		expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBe(
			"freshly-minted-token",
		);
	});

	it("keeps the dialog open and says so when the answer could not be saved", async () => {
		const { result } = renderConsent(undecided, {
			recordDecision: vi.fn().mockRejectedValue(new Error("backend is down")),
		});

		act(() => {
			result.current.openDialog();
		});
		await act(async () => {
			await result.current.decide("granted");
		});

		expect(result.current.failedToRecord).toBe(true);
		expect(result.current.isDialogOpen).toBe(true);
		// Telling somebody their choice was taken when it was not is worse than the failure itself.
		expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBeNull();
	});

	// Each of the three conditions guarding the withdrawal is load-bearing on its own, and dropping
	// any one of them sends the wrong request: withdrawing a grant that was never given, or minting
	// a second row while the live grant keeps the instance sending.
	it("records a refusal from a browser holding a token but no prior grant", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-from-an-earlier-refusal");
		const { result, usageDataService } = renderConsent(undecided);

		await waitFor(() =>
			expect(result.current.indicatorState).toBe("not-sending"),
		);
		await act(async () => {
			await result.current.decide("declined");
		});

		expect(usageDataService.recordDecision).toHaveBeenCalledWith("declined");
		expect(usageDataService.revoke).not.toHaveBeenCalled();
	});

	it("records a fresh grant rather than withdrawing when an existing grant is reaffirmed", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const { result, usageDataService } = renderConsent(granted);

		await waitFor(() => expect(result.current.indicatorState).toBe("sending"));
		await act(async () => {
			await result.current.decide("granted");
		});

		expect(usageDataService.recordDecision).toHaveBeenCalledWith("granted");
		expect(usageDataService.revoke).not.toHaveBeenCalled();
	});

	it("records a refusal when the grant belongs to some other browser, because there is no token to withdraw", async () => {
		const { result, usageDataService } = renderConsent(granted);

		await waitFor(() => expect(result.current.indicatorState).toBe("sending"));
		await act(async () => {
			await result.current.decide("declined");
		});

		expect(usageDataService.recordDecision).toHaveBeenCalledWith("declined");
		expect(usageDataService.revoke).not.toHaveBeenCalled();
	});

	it("starts out admitting it does not know yet, and claims nothing about a dialog or a failure", () => {
		const { result } = renderConsent(undecided, {
			getState: vi.fn().mockReturnValue(new Promise(() => {})),
		});

		expect(result.current.indicatorState).toBe("unknown");
		expect(result.current.willAskAgain).toBe(false);
		expect(result.current.isDialogOpen).toBe(false);
		expect(result.current.failedToRecord).toBe(false);
	});

	// The indicator fails closed: it goes back to saying nothing rather than keeping the last good
	// answer on screen. Asserting this from a cold start would prove nothing, because not knowing is
	// also where it starts - so this one has to know first, and then stop knowing.
	it("stops claiming to know once the state can no longer be fetched", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		try {
			const getState = vi
				.fn()
				.mockResolvedValueOnce(granted)
				.mockRejectedValue(new Error("backend is down"));
			const { result } = renderConsent(undecided, { getState });

			await waitFor(() =>
				expect(result.current.indicatorState).toBe("sending"),
			);

			await act(async () => {
				await vi.advanceTimersByTimeAsync(60 * 60 * 1000);
			});

			expect(result.current.indicatorState).toBe("unknown");
		} finally {
			vi.useRealTimers();
		}
	});

	// A browser that cannot be asked has not consented, which is the safe reading. The token has to
	// be in storage first, or the null this asserts on is just the empty store answering normally and
	// the test passes without the throw ever happening.
	it("treats a browser that cannot be asked for a token as one that holds none", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "a-token-it-cannot-read-back");
		vi.spyOn(globalThis.localStorage, "getItem").mockImplementation(() => {
			throw new Error("this is a private window");
		});
		const { result, usageDataService } = renderConsent(undecided);

		await waitFor(() =>
			expect(result.current.indicatorState).toBe("not-sending"),
		);
		expect(usageDataService.getState).toHaveBeenCalledWith(null);
	});

	it("closes the dialog and clears an earlier failure once an answer gets through", async () => {
		const recordDecision = vi
			.fn()
			.mockRejectedValueOnce(new Error("backend is down"))
			.mockResolvedValue("freshly-minted-token");
		const { result } = renderConsent(undecided, { recordDecision });

		act(() => {
			result.current.openDialog();
		});
		await act(async () => {
			await result.current.decide("granted");
		});
		expect(result.current.failedToRecord).toBe(true);

		await act(async () => {
			await result.current.decide("granted");
		});

		expect(result.current.failedToRecord).toBe(false);
		expect(result.current.isDialogOpen).toBe(false);
	});

	it("closes a dialog it opened", async () => {
		const { result } = renderConsent(undecided);

		act(() => {
			result.current.openDialog();
		});
		expect(result.current.isDialogOpen).toBe(true);

		act(() => {
			result.current.closeDialog();
		});
		expect(result.current.isDialogOpen).toBe(false);
	});

	// A tab left open for weeks never mounts the footer again, so this interval is the only thing
	// keeping the browser's consent from ageing out under somebody using Lighthouse every day. An
	// interval of the wrong length looks identical at mount and only goes wrong an hour later.
	it("re-asks for the state once an hour, not sooner", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		try {
			const { result, usageDataService } = renderConsent(undecided);
			await waitFor(() =>
				expect(result.current.indicatorState).toBe("not-sending"),
			);
			expect(usageDataService.getState).toHaveBeenCalledTimes(1);

			await act(async () => {
				await vi.advanceTimersByTimeAsync(59 * 60 * 1000);
			});
			expect(usageDataService.getState).toHaveBeenCalledTimes(1);

			await act(async () => {
				await vi.advanceTimersByTimeAsync(60 * 1000);
			});
			expect(usageDataService.getState).toHaveBeenCalledTimes(2);
		} finally {
			vi.useRealTimers();
		}
	});

	it("stops re-asking once the component holding it is gone", async () => {
		vi.useFakeTimers({ shouldAdvanceTime: true });
		try {
			const { result, unmount, usageDataService } = renderConsent(undecided);
			await waitFor(() =>
				expect(result.current.indicatorState).toBe("not-sending"),
			);

			unmount();
			await act(async () => {
				await vi.advanceTimersByTimeAsync(3 * 60 * 60 * 1000);
			});

			expect(usageDataService.getState).toHaveBeenCalledTimes(1);
		} finally {
			vi.useRealTimers();
		}
	});

	it("writes nothing to browser storage just by being used", async () => {
		const { result } = renderConsent(undecided);

		await waitFor(() =>
			expect(result.current.indicatorState).toBe("not-sending"),
		);
		act(() => {
			result.current.openDialog();
			result.current.closeDialog();
		});

		expect(localStorage.length).toBe(0);
	});
});
