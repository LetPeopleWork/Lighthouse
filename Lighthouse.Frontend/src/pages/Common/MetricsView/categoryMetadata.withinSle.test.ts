import { describe, expect, it } from "vitest";
import { getTrendPolicy } from "./categoryMetadata";

describe("the Within SLE widget's header trend", () => {
	it("compares against the preceding period of the same length", () => {
		expect(getTrendPolicy("withinSle")).toBe("previous-period");
	});
});
