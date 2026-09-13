import { describe, expect, it } from "vitest";
import {
	administratorStoppedItSentence,
	theVetoIsAvailableSentence,
} from "./usageDataAdminVetoCopy";

/**
 * Epic 5733 slice 03 (ADO #5836) - the words that carry AC-06.7 and the settings-page promise into
 * the two places a reader meets them.
 *
 * Skipped until DELIVER writes the sentences. Every call sits inside a test body: a describe block
 * still evaluates, so a call hoisted to describe scope would throw during collection and report a
 * failed suite rather than a pending one.
 */
describe("usage data admin veto copy", () => {
	it("names the administrator, because otherwise a reader assumes they did it themselves", () => {
		expect(administratorStoppedItSentence().toLowerCase()).toContain(
			"administrator",
		);
	});

	it("does not tell a reader they declined something they never answered", () => {
		const sentence = administratorStoppedItSentence().toLowerCase();

		expect(sentence).not.toContain("you declined");
		expect(sentence).not.toContain("your choice");
		expect(sentence).not.toContain("you chose");
	});

	it("says nothing is being sent, so the sentence stands on its own without the icon", () => {
		expect(administratorStoppedItSentence().toLowerCase()).toContain("not");
	});

	it("names Premium, which is the whole reason this sentence is shown to everybody", () => {
		expect(theVetoIsAvailableSentence().toLowerCase()).toContain("premium");
	});

	it("says where the switch is, so a reader who can reach it does not have to hunt", () => {
		expect(theVetoIsAvailableSentence().toLowerCase()).toContain(
			"behaviour settings",
		);
	});

	// Most readers of this dialog cannot reach Settings. An unconditional "you can switch this off"
	// sends them looking for a control that is not theirs, and the sentence meant to sell the
	// feature becomes the one that wastes their afternoon.
	it("does not promise the switch to a reader who may not have it", () => {
		const sentence = theVetoIsAvailableSentence();

		expect(sentence).toMatch(/\bif you\b/i);
		expect(sentence).not.toMatch(/^you can\b/i);
	});

	it("keeps the two sentences apart, so the hint never becomes the advertisement", () => {
		expect(administratorStoppedItSentence()).not.toEqual(
			theVetoIsAvailableSentence(),
		);
		expect(administratorStoppedItSentence().toLowerCase()).not.toContain(
			"premium",
		);
	});
});
