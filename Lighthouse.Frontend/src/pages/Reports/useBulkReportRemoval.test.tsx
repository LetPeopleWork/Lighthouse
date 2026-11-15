import {
	QueryClient,
	QueryClientProvider,
	useQuery,
} from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import type {
	IReportAcrossOwners,
	IReportRemovalOutcome,
} from "../../models/Reports/ReportsAcrossOwners";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import {
	aReportOn,
	GRAVITY,
	MONTH,
	notRemoved,
	OCEAN_EXPLORER,
	QUARTER,
	removed,
	ZENITH,
} from "../../tests/BulkReportsTestKit";
import { createMockApiServiceContext } from "../../tests/MockApiServiceProvider";
import { aReportService } from "../../tests/ReportsTestKit";
import {
	ALL_REPORTS_QUERY_KEY,
	ownerOfReport,
	reportQueryKeyOf,
	reportsQueryKeyOf,
} from "../Common/Reports/reportOwner";
import { useBulkReportRemoval } from "./useBulkReportRemoval";

/** Several reports removed in one request: which ones are forgotten, and that every report list is read again. */

const zenithsQuarter = aReportOn(ZENITH, QUARTER, { id: 801 });
const gravitysQuarter = aReportOn(GRAVITY, QUARTER, { id: 802 });
const oceanExplorersMonth = aReportOn(OCEAN_EXPLORER, MONTH, { id: 803 });
const theThree = [zenithsQuarter, gravitysQuarter, oceanExplorersMonth];

const keyOf = (report: IReportAcrossOwners) =>
	reportQueryKeyOf(ownerOfReport(report), report.id);

const sceneAnswering = (
	removeReportsInBulk: () => Promise<IReportRemovalOutcome[]>,
) => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false } },
	});
	const services = createMockApiServiceContext({
		reportService: aReportService({ removeReportsInBulk }),
	});
	const wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={services}>
				{children}
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
	return { queryClient, wrapper };
};

const hasReportInCache = (
	queryClient: QueryClient,
	reports: readonly IReportAcrossOwners[],
) =>
	reports.map(
		(report) => queryClient.getQueryData(keyOf(report)) !== undefined,
	);

const remembered = (
	queryClient: QueryClient,
	reports: readonly IReportAcrossOwners[],
) => {
	for (const report of reports) {
		queryClient.setQueryData(keyOf(report), report);
	}
};

const removeThem = async (
	mutate: (reports: IReportAcrossOwners[]) => Promise<unknown>,
) => {
	await act(async () => {
		await mutate(theThree).catch(() => undefined);
	});
};

describe("Removing several reports in one request", () => {
	it("forgets each report removed or found already gone, and keeps one it could not remove", async () => {
		const { queryClient, wrapper } = sceneAnswering(
			vi
				.fn()
				.mockResolvedValue([
					removed(801),
					removed(802, "already-gone"),
					notRemoved(803, "not-admin"),
				]),
		);
		remembered(queryClient, theThree);
		const { result } = renderHook(() => useBulkReportRemoval(), { wrapper });

		await removeThem(result.current.mutateAsync);

		expect(hasReportInCache(queryClient, theThree)).toEqual([
			false,
			false,
			true,
		]);
	});

	it("forgets the report that was removed, not one picked before it", async () => {
		// The answer comes in another order than the reports were picked.
		const { queryClient, wrapper } = sceneAnswering(
			vi
				.fn()
				.mockResolvedValue([
					removed(803),
					notRemoved(801, "not-admin"),
					removed(802),
				]),
		);
		remembered(queryClient, theThree);
		const { result } = renderHook(() => useBulkReportRemoval(), { wrapper });

		await removeThem(result.current.mutateAsync);

		expect(hasReportInCache(queryClient, theThree)).toEqual([
			true,
			false,
			false,
		]);
	});

	it("reads every report list again, also when no answer arrives", async () => {
		const { wrapper } = sceneAnswering(
			vi.fn().mockRejectedValue(new Error("the network dropped")),
		);
		const readEveryReport = vi.fn().mockResolvedValue({ reports: [] });
		const readZeniths = vi.fn().mockResolvedValue([]);
		const { result } = renderHook(
			() => {
				useQuery({
					queryKey: ALL_REPORTS_QUERY_KEY,
					queryFn: readEveryReport,
				});
				useQuery({
					queryKey: reportsQueryKeyOf(ZENITH),
					queryFn: readZeniths,
				});
				return useBulkReportRemoval();
			},
			{ wrapper },
		);
		await waitFor(() => expect(readZeniths).toHaveBeenCalledTimes(1));
		await waitFor(() => expect(readEveryReport).toHaveBeenCalledTimes(1));

		await removeThem(result.current.mutateAsync);

		await waitFor(() => expect(readEveryReport).toHaveBeenCalledTimes(2));
		await waitFor(() => expect(readZeniths).toHaveBeenCalledTimes(2));
	});
});
