import { describe, expect, it } from "vitest";
import { OVER_TIME_EMPTY_COPY } from "./overTimeEmptyState";

describe("OVER_TIME_EMPTY_COPY", () => {
	// Pinned as a literal, not compared to itself, so a blanked or reworded constant
	// fails here rather than passing every test that reads it back. True whether or not
	// this instance fills in past days, because the widgets never learn which it does.
	// Skipped until the sentence changes to this one; un-skip with that change.
	it.skip("states the one sentence that is true of every empty over-time chart", () => {
		expect(OVER_TIME_EMPTY_COPY).toBe(
			"Nothing to show for the selected range. Days appear here as Lighthouse records them.",
		);
	});

	it("no longer claims the chart only builds forward from today", () => {
		expect(OVER_TIME_EMPTY_COPY).not.toContain("builds forward from today");
	});

	// An instance that leaves filling in past days switched off never fills a day in on a
	// later visit, and that is every instance until an administrator opts in.
	// Skipped until the sentence changes; un-skip with that change.
	it.skip("does not promise that days fill in on a later visit", () => {
		expect(OVER_TIME_EMPTY_COPY).not.toContain("fill in on a later visit");
	});
});
