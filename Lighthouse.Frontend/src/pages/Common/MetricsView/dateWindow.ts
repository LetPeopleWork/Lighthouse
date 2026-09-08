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

export function getPresetsForOwner(
	ownerType: MetricsOwnerType,
): readonly DateWindowPreset[] {
	throw new Error(`getPresetsForOwner(${ownerType}) is not implemented`);
}

export function getStepDaysForOwner(ownerType: MetricsOwnerType): number {
	throw new Error(`getStepDaysForOwner(${ownerType}) is not implemented`);
}

export function presetWindow(days: number, today: Date): DateWindow {
	throw new Error(`presetWindow(${days}, ${today}) is not implemented`);
}

export function windowLengthInDays(window: DateWindow): number {
	throw new Error(`windowLengthInDays(${window.start}) is not implemented`);
}

export function shiftWindow(window: DateWindow, stepDays: number): DateWindow {
	throw new Error(
		`shiftWindow(${window.start}, ${stepDays}) is not implemented`,
	);
}

export function clampWindowToToday(
	window: DateWindow,
	today: Date,
): DateWindow {
	throw new Error(
		`clampWindowToToday(${window.end}, ${today}) is not implemented`,
	);
}

export function canStepForward(window: DateWindow, today: Date): boolean {
	throw new Error(`canStepForward(${window.end}, ${today}) is not implemented`);
}

export function matchingPresetDays(
	window: DateWindow,
	presets: readonly DateWindowPreset[],
	today: Date,
): number | null {
	throw new Error(
		`matchingPresetDays(${window.end}, ${presets.length}, ${today}) is not implemented`,
	);
}
