import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { dateErrorOf, isValidFutureDate } from "./deliveryFormRules";

const DELIVERY_TERM = "Launch";
const MUST_BE_IN_THE_FUTURE = "Launch date must be in the future";

// The date field holds a day on this browser's calendar, so the string is built from the local
// calendar, never from a UTC instant.
const dayOnThisBrowsersCalendar = (daysFromToday: number): string => {
	const day = new Date();
	day.setDate(day.getDate() + daysFromToday);
	const month = `${day.getMonth() + 1}`.padStart(2, "0");
	const dayOfMonth = `${day.getDate()}`.padStart(2, "0");

	return `${day.getFullYear()}-${month}-${dayOfMonth}`;
};

// 06:30 in New York on 10 March 2027, and 22:30 there on the same day, when the UTC calendar has
// already moved on to the 11th.
const EARLY_MORNING_IN_NEW_YORK = "2027-03-10T11:30:00Z";
const LATE_EVENING_IN_NEW_YORK = "2027-03-11T03:30:00Z";

describe("a browser west of UTC", () => {
	beforeEach(() => {
		vi.useFakeTimers();
	});
	afterEach(() => {
		vi.useRealTimers();
	});

	it("runs on a clock behind UTC, or this file proves nothing", () => {
		expect(
			new Date().getTimezoneOffset(),
			"these checks only mean something west of UTC, where the offset is positive; fix the west-of-UTC project in vitest.config.ts so this file runs with a New York clock",
		).toBeGreaterThan(0);
	});

	it.each([
		["early morning", EARLY_MORNING_IN_NEW_YORK],
		[
			"late evening, when UTC is already the next day",
			LATE_EVENING_IN_NEW_YORK,
		],
	])("accepts tomorrow and refuses today and the past in the %s", (moment, now) => {
		vi.setSystemTime(new Date(now));

		const tomorrow = dayOnThisBrowsersCalendar(1);
		const today = dayOnThisBrowsersCalendar(0);
		const yesterday = dayOnThisBrowsersCalendar(-1);

		expect(
			[
				isValidFutureDate(tomorrow),
				isValidFutureDate(today),
				isValidFutureDate(yesterday),
			],
			`${moment}: tomorrow (${tomorrow}) must be accepted and today (${today}) and yesterday (${yesterday}) refused on a browser's own calendar; read the day the field holds as a local calendar day, not as UTC midnight`,
		).toEqual([true, false, false]);
		expect(
			[
				dateErrorOf(tomorrow, DELIVERY_TERM),
				dateErrorOf(today, DELIVERY_TERM),
				dateErrorOf(yesterday, DELIVERY_TERM),
			],
			`${moment}: the reader must hear no date error for tomorrow and the future-date message for today and yesterday; fix the date rule so the message follows the same local-day comparison`,
		).toEqual([undefined, MUST_BE_IN_THE_FUTURE, MUST_BE_IN_THE_FUTURE]);
	});
});
