import { describe, expect, it } from "vitest";
import { outlierWording } from "./outlierWording";

const wording = outlierWording({
	workItems: "Stories",
	sle: "Target",
	cycleTime: "Lead Time",
});

describe("outlierWording", () => {
	it("counts one outlier in the singular and the rest in the plural", () => {
		expect(wording.chipLabel(1)).toBe("1 outlier");
		expect(wording.chipLabel(2)).toBe("2 outliers");
		expect(wording.popoverTitle(1)).toBe("1 outlier");
		expect(wording.popoverTitle(3)).toBe("3 outliers");
		expect(wording.chipAccessibleName(1)).toBe(
			"1 outlier not drawn on the chart. Show details.",
		);
		expect(wording.chipAccessibleName(4)).toBe(
			"4 outliers not drawn on the chart. Show details.",
		);
	});

	it("names no count while the widget reloads", () => {
		expect(wording.chipLabelWhileLoading).toBe("Outliers");
		expect(wording.chipAccessibleNameWhileLoading).toBe("Outliers, loading");
	});

	it("puts the configured terms into the list title and the column heads", () => {
		expect(wording.listTitle).toBe(
			"Stories with a Lead Time more than 5 times the median",
		);
		expect(wording.columns).toEqual({
			reference: "ID",
			name: "Name",
			closed: "Closed",
			days: "Lead Time (days)",
			times: "Times the median",
		});
		expect(wording.allVisibleAreOutliers).toBe(
			"All visible Stories are outliers.",
		);
	});

	it("states the cut, what stays counted and the lines above the axis", () => {
		expect(wording.notDrawn(30, 6)).toBe(
			"Not drawn: longer than 30 days (5 times the median, 6 days).",
		);
		expect(wording.stillCounted).toBe(
			"Still counted in the percentiles, Target and View Data.",
		);
		expect(
			wording.aboveTheAxis([
				{ kind: "percentile", percentile: 95, value: 33 },
				{ kind: "sle", value: 40 },
			]),
		).toBe("Above the axis: 95% at 33 days, Target at 40 days.");
		expect(wording.moreRows(7)).toBe("and 7 more");
		expect(wording.openInAList).toBe("Open in a list");
		expect(wording.allTypesHidden).toBe("All types are hidden.");
	});
});
