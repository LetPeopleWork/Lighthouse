import { describe, expect, it } from "vitest";
import type { IReportTemplates } from "../../../../../models/Reports/Report";
import {
	defaultNameOf,
	lengthOf,
	newThenAndNowDraft,
	offeredMetricsOf,
	resolvedThenDays,
	type ThenAndNowDraft,
	thenRefusalOf,
} from "./thenAndNowDraft";

const templates: IReportTemplates = {
	today: "2026-10-02",
	earliestStart: null,
	minimumLengthDays: 14,
	templates: [
		{ templateKey: "then-and-now", metrics: [{ key: "cycle-time" }] },
	],
};

const draftWith = (changes: Partial<ThenAndNowDraft>): ThenAndNowDraft => ({
	...newThenAndNowDraft(),
	...changes,
});

describe("lengthOf", () => {
	it.each([
		["14", 14],
		["", null],
		["   ", null],
		["0", null],
		["-3", null],
		["1.5", null],
		["ninety", null],
	])("reads %j as %j days", (text, expected) => {
		expect(lengthOf(text)).toBe(expected);
	});
});

describe("thenRefusalOf", () => {
	it.each(["", "   ", "1.5"])(
		"refuses nothing while Then's length %j is no whole number of days",
		(thenLength) => {
			expect(thenRefusalOf(draftWith({ thenLength }), templates)).toBeNull();
		},
	);

	it("refuses a whole number of days shorter than the server allows", () => {
		expect(thenRefusalOf(draftWith({ thenLength: "0" }), templates)).toBe(
			"then-too-short",
		);
	});
});

describe("resolvedThenDays", () => {
	it("names both days of a Then", () => {
		expect(resolvedThenDays("2026-10-02", 90)).toBe("5 Jul – 2 Oct 2026");
	});

	it.each([
		["2026-10-02", null],
		["not a day", 90],
	])("names nothing for an end of %j and %j days", (end, lengthDays) => {
		expect(resolvedThenDays(end, lengthDays)).toBeNull();
	});
});

describe("defaultNameOf", () => {
	it("names the report after its Then", () => {
		expect(defaultNameOf(draftWith({}), templates)).toBe(
			"Then & Now — 90 days to 2 Oct 2026",
		);
	});

	it.each([
		["a Then length that is no number of days", { thenLength: "" }],
		["a Then end that is no day", { thenEnd: "not a day" }],
	])("is the Type alone for %s", (_, changes) => {
		expect(defaultNameOf(draftWith(changes), templates)).toBe("Then & Now");
	});
});

describe("offeredMetricsOf", () => {
	it("reads the metrics of Then & Now, not of the first Type listed", () => {
		const withAnotherTypeFirst = {
			...templates,
			templates: [
				{ templateKey: "another-type", metrics: [{ key: "throughput" }] },
				...templates.templates,
			],
		} as unknown as IReportTemplates;

		expect(offeredMetricsOf(withAnotherTypeFirst)).toEqual(["cycle-time"]);
	});

	it("offers nothing when the server offers no Then & Now", () => {
		expect(offeredMetricsOf({ ...templates, templates: [] })).toEqual([]);
	});
});
