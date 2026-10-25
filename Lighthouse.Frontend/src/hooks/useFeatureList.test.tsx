import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import type { FeatureListOwner, IFeatureList } from "../models/FeatureList";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	aFeatureServiceAnswering,
	aList,
	NorthwindPlatformsList,
} from "../tests/FeatureListTestKit";
import { deferred } from "../tests/HeldMetricsService";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { featureListKey, useFeatureList } from "./useFeatureList";

const portfolio = (id: number): FeatureListOwner => ({ kind: "portfolio", id });

const renderTheHook = (
	featureService: ReturnType<typeof aFeatureServiceAnswering>,
	initialOwner: FeatureListOwner,
) => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const services = createMockApiServiceContext({ featureService });
	const wrapper = ({ children }: { children: ReactNode }) => (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={services}>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
	return renderHook((owner: FeatureListOwner) => useFeatureList(owner), {
		wrapper,
		initialProps: initialOwner,
	});
};

describe("useFeatureList", () => {
	it("keys the list by the kind and id of its owner", () => {
		expect(featureListKey({ kind: "team", id: 7 })).toEqual([
			"feature-list",
			"team",
			7,
		]);
	});

	it("is loading until the answer lands, then ready with it", async () => {
		const answer = deferred<IFeatureList>();
		const { result } = renderTheHook(
			aFeatureServiceAnswering(answer.promise),
			portfolio(2),
		);

		expect(result.current.look).toBe("loading");
		expect(result.current.data).toBeUndefined();

		await act(async () => answer.resolve(NorthwindPlatformsList));

		await waitFor(() => expect(result.current.look).toBe("ready"));
		expect(result.current.data).toBe(NorthwindPlatformsList);
	});

	it("looks failed when the list cannot be read", async () => {
		const { result } = renderTheHook(
			aFeatureServiceAnswering(Promise.reject(new Error("dropped"))),
			portfolio(2),
		);

		await waitFor(() => expect(result.current.look).toBe("error"));
	});

	it("never lets an answer for an owner already left replace the current one", async () => {
		const first = deferred<IFeatureList>();
		const second = deferred<IFeatureList>();
		const { result, rerender } = renderTheHook(
			aFeatureServiceAnswering(first.promise, second.promise),
			portfolio(2),
		);

		rerender(portfolio(5));
		await act(async () => second.resolve(aList([])));
		await act(async () => first.resolve(NorthwindPlatformsList));

		await waitFor(() => expect(result.current.look).toBe("ready"));
		expect(result.current.data?.features).toEqual([]);
	});
});
