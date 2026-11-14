import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	dateErrorOf,
	getFirstBlockingError,
	isValidFutureDate,
} from "./deliveryFormRules";
import {
	type DeliverySelectionTerms,
	deliverySelectionTabsFor,
	MANUAL_SELECTION_TAB_KEY,
} from "./deliverySelectionTabs";

const DELIVERY_TERM = "Launch";
const MUST_BE_IN_THE_FUTURE = "Launch date must be in the future";

const terms: DeliverySelectionTerms = {
	featureTerm: "Deliverable",
	deliveryTerm: DELIVERY_TERM,
	deliveriesTerm: "Launches",
};

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

const MOMENTS_IN_A_NEW_YORK_DAY = [
	["early morning", EARLY_MORNING_IN_NEW_YORK],
	["late evening, when UTC is already the next day", LATE_EVENING_IN_NEW_YORK],
];

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

	it.each(MOMENTS_IN_A_NEW_YORK_DAY)(
		"accepts tomorrow and refuses today and the past in the %s",
		(moment, now) => {
			vi.setSystemTime(new Date(now));

			const tomorrow = dayOnThisBrowsersCalendar(1);
			const today = dayOnThisBrowsersCalendar(0);
			const yesterday = dayOnThisBrowsersCalendar(-1);
			const aMonthAgo = dayOnThisBrowsersCalendar(-30);

			expect(
				[
					isValidFutureDate(tomorrow),
					isValidFutureDate(today),
					isValidFutureDate(yesterday),
					isValidFutureDate(aMonthAgo),
				],
				`${moment}: tomorrow (${tomorrow}) must be accepted and today (${today}), yesterday (${yesterday}) and a month ago (${aMonthAgo}) refused on a browser's own calendar; read the day the field holds as a local calendar day, not as UTC midnight`,
			).toEqual([true, false, false, false]);
			expect(
				[
					dateErrorOf(tomorrow, DELIVERY_TERM),
					dateErrorOf(today, DELIVERY_TERM),
					dateErrorOf(yesterday, DELIVERY_TERM),
					dateErrorOf(aMonthAgo, DELIVERY_TERM),
				],
				`${moment}: the reader must hear no date error for tomorrow and the future-date message for today, yesterday and a month ago; fix the date rule so the message follows the same local-day comparison`,
			).toEqual([
				undefined,
				MUST_BE_IN_THE_FUTURE,
				MUST_BE_IN_THE_FUTURE,
				MUST_BE_IN_THE_FUTURE,
			]);
		},
	);

	it.each(MOMENTS_IN_A_NEW_YORK_DAY)(
		"lets a named launch dated tomorrow be saved and blocks today in the %s",
		(moment, now) => {
			vi.setSystemTime(new Date(now));
			const manualTab = deliverySelectionTabsFor([], terms).find(
				(tab) => tab.key === MANUAL_SELECTION_TAB_KEY,
			);
			if (!manualTab) {
				throw new Error("the manual tab must be offered");
			}
			const savedOn = (date: string) =>
				getFirstBlockingError({
					name: "Autumn launch",
					date,
					tab: manualTab,
					state: {
						selectedFeatureIds: [1],
						rules: [],
						mode: "and",
						sourceReference: null,
						publishForecastToSource: false,
						rulesValidated: false,
						matchedFeaturesLength: 0,
					},
					terms,
					deliveryTerm: DELIVERY_TERM,
				});

			expect(
				[
					savedOn(dayOnThisBrowsersCalendar(1)),
					savedOn(dayOnThisBrowsersCalendar(0)),
				],
				`${moment}: with a name and a feature picked, Save must stay open for tomorrow and stay blocked with the future-date message for today; fix the save gate so it follows the local-day comparison`,
			).toEqual([null, MUST_BE_IN_THE_FUTURE]);
		},
	);

	it.each([["2027-13-01"], ["2027-00-10"]])(
		"refuses %s, a day that is not on any calendar",
		(date) => {
			vi.setSystemTime(new Date(EARLY_MORNING_IN_NEW_YORK));

			expect(
				[isValidFutureDate(date), dateErrorOf(date, DELIVERY_TERM)],
				`${date} has no such month and must be refused with the future-date message, not rolled over into another year; fix the date rule to reject a day the calendar does not have`,
			).toEqual([false, MUST_BE_IN_THE_FUTURE]);
		},
	);
});
