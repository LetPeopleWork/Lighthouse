import { beforeEach, describe, expect, it } from "vitest";
import { readAskedMarker, writeAskedMarker } from "./usageDataAskMarker";

/**
 * Slice 02, AC-05.2. describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 */

const ASKED_AT = "2026-09-13T08:30:00.000Z";

describe.skip("usageDataAskMarker", () => {
	beforeEach(() => {
		localStorage.clear();
	});

	it("reads back what was recorded", () => {
		writeAskedMarker(ASKED_AT);

		expect(readAskedMarker()).toBe(ASKED_AT);
	});

	it("reports nothing for a browser that has never been shown the dialog", () => {
		expect(readAskedMarker()).toBeNull();
	});

	// Reading is not writing. A browser that has only ever loaded the page must leave no trace at
	// all - that is the whole basis on which anything may be stored here without asking first.
	it("leaves storage untouched when it only reads", () => {
		readAskedMarker();

		expect(localStorage.length).toBe(0);
	});

	// Private windows throw rather than returning null. A browser that cannot remember being asked
	// is simply a browser that gets asked again; failing the page over it would be worse.
	it("survives a browser that refuses local storage", () => {
		const original = Object.getOwnPropertyDescriptor(
			window,
			"localStorage",
		) as PropertyDescriptor;

		Object.defineProperty(window, "localStorage", {
			configurable: true,
			get: () => {
				throw new Error("denied");
			},
		});

		try {
			expect(() => writeAskedMarker(ASKED_AT)).not.toThrow();
			expect(readAskedMarker()).toBeNull();
		} finally {
			Object.defineProperty(window, "localStorage", original);
		}
	});

	// The marker sits beside the consent token and must not be mistaken for it. The token is the
	// only handle on a consent record and cannot be reissued, so anything that overwrote it would
	// take away this browser's ability to withdraw.
	it("does not disturb the consent token", () => {
		localStorage.setItem("lighthouse:usagedata:consent", "a-token");

		writeAskedMarker(ASKED_AT);

		expect(localStorage.getItem("lighthouse:usagedata:consent")).toBe(
			"a-token",
		);
	});
});
