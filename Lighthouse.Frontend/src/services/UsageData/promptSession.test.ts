import { beforeEach, describe, expect, it } from "vitest";
import { claimPromptSlot, promptSlotHolder } from "./promptSession";

/**
 * Slice 02, AC-05.6. describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 */

describe("promptSession", () => {
	beforeEach(() => {
		sessionStorage.clear();

		// The page-level copy of the claim is module state and outlives one test, so clearing
		// storage is not on its own a fresh page. One read with storage working puts the two level
		// again - which is what a real page load does before either prompt claims anything.
		promptSlotHolder();
	});

	it("hands the free slot to whoever asks first", () => {
		expect(claimPromptSlot("usage-data")).toBe(true);
		expect(promptSlotHolder()).toBe("usage-data");
	});

	it("refuses a second owner for the rest of the session", () => {
		claimPromptSlot("survey-nudge");

		expect(claimPromptSlot("usage-data")).toBe(false);
		expect(promptSlotHolder()).toBe("survey-nudge");
	});

	// A component re-renders for reasons that have nothing to do with prompts. Losing the slot it
	// already holds would let the other prompt in behind it, and both would be on screen.
	it("lets the holder claim again", () => {
		claimPromptSlot("usage-data");

		expect(claimPromptSlot("usage-data")).toBe(true);
	});

	it("reports nobody holding a fresh session", () => {
		expect(promptSlotHolder()).toBeNull();
	});

	// Private windows and locked-down browsers throw on storage rather than returning nothing. A
	// browser that cannot hold the slot must fall back to showing one prompt, not to showing none
	// and not to throwing through whichever component asked.
	//
	// The earlier version of this test asserted only that neither call threw, while its own
	// preamble promised the stronger thing. It did not hold: a storage failure answered "free" to
	// both askers, so a private window got the survey popup and the consent dialog at once - the
	// one arrangement AC-05.6 exists to forbid, and the reason consent asked beside an unrelated
	// request is not freely given.
	it("still lets exactly one prompt through when session storage refuses", () => {
		const original = Object.getOwnPropertyDescriptor(
			window,
			"sessionStorage",
		) as PropertyDescriptor;

		Object.defineProperty(window, "sessionStorage", {
			configurable: true,
			get: () => {
				throw new Error("denied");
			},
		});

		try {
			expect(claimPromptSlot("survey-nudge")).toBe(true);
			expect(claimPromptSlot("usage-data")).toBe(false);
			expect(promptSlotHolder()).toBe("survey-nudge");
		} finally {
			Object.defineProperty(window, "sessionStorage", original);
		}
	});
});
