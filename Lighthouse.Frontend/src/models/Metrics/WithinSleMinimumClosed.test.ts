import { describe, expect, it } from "vitest";
import { minimumClosedOf } from "./WithinSleMinimumClosed";

describe("minimumClosedOf", () => {
	it("takes the minimum the server sent", () => {
		expect(minimumClosedOf({ minimumClosed: 7 })).toBe(7);
	});

	it("falls back to five closed Work Items when the answer carries no minimum", () => {
		expect(minimumClosedOf({})).toBe(5);
	});
});
