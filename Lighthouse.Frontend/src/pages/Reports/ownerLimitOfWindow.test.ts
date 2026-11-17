import { describe, expect, it } from "vitest";
import type { IReportTemplates } from "../../models/Reports/Report";
import type { IReportCreationOwner } from "../../models/Reports/ReportsAcrossOwners";
import { newThenAndNowDraft } from "../Common/Reports/wizard/thenAndNow/thenAndNowDraft";
import { ownerKeyOf } from "./OwnerStep";
import {
	holdableOwners,
	limitOfFor,
	pickedOwnersOf,
	picksThatCanStillHold,
} from "./ownerLimitOfWindow";

const templates: IReportTemplates = {
	today: "2026-10-02",
	earliestStart: null,
	minimumLengthDays: 14,
	templates: [],
};

const ownerKeeping = (
	id: number,
	earliestStart: string | null,
): IReportCreationOwner => ({
	ownerKind: "team",
	ownerId: id,
	ownerName: `Team ${id}`,
	earliestStart,
});

const keepsEverything = ownerKeeping(1, null);
const keepsAYear = ownerKeeping(2, "2025-10-02");
const keepsSinceAugust = ownerKeeping(3, "2026-08-01");
const keepsSinceSeptember = ownerKeeping(4, "2026-09-01");
const everyOwner = [
	keepsEverything,
	keepsAYear,
	keepsSinceAugust,
	keepsSinceSeptember,
];

/** A Then left at its default end, which is the day before a Now of the same length starts. */
const backToBack = (thenLength: string) => ({
	...newThenAndNowDraft(),
	thenLength,
});

describe("what stops an owner from holding the window", () => {
	it("names no limit for an owner that keeps every finished item", () => {
		const limitOf = limitOfFor(backToBack("90"), templates);

		expect(limitOf(keepsEverything)).toBeNull();
	});

	it("names no limit while Then's length is no number of days", () => {
		const limitOf = limitOfFor(backToBack(""), templates);

		expect(limitOf(keepsSinceSeptember)).toBeNull();
	});

	it("names no limit for an owner whose kept history reaches back past the whole window", () => {
		const limitOf = limitOfFor(backToBack("90"), templates);

		expect(limitOf(keepsAYear)).toBeNull();
	});

	it("names the day Then starts and the day the history starts, when Then reaches back past the kept history", () => {
		const limitOf = limitOfFor(backToBack("90"), templates);

		expect(limitOf(keepsSinceSeptember)).toEqual(
			expect.objectContaining({
				keptSince: "1 Sep 2026",
				windowStarts: "6 Apr 2026",
			}),
		);
	});

	it("holds a window that starts on the first day of the kept history and refuses one that starts a day before it", () => {
		const limitOf = limitOfFor(backToBack("90"), templates);
		const keptFromTheFirstDay = ownerKeeping(5, "2026-04-06");
		const keptFromADayLater = ownerKeeping(6, "2026-04-07");

		expect(limitOf(keptFromTheFirstDay)).toBeNull();
		expect(limitOf(keptFromADayLater)).toEqual(
			expect.objectContaining({
				keptSince: "7 Apr 2026",
				windowStarts: "6 Apr 2026",
			}),
		);
	});

	it("names the day a Then with an entered end starts", () => {
		const draft = { ...backToBack("14"), thenEnd: "2026-06-30" };

		expect(limitOfFor(draft, templates)(keepsSinceAugust)).toEqual(
			expect.objectContaining({
				keptSince: "1 Aug 2026",
				windowStarts: "17 Jun 2026",
			}),
		);
	});

	it("never names Now: a Then the owner holds leaves room for a Now as long as it", () => {
		const draft = { ...backToBack("14"), thenEnd: "2026-09-30" };

		expect(limitOfFor(draft, templates)(keepsSinceAugust)).toBeNull();
	});

	it("names no limit while the day Then ends is no calendar day", () => {
		const draft = { ...backToBack("90"), thenEnd: "not-a-day" };

		expect(limitOfFor(draft, templates)(keepsSinceAugust)).toBeNull();
	});
});

describe("which owners the window can be created on", () => {
	const limitOf = limitOfFor(backToBack("90"), templates);

	it("holds only the owners the window fits", () => {
		expect(holdableOwners(everyOwner, limitOf)).toEqual([
			keepsEverything,
			keepsAYear,
		]);
	});

	it("counts a pick the window cannot hold as no pick", () => {
		const picked = new Set(everyOwner.map(ownerKeyOf));

		expect(pickedOwnersOf(everyOwner, picked, limitOf)).toEqual([
			keepsEverything,
			keepsAYear,
		]);
	});

	it("leaves out an owner that fits but was not picked", () => {
		const picked = new Set([ownerKeyOf(keepsAYear)]);

		expect(pickedOwnersOf(everyOwner, picked, limitOf)).toEqual([keepsAYear]);
	});

	it("keeps the picks the new window can still hold and drops the rest", () => {
		const picked = new Set([
			ownerKeyOf(keepsAYear),
			ownerKeyOf(keepsSinceSeptember),
		]);
		const longWindow = limitOfFor(backToBack("90"), templates);

		const afterLonger = picksThatCanStillHold(everyOwner, picked, longWindow);

		expect([...afterLonger]).toEqual([ownerKeyOf(keepsAYear)]);
	});
});
