import type { UsageDataFeaturesHeld } from "./UsageData";

// SCAFFOLD: replaced in DELIVER by the band a page's Feature count falls in.
export const __SCAFFOLD__ = true;

/**
 * The band a page's Feature count falls in. The count itself never leaves this function: only the band
 * travels with a tab opening.
 */
export const featuresHeldOf = (
	_count: number | undefined,
): UsageDataFeaturesHeld => {
	throw new Error("Not yet implemented: featuresHeldOf");
};
