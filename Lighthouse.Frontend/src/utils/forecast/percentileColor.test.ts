import { describe, expect, it } from "vitest";
import {
	certainColor,
	confidentColor,
	defaultColor,
	realisticColor,
	riskyColor,
} from "../theme/colors";
import { getPercentileColor } from "./percentileColor";

describe("getPercentileColor", () => {
	it.each([
		[50, riskyColor],
		[70, realisticColor],
		[85, confidentColor],
		[95, certainColor],
	])("colours the %i%% level in the forecast palette", (percentile, colour) => {
		expect(getPercentileColor(percentile)).toBe(colour);
	});

	it.each([0, 49, 51, 80, 90, 99, 100])(
		"falls back to the default colour for the %i%% level, which is not one of the four",
		(percentile) => {
			expect(getPercentileColor(percentile)).toBe(defaultColor);
		},
	);
});
