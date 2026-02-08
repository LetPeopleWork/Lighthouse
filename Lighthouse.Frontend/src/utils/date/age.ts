import type { IWorkItem } from "../../models/WorkItem";

export const getAgeInDaysFromStart = (
	// support Date or ISO string inputs since API sometimes returns strings
	startDate: Date | string,
	referenceDate: Date,
): number => {
	const start =
		startDate instanceof Date ? startDate : new Date(String(startDate));
	// Work out age in days using UTC date-only arithmetic to avoid timezone issues
	const startDateOnly = Date.UTC(
		start.getUTCFullYear(),
		start.getUTCMonth(),
		start.getUTCDate(),
	);

	const referenceDateOnly = Date.UTC(
		referenceDate.getUTCFullYear(),
		referenceDate.getUTCMonth(),
		referenceDate.getUTCDate(),
	);

	const diffMs = referenceDateOnly - startDateOnly;
	const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24)) + 1;

	// Ensure at least 1 day is returned (an item started today counts as 1)
	return Math.max(1, diffDays);
};

export const calculateAgeFromStartForItem = (
	startDate: Date,
	referenceDate?: Date,
): number => {
	const ref = referenceDate ?? new Date();
	return getAgeInDaysFromStart(startDate, ref);
};

/**
 * Calculate the historical age of a work item on a specific date
 * Age = days between startedDate and the historical date
 * (An item started today has age 1, not 0)
 *
 * This matches the backend calculation: ((end.Date - start.Date).TotalDays) + 1
 * We use UTC date-only comparison to avoid timezone issues.
 */
export const calculateHistoricalAge = (
	item: IWorkItem,
	historicalDate: Date,
): number => {
	return getAgeInDaysFromStart(item.startedDate, historicalDate);
};

export default getAgeInDaysFromStart;
