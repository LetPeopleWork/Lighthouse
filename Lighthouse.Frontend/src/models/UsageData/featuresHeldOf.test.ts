import { describe, expect, it } from "vitest";
import { featuresHeldOf } from "./featuresHeldOf";
import { UsageDataFeaturesHeld } from "./UsageData";

describe("Story 6404 (slice 01): the band a tab opening carries for the Features its page holds", () => {
	it.skip.each([
		[0, UsageDataFeaturesHeld.UpTo20],
		[20, UsageDataFeaturesHeld.UpTo20],
		[21, UsageDataFeaturesHeld.From21To50],
		[50, UsageDataFeaturesHeld.From21To50],
		[51, UsageDataFeaturesHeld.From51To100],
		[100, UsageDataFeaturesHeld.From51To100],
		[101, UsageDataFeaturesHeld.From101To399],
		[399, UsageDataFeaturesHeld.From101To399],
		[400, UsageDataFeaturesHeld.From400],
		[920, UsageDataFeaturesHeld.From400],
	])("a page holding %i Features travels as %s", (count, band) => {
		expect(featuresHeldOf(count)).toBe(band);
	});

	it.skip("a page that has not counted its Features yet travels as Unknown", () => {
		expect(featuresHeldOf(undefined)).toBe(UsageDataFeaturesHeld.Unknown);
	});

	it.skip.each([-1, 2.5, Number.NaN])(
		"a count no page can hold (%s) travels as Unknown, never as a number",
		(count) => {
			expect(featuresHeldOf(count)).toBe(UsageDataFeaturesHeld.Unknown);
		},
	);
});
