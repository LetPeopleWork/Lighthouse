import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import {
	useCumulativeScopeChoice,
	useCumulativeStateTimeCandidates,
} from "./useCumulativeStateTimeChoices";

const LAST_30_DAYS = "2026-07-01|2026-07-30";
const LAST_90_DAYS = "2026-05-02|2026-07-30";

describe("the stretch the reader narrows Cumulative Time per State to", () => {
	it("is chosen for the window the reader is on now, even after the window has moved", () => {
		const { result, rerender } = renderHook(
			({ question }: { question: string }) =>
				useCumulativeScopeChoice(question),
			{ initialProps: { question: LAST_30_DAYS } },
		);

		rerender({ question: LAST_90_DAYS });
		act(() => result.current.chooseScope(7));

		expect(result.current.scopeDefinitionId).toBe(7);
	});
});

describe("the Work Items the Cumulative Time per State picker offers", () => {
	const owner = { ownerId: 4, ownerUpdatedAt: 0 };
	const startDate = new Date(2026, 6, 1);
	const endDate = new Date(2026, 6, 30);
	const offered = [{ id: 11 }];

	function pickerFor(ownerType: "team" | "portfolio") {
		const metricsService = {
			getCumulativeStateTimeCandidatesForTeam: vi
				.fn()
				.mockResolvedValue({ items: offered }),
			getCumulativeStateTimeCandidatesForPortfolio: vi
				.fn()
				.mockResolvedValue({ items: offered }),
		};
		const queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false } },
		});
		const rendered = renderHook(
			() =>
				useCumulativeStateTimeCandidates({
					metricsService: metricsService as unknown as IMetricsService<
						IWorkItem | IFeature
					>,
					ownerType,
					owner,
					startDate,
					endDate,
					selectedQuestion: LAST_30_DAYS,
				}),
			{
				wrapper: ({ children }: { children: ReactNode }) =>
					createElement(QueryClientProvider, { client: queryClient }, children),
			},
		);
		return { metricsService, ...rendered };
	}

	it("offers nothing before the reader opens the picker", () => {
		const { result } = pickerFor("team");

		expect(result.current.candidates).toEqual([]);
		expect(result.current.candidatesLoaded).toBe(false);
	});

	it("asks a portfolio's own Work Items once the reader opens the picker", async () => {
		const { result, metricsService } = pickerFor("portfolio");

		act(() => result.current.onPickerOpen());

		await waitFor(() => expect(result.current.candidates).toEqual(offered));
		expect(
			metricsService.getCumulativeStateTimeCandidatesForPortfolio,
		).toHaveBeenCalledWith(owner.ownerId, startDate, endDate);
		expect(
			metricsService.getCumulativeStateTimeCandidatesForTeam,
		).not.toHaveBeenCalled();
	});
});
