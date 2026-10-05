import { describe, expect, it } from "vitest";
import { bandErrors, hasBandErrors } from "./refinementBand";

const OUT_OF_RANGE = "Between 50% and 95%.";

describe("What is wrong with the likelihoods the band is read at", () => {
	it.each([
		[50, 85, null, null],
		[50, 95, null, null],
		[94, 95, null, null],
		[50, 51, null, null],
		[90, 85, "The low end (90%) must be below the high end (85%).", null],
		[85, 85, "The low end (85%) must be below the high end (85%).", null],
		[95, 50, "The low end (95%) must be below the high end (50%).", null],
		[49, 85, OUT_OF_RANGE, null],
		[50, 96, null, OUT_OF_RANGE],
		[96, 49, OUT_OF_RANGE, OUT_OF_RANGE],
		[Number.NaN, 85, OUT_OF_RANGE, null],
		[50, Number.NaN, null, OUT_OF_RANGE],
		[50.5, 85, OUT_OF_RANGE, null],
	])(
		"low %s and high %s: low says %s, high says %s",
		(lowPercentile, highPercentile, low, high) => {
			const band = { lowPercentile, highPercentile };

			expect(bandErrors(band)).toEqual({
				lowPercentile: low,
				highPercentile: high,
			});
			expect(hasBandErrors(band)).toBe(low !== null || high !== null);
		},
	);
});
