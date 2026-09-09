import { addDays, differenceInCalendarDays } from "date-fns";

export type MetricsOwnerType = "team" | "portfolio";

export interface DateWindow {
	readonly start: Date;
	readonly end: Date;
}

export interface DateWindowPreset {
	readonly label: string;
	readonly days: number;
}

// Every function here works in calendar days in the viewer's own zone. The window is written to the
// URL and sent to the backend as local Y/M/D, so routing any of this through UTC shifts a reader
// west of Greenwich by a day — and a UTC test runner never sees it (Bug #5566).

const presetsOf = (lengths: readonly number[]): readonly DateWindowPreset[] =>
	lengths.map((days) => ({ label: `Last ${days} days`, days }));

const TEAM_PRESETS = presetsOf([7, 14, 30, 90]);
const PORTFOLIO_PRESETS = presetsOf([30, 90, 180]);

export function getPresetsForOwner(
	ownerType: MetricsOwnerType,
): readonly DateWindowPreset[] {
	return ownerType === "team" ? TEAM_PRESETS : PORTFOLIO_PRESETS;
}

export function getStepDaysForOwner(ownerType: MetricsOwnerType): number {
	return ownerType === "team" ? 7 : 28;
}

// The window ends at the instant the caller passed, not at midnight of that day. Widgets compare
// the end as a point in time, so rounding it down to midnight puts every reading taken so far
// today after the window and quietly drops them from the trend.
export function presetWindow(days: number, today: Date): DateWindow {
	return { start: addDays(today, -days), end: today };
}

export function windowLengthInDays(window: DateWindow): number {
	return differenceInCalendarDays(window.end, window.start);
}

export function shiftWindow(window: DateWindow, stepDays: number): DateWindow {
	return {
		start: addDays(window.start, stepDays),
		end: addDays(window.end, stepDays),
	};
}

const daysEndIsPastToday = (window: DateWindow, today: Date): number =>
	differenceInCalendarDays(window.end, today);

export const endsToday = (window: DateWindow, today: Date): boolean =>
	daysEndIsPastToday(window, today) === 0;

// Shifting a window carries its old time of day along, so a window walked forward onto today would
// end at whatever hour it was built at rather than at this moment — and everything that reads the
// end as a point in time would then drop the hours since. Landing on today re-anchors it.
export function endWindowAt(window: DateWindow, today: Date): DateWindow {
	return { start: window.start, end: today };
}

export function clampWindowToToday(
	window: DateWindow,
	today: Date,
): DateWindow {
	const overshoot = daysEndIsPastToday(window, today);
	const clamped = overshoot > 0 ? shiftWindow(window, -overshoot) : window;
	return endsToday(clamped, today) ? endWindowAt(clamped, today) : clamped;
}

export function canStepForward(window: DateWindow, today: Date): boolean {
	return daysEndIsPastToday(window, today) < 0;
}

export function matchingPresetDays(
	window: DateWindow,
	presets: readonly DateWindowPreset[],
	today: Date,
): number | null {
	if (!endsToday(window, today)) {
		return null;
	}

	const length = windowLengthInDays(window);
	return presets.find((preset) => preset.days === length)?.days ?? null;
}
