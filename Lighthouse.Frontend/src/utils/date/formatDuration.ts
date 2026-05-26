export type DurationUnit = "minutes" | "hours" | "days" | "weeks";

const HOURS_PER_DAY = 24;
const MINUTES_PER_DAY = 24 * 60;
const DAYS_PER_WEEK = 7;

const HOURS_THRESHOLD_DAYS = 0.05;
const DAYS_THRESHOLD_DAYS = 1;
const WEEKS_THRESHOLD_DAYS = 14;

const UNIT_SUFFIX: Record<DurationUnit, string> = {
	minutes: "m",
	hours: "h",
	days: "d",
	weeks: "w",
};

export function chooseDurationUnit(maxDays: number): DurationUnit {
	if (maxDays >= WEEKS_THRESHOLD_DAYS) {
		return "weeks";
	}
	if (maxDays >= DAYS_THRESHOLD_DAYS) {
		return "days";
	}
	if (maxDays >= HOURS_THRESHOLD_DAYS) {
		return "hours";
	}
	return "minutes";
}

function toUnitMagnitude(valueDays: number, unit: DurationUnit): number {
	switch (unit) {
		case "minutes":
			return valueDays * MINUTES_PER_DAY;
		case "hours":
			return valueDays * HOURS_PER_DAY;
		case "weeks":
			return valueDays / DAYS_PER_WEEK;
		default:
			return valueDays;
	}
}

export function formatDuration(valueDays: number, unit: DurationUnit): string {
	const magnitude = toUnitMagnitude(valueDays, unit);
	const decimals = unit === "minutes" ? 0 : 1;
	const rounded = Number(magnitude.toFixed(decimals));
	return `${rounded} ${UNIT_SUFFIX[unit]}`;
}
