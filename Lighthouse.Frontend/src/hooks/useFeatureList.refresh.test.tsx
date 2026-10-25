import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { FeatureListOwner } from "../models/FeatureList";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	aFeatureServiceAnswering,
	NorthwindPlatformsList,
} from "../tests/FeatureListTestKit";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import {
	featureListKey,
	useFeatureList,
	useRefreshFeatureList,
} from "./useFeatureList";

const team: FeatureListOwner = { kind: "team", id: 7 };

describe("useRefreshFeatureList", () => {
	it("makes the owner's list ask again, and only that owner's", () => {
		const queryClient = new QueryClient();
		const invalidate = vi.spyOn(queryClient, "invalidateQueries");
		const wrapper = ({ children }: { children: ReactNode }) => (
			<QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
		);

		const { result } = renderHook(() => useRefreshFeatureList(team), {
			wrapper,
		});
		result.current();

		expect(invalidate).toHaveBeenCalledTimes(1);
		expect(invalidate).toHaveBeenCalledWith({
			queryKey: featureListKey(team),
		});
	});

	it("does nothing, and does not fail, on a page shown without a query client", () => {
		const { result } = renderHook(() => useRefreshFeatureList(team));

		expect(() => result.current()).not.toThrow();
	});
});

describe("useFeatureList parents", () => {
	it("names no parent until the answer arrives, then the parents of the answer by reference id", async () => {
		const queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false } },
		});
		const services = createMockApiServiceContext({
			featureService: aFeatureServiceAnswering(
				Promise.resolve(NorthwindPlatformsList),
			),
		});
		const wrapper = ({ children }: { children: ReactNode }) => (
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={services}>
					{children}
				</ApiServiceContext.Provider>
			</QueryClientProvider>
		);

		const { result } = renderHook(() => useFeatureList(team), { wrapper });

		expect(result.current.parentsByReferenceId.size).toBe(0);
		await waitFor(() =>
			expect(result.current.parentsByReferenceId.size).toBe(1),
		);
		expect(result.current.parentsByReferenceId.get("ST-1")?.name).toBe(
			"Platform 2027",
		);
	});
});
