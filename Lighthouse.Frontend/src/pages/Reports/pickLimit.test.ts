import { describe, expect, it } from "vitest";
import {
	exportBlockedBecause,
	removeBlockedBecause,
	TOO_MANY_PICKED,
} from "./pickLimit";

const termOf = (key: string) => `the ${key}`;
const removeWith = (picked: number, permissionsFailed = false, notAdmin = 0) =>
	removeBlockedBecause(picked, permissionsFailed, notAdmin, termOf);

describe("The most reports that may be picked", () => {
	it("is stated as picking at most 50 reports", () => {
		expect(TOO_MANY_PICKED).toBe("Pick at most 50 reports.");
	});

	describe("Remove", () => {
		it.each([1, 50])("gives no reason to hold back at %i picked", (picked) => {
			expect(removeWith(picked)).toBe("");
		});

		it.each([51, 101])("is held back above 50, at %i picked", (picked) => {
			expect(removeWith(picked)).toBe(TOO_MANY_PICKED);
		});

		it("names too many picked before permissions that never arrived or owners she cannot change", () => {
			expect(removeWith(51, true, 3)).toBe(TOO_MANY_PICKED);
		});

		it("names permissions that never arrived before owners she cannot change", () => {
			expect(removeWith(4, true, 2)).toBe(
				"Your permissions couldn't be loaded. Reload the page to try again.",
			);
		});

		it("says one of the picked reports is on an owner she is not Admin of", () => {
			expect(removeWith(4, false, 1)).toBe(
				"1 of the 4 selected reports is on a the team or the portfolio you are not Admin of.",
			);
		});

		it("says several picked reports are on owners she is not Admin of", () => {
			expect(removeWith(3, false, 2)).toBe(
				"2 of the 3 selected reports are on a the team or the portfolio you are not Admin of.",
			);
		});
	});

	describe("Export PDF", () => {
		it("asks for at least one report when none is picked", () => {
			expect(exportBlockedBecause(0)).toBe("Select at least one report.");
		});

		it.each([1, 50])("gives no reason to hold back at %i picked", (picked) => {
			expect(exportBlockedBecause(picked)).toBe("");
		});

		it.each([51, 101])("is held back above 50, at %i picked", (picked) => {
			expect(exportBlockedBecause(picked)).toBe(TOO_MANY_PICKED);
		});
	});
});
