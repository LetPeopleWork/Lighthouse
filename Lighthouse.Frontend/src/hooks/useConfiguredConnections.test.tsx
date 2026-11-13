import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import { WorkTrackingSystemConnection } from "../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../services/Api/ApiError";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockWorkTrackingSystemService,
} from "../tests/MockApiServiceProvider";
import { useConfiguredConnections } from "./useConfiguredConnections";

const stateWhenTheListAnswers = (answer: () => Promise<unknown>) => {
	const workTrackingSystemService = createMockWorkTrackingSystemService();
	vi.mocked(
		workTrackingSystemService.getConfiguredWorkTrackingSystems,
	).mockImplementation(
		answer as () => Promise<WorkTrackingSystemConnection[]>,
	);
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const wrapper = ({ children }: { children: React.ReactNode }) => (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider
				value={createMockApiServiceContext({ workTrackingSystemService })}
			>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
	return renderHook(() => useConfiguredConnections(), { wrapper });
};

describe("useConfiguredConnections", () => {
	it("is ready with the list once it arrives", async () => {
		const connection = new WorkTrackingSystemConnection({
			id: 1,
			name: "Jira",
			workTrackingSystem: "Jira",
			options: [],
			authenticationMethodKey: "jira.cloud",
		});
		const { result } = stateWhenTheListAnswers(() =>
			Promise.resolve([connection]),
		);

		await waitFor(() =>
			expect(result.current).toEqual({
				status: "ready",
				connections: [connection],
			}),
		);
	});

	it("is refused when the server answers 403", async () => {
		const { result } = stateWhenTheListAnswers(() =>
			Promise.reject(new ApiError(403, "Forbidden")),
		);

		await waitFor(() => expect(result.current).toEqual({ status: "refused" }));
	});

	it("has failed when the server answers with another error", async () => {
		const { result } = stateWhenTheListAnswers(() =>
			Promise.reject(new ApiError(500, "Server error")),
		);

		await waitFor(() => expect(result.current).toEqual({ status: "failed" }));
	});
});
