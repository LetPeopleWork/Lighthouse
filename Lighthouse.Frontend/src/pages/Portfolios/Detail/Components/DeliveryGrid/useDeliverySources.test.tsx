import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
} from "../../../../../tests/MockApiServiceProvider";
import { JIRA_RELEASE_SOURCE } from "./DeliverySourceTabFixture";
import { useDeliverySources } from "./useDeliverySources";

/**
 * The answer the Deliveries page and its dialog share. Counted on the requests it makes, because "the
 * dialog asks nothing more" is a claim about requests, not about what ends up on screen.
 */

const deliveryService = createMockDeliveryService();

const aPageAndItsDialog = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const context = createMockApiServiceContext({ deliveryService });
	const wrapper = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={context}>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);

	return (portfolioId: number) =>
		renderHook(() => useDeliverySources(portfolioId), { wrapper });
};

const requestsMade = () =>
	vi.mocked(deliveryService.getDeliverySources).mock.calls.length;

beforeEach(() => {
	vi.clearAllMocks();
	vi.mocked(deliveryService.getDeliverySources).mockResolvedValue([
		JIRA_RELEASE_SOURCE,
	]);
});

describe("the ways to pick a Portfolio's connection offers", () => {
	it("are asked for once however many readers there are on the page", async () => {
		const observe = aPageAndItsDialog();

		const page = observe(1);
		const dialog = observe(1);

		await waitFor(() => expect(dialog.result.current.status).toBe("success"));
		expect(page.result.current.data).toEqual([JIRA_RELEASE_SOURCE]);
		expect(requestsMade()).toBe(1);
	});

	it("asking again asks exactly once more", async () => {
		const observe = aPageAndItsDialog();
		const dialog = observe(1);
		await waitFor(() => expect(dialog.result.current.status).toBe("success"));

		await act(async () => {
			await dialog.result.current.refetch();
		});

		expect(requestsMade()).toBe(2);
	});

	it("a failed answer reads as failed, never as a connection that offers nothing", async () => {
		vi.mocked(deliveryService.getDeliverySources).mockRejectedValue(
			new Error("The server could not answer"),
		);
		const observe = aPageAndItsDialog();

		const dialog = observe(1);

		await waitFor(() => expect(dialog.result.current.status).toBe("error"));
		expect(dialog.result.current.data).toBeUndefined();
		expect(requestsMade()).toBe(1);
	});

	it("another Portfolio has an answer of its own", async () => {
		const observe = aPageAndItsDialog();

		const paymentsPlatform = observe(1);
		const onboarding = observe(2);

		await waitFor(() =>
			expect(onboarding.result.current.status).toBe("success"),
		);
		expect(paymentsPlatform.result.current.status).toBe("success");
		expect(deliveryService.getDeliverySources).toHaveBeenCalledWith(1);
		expect(deliveryService.getDeliverySources).toHaveBeenCalledWith(2);
	});
});
