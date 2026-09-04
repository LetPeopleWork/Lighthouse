import { describe, expect, it } from "vitest";
import { isValidDate } from "./isValidDate";

describe("isValidDate", () => {
	it("accepts a real date", () => {
		expect(isValidDate(new Date(2026, 6, 26))).toBe(true);
	});

	it("rejects an Invalid Date", () => {
		// The whole reason this predicate exists: an unparseable date is still a
		// Date object, so it is truthy, so a plain `if (!date)` guard lets it
		// through and the failure lands somewhere further downstream instead.
		const invalid = new Date("nonsense");

		expect(invalid).toBeTruthy();
		expect(isValidDate(invalid)).toBe(false);
	});

	it.each([
		["null", null],
		["undefined", undefined],
		["a date-shaped string", "2026-07-26"],
		["a timestamp number", Date.now()],
		["a date-like object", { getTime: () => 0 }],
	])("rejects %s", (_label, value) => {
		expect(isValidDate(value)).toBe(false);
	});

	it("narrows an unknown value to Date", () => {
		const value: unknown = new Date(2026, 6, 26);

		if (!isValidDate(value)) {
			throw new Error("expected the predicate to accept a real date");
		}

		expect(value.getFullYear()).toBe(2026);
	});
});
