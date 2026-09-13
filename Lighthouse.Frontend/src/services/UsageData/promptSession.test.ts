import { beforeEach, describe, expect, it } from "vitest";
import { claimPromptSlot, promptSlotHolder } from "./promptSession";

/**
 * Slice 02, AC-05.6. describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 */

describe.skip("promptSession", () => {
	beforeEach(() => {
		sessionStorage.clear();
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
	it("survives a browser that refuses session storage", () => {
		const denied = () => {
			throw new Error("denied");
		};
		const original = Object.getOwnPropertyDescriptor(
			window,
			"sessionStorage",
		) as PropertyDescriptor;

		Object.defineProperty(window, "sessionStorage", {
			configurable: true,
			get: denied,
		});

		try {
			expect(() => claimPromptSlot("usage-data")).not.toThrow();
			expect(() => promptSlotHolder()).not.toThrow();
		} finally {
			Object.defineProperty(window, "sessionStorage", original);
		}
	});
});
