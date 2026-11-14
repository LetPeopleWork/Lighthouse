import { describe, expect, it } from "vitest";
import { outlierWording } from "./outlierWording";

const wording = outlierWording({
	workItems: "Stories",
	sle: "Target",
	cycleTime: "Lead Time",
});

const percentileAbove = wording.aboveTheAxis([
	{ kind: "percentile", percentile: 95, value: 33 },
]);
const sleAbove = wording.aboveTheAxis([{ kind: "sle", value: 40 }]);
const bothAbove = wording.aboveTheAxis([
	{ kind: "percentile", percentile: 95, value: 33 },
	{ kind: "sle", value: 40 },
]);

describe("outlierWording", () => {
	it("counts one outlier in the singular and the rest in the plural", () => {
		expect(wording.chipLabel(1)).toBe("1 outlier");
		expect(wording.chipLabel(2)).toBe("2 outliers");
	});

	it.each([
		[
			"one outlier, no line above the axis",
			1,
			null,
			"1 outlier not drawn on the chart. Show the list.",
		],
		[
			"three outliers, the 95% line above the axis",
			3,
			percentileAbove,
			"3 outliers not drawn on the chart. Above the axis: 95% at 33 days. Show the list.",
		],
		[
			"one outlier, the SLE above the axis",
			1,
			sleAbove,
			"1 outlier not drawn on the chart. Above the axis: Target at 40 days. Show the list.",
		],
		[
			"two outliers, the percentile and the SLE above the axis",
			2,
			bothAbove,
			"2 outliers not drawn on the chart. Above the axis: 95% at 33 days, Target at 40 days. Show the list.",
		],
	])(
		"builds the accessible name for %s",
		(_case, count, aboveTheAxis, expected) => {
			expect(wording.chipAccessibleName(count, aboveTheAxis)).toBe(expected);
		},
	);

	it("names no count while the widget reloads", () => {
		expect(wording.chipLabelWhileLoading).toBe("Outliers");
		expect(wording.chipAccessibleNameWhileLoading).toBe("Outliers, loading");
	});

	it("puts the configured terms into the list title and the empty-plot copy", () => {
		expect(wording.listTitle).toBe(
			"Stories with a Lead Time more than 5 times the median",
		);
		expect(wording.allVisibleAreOutliers).toBe(
			"All visible Stories are outliers.",
		);
		expect(wording.allTypesHidden).toBe("All types are hidden.");
	});

	it("names the lines above the axis", () => {
		expect(bothAbove).toBe(
			"Above the axis: 95% at 33 days, Target at 40 days.",
		);
	});
});
