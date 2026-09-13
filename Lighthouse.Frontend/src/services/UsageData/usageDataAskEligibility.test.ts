import { describe, expect, it } from "vitest";
import {
	type AskEligibilityInput,
	evaluateAskEligibility,
} from "./usageDataAskEligibility";

/**
 * Slice 02, US-05. describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 *
 * The division of labour these tests encode: the server answers everything it can see, and this
 * function answers the single thing it cannot - that a browser with no consent row was already
 * shown the dialog once and walked away from it.
 */

const NOW = new Date("2026-09-13T12:00:00.000Z");
const RE_ASK_AFTER_DAYS = 90;

const daysBefore = (days: number): string =>
	new Date(NOW.getTime() - days * 24 * 60 * 60 * 1000).toISOString();

const input = (
	overrides?: Partial<AskEligibilityInput>,
): AskEligibilityInput => ({
	mayAsk: true,
	decision: null,
	lastAskedAt: null,
	reAskAfterDays: RE_ASK_AFTER_DAYS,
	promptSlotTaken: false,
	now: NOW,
	...overrides,
});

describe("evaluateAskEligibility", () => {
	it("asks a browser that has never been asked and never answered", () => {
		expect(evaluateAskEligibility(input()).shouldAsk).toBe(true);
	});

	// AC-05.1, AC-05.4, AC-05.7, AC-05.8 all arrive here as one false. Install age, the
	// administrator's switch, and a decision that closes the question for good are the server's to
	// weigh; a browser that re-derived any of them could be told a different answer by a clock it
	// controls.
	it("stays silent whenever the server says this browser is not due", () => {
		expect(evaluateAskEligibility(input({ mayAsk: false })).shouldAsk).toBe(
			false,
		);
	});

	// AC-05.2. The marker is the only record that the question was ever put to this browser: it
	// closed the dialog, so no consent row exists and the server has nothing to remember it by.
	it("does not ask an undecided browser again straight away", () => {
		const decision = evaluateAskEligibility(
			input({ lastAskedAt: daysBefore(1) }),
		);

		expect(decision.shouldAsk).toBe(false);
	});

	it("still leaves it alone the day before its quiet period is up", () => {
		const decision = evaluateAskEligibility(
			input({ lastAskedAt: daysBefore(RE_ASK_AFTER_DAYS - 1) }),
		);

		expect(decision.shouldAsk).toBe(false);
	});

	it("asks it once the quiet period has run out", () => {
		const decision = evaluateAskEligibility(
			input({ lastAskedAt: daysBefore(RE_ASK_AFTER_DAYS) }),
		);

		expect(decision.shouldAsk).toBe(true);
	});

	// Nothing rewrites the marker except an ask, so a browser silenced on a value it cannot use is
	// silenced for as long as that value survives - for ever, when nothing can read it. Asking once
	// re-stamps it from a working clock and returns the browser to the ordinary cadence.
	//
	// Both of these once fell through to the arithmetic and reached "leave it alone" by accident:
	// NaN and a negative elapsed time are each never greater than the window. Mutation testing found
	// it, by deleting both guards and changing no answer.
	it("asks a browser whose marker cannot be read, rather than silencing it for ever", () => {
		const decision = evaluateAskEligibility(
			input({ lastAskedAt: "not a date" }),
		);

		expect(decision.shouldAsk).toBe(true);
	});

	it("asks a browser whose marker is dated ahead of now, rather than waiting for the clock", () => {
		const decision = evaluateAskEligibility(
			input({ lastAskedAt: daysBefore(-365) }),
		);

		expect(decision.shouldAsk).toBe(true);
	});

	// AC-05.5: a browser that answered is asked again once the server says its window has run out,
	// and the marker from that earlier ask is by then older than the window too, so it agrees.
	it("asks again when the server says a browser that answered is due", () => {
		const decision = evaluateAskEligibility(
			input({
				decision: "Declined",
				lastAskedAt: daysBefore(RE_ASK_AFTER_DAYS),
			}),
		);

		expect(decision.shouldAsk).toBe(true);
	});

	// The browser's only defence when the server did not hear about the last ask.
	//
	// Telling the server is one fire-and-forget request. When it does not land the server goes on
	// saying "due", because closing the dialog changes nothing it can see - so without this the
	// question returns on every page load until some later request happens to succeed. That is the
	// nag the whole cadence exists to rule out, arriving through the mechanism built to prevent it.
	//
	// The previous version of this test used a marker four months old, which is past the window: it
	// returned true whether or not the marker was read at all, and so could not tell a working
	// implementation from a broken one.
	it("leaves a browser alone that it asked minutes ago, even though it had answered before", () => {
		const decision = evaluateAskEligibility(
			input({
				mayAsk: true,
				decision: "Declined",
				lastAskedAt: daysBefore(0),
			}),
		);

		expect(decision.shouldAsk).toBe(false);
	});

	// The pair that matters, side by side, because an earlier version of this had them disagree.
	//
	// Closing the dialog used to silence it for ever while an explicit refusal came back after a
	// few months - the weaker signal producing the stronger effect, and a quiet drain on the
	// population the feature exists to measure. Closing is not a refusal: nobody withheld anything,
	// they declined to engage. So both wait the same period and then both are asked.
	it("treats a dismissal and a refusal the same once the quiet period is up", () => {
		const longEnoughAgo = daysBefore(RE_ASK_AFTER_DAYS);

		const dismissed = evaluateAskEligibility(
			input({ decision: null, lastAskedAt: longEnoughAgo }),
		);
		const refused = evaluateAskEligibility(
			input({ decision: "Declined", lastAskedAt: longEnoughAgo }),
		);

		expect(dismissed.shouldAsk).toBe(true);
		expect(refused.shouldAsk).toBe(true);
	});

	// AC-05.6. Consent collected beside an unrelated request is not freely given, so this outranks
	// being due: a browser the server says is due still waits for the next session.
	it("yields to a prompt that already holds the session", () => {
		const decision = evaluateAskEligibility(input({ promptSlotTaken: true }));

		expect(decision.shouldAsk).toBe(false);
	});

	it("yields the session even to a browser that has answered and come due again", () => {
		const decision = evaluateAskEligibility(
			input({ decision: "Declined", promptSlotTaken: true }),
		);

		expect(decision.shouldAsk).toBe(false);
	});
});
