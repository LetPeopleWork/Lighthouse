import { describe, expect, it } from "vitest";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	notCreatedLineFor,
	notRemovedLineFor,
	type RefusalWords,
	type ReportRefusalCode,
	refusalCopyFor,
} from "./reportRefusalCopy";

const words: RefusalWords = {
	minimumLengthDays: 14,
	earliestStart: "2025-10-02",
	ownerTerm: "Team",
	workItemsTerm: "Work Items",
	ownerName: "Lightspeed",
	thenLengthDays: 90,
};

const COULD_NOT_CREATE = "The report couldn't be created. Try again.";

const refusal = (problemCode?: string, earliestStart?: string | null) =>
	new ApiError(
		400,
		"Bad Request",
		undefined,
		undefined,
		problemCode,
		earliestStart,
	);

describe("refusalCopyFor", () => {
	it.each<[ReportRefusalCode, string]>([
		["then-too-short", "Then needs at least 14 days."],
		["then-too-long", "Then can't reach back that far."],
		["then-ends-in-the-future", "Then cannot end after today."],
		[
			"then-before-cutoff",
			"Lightspeed keeps history from 2 Oct 2025: too little for a 90-day Then and Now. Pick a shorter length or a later end date.",
		],
		[
			"now-length-differs-from-then",
			"Now is always as long as Then.",
		],
		["name-missing", "Give the report a name."],
		["name-too-long", "A report name can be at most 200 characters."],
		["no-metric-shown", "Choose at least one metric"],
		["duplicate-metric", "A metric is listed twice."],
		[
			"unknown-metric",
			"One of the chosen metrics isn't known to this version of Lighthouse.",
		],
		[
			"metric-not-captured",
			"One of the chosen metrics was not captured for this report.",
		],
		[
			"template-not-supported",
			"This Type of report can't be created by this version of Lighthouse.",
		],
		[
			"report-cap-reached",
			"Community includes 1 Then & Now report per Team. Delete it, or use Premium for unlimited reports.",
		],
		["owner-not-found", "This Team no longer exists."],
		["not-admin", "You are no longer Admin of this Team."],
		["not-saved", "The report couldn't be saved. Try again."],
	])("words %s as %j", (code, expected) => {
		expect(refusalCopyFor(refusal(code), words)).toBe(expected);
	});

	it('names the oldest kept day the refusal gives over the one the wizard opened with, "keeps history from 15 Nov 2025"', () => {
		expect(
			refusalCopyFor(refusal("then-before-cutoff", "2025-11-15"), words),
		).toContain("Lightspeed keeps history from 15 Nov 2025:");
	});

	it('keeps the day the wizard opened with when the refusal names none, "keeps history from 2 Oct 2025"', () => {
		expect(
			refusalCopyFor(refusal("then-before-cutoff", undefined), words),
		).toContain("Lightspeed keeps history from 2 Oct 2025:");
	});

	it('names the length the Then was entered with, "too little for a 30-day Then and Now"', () => {
		expect(
			refusalCopyFor(refusal("then-before-cutoff"), {
				...words,
				thenLengthDays: 30,
			}),
		).toContain("too little for a 30-day Then and Now.");
	});

	it('says "This Team keeps history from 2 Oct 2025" when it has no owner name', () => {
		expect(
			refusalCopyFor(refusal("then-before-cutoff"), {
				...words,
				ownerName: undefined,
			}),
		).toBe(
			"This Team keeps history from 2 Oct 2025: too little for a 90-day Then and Now. Pick a shorter length or a later end date.",
		);
	});

	it.each([
		["a failure that is no refusal", new Error("Network Error")],
		[
			"something shaped like a refusal that is no ApiError",
			{ problemCode: "name-missing" },
		],
		["a refusal naming no reason", refusal(undefined)],
		["a refusal naming a reason this version does not know", refusal("x")],
	])("asks to try again after %s", (_, error) => {
		expect(refusalCopyFor(error, words)).toBe(COULD_NOT_CREATE);
	});
});

describe("notCreatedLineFor", () => {
	it.each([
		["no reason", undefined],
		["a reason this version does not know", "x"],
	])("asks to try again for an owner refused with %s", (_, code) => {
		expect(notCreatedLineFor(code, { ...words, ownerName: "Zenith" })).toBe(
			`Not created: ${COULD_NOT_CREATE}`,
		);
	});
});

describe("notRemovedLineFor", () => {
	it("names the owner the reader is no longer Admin of", () => {
		expect(notRemovedLineFor("not-admin", { ownerName: "Gravity" })).toBe(
			"Not removed: you are no longer Admin of Gravity.",
		);
	});

	it.each([
		["a report that could not be saved", "not-saved"],
		["no reason", undefined],
		["a reason this version does not know", "x"],
	])("asks to try again for %s, as removing one report does", (_, code) => {
		expect(notRemovedLineFor(code, { ownerName: "Gravity" })).toBe(
			"Not removed: This report couldn't be deleted. Try again.",
		);
	});
});
