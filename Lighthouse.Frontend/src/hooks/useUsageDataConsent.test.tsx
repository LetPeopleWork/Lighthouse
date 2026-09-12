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
