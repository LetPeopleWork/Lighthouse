import { describe, expect, it } from "vitest";
import { chooseDurationUnit, formatDuration } from "./formatDuration";

describe("chooseDurationUnit", () => {
	it.each([
		{ maxDays: 0.01, expected: "minutes" },
		{ maxDays: 0.04, expected: "minutes" },
		{ maxDays: 0.05, expected: "hours" },
		{ maxDays: 0.9, expected: "hours" },
		{ maxDays: 1, expected: "days" },
		{ maxDays: 13, expected: "days" },
		{ maxDays: 14, expected: "days" },
		{ maxDays: 120, expected: "days" },
	])("picks $expected when the largest bar magnitude is $maxDays days", ({
		maxDays,
		expected,
	}) => {
		expect(chooseDurationUnit(maxDays)).toBe(expected);
	});
});

describe("formatDuration", () => {
	it.each([
		{ valueDays: 0.5, unit: "days" as const, expected: "0.5 d" },
		{ valueDays: 3, unit: "days" as const, expected: "3 d" },
		{ valueDays: 0.02, unit: "hours" as const, expected: "0.5 h" },
		{ valueDays: 0.25, unit: "hours" as const, expected: "6 h" },
		{ valueDays: 0.01, unit: "minutes" as const, expected: "14 m" },
		{ valueDays: 21, unit: "days" as const, expected: "21 d" },
		{ valueDays: 120, unit: "days" as const, expected: "120 d" },
	])("formats $valueDays days as $expected in $unit", ({
		valueDays,
		unit,
		expected,
	}) => {
		expect(formatDuration(valueDays, unit)).toBe(expected);
	});

	it("renders a sub-day magnitude in hours, never as a fractional day", () => {
		const subDayMagnitude = 0.3;
		const unit = chooseDurationUnit(subDayMagnitude);

		const formatted = formatDuration(subDayMagnitude, unit);

		expect(unit).toBe("hours");
		expect(formatted).not.toContain("d");
		expect(formatted).toContain("h");
	});

	it("formats every value with the same unit chosen from the largest bar", () => {
		const largestBar = 30;
		const unit = chooseDurationUnit(largestBar);

		const formattedValues = [largestBar, 14, 2].map((value) =>
			formatDuration(value, unit),
		);

		expect(unit).toBe("days");
		expect(formattedValues.every((value) => value.endsWith(" d"))).toBe(true);
	});
});
