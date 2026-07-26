import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { ProcessBehaviorSnapshot } from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { usePbcOverTime } from "./usePbcOverTime";

/**
 * The cache seam (US-06 AC4 / Scenario 22). Both the metric family and the date
 * range determine which series a request answers with, so both belong in the cache
 * key — a family-only key serves the previous range's limits after the dashboard
 * pickers move, silently and with no failing render.
 */

const OWNER_ID = 42;
const RANGE_START = new Date(2026, 6, 1);
const RANGE_END = new Date(2026, 6, 26);
const OTHER_RANGE_START = new Date(2026, 4, 1);
const OTHER_RANGE_END = new Date(2026, 4, 15);

const FIRST_RANGE_SERIES: ProcessBehaviorSnapshot[] = [
	{ recordedAt: "2026-07-02", unpl: 13, average: 8, lnpl: 3 },
];
const SECOND_RANGE_SERIES: ProcessBehaviorSnapshot[] = [
	{ recordedAt: "2026-05-02", unpl: 20, average: 12, lnpl: 4 },
	{ recordedAt: "2026-05-03", unpl: 21, average: 12, lnpl: 3 },
];

function createMetricsService(
	getProcessBehaviorOverTime: ReturnType<typeof vi.fn>,
): IMetricsService<IWorkItem | IFeature> {
	return { getProcessBehaviorOverTime } as unknown as IMetricsService<
		IWorkItem | IFeature
	>;
}

describe("usePbcOverTime", () => {
	it("fetches once per family-and-range pair and passes the range through", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(FIRST_RANGE_SERIES);

		const { result } = renderHook(() =>
			usePbcOverTime(
				OWNER_ID,
				createMetricsService(getProcessBehaviorOverTime),
				RANGE_START,
				RANGE_END,
			),
		);

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);
		expect(getProcessBehaviorOverTime).toHaveBeenCalledWith(
			OWNER_ID,
			"Throughput",
			RANGE_START,
			RANGE_END,
		);
		expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(1);
	});

	it("refetches when the range changes and never serves the previous range's limits", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValueOnce(FIRST_RANGE_SERIES)
			.mockResolvedValueOnce(SECOND_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ startDate, endDate }: { startDate: Date; endDate: Date }) =>
				usePbcOverTime(
					OWNER_ID,
					createMetricsService(getProcessBehaviorOverTime),
					startDate,
					endDate,
				),
			{ initialProps: { startDate: RANGE_START, endDate: RANGE_END } },
		);

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);

		rerender({ startDate: OTHER_RANGE_START, endDate: OTHER_RANGE_END });

		await waitFor(() =>
			expect(result.current.series).toEqual(SECOND_RANGE_SERIES),
		);
		expect(getProcessBehaviorOverTime).toHaveBeenLastCalledWith(
			OWNER_ID,
			"Throughput",
			OTHER_RANGE_START,
			OTHER_RANGE_END,
		);
	});

	it("replays a range it has already fetched without a second request", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValueOnce(FIRST_RANGE_SERIES)
			.mockResolvedValueOnce(SECOND_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ startDate, endDate }: { startDate: Date; endDate: Date }) =>
				usePbcOverTime(
					OWNER_ID,
					createMetricsService(getProcessBehaviorOverTime),
					startDate,
					endDate,
				),
			{ initialProps: { startDate: RANGE_START, endDate: RANGE_END } },
		);

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);
		rerender({ startDate: OTHER_RANGE_START, endDate: OTHER_RANGE_END });
		await waitFor(() =>
			expect(result.current.series).toEqual(SECOND_RANGE_SERIES),
		);

		rerender({ startDate: RANGE_START, endDate: RANGE_END });

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);
		expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(2);
	});

	it("reports null while loading and [] once loaded-but-empty", async () => {
		const getProcessBehaviorOverTime = vi.fn().mockResolvedValue([]);

		const { result } = renderHook(() =>
			usePbcOverTime(
				OWNER_ID,
				createMetricsService(getProcessBehaviorOverTime),
				RANGE_START,
				RANGE_END,
			),
		);

		// The widget's empty-state branch depends on the null/[] distinction.
		expect(result.current.series).toBeNull();
		await waitFor(() => expect(result.current.series).toEqual([]));
	});
});
