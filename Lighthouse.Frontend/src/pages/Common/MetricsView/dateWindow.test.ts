import { describe, expect, it } from "vitest";
import {
	canStepForward,
	clampWindowToToday,
	type DateWindow,
	endsToday,
	getPresetsForOwner,
	getStepDaysForOwner,
	matchingPresetDays,
	presetWindow,
	shiftWindow,
	windowLengthInDays,
} from "./dateWindow";

// Local-midnight construction throughout. Writing a date as "2026-09-08" instead would parse as UTC
// midnight, which is the previous day for any reader west of Greenwich — the exact confusion these
// functions exist to keep out of the window.
const localDay = (year: number, month1Based: number, day: number): Date =>
	new Date(year, month1Based - 1, day);

const asLocalParts = (date: Date): [number, number, number] => [
	date.getFullYear(),
	date.getMonth() + 1,
	date.getDate(),
];

const TODAY = localDay(2026, 9, 8);

describe("dateWindow — owner-aware configuration", () => {
	it("offers a team four named windows, shortest first", () => {
		expect(getPresetsForOwner("team").map((p) => p.days)).toEqual([
			7, 14, 30, 90,
		]);
	});

	it("labels a team's windows in plain days", () => {
		expect(getPresetsForOwner("team").map((p) => p.label)).toEqual([
			"Last 7 days",
			"Last 14 days",
			"Last 30 days",
			"Last 90 days",
		]);
	});

	it("offers a portfolio three named windows, starting where a team's stop", () => {
		expect(getPresetsForOwner("portfolio").map((p) => p.days)).toEqual([
			30, 90, 180,
		]);
	});

	it("labels a portfolio's windows in plain days", () => {
		expect(getPresetsForOwner("portfolio").map((p) => p.label)).toEqual([
			"Last 30 days",
			"Last 90 days",
			"Last 180 days",
		]);
	});

	it("steps a team's window by one week", () => {
		expect(getStepDaysForOwner("team")).toBe(7);
	});

	it("steps a portfolio's window by four weeks", () => {
		expect(getStepDaysForOwner("portfolio")).toBe(28);
	});
});

describe("dateWindow — a named window ends today", () => {
	it("ends a preset window on today and starts it the named number of days earlier", () => {
		const window = presetWindow(30, TODAY);

		expect(asLocalParts(window.end)).toEqual([2026, 9, 8]);
		expect(asLocalParts(window.start)).toEqual([2026, 8, 9]);
	});

	it("names the same calendar days a reader would count on a wall calendar", () => {
		// A window built through UTC lands a day early for anyone west of Greenwich and a day late
		// for anyone far enough east. Asserting local Y/M/D rather than an ISO string is what makes
		// this test able to fail when the suite is run outside UTC.
		const window = presetWindow(7, localDay(2026, 3, 2));

		expect(asLocalParts(window.start)).toEqual([2026, 2, 23]);
		expect(asLocalParts(window.end)).toEqual([2026, 3, 2]);
	});

	it("counts a preset window's length as the number the preset is named after", () => {
		expect(windowLengthInDays(presetWindow(90, TODAY))).toBe(90);
	});

	it("counts length in calendar days, so a clock change inside the window does not shorten it", () => {
		const window = presetWindow(30, localDay(2026, 4, 1));

		expect(windowLengthInDays(window)).toBe(30);
	});

	it("crosses a year boundary without losing a day", () => {
		const window = presetWindow(30, localDay(2026, 1, 15));

		expect(asLocalParts(window.start)).toEqual([2025, 12, 16]);
		expect(windowLengthInDays(window)).toBe(30);
	});
});

describe("dateWindow — walking the window through time", () => {
	// Built per test, never once at describe scope: a skipped describe still evaluates its body,
	// so calling into the module here would throw during collection and report the whole file as
	// broken rather than as pending.
	const thirtyDaysToToday = (): DateWindow => presetWindow(30, TODAY);

	it("moves both ends of a team's window one week earlier", () => {
		const moved = shiftWindow(thirtyDaysToToday(), -7);

		expect(asLocalParts(moved.end)).toEqual([2026, 9, 1]);
		expect(asLocalParts(moved.start)).toEqual([2026, 8, 2]);
	});

	it("moves both ends of a portfolio's window four weeks earlier", () => {
		const moved = shiftWindow(thirtyDaysToToday(), -28);

		expect(asLocalParts(moved.end)).toEqual([2026, 8, 11]);
		expect(asLocalParts(moved.start)).toEqual([2026, 7, 12]);
	});

	it("keeps the window exactly as long as it was", () => {
		const moved = shiftWindow(thirtyDaysToToday(), -7);

		expect(windowLengthInDays(moved)).toBe(
			windowLengthInDays(thirtyDaysToToday()),
		);
	});

	it("keeps the window as long as it was however far back it is walked", () => {
		let walked = thirtyDaysToToday();
		for (let click = 0; click < 12; click += 1) {
			walked = shiftWindow(walked, -7);
		}

		expect(windowLengthInDays(walked)).toBe(30);
	});

	it("walks forward the same distance it walks back", () => {
		const there = shiftWindow(thirtyDaysToToday(), -7);
		const back = shiftWindow(there, 7);

		expect(asLocalParts(back.start)).toEqual(
			asLocalParts(thirtyDaysToToday().start),
		);
		expect(asLocalParts(back.end)).toEqual(
			asLocalParts(thirtyDaysToToday().end),
		);
	});

	it("lands four single-week steps exactly where one four-week step lands", () => {
		// The arithmetic half of "a burst of clicks is one window". The other half — that the burst
		// costs one round of requests — lives in useDateRange.test.ts.
		let clicked = thirtyDaysToToday();
		for (let click = 0; click < 4; click += 1) {
			clicked = shiftWindow(clicked, -7);
		}

		const atOnce = shiftWindow(thirtyDaysToToday(), -28);

		expect(asLocalParts(clicked.start)).toEqual(asLocalParts(atOnce.start));
		expect(asLocalParts(clicked.end)).toEqual(asLocalParts(atOnce.end));
	});

	it("walks back across a year boundary without losing a day", () => {
		const january = presetWindow(30, localDay(2026, 1, 10));
		const walked = shiftWindow(january, -28);

		expect(asLocalParts(walked.end)).toEqual([2025, 12, 13]);
		expect(windowLengthInDays(walked)).toBe(30);
	});
});

describe("dateWindow — the window never ends in the future", () => {
	it("lets the window be walked forward while it still ends in the past", () => {
		const lastMonth = shiftWindow(presetWindow(30, TODAY), -28);

		expect(canStepForward(lastMonth, TODAY)).toBe(true);
	});

	it("refuses to walk forward once the window already ends today", () => {
		expect(canStepForward(presetWindow(30, TODAY), TODAY)).toBe(false);
	});

	it("refuses to walk forward from a window that already ends after today", () => {
		// Reachable today: the end picker has no upper bound, so a hand-typed future end is legal
		// and the stepper must not offer to push it further out.
		const future: DateWindow = {
			start: localDay(2026, 9, 1),
			end: localDay(2026, 9, 20),
		};

		expect(canStepForward(future, TODAY)).toBe(false);
	});

	it("lands the end exactly on today when the last step forward would overshoot", () => {
		const threeDaysBack: DateWindow = {
			start: localDay(2026, 8, 6),
			end: localDay(2026, 9, 5),
		};
		const overshot = shiftWindow(threeDaysBack, 7);

		expect(asLocalParts(clampWindowToToday(overshot, TODAY).end)).toEqual([
			2026, 9, 8,
		]);
	});

	it("moves the start with the end when clamping, so the window keeps its length", () => {
		const threeDaysBack: DateWindow = {
			start: localDay(2026, 8, 6),
			end: localDay(2026, 9, 5),
		};
		const clamped = clampWindowToToday(shiftWindow(threeDaysBack, 7), TODAY);

		expect(windowLengthInDays(clamped)).toBe(windowLengthInDays(threeDaysBack));
		expect(asLocalParts(clamped.start)).toEqual([2026, 8, 9]);
	});

	it("leaves a window that already ends today alone", () => {
		const window = presetWindow(30, TODAY);
		const clamped = clampWindowToToday(window, TODAY);

		expect(asLocalParts(clamped.start)).toEqual(asLocalParts(window.start));
		expect(asLocalParts(clamped.end)).toEqual(asLocalParts(window.end));
	});

	it("leaves a window that ends well before today alone", () => {
		const window = shiftWindow(presetWindow(30, TODAY), -28);
		const clamped = clampWindowToToday(window, TODAY);

		expect(asLocalParts(clamped.end)).toEqual(asLocalParts(window.end));
	});
});

// Every test above builds its dates at local midnight and reads them back as Y/M/D, so none of them
// can see what time of day a window ends at. That blindness let a window end at the start of today
// instead of at this moment, which silently drops every reading taken so far today from the widgets
// that compare a timestamp against the end. These assert the instant.
describe("dateWindow — a window that ends today ends now", () => {
	const atNine = new Date(2026, 8, 8, 9, 0, 0);
	const atEleven = new Date(2026, 8, 8, 23, 0, 0);

	it("ends a walked-forward window at this moment, not at the hour it was built", () => {
		const builtAtNine = presetWindow(7, atNine);
		const walkedBack = shiftWindow(builtAtNine, -7);

		const walkedForward = clampWindowToToday(
			shiftWindow(walkedBack, 7),
			atEleven,
		);

		expect(walkedForward.end.getTime()).toBe(atEleven.getTime());
	});

	it("re-anchors a window that was restored ending at the start of today", () => {
		const restoredFromTheAddress: DateWindow = {
			start: localDay(2026, 9, 1),
			end: localDay(2026, 9, 8),
		};

		const clamped = clampWindowToToday(restoredFromTheAddress, atEleven);

		expect(clamped.end.getTime()).toBe(atEleven.getTime());
	});

	it("keeps the window's length while re-anchoring its end", () => {
		const restoredFromTheAddress: DateWindow = {
			start: localDay(2026, 9, 1),
			end: localDay(2026, 9, 8),
		};

		const clamped = clampWindowToToday(restoredFromTheAddress, atEleven);

		expect(windowLengthInDays(clamped)).toBe(7);
		expect(asLocalParts(clamped.start)).toEqual([2026, 9, 1]);
	});

	it("never lets a step taken after midnight leave the end in the future", () => {
		// The window was built yesterday afternoon and the tab was left open. Stepping forward now
		// clamps onto the new today, and carrying yesterday's hour along would put the end ahead of
		// the clock.
		const yesterdayAfternoon = new Date(2026, 8, 8, 16, 0, 0);
		const justAfterMidnight = new Date(2026, 8, 9, 0, 5, 0);
		const built = presetWindow(7, yesterdayAfternoon);

		const stepped = clampWindowToToday(
			shiftWindow(built, 7),
			justAfterMidnight,
		);

		expect(stepped.end.getTime()).toBeLessThanOrEqual(
			justAfterMidnight.getTime(),
		);
		expect(stepped.end.getTime()).toBe(justAfterMidnight.getTime());
	});

	it("leaves a window that ends before today at the time of day it carried", () => {
		const built = presetWindow(7, atNine);
		const walkedBack = shiftWindow(built, -7);

		const clamped = clampWindowToToday(walkedBack, atEleven);

		expect(clamped.end.getHours()).toBe(9);
	});

	it("says a window ends today whatever time of day either side carries", () => {
		const endingAtMidnight: DateWindow = {
			start: localDay(2026, 9, 1),
			end: localDay(2026, 9, 8),
		};

		expect(endsToday(endingAtMidnight, atEleven)).toBe(true);
		expect(endsToday(shiftWindow(endingAtMidnight, -1), atEleven)).toBe(false);
		expect(endsToday(shiftWindow(endingAtMidnight, 1), atEleven)).toBe(false);
	});
});

describe("dateWindow — which named window is showing", () => {
	const teamPresets = () => getPresetsForOwner("team");

	it("recognises a window that is exactly a named one", () => {
		expect(
			matchingPresetDays(presetWindow(30, TODAY), teamPresets(), TODAY),
		).toBe(30);
	});

	it("recognises no named window behind a hand-picked length", () => {
		const handPicked: DateWindow = {
			start: localDay(2026, 8, 20),
			end: TODAY,
		};

		expect(matchingPresetDays(handPicked, teamPresets(), TODAY)).toBeNull();
	});

	it("recognises no named window once the window has been walked into the past", () => {
		// A stepped-back window is 30 days long but is not "the last 30 days", so no chip may
		// claim it — otherwise the chip row would say the user is looking at something they are not.
		const stepped = shiftWindow(presetWindow(30, TODAY), -7);

		expect(matchingPresetDays(stepped, teamPresets(), TODAY)).toBeNull();
	});

	it("does not offer a team's shortest window to a portfolio", () => {
		const portfolioPresets = getPresetsForOwner("portfolio");

		expect(
			matchingPresetDays(presetWindow(7, TODAY), portfolioPresets, TODAY),
		).toBeNull();
	});
});
