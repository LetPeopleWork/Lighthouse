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
	it.each([0, 1, 13, 14, 15, 30, 200, 3650])(
		"never shows the nudge for a premium instance at install age %i days",
		(installAgeInDays) => {
			const decision = evaluateNudgeEligibility(
				getMockInput({
					isPremium: true,
					installTimestamp: daysBefore(FIXED_NOW, installAgeInDays),
				}),
			);

			expect(decision.shouldShow).toBe(false);
		},
	);

	it.each([
		[undefined, "premium status unknown"],
		[null, "premium status absent"],
	])(
		"fails closed when premium status is uncertain (%s)",
		(uncertainPremium, _description) => {
			const decision = evaluateNudgeEligibility(
				getMockInput({ isPremium: uncertainPremium }),
			);

			expect(decision.shouldShow).toBe(false);
		},
	);

	it.each([
		[undefined, "absent"],
		[null, "null"],
		["", "empty"],
		["not-a-date", "unparseable"],
	])(
		"fails closed when the install timestamp is %s",
		(installTimestamp, _description) => {
			const decision = evaluateNudgeEligibility(
				getMockInput({ installTimestamp }),
			);

			expect(decision.shouldShow).toBe(false);
		},
	);

	it.each([
		[0, false],
		[7, false],
		[13, false],
		[13.9, false],
		[14, true],
		[15, true],
		[90, true],
	])(
		"shows only at or beyond the two-week threshold (age %i days -> %s)",
		(installAgeInDays, expected) => {
			const decision = evaluateNudgeEligibility(
				getMockInput({
					installTimestamp: daysBefore(FIXED_NOW, installAgeInDays),
				}),
			);

			expect(decision.shouldShow).toBe(expected);
		},
	);

	it("compares absolute UTC instants regardless of the now timezone offset", () => {
		const justOverTwoWeeks = new Date("2026-06-01T12:00:00.000Z");

		const decision = evaluateNudgeEligibility({
			isPremium: false,
			installTimestamp: "2026-05-18T11:59:00.000Z",
			now: justOverTwoWeeks,
		});

		expect(decision.shouldShow).toBe(true);
	});

	it.each([
		[1, false],
		[7, false],
		[180, false],
	])(
		"stays quiet while the server-computed next-eligible instant is still in the future (in %i days)",
		(daysAhead, expected) => {
			const nextEligibleAt = new Date(
				FIXED_NOW.getTime() + daysAhead * 24 * 60 * 60 * 1000,
			).toISOString();

			const decision = evaluateNudgeEligibility(
				getMockInput({ nextEligibleAt }),
			);

			expect(decision.shouldShow).toBe(expected);
		},
	);

	it.each([
		[1, true],
		[7, true],
	])(
		"shows again once the next-eligible instant has passed (%i days ago)",
		(daysAgo, expected) => {
			const nextEligibleAt = new Date(
				FIXED_NOW.getTime() - daysAgo * 24 * 60 * 60 * 1000,
			).toISOString();

			const decision = evaluateNudgeEligibility(
				getMockInput({ nextEligibleAt }),
			);

			expect(decision.shouldShow).toBe(expected);
		},
	);

	it.each([
		["a backward clock skew", "2026-05-15T00:00:00.000Z"],
		["an epoch reset", "1970-01-01T00:00:00.000Z"],
	])(
		"never re-shows a quieted nudge early when the clock jumps backward (%s)",
		(_description, skewedNow) => {
			const nextEligibleAt = new Date(
				FIXED_NOW.getTime() + 180 * 24 * 60 * 60 * 1000,
			).toISOString();

			const decision = evaluateNudgeEligibility(
				getMockInput({ nextEligibleAt, now: new Date(skewedNow) }),
			);

			expect(decision.shouldShow).toBe(false);
		},
	);
});
