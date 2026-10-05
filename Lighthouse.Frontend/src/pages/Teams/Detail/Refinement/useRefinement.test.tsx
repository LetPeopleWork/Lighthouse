import { act, renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { ErrorSnackbarContext } from "../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { IRefinementView } from "../../../../models/Refinement/Refinement";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { IRefinementService } from "../../../../services/Api/RefinementService";
import { createMockApiServiceContext } from "../../../../tests/MockApiServiceProvider";
import { GRAVITY_TEAM_ID } from "../../../../tests/RefinementTabTestKit";
import { useRefinement } from "./useRefinement";

/**
 * The error snackbar outlives the Refinement tab, so a read that fails after the tab was closed would
 * otherwise put up an error about a tab nobody is looking at.
 */

const aTabWhoseReadIsStillOnItsWay = () => {
	let failTheRead: (error: Error) => void = () => {};
	const refinementService: IRefinementService = {
		getRefinement: vi.fn(
			() =>
				new Promise<IRefinementView>((_answer, fail) => {
					failTheRead = fail;
				}),
		),
	};
	const snackbar = { showError: vi.fn() };
	const services = createMockApiServiceContext({ refinementService });
	const wrapper = ({ children }: { children: ReactNode }) => (
		<ErrorSnackbarContext.Provider value={snackbar}>
			<ApiServiceContext.Provider value={services}>
				{children}
			</ApiServiceContext.Provider>
		</ErrorSnackbarContext.Provider>
	);

	const { unmount } = renderHook(() => useRefinement(GRAVITY_TEAM_ID), {
		wrapper,
	});

	return {
		showError: snackbar.showError,
		closeTheTab: unmount,
		theReadFails: (message: string) =>
			act(async () => {
				failTheRead(new Error(message));
			}),
	};
};

describe("useRefinement", () => {
	it("says why the Refinement could not be read while the tab is open", async () => {
		const { showError, theReadFails } = aTabWhoseReadIsStillOnItsWay();

		await theReadFails("Service unavailable");

		expect(showError).toHaveBeenCalledWith("Service unavailable");
	});

	it("says nothing about a read that fails after the tab was closed", async () => {
		const { showError, closeTheTab, theReadFails } =
			aTabWhoseReadIsStillOnItsWay();

		closeTheTab();
		await theReadFails("Service unavailable");

		expect(showError).not.toHaveBeenCalled();
	});
});
