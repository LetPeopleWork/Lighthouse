import { describe, expect, it } from "vitest";
import { timeAFrameIsGivenMs } from "./PrintFrameHost";

describe("the time a print frame is given", () => {
	it.each([
		[1, 30_000],
		[2, 32_000],
		[50, 128_000],
	])(
		"for %i reports is %i ms: 30 s, and 2 s for each report beyond the first",
		(reports, ms) => {
			expect(timeAFrameIsGivenMs(reports)).toBe(ms);
		},
	);
});
