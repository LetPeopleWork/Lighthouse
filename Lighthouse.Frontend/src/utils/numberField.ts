/** A cleared number field holds NaN, which the input shows as empty rather than as "NaN". */
export const shownNumber = (value: number): number | "" =>
	Number.isNaN(value) ? "" : value;

// The server binds these settings as 32-bit ints and answers anything larger with a bare 400.
const LARGEST_SERVER_INT = 2147483647;

/** A whole number the server can take. */
export const isAWholeNumber = (value: number): boolean =>
	Number.isInteger(value) && value <= LARGEST_SERVER_INT;

/** A whole number the server can take, and at least one. */
export const isAtLeastOne = (value: number): boolean =>
	isAWholeNumber(value) && value >= 1;
