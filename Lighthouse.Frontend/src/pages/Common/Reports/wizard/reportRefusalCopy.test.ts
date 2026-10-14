import { describe, expect, it } from "vitest";
import { ApiError } from "../../../../services/Api/ApiError";
import {
	type RefusalWords,
	type ReportRefusalCode,
	refusalCopyFor,
} from "./reportRefusalCopy";

const words: RefusalWords = {
	minimumLengthDays: 14,
	earliestStart: "2025-10-02",
	ownerTerm: "Team",
	workItemsTerm: "Work Items",
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
			"The Then window must start on or after 2 Oct 2025 — older finished Work Items are no longer kept.",
		],
		["now-too-short", "Now needs at least 14 days."],
		["now-too-long", "Now can't reach back that far."],
		[
			"now-before-cutoff",
			"The Now window must start on or after 2 Oct 2025 — older finished Work Items are no longer kept.",
		],
		["name-missing", "Give the report a name."],
		["name-too-long", "A report name can be at most 200 characters."],
		["no-metric-shown", "Choose at least one metric"],
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
			"Community includes 2 reports per Team. Delete one, or use Premium for unlimited reports.",
		],
	])("words %s as %j", (code, expected) => {
		expect(refusalCopyFor(refusal(code), words)).toBe(expected);
	});

	it("names the oldest kept day the refusal gives over the one the wizard opened with", () => {
		expect(
			refusalCopyFor(refusal("then-before-cutoff", "2025-11-15"), words),
		).toContain("on or after 15 Nov 2025");
	});

	it("keeps the day the wizard opened with when the refusal names none", () => {
		expect(
			refusalCopyFor(refusal("then-before-cutoff", undefined), words),
		).toContain("on or after 2 Oct 2025");
	});

	it("speaks of the oldest kept day when the refusal says no day is cut off", () => {
		expect(
			refusalCopyFor(refusal("then-before-cutoff", null), words),
		).toContain("on or after the oldest kept day");
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
