import getAgeInDaysFromStart from "./age";

describe("getAgeInDaysFromStart", () => {
	it("returns 1 day for an item started on the same date (UTC)", () => {
		const start = new Date("2025-11-16T00:00:00.000Z");
		const reference = new Date("2025-11-16T12:00:00.000Z");

		const age = getAgeInDaysFromStart(start, reference);
		expect(age).toBe(1);
	});

	it("returns diffDays for items started earlier (no +1)", () => {
		const start = new Date("2025-11-13T00:00:00.000Z");
		const reference = new Date("2025-11-16T00:00:00.000Z");

		// 3 days between start and reference, age should be 3 (not 4)
		const age = getAgeInDaysFromStart(start, reference);
		expect(age).toBe(3);
	});

	it("handles timezone offsets and calculates using UTC date-only values", () => {
		// Start at 2025-11-12 in UTC, reference date at 2025-11-13
		const start = new Date("2025-11-12T00:00:00.000Z");
		const reference = new Date("2025-11-13T00:00:00.000Z");

		const age = getAgeInDaysFromStart(start.toISOString(), reference);
		expect(age).toBe(1);
	});
});
