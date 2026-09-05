import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { PercentilesOverTimeSnapshot } from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { cacheKey, usePercentilesOverTime } from "./usePercentilesOverTime";

/**
 * The cache seam. Both the selection and the date range determine which series a
 * request answers with, so both belong in the cache key — a selection-only key
 * serves the previous range's series after the dashboard pickers move, silently
 * and with no failing render.
 */

const OWNER_ID = 42;
const OTHER_OWNER_ID = 43;
const RANGE_START = new Date(2026, 6, 1);
const RANGE_END = new Date(2026, 6, 26);
const OTHER_RANGE_START = new Date(2026, 4, 1);
const OTHER_RANGE_END = new Date(2026, 4, 15);

function snapshot(
	recordedAt: string,
	p50: number,
): PercentilesOverTimeSnapshot {
	return { recordedAt, metricType: "CycleTime", p50, p70: 5, p85: 8, p95: 13 };
}

const FIRST_RANGE_SERIES = [snapshot("2026-07-02", 3)];
const SECOND_RANGE_SERIES = [
	snapshot("2026-05-02", 9),
	snapshot("2026-05-03", 9),
];

function createMetricsService(
	getPercentilesOverTime: ReturnType<typeof vi.fn>,
): IMetricsService<IWorkItem | IFeature> {
	return { getPercentilesOverTime } as unknown as IMetricsService<
		IWorkItem | IFeature
	>;
}

describe("usePercentilesOverTime", () => {
	it("fetches once per selection-and-range pair and passes the range through", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockResolvedValue(FIRST_RANGE_SERIES);

		const { result } = renderHook(() =>
			usePercentilesOverTime(
				OWNER_ID,
				createMetricsService(getPercentilesOverTime),
				RANGE_START,
				RANGE_END,
			),
		);

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);
		expect(getPercentilesOverTime).toHaveBeenCalledWith(
			OWNER_ID,
			30,
			RANGE_START,
			RANGE_END,
		);
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(1);
	});

	it("refetches when the range changes and never serves the previous range's series", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockResolvedValueOnce(FIRST_RANGE_SERIES)
			.mockResolvedValueOnce(SECOND_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ startDate, endDate }: { startDate: Date; endDate: Date }) =>
				usePercentilesOverTime(
					OWNER_ID,
					createMetricsService(getPercentilesOverTime),
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
		expect(getPercentilesOverTime).toHaveBeenLastCalledWith(
			OWNER_ID,
			30,
			OTHER_RANGE_START,
			OTHER_RANGE_END,
		);
	});

	it("replays a range it has already fetched without a second request", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockResolvedValueOnce(FIRST_RANGE_SERIES)
			.mockResolvedValueOnce(SECOND_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ startDate, endDate }: { startDate: Date; endDate: Date }) =>
				usePercentilesOverTime(
					OWNER_ID,
					createMetricsService(getPercentilesOverTime),
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
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(2);
	});

	it("keeps caching per selection within one range (no recompute on toggle)", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockResolvedValue(FIRST_RANGE_SERIES);

		const { result } = renderHook(() =>
			usePercentilesOverTime(
				OWNER_ID,
				createMetricsService(getPercentilesOverTime),
				RANGE_START,
				RANGE_END,
			),
		);

		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);

		act(() => result.current.setSelection(60));
		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledTimes(2),
		);

		act(() => result.current.setSelection(30));
		await waitFor(() =>
			expect(result.current.series).toEqual(FIRST_RANGE_SERIES),
		);
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(2);
	});
});

/**
 * The selected range names two calendar days, not two instants. Written through
 * UTC it names the day before for every viewer at a positive offset, and it turns
 * each clock time within one selected day into a separate entry. These cases only
 * bite at a non-zero UTC offset — the suite pins one (see the `test` script).
 */
describe("usePercentilesOverTime cache key", () => {
	const LOCAL_MIDNIGHT_JULY_1 = new Date(2026, 6, 1);
	const SAME_DAY_MID_AFTERNOON = new Date(2026, 6, 1, 14, 30);

	it("names the local calendar day, not the UTC day the instant falls on", () => {
		expect(cacheKey(30, LOCAL_MIDNIGHT_JULY_1, RANGE_END)).toBe(
			"30|2026-07-01|2026-07-26",
		);
	});

	it("does not refetch when only the clock time within a selected day moves", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockResolvedValue(FIRST_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ startDate }: { startDate: Date }) =>
				usePercentilesOverTime(
					OWNER_ID,
					createMetricsService(getPercentilesOverTime),
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
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(1);
	});
});

/**
 * The selected range does not move when the dashboard switches from one team to
 * the next, so both requests are filed under the same key. The first team's answer
 * arriving last would plot its percentiles under the second team's name, and
 * nothing on screen would admit the swap.
 */
describe("usePercentilesOverTime when a response outlives the request that asked for it", () => {
	it("keeps the series of the owner now on screen when a superseded response lands late", async () => {
		let answerFirstRequest: (series: PercentilesOverTimeSnapshot[]) => void =
			() => {
				// Replaced while the promise is being constructed, below.
			};
		const firstRequest = new Promise<PercentilesOverTimeSnapshot[]>(
			(resolve) => {
				answerFirstRequest = resolve;
			},
		);
		const getPercentilesOverTime = vi
			.fn()
			.mockReturnValueOnce(firstRequest)
			.mockResolvedValueOnce(SECOND_RANGE_SERIES);

		const { result, rerender } = renderHook(
			({ ownerId }: { ownerId: number }) =>
				usePercentilesOverTime(
					ownerId,
					createMetricsService(getPercentilesOverTime),
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
		const getPercentilesOverTime = vi.fn().mockRejectedValue(failure);

		const { result } = renderHook(() =>
			usePercentilesOverTime(
				OWNER_ID,
				createMetricsService(getPercentilesOverTime),
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
