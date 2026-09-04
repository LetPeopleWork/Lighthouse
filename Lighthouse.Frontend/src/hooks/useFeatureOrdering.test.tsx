import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type { FeatureOrderingPolicy } from "../models/FeatureOrdering";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockOptionalFeatureService,
} from "../tests/MockApiServiceProvider";
import { useFeatureOrdering } from "./useFeatureOrdering";

// The one place the client reads who owns the order, and the one place the position column's header is
// decided. "The header reads Manual once the instance owns the order" and "it goes back to # when the
// order is handed back" are the same question asked twice, so both are asked here.
//
// The instance keeps its answer as an on/off row in the settings store, not as a policy of its own, so
// the arrangement below is what the store really serves.
const theOrderingSettingWhen = (policy: FeatureOrderingPolicy) => ({
	id: 0,
	key: "FeatureOrdering",
	name: "Let Lighthouse own the order of your {{features}}",
	description: "Turn this on to arrange your {{features}} yourself.",
	enabled: policy === "ManualOrder",
	isPremium: true,
	isPreview: false,
});

const renderTheHookOnAnInstanceWhere = (policy: FeatureOrderingPolicy) => {
	const optionalFeatureService = createMockOptionalFeatureService();
	optionalFeatureService.getFeatureByKey = vi
		.fn()
		.mockResolvedValue(theOrderingSettingWhen(policy));

	const apiContext = createMockApiServiceContext({ optionalFeatureService });

	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<QueryClientProvider
			client={
				new QueryClient({ defaultOptions: { queries: { retry: false } } })
			}
		>
			<ApiServiceContext.Provider value={apiContext}>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);

	return renderHook(() => useFeatureOrdering(), { wrapper });
};

describe("useFeatureOrdering", () => {
	it("reports the tracker owning the order before anybody has chosen", async () => {
		const { result } = renderTheHookOnAnInstanceWhere("SourceOrder");

		await waitFor(() => {
			expect(result.current.policy).toBe("SourceOrder");
		});
	});

	it("reports this instance owning the order once it has been handed over", async () => {
		const { result } = renderTheHookOnAnInstanceWhere("ManualOrder");

		await waitFor(() => {
			expect(result.current.policy).toBe("ManualOrder");
		});
	});

	// The header names whoever owns the order. The column factory stays policy-ignorant — it takes the
	// label it is given — so this is the only place the two labels may be decided.
	it("names the position column after whoever owns the order", async () => {
		const { result } = renderTheHookOnAnInstanceWhere("ManualOrder");

		await waitFor(() => {
			expect(result.current.positionColumnLabel).toBe("Manual");
		});
	});

	it("gives the position column its plain heading back when the tracker owns the order", async () => {
		const { result } = renderTheHookOnAnInstanceWhere("SourceOrder");

		await waitFor(() => {
			expect(result.current.positionColumnLabel).toBe("#");
		});
	});

	// An instance that cannot answer must read as following the tracker, which is what it did before
	// anyone could choose. Guessing the other way would re-sequence every forecast on a failed request.
	it("follows the tracker when the instance cannot say who owns the order", async () => {
		const optionalFeatureService = createMockOptionalFeatureService();
		optionalFeatureService.getFeatureByKey = vi
			.fn()
			.mockRejectedValue(new Error("the instance is unreachable"));

		const apiContext = createMockApiServiceContext({ optionalFeatureService });

		const wrapper = ({ children }: { children: React.ReactNode }) => (
			<QueryClientProvider
				client={
					new QueryClient({ defaultOptions: { queries: { retry: false } } })
				}
			>
				<ApiServiceContext.Provider value={apiContext}>
					{children}
				</ApiServiceContext.Provider>
			</QueryClientProvider>
		);

		const { result } = renderHook(() => useFeatureOrdering(), { wrapper });

		await waitFor(() => {
			expect(optionalFeatureService.getFeatureByKey).toHaveBeenCalled();
		});
		expect(result.current.policy).toBe("SourceOrder");
		expect(result.current.positionColumnLabel).toBe("#");
	});
});
