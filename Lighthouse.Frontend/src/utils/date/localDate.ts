/**
 * One encoding for the calendar days the dashboards exchange — the metrics URL
 * params and the metrics request query string alike.
 *
 * A `YYYY-MM-DD` string names a calendar day, not an instant, so it has to be
 * written from and read back into the viewer's local day. `toISOString()` and
 * `new Date("YYYY-MM-DD")` both go through UTC, which shifts the day for every
 * viewer at a non-zero offset.
 */

import { isValidDate } from "./isValidDate";

const DATE_ONLY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

export function formatLocalDate(date: Date): string {
	// Without this the result is the string "NaN-NaN-NaN", which travels on as a
	// URL param or a query string and looks like a day to everything it reaches.
	if (!isValidDate(date)) {
		throw new TypeError(
			`formatLocalDate cannot encode ${String(date)} as a calendar day.`,
		);
	}

	const year = date.getFullYear();
	const month = String(date.getMonth() + 1).padStart(2, "0");
	const day = String(date.getDate()).padStart(2, "0");
	return `${year}-${month}-${day}`;
}

export function parseLocalDate(dateString: string): Date | null {
	if (!DATE_ONLY_PATTERN.test(dateString)) {
		return null;
	}

	const [year, month, day] = dateString.split("-").map(Number);
	const parsed = new Date(year, month - 1, day);

	// Rejects overflow dates such as 2026-07-99, which the Date constructor would
	// silently roll forward into the next month.
	const isTheDayItClaimsToBe =
		parsed.getFullYear() === year &&
		parsed.getMonth() === month - 1 &&
		parsed.getDate() === day;

	return isTheDayItClaimsToBe ? parsed : null;
}
