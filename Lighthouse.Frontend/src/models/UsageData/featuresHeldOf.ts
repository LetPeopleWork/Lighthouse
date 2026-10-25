import { UsageDataFeaturesHeld } from "./UsageData";

const bandsByUpperBound: ReadonlyArray<
	readonly [upperBound: number, band: UsageDataFeaturesHeld]
> = [
	[20, UsageDataFeaturesHeld.UpTo20],
	[50, UsageDataFeaturesHeld.From21To50],
	[100, UsageDataFeaturesHeld.From51To100],
	[399, UsageDataFeaturesHeld.From101To399],
];

/**
 * The band a page's Feature count falls in. The count itself never leaves this function: only the band
 * travels with a tab opening.
 */
export const featuresHeldOf = (
	count: number | undefined,
): UsageDataFeaturesHeld => {
	if (count === undefined || !Number.isInteger(count) || count < 0) {
		return UsageDataFeaturesHeld.Unknown;
	}

	const match = bandsByUpperBound.find(([upperBound]) => count <= upperBound);
	return match ? match[1] : UsageDataFeaturesHeld.From400;
};
