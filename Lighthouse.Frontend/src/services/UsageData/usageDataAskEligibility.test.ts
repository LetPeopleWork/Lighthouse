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

	// A browser cannot be silenced for ever by a value nothing can read. Treating an unparseable
	// marker as "never asked" would be worse - the dialog would return on every single visit - so
	// it counts as a recent ask, costing one cycle rather than all of them.
	it("treats a marker it cannot read as a recent ask", () => {
		const decision = evaluateAskEligibility(
			input({ lastAskedAt: "not a date" }),
		);

		expect(decision.shouldAsk).toBe(false);
	});

	// AC-05.5, and the reason the marker is consulted for undecided browsers only. A browser that
	// declined carries a marker from the day it was asked; three months later the server says it is
	// due again, and a marker left over from the first ask must not be what silences the second.
	it("asks again when the server says a browser that answered is due, marker or not", () => {
		const decision = evaluateAskEligibility(
			input({
				decision: "Declined",
				lastAskedAt: "2026-06-01T09:00:00.000Z",
			}),
		);

		expect(decision.shouldAsk).toBe(true);
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
