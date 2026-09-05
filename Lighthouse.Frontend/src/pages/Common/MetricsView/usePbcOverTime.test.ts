import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { ProcessBehaviorSnapshot } from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { cacheKey, usePbcOverTime } from "./usePbcOverTime";

/**
 * The cache seam. Both the metric family and the date range determine which series
 * a request answers with, so both belong in the cache key — a family-only key
 * serves the previous range's limits after the dashboard pickers move, silently
 * and with no failing render.
 */

const OWNER_ID = 42;
const OTHER_OWNER_ID = 43;
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

/**
 * The selected range names two calendar days, not two instants. Written through
 * UTC it names the day before for every viewer at a positive offset, and it turns
 * each clock time within one selected day into a separate entry. These cases only
 * bite at a non-zero UTC offset — the suite pins one (see the `test` script).
 */
describe("usePbcOverTime cache key", () => {
	const LOCAL_MIDNIGHT_JULY_1 = new Date(2026, 6, 1);
	const SAME_DAY_MID_AFTERNOON = new Date(2026, 6, 1, 14, 30);

	it("names the local calendar day, not the UTC day the instant falls on", () => {
		expect(cacheKey("Throughput", LOCAL_MIDNIGHT_JULY_1, RANGE_END)).toBe(
			"Throughput|2026-07-01|2026-07-26",
		);
	});

	it("does not refetch when only the clock time within a selected day moves", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(FIRST_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ startDate }: { startDate: Date }) =>
				usePbcOverTime(
					OWNER_ID,
					createMetricsService(getProcessBehaviorOverTime),
					startDate,
					RANGE_END,
				),
			{ initialProps: { startDate: LOCAL_MIDNIGHT_JULY_1 } },
		);

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);

		rerender({ startDate: SAME_DAY_MID_AFTERNOON });

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);
		expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(1);
	});
});

/**
 * The selected range does not move when the dashboard switches from one team to
 * the next, so both requests are filed under the same key. The first team's answer
 * arriving last would plot its limits under the second team's name, and nothing on
 * screen would admit the swap.
 */
describe("usePbcOverTime when a response outlives the request that asked for it", () => {
	it("keeps the series of the owner now on screen when a superseded response lands late", async () => {
		let answerFirstRequest: (series: ProcessBehaviorSnapshot[]) => void =
			() => {
				// Replaced while the promise is being constructed, below.
			};
		const firstRequest = new Promise<ProcessBehaviorSnapshot[]>((resolve) => {
			answerFirstRequest = resolve;
		});
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockReturnValueOnce(firstRequest)
			.mockResolvedValueOnce(SECOND_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ ownerId }: { ownerId: number }) =>
				usePbcOverTime(
					ownerId,
					createMetricsService(getProcessBehaviorOverTime),
					RANGE_START,
					RANGE_END,
				),
			{ initialProps: { ownerId: OWNER_ID } },
		);

		rerender({ ownerId: OTHER_OWNER_ID });
		await waitFor(() =>
			expect(result.current.series).toEqual(SECOND_RANGE_SERIES),
		);

		await act(async () => {
			answerFirstRequest(FIRST_RANGE_SERIES);
		});

		expect(result.current.series).toEqual(SECOND_RANGE_SERIES);
	});

	it("surfaces a failed request and leaves the widget with nothing to plot", async () => {
		const consoleError = vi.spyOn(console, "error").mockImplementation(() => {
			// Kept out of the test output; that it was called is what is asserted.
		});
		const failure = new Error("the metrics endpoint is down");
		const getProcessBehaviorOverTime = vi.fn().mockRejectedValue(failure);

		const { result } = renderHook(() =>
			usePbcOverTime(
				OWNER_ID,
				createMetricsService(getProcessBehaviorOverTime),
				RANGE_START,
				RANGE_END,
			),
		);

		await waitFor(() =>
			expect(consoleError).toHaveBeenCalledWith(expect.any(String), failure),
		);
		expect(result.current.series).toBeNull();

		consoleError.mockRestore();
	});
});
