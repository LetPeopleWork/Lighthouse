import { describe, expect, it } from "vitest";
import type { IReportTemplates } from "../../models/Reports/Report";
import type { IReportCreationOwner } from "../../models/Reports/ReportsAcrossOwners";
import { newBackToBackDraft } from "../Common/Reports/wizard/thenAndNow/thenAndNowDraft";
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

const backToBack = (thenLength: string, nowLength: string) => ({
	...newBackToBackDraft(),
	thenLength,
	nowLength,
});

describe("what stops an owner from holding the window", () => {
	it("names no limit for an owner that keeps every finished item", () => {
		const limitOf = limitOfFor(backToBack("90", "90"), templates);

		expect(limitOf(keepsEverything)).toBeNull();
	});

	it("names no limit while Then's length is no number of days", () => {
		const limitOf = limitOfFor(backToBack("", "90"), templates);

		expect(limitOf(keepsSinceSeptember)).toBeNull();
	});

	it("names no limit for an owner whose kept history reaches back past the whole window", () => {
		const limitOf = limitOfFor(backToBack("90", "90"), templates);

		expect(limitOf(keepsAYear)).toBeNull();
	});

	it("names Then, the day it starts and the day the history starts, when Then reaches back past the kept history", () => {
		const limitOf = limitOfFor(backToBack("90", "30"), templates);

		expect(limitOf(keepsSinceSeptember)).toEqual({
			keptSince: "1 Sep 2026",
			window: "Then",
			windowStarts: "5 Jun 2026",
		});
	});

	it("names Now when only Now reaches back past the kept history", () => {
		const draft = { ...backToBack("14", "90"), thenEnd: "2026-09-30" };

		expect(limitOfFor(draft, templates)(keepsSinceAugust)).toEqual({
			keptSince: "1 Aug 2026",
			window: "Now",
			windowStarts: "5 Jul 2026",
		});
	});

	it("lets Then end today when Now reaches back past the first day the calendar can name", () => {
		const limitOf = limitOfFor(backToBack("90", "99999999999999"), templates);

		expect(limitOf(keepsSinceAugust)).toEqual({
			keptSince: "1 Aug 2026",
			window: "Then",
			windowStarts: "5 Jul 2026",
		});
	});

	it("names the window that reaches back furthest when both do", () => {
		const draft = { ...backToBack("14", "90"), thenEnd: "2026-06-30" };

		expect(limitOfFor(draft, templates)(keepsSinceAugust)).toEqual({
			keptSince: "1 Aug 2026",
			window: "Then",
			windowStarts: "17 Jun 2026",
		});
	});
});

describe("which owners the window can be created on", () => {
	const limitOf = limitOfFor(backToBack("90", "30"), templates);

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

	it("keeps the picks the new window can still hold and drops the rest for good", () => {
		const picked = new Set([
			ownerKeyOf(keepsAYear),
			ownerKeyOf(keepsSinceSeptember),
		]);
		const shortWindow = limitOfFor(backToBack("14", "14"), templates);
		const longWindow = limitOfFor(backToBack("90", "90"), templates);

		const afterLonger = picksThatCanStillHold(everyOwner, picked, longWindow);
		const afterShorterAgain = picksThatCanStillHold(
			everyOwner,
			afterLonger,
			shortWindow,
		);

		expect([...afterLonger]).toEqual([ownerKeyOf(keepsAYear)]);
		expect([...afterShorterAgain]).toEqual([ownerKeyOf(keepsAYear)]);
	});
});
