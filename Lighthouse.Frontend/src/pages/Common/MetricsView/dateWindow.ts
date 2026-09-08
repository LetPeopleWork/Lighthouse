import { addDays, differenceInCalendarDays, startOfDay } from "date-fns";

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

export function presetWindow(days: number, today: Date): DateWindow {
	const end = startOfDay(today);
	return { start: addDays(end, -days), end };
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

export function clampWindowToToday(
	window: DateWindow,
	today: Date,
): DateWindow {
	const daysPastToday = differenceInCalendarDays(window.end, today);
	return daysPastToday > 0 ? shiftWindow(window, -daysPastToday) : window;
}

export function canStepForward(window: DateWindow, today: Date): boolean {
	return differenceInCalendarDays(window.end, today) < 0;
}

export function matchingPresetDays(
	window: DateWindow,
	presets: readonly DateWindowPreset[],
	today: Date,
): number | null {
	if (differenceInCalendarDays(window.end, today) !== 0) {
		return null;
	}

	const length = windowLengthInDays(window);
	return presets.find((preset) => preset.days === length)?.days ?? null;
}
