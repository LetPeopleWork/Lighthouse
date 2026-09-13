import { describe, expect, it } from "vitest";
import { takeWhatWasNoticed } from "./usageDataBuffer";

/**
 * This file exists for the one claim below, which cannot be made anywhere else.
 *
 * Every other test of this buffer reaches it through the detector, and by the time those run
 * something has already been handed in and the buffer emptied. The state a freshly opened tab
 * starts in is only visible to whoever looks first - so it needs a file where it is the first
 * thing that happens.
 */
describe("usageDataBuffer", () => {
	it("remembers no pages until somebody opens one", () => {
		expect(takeWhatWasNoticed()).toEqual([]);
	});
});
