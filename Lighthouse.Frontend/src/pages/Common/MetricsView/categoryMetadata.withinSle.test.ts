import { describe, expect, it } from "vitest";
import { getTrendPolicy } from "./categoryMetadata";

describe("the Within SLE widget's header trend", () => {
	it("compares the share with the previous period", () => {
		expect(getTrendPolicy("withinSle")).toBe("previous-period");
	});
});
