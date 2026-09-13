import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { UsageDataConsentProvider } from "../../hooks/useUsageDataConsent";
import type { IUsageDataState } from "../../models/UsageData/UsageData";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import { ApiServiceContext } from "../Api/ApiServiceContext";
import type { IUsageDataService } from "../Api/UsageDataService";
import {
	FLUSH_INTERVAL_MS,
	useUsageDataEventDetector,
} from "./usageDataEvents";

const TOKEN_STORAGE_KEY = "lighthouse:usagedata:consent";

const granted: IUsageDataState = {
	sending: true,
	decision: "Granted",
	mayAsk: false,
	reAskAfterDays: 90,
};

const declined: IUsageDataState = {
	sending: false,
	decision: "Declined",
	mayAsk: false,
	reAskAfterDays: 90,
};

const renderDetector = (state: IUsageDataState, path: string) => {
	const usageDataService: IUsageDataService = {
		getState: vi.fn().mockResolvedValue(state),
		recordDecision: vi.fn().mockResolvedValue("freshly-minted-token"),
		revoke: vi.fn().mockResolvedValue(undefined),
		acknowledgeAsked: vi.fn().mockResolvedValue(undefined),
		postEvents: vi.fn().mockResolvedValue(undefined),
	};

	const wrapper = ({ children }: { children: ReactNode }) =>
		createElement(
			MemoryRouter,
			{ initialEntries: [path] },
			createElement(
				ApiServiceContext.Provider,
				{ value: createMockApiServiceContext({ usageDataService }) },
				createElement(UsageDataConsentProvider, null, children),
			),
		);

	const rendered = renderHook(() => useUsageDataEventDetector(), { wrapper });
	return { ...rendered, usageDataService };
};

/**
 * Lets the answer about this browser arrive and the page be noticed, without waiting on a clock.
 * Everything after this point in a test is about flushing, not about getting started.
 */
const settle = async (usageDataService: IUsageDataService) => {
	await waitFor(() => expect(usageDataService.getState).toHaveBeenCalled());
	await act(async () => {
		await Promise.resolve();
	});
};

/** The same as `settle`, for a test that is driving the clock rather than letting it run. */
const settleOnTheHeldClock = async () => {
	await act(async () => {
		await vi.advanceTimersByTimeAsync(1);
	});
};

const hideTheTab = () => {
	vi.spyOn(document, "visibilityState", "get").mockReturnValue("hidden");
	act(() => {
		document.dispatchEvent(new Event("visibilitychange"));
	});
};

const bringTheTabBack = () => {
	vi.spyOn(document, "visibilityState", "get").mockReturnValue("visible");
	act(() => {
		document.dispatchEvent(new Event("visibilitychange"));
	});
};

afterEach(() => {
	localStorage.clear();
	sessionStorage.clear();
	vi.restoreAllMocks();
});

describe("useUsageDataEventDetector", () => {
	// The whole of this buffer's home is one array that dies with the tab. A browser-side queue in
	// storage would outlive the withdrawal it is meant to obey and send afterwards, so the claim
	// worth pinning is the absence: across noticing a page and handing it in, nothing is written to
	// either store, and the only key either store holds afterwards is the one that was already
	// there before any of this ran.
	it("keeps what it noticed in memory, writing to neither browser store", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const writes = vi.spyOn(Storage.prototype, "setItem");

		const { usageDataService } = renderDetector(granted, "/teams/42/metrics");
		await settle(usageDataService);
		hideTheTab();

		await waitFor(() => expect(usageDataService.postEvents).toHaveBeenCalled());
		expect(writes).not.toHaveBeenCalled();
		expect(localStorage.length).toBe(1);
		expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBe("this-browsers-token");
		expect(sessionStorage.length).toBe(0);
	});

	it("hands in what it noticed when the clock says so, and not before", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			const { usageDataService } = renderDetector(granted, "/teams/42/metrics");
			await act(async () => {
				await vi.advanceTimersByTimeAsync(FLUSH_INTERVAL_MS - 1);
			});
			expect(usageDataService.postEvents).not.toHaveBeenCalled();

			await act(async () => {
				await vi.advanceTimersByTimeAsync(1);
			});

			expect(usageDataService.postEvents).toHaveBeenCalledWith(
				"this-browsers-token",
				[
					expect.objectContaining({
						name: "TeamOrPortfolioTabOpened",
						route: "TeamDetail_Metrics",
					}),
				],
			);
		} finally {
			vi.useRealTimers();
		}
	});

	// Somebody who closes the laptop lid is the ordinary case, not the exception, and a timer that
	// has not come round yet loses everything that happened since the last one.
	it("hands in what it noticed when the tab is put away", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const { usageDataService } = renderDetector(
			granted,
			"/portfolios/7/deliveries",
		);
		await settle(usageDataService);

		hideTheTab();

		await waitFor(() =>
			expect(usageDataService.postEvents).toHaveBeenCalledWith(
				"this-browsers-token",
				[expect.objectContaining({ route: "PortfolioDetail_Deliveries" })],
			),
		);
	});

	// Saying no mints a token too, so holding one says nothing about having agreed. Anything that
	// decided whether to notice a page by asking "is there a token" would collect on behalf of
	// somebody who said not to, and every other test here would carry on passing. What decides is
	// the answer the server gave about this browser.
	it("notices nothing and hands in nothing for a browser that refused, token and all", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "the-token-a-refusal-also-mints");
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			const { usageDataService } = renderDetector(
				declined,
				"/teams/42/metrics",
			);
			await act(async () => {
				await vi.advanceTimersByTimeAsync(FLUSH_INTERVAL_MS * 2);
			});
			hideTheTab();
			await act(async () => {
				await Promise.resolve();
			});

			expect(usageDataService.postEvents).not.toHaveBeenCalled();
		} finally {
			vi.useRealTimers();
		}
	});

	// A page with no name on the list is not reported under some stand-in name, because that would
	// be counted as a page nobody opened.
	it("says nothing about a page that is not on the list", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const { usageDataService } = renderDetector(granted, "/features");
		await settle(usageDataService);

		hideTheTab();
		await act(async () => {
			await Promise.resolve();
		});

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	// What is unflushed when the tab goes away is gone, and that costs nobody anything. The
	// alternative - keeping it somewhere it would survive - is the thing this whole design refuses.
	it("loses what it had not handed in when the tab goes away", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const first = renderDetector(granted, "/teams/42/metrics");
		await settle(first.usageDataService);
		first.unmount();

		const second = renderDetector(granted, "/features");
		await settle(second.usageDataService);
		hideTheTab();
		await act(async () => {
			await Promise.resolve();
		});

		expect(first.usageDataService.postEvents).not.toHaveBeenCalled();
		expect(second.usageDataService.postEvents).not.toHaveBeenCalled();
		expect(sessionStorage.length).toBe(0);
	});

	// The server has no timestamp of its own for any of this: it works out when a page was opened by
	// subtracting this figure from the moment the batch reached it. So the figure has to be an age -
	// how long ago - rather than a reading of the clock, and rather than nothing at all.
	it("says how long ago a page was opened, not what the clock read", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			const { usageDataService } = renderDetector(granted, "/teams/42/metrics");
			await settleOnTheHeldClock();
			await act(async () => {
				await vi.advanceTimersByTimeAsync(FLUSH_INTERVAL_MS);
			});

			const [, events] = vi.mocked(usageDataService.postEvents).mock.calls[0];

			// Roughly the one interval that passed. The bounds are wide because the page is noticed a
			// moment after mounting rather than exactly on it, and both ways of getting this wrong miss
			// by far more than a moment: one reports nothing elapsed, the other hands over a date in
			// this decade as though it were a duration.
			expect(events).toHaveLength(1);
			expect(events[0].offsetMs).toBeGreaterThan(FLUSH_INTERVAL_MS / 2);
			expect(events[0].offsetMs).toBeLessThan(FLUSH_INTERVAL_MS * 2);
		} finally {
			vi.useRealTimers();
		}
	});

	// A tab being put away is the moment to hand in; a tab coming back is not. The browser announces
	// both with the same event, so something that listened without asking which one it was would also
	// hand in every time somebody returned to the window.
	it("hands nothing in when the tab comes back", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const { usageDataService } = renderDetector(granted, "/teams/42/metrics");
		await settle(usageDataService);

		bringTheTabBack();
		await act(async () => {
			await Promise.resolve();
		});

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});

	// A detector whose component is gone has to let go of its clock as well. One left running keeps
	// its own handle on the service, and every later mount adds another, so a tab somebody leaves
	// open for a day ends up handing pages in through a growing crowd of detectors nobody can see.
	it("stops handing pages in once the component holding it is gone", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		vi.useFakeTimers({ shouldAdvanceTime: true });

		try {
			const abandoned = renderDetector(granted, "/teams/42/metrics");
			abandoned.unmount();

			const current = renderDetector(granted, "/teams/42/metrics");
			await settleOnTheHeldClock();
			await act(async () => {
				await vi.advanceTimersByTimeAsync(FLUSH_INTERVAL_MS);
			});

			expect(current.usageDataService.postEvents).toHaveBeenCalled();
			expect(abandoned.usageDataService.postEvents).not.toHaveBeenCalled();
		} finally {
			vi.useRealTimers();
		}
	});

	// Nothing leaves without the token that says this browser agreed. A tab can outlive its own
	// storage - somebody clears site data, or the browser reclaims it - and what is left then is a
	// batch with no consent attached to it, which is not a thing to send and find out about later.
	it("hands nothing in once this browser's token is gone", async () => {
		localStorage.setItem(TOKEN_STORAGE_KEY, "this-browsers-token");
		const { usageDataService } = renderDetector(granted, "/teams/42/metrics");
		await settle(usageDataService);

		localStorage.removeItem(TOKEN_STORAGE_KEY);
		hideTheTab();
		await act(async () => {
			await Promise.resolve();
		});

		expect(usageDataService.postEvents).not.toHaveBeenCalled();
	});
});
