import { describe, expect, it } from "vitest";
import {
	evaluateNudgeEligibility,
	type NudgeEligibilityInput,
} from "./nudgeEligibility";

const FIXED_NOW = new Date("2026-06-01T00:00:00.000Z");

const daysBefore = (reference: Date, days: number): string =>
	new Date(reference.getTime() - days * 24 * 60 * 60 * 1000).toISOString();

const getMockInput = (
	overrides?: Partial<NudgeEligibilityInput>,
): NudgeEligibilityInput => ({
	isPremium: false,
	installTimestamp: daysBefore(FIXED_NOW, 30),
	now: FIXED_NOW,
	...overrides,
});

describe("evaluateNudgeEligibility", () => {
	it.each([
		0, 1, 13, 14, 15, 30, 200, 3650,
	])("never shows the nudge for a premium instance at install age %i days", (installAgeInDays) => {
		const decision = evaluateNudgeEligibility(
			getMockInput({
				isPremium: true,
				installTimestamp: daysBefore(FIXED_NOW, installAgeInDays),
			}),
		);

		expect(decision.shouldShow).toBe(false);
	});

	it.each([
		[undefined, "premium status unknown"],
		[null, "premium status absent"],
	])("fails closed when premium status is uncertain (%s)", (uncertainPremium, _description) => {
		const decision = evaluateNudgeEligibility(
			getMockInput({ isPremium: uncertainPremium }),
		);

		expect(decision.shouldShow).toBe(false);
	});

	it.each([
		[undefined, "absent"],
		[null, "null"],
		["", "empty"],
		["not-a-date", "unparseable"],
	])("fails closed when the install timestamp is %s", (installTimestamp, _description) => {
		const decision = evaluateNudgeEligibility(
			getMockInput({ installTimestamp }),
		);

		expect(decision.shouldShow).toBe(false);
	});

	it.each([
		[0, false],
		[7, false],
		[13, false],
		[13.9, false],
		[14, true],
		[15, true],
		[90, true],
	])("shows only at or beyond the two-week threshold (age %i days -> %s)", (installAgeInDays, expected) => {
		const decision = evaluateNudgeEligibility(
			getMockInput({
				installTimestamp: daysBefore(FIXED_NOW, installAgeInDays),
			}),
		);

		expect(decision.shouldShow).toBe(expected);
	});

	it("compares absolute UTC instants regardless of the now timezone offset", () => {
		const justOverTwoWeeks = new Date("2026-06-01T12:00:00.000Z");

		const decision = evaluateNudgeEligibility({
			isPremium: false,
			installTimestamp: "2026-05-18T11:59:00.000Z",
			now: justOverTwoWeeks,
		});

		expect(decision.shouldShow).toBe(true);
	});
});
