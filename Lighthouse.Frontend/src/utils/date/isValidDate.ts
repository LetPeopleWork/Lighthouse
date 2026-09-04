/**
 * The one check for "is this really a usable date".
 *
 * A date that could not be parsed is still a `Date` object, and therefore
 * truthy, so the obvious `if (!date) return;` guard waves it through and the
 * failure surfaces much later, somewhere that has no idea a date picker was
 * involved. Every caller that takes a date from a picker, a URL or an API has
 * to ask this question, and asking it by name keeps them all asking the same one.
 */
export function isValidDate(value: unknown): value is Date {
	return value instanceof Date && !Number.isNaN(value.getTime());
}
