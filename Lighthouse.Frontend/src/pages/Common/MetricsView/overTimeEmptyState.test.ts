import { describe, expect, it } from "vitest";
import { OVER_TIME_EMPTY_COPY } from "./overTimeEmptyState";

describe("OVER_TIME_EMPTY_COPY", () => {
	// Pinned as a literal, not compared to itself, so a blanked or reworded constant
	// fails here rather than passing every test that reads it back.
	it("states the one sentence that is true of every empty over-time chart", () => {
		expect(OVER_TIME_EMPTY_COPY).toBe(
			"Nothing to show for the selected range. Days the stored history covers can fill in on a later visit; days it does not cover stay empty.",
		);
	});

	it("no longer claims the chart only builds forward from today", () => {
		expect(OVER_TIME_EMPTY_COPY).not.toContain("builds forward from today");
	});
});
