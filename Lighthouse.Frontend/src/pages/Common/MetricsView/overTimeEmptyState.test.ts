import { describe, expect, it } from "vitest";
import {
	OVER_TIME_FORWARD_ONLY_EMPTY_COPY,
	OVER_TIME_RANGE_EMPTY_COPY,
	resolveOverTimeEmptyCopy,
} from "./overTimeEmptyState";

/**
 * D10 / DDD-13. The same-day cases are the ones that matter: the dashboard's default
 * endDate is seeded from `new Date()` and carries a time-of-day, so an instant
 * comparison instead of a calendar-day one would flip the default range to the
 * in-range copy and break both shipped E2Es.
 */

function todayAt(hours: number, minutes = 0): Date {
	const today = new Date();
	return new Date(
		today.getFullYear(),
		today.getMonth(),
		today.getDate(),
		hours,
		minutes,
	);
}

function daysFromToday(offset: number): Date {
	const today = new Date();
	return new Date(
		today.getFullYear(),
		today.getMonth(),
		today.getDate() + offset,
	);
}

describe("resolveOverTimeEmptyCopy", () => {
	// Both strings are pinned as LITERALS, not compared to themselves: the assertions
	// below check resolveOverTimeEmptyCopy against the constants, which stays green if a
	// constant is blanked. Only these two tie the constants to the shipped prose.
	it("keeps the shipped forward-only copy byte-for-byte", () => {
		expect(OVER_TIME_FORWARD_ONLY_EMPTY_COPY).toBe(
			"builds forward from today — no snapshots recorded yet",
		);
	});

	it("states the in-range copy verbatim", () => {
		expect(OVER_TIME_RANGE_EMPTY_COPY).toBe(
			"no data recorded in the selected range",
		);
	});

	it("says the range is empty when the window ended before today", () => {
		expect(resolveOverTimeEmptyCopy(daysFromToday(-1))).toBe(
			OVER_TIME_RANGE_EMPTY_COPY,
		);
		expect(resolveOverTimeEmptyCopy(daysFromToday(-30))).toBe(
			OVER_TIME_RANGE_EMPTY_COPY,
		);
	});

	it("keeps the forward-only copy when the window ends today, whatever the time of day", () => {
		expect(resolveOverTimeEmptyCopy(todayAt(0))).toBe(
			OVER_TIME_FORWARD_ONLY_EMPTY_COPY,
		);
		expect(resolveOverTimeEmptyCopy(todayAt(23, 59))).toBe(
			OVER_TIME_FORWARD_ONLY_EMPTY_COPY,
		);
	});

	it("keeps the forward-only copy when the window ends in the future", () => {
		expect(resolveOverTimeEmptyCopy(daysFromToday(1))).toBe(
			OVER_TIME_FORWARD_ONLY_EMPTY_COPY,
		);
	});
});
