import { expect } from "@playwright/test";

/**
 * Mirrors the frontend's `src/utils/date/localDate.ts`: a `YYYY-MM-DD` string
 * names a calendar day, not an instant. `toISOString()` encodes it through UTC,
 * so at any positive offset the fixture writes yesterday's day while the backend
 * — pinned to the same zone as this runner — reads today's (Bug #5566 / #5567).
 */
export function formatLocalDate(date: Date): string {
	const year = date.getFullYear();
	const month = String(date.getMonth() + 1).padStart(2, "0");
	const day = String(date.getDate()).padStart(2, "0");
	return `${year}-${month}-${day}`;
}

export function expectDateToBeRecent(received: Date, slack = 3000): void {
	const now = new Date();

	expect(
		Math.abs(received.getUTCMilliseconds() - now.getUTCMilliseconds()),
	).toBeLessThanOrEqual(slack);
}

export function getLastUpdatedDateFromText(lastUpdatedText: string): Date {
	const dateMatch = /Last Updated on (.*)/.exec(lastUpdatedText);
	if (!dateMatch) {
		return new Date();
	}

	return new Date(dateMatch[1]);
}
