import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { WindowRequest } from "./metricsOwnerKey";
import { dayOf, listOf, useMetricsQuery } from "./useMetricsQuery";

const request: WindowRequest = {
	ownerType: "team",
	ownerId: 1,
	ownerUpdatedAt: 0,
	from: "2026-01-01",
	to: "2026-01-31",
};

function renderFailingQuery(failureMessage?: string) {
	return renderHook(
		() =>
			useMetricsQuery(
				"getSomething",
				request,
				() => Promise.reject(new Error("backend down")),
				true,
				failureMessage,
			),
		{
			wrapper: ({ children }: { children: ReactNode }) =>
				createElement(
					QueryClientProvider,
					{ client: new QueryClient() },
					children,
				),
		},
	);
}

afterEach(() => {
	vi.restoreAllMocks();
});

describe("dayOf", () => {
	it("reads a calendar day as that day in the viewer's own zone", () => {
		const day = dayOf("2026-03-09");

		expect(day.getFullYear()).toBe(2026);
		expect(day.getMonth()).toBe(2);
		expect(day.getDate()).toBe(9);
	});

	it("refuses something that is not a calendar day and names it", () => {
		expect(() => dayOf("2026-02-30")).toThrow(
			new TypeError("2026-02-30 is not a calendar day."),
		);
	});
});

describe("listOf", () => {
	it("gives an empty list while the query has no answer", () => {
		expect(listOf({ answer: null, progress: undefined as never })).toEqual([]);
	});

	it("gives the answer when there is one", () => {
		expect(listOf({ answer: [1, 2], progress: undefined as never })).toEqual([
			1, 2,
		]);
	});
});

describe("useMetricsQuery failure reporting", () => {
	it("logs the caller's failure message with the error when the request fails", async () => {
		const logged = vi.spyOn(console, "error").mockImplementation(() => {});

		const { result } = renderFailingQuery("Could not load the thing");

		await waitFor(() => expect(result.current.progress.isError).toBe(true));
		expect(logged).toHaveBeenCalledExactlyOnceWith(
			"Could not load the thing",
			new Error("backend down"),
		);
	});

	it("logs nothing of its own when the caller gave no failure message", async () => {
		const logged = vi.spyOn(console, "error").mockImplementation(() => {});

		const { result } = renderFailingQuery();

		await waitFor(() => expect(result.current.progress.isError).toBe(true));
		expect(logged).not.toHaveBeenCalled();
	});
});
