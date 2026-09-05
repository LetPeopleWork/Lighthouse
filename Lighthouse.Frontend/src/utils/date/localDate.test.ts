import { describe, expect, it } from "vitest";
import { formatLocalDate, parseLocalDate } from "./localDate";

/**
 * Regression cover for Bug #5566: the metrics dashboards used to encode the
 * startDate/endDate URL params in UTC (`toISOString().split("T")[0]`) while the
 * request layer built them from local Y/M/D parts. On any non-zero UTC offset a
 * round-trip through the URL therefore lost exactly one calendar day.
 *
 * The suite runs under a pinned non-UTC timezone (see the `test` script) — under
 * UTC these assertions are all trivially satisfied by the buggy encoding, which
 * is why the defect survived CI for so long.
 */
describe("formatLocalDate", () => {
	it("keeps the calendar day a local midnight date stands for", () => {
		// The date pickers hand out local midnight. That is the exact instant a
		// UTC encoding pushes across the date boundary at a positive offset.
		const localMidnight = new Date(2026, 6, 26, 0, 0, 0, 0);

		expect(formatLocalDate(localMidnight)).toBe("2026-07-26");
	});

	it("keeps the calendar day for a late-evening local time", () => {
		// The mirror case: at a negative offset the end of the day is what crosses.
		const lateEvening = new Date(2026, 6, 26, 23, 59, 59, 999);

		expect(formatLocalDate(lateEvening)).toBe("2026-07-26");
	});

	it("zero-pads month and day", () => {
		expect(formatLocalDate(new Date(2026, 0, 5, 0, 0, 0, 0))).toBe(
			"2026-01-05",
		);
	});

	it("refuses a date that could not be parsed", () => {
		// Left unchecked this returns the string "NaN-NaN-NaN", which reads as a
		// day everywhere it is used and corrupts a URL param or a query string
		// without anything noticing.
		expect(() => formatLocalDate(new Date("nonsense"))).toThrow();
	});

	it("names the value it refused", () => {
		expect(() => formatLocalDate(new Date("nonsense"))).toThrow(/Invalid Date/);
	});
});

describe("parseLocalDate", () => {
	it("anchors the parsed date at local midnight, not UTC midnight", () => {
		const parsed = parseLocalDate("2026-07-26");

		expect(parsed).not.toBeNull();
		expect(parsed?.getFullYear()).toBe(2026);
		expect(parsed?.getMonth()).toBe(6);
		expect(parsed?.getDate()).toBe(26);
		expect(parsed?.getHours()).toBe(0);
	});

	it("returns null for a value that is not a date", () => {
		expect(parseLocalDate("not-a-date")).toBeNull();
	});

	it("returns null for an empty string", () => {
		expect(parseLocalDate("")).toBeNull();
	});

	it("returns null for a malformed day", () => {
		expect(parseLocalDate("2026-07-99")).toBeNull();
	});
});

describe("round-trip", () => {
	it.each([
		[2026, 0, 1],
		[2026, 2, 29],
		[2026, 6, 26],
		[2026, 11, 31],
	])("survives format then parse for %i-%i-%i", (year, month, day) => {
		const original = new Date(year, month, day, 0, 0, 0, 0);

		const roundTripped = parseLocalDate(formatLocalDate(original));

		expect(roundTripped?.getTime()).toBe(original.getTime());
	});
});
