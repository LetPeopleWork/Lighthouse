export const getAgeInDaysFromStart = (
	// support Date or ISO string inputs since API sometimes returns strings
	startDate: Date | string,
	referenceDate: Date,
): number => {
	const start = startDate instanceof Date ? startDate : new Date(String(startDate));
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
	const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

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

export default getAgeInDaysFromStart;
