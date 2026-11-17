import fc from "fast-check";
import { describe, expect, it } from "vitest";
import type { IReportTemplates } from "../../../../../models/Reports/Report";
import { theEngagementStartReport } from "../../../../../tests/ReportsTestKit";
import { formatLocalDate } from "../../../../../utils/date/localDate";
import {
	defaultNameOf,
	lengthOf,
	newReportForOwnersOf,
	newReportOf,
	newThenAndNowDraft,
	offeredMetricsOf,
	reportChangeOf,
	resolvedThenDays,
	type ThenAndNowDraft,
	thenAndNowDraftOf,
	thenRefusalOf,
	windowDaysOf,
	windowsOf,
	windowsRelationOf,
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
	it("names the report after its Then, which by default ends the day before Now starts", () => {
		expect(defaultNameOf(draftWith({}), templates)).toBe(
			"Then & Now — 90 days to 4 Jul 2026",
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

describe("thenAndNowDraftOf", () => {
	it("counts as captured only the metrics the report holds values for", () => {
		const report = theEngagementStartReport();

		const draft = thenAndNowDraftOf({
			...report,
			thenAndNow: {
				...report.thenAndNow,
				metrics: { ...report.thenAndNow.metrics, "work-item-age": null },
			},
		});

		expect(draft.capturedMetricKeys).toEqual([
			"cycle-time",
			"throughput",
			"wip",
		]);
	});

	it("keeps no Now length of the report it was read from", () => {
		expect(thenAndNowDraftOf(theEngagementStartReport())).not.toHaveProperty(
			"nowLength",
		);
	});
});

describe("windowDaysOf", () => {
	it("names a window's first and last day", () => {
		expect(windowDaysOf("2026-07-05", "2026-10-02")).toBe("5 Jul – 2 Oct 2026");
	});

	it.each([
		["", "2026-10-02"],
		["2026-07-05", "not a day"],
	])("names no days when %j or %j is not a day", (start, end) => {
		expect(windowDaysOf(start, end)).toBeNull();
	});
});

const midNovember: IReportTemplates = { ...templates, today: "2026-11-15" };

describe("windowsOf", () => {
	it("puts a 30-day Then left at its default end back to back with a 30-day Now ending today", () => {
		expect(windowsOf(draftWith({ thenLength: "30" }), midNovember)).toEqual({
			// biome-ignore lint/suspicious/noThenProperty: the window contract names its first window "then", as the report reads it
			then: { start: "2026-09-17", end: "2026-10-16", lengthDays: 30 },
			now: { start: "2026-10-17", end: "2026-11-15", lengthDays: 30 },
		});
	});

	it("gives Now the length of a Then that ended earlier", () => {
		expect(
			windowsOf(
				draftWith({ thenEnd: "2026-10-31", thenLength: "30" }),
				midNovember,
			),
		).toEqual({
			// biome-ignore lint/suspicious/noThenProperty: the window contract names its first window "then", as the report reads it
			then: { start: "2026-10-02", end: "2026-10-31", lengthDays: 30 },
			now: { start: "2026-10-17", end: "2026-11-15", lengthDays: 30 },
		});
	});

	it.each([
		["a Then length that is no whole number of days", { thenLength: "14.5" }],
		["a Then end that is no day", { thenEnd: "not a day" }],
	])("names no windows for %s", (_, changes) => {
		expect(windowsOf(draftWith(changes), midNovember)).toBeNull();
		expect(windowsRelationOf(draftWith(changes), midNovember)).toBeNull();
	});
});

describe("windowsRelationOf", () => {
	it.each([
		["left at its default end", null, { kind: "apart" }],
		["ending the day before Now starts", "2026-10-16", { kind: "apart" }],
		[
			"ending on the day Now starts",
			"2026-10-17",
			{ kind: "overlapUntil", until: "2026-10-17" },
		],
		[
			"ending inside Now",
			"2026-10-31",
			{ kind: "overlapUntil", until: "2026-10-31" },
		],
		[
			"ending the day before today",
			"2026-11-14",
			{ kind: "overlapUntil", until: "2026-11-14" },
		],
		["ending today", "2026-11-15", { kind: "sameDays" }],
	])("relates a 30-day Then %s to Now", (_, thenEnd, expected) => {
		expect(
			windowsRelationOf(draftWith({ thenEnd, thenLength: "30" }), midNovember),
		).toEqual(expected);
	});
});

// Day numbers counted on yyyy-MM-dd read as UTC, independently of the code under test.
const MS_PER_DAY = 86_400_000;
const dayNumberOf = (day: string) => {
	const [year, month, date] = day.split("-").map(Number);
	// biome-ignore lint/plugin/no-day-count-from-milliseconds: a UTC day is always 24 hours, so this division names the UTC day exactly
	return Date.UTC(year, month - 1, date) / MS_PER_DAY;
};
const dayOf = (dayNumber: number) => {
	const utc = new Date(dayNumber * MS_PER_DAY);
	return formatLocalDate(
		new Date(utc.getUTCFullYear(), utc.getUTCMonth(), utc.getUTCDate()),
	);
};

/** The relation the law expects, from the day numbers alone. */
const expectedRelation = (thenEnd: number, nowStart: number, today: number) => {
	if (thenEnd === today) return { kind: "sameDays" };
	if (thenEnd >= nowStart)
		return { kind: "overlapUntil", until: dayOf(thenEnd) };
	return { kind: "apart" };
};

describe("the window law: the overlap line shows exactly when Then and Now share a day (L8)", () => {
	const today = dayNumberOf(midNovember.today);

	it("relates every Then of 14 to 400 days, ending up to 800 days back or at its default end, to Now by the days they share", () => {
		fc.assert(
			fc.property(
				fc.integer({ min: 14, max: 400 }),
				fc.option(fc.integer({ min: 0, max: 800 }), { nil: null }),
				(lengthDays, daysBack) => {
					const thenEnd =
						daysBack === null ? today - lengthDays : today - daysBack;
					const nowStart = today - lengthDays + 1;
					const draft = draftWith({
						thenEnd: daysBack === null ? null : dayOf(thenEnd),
						thenLength: String(lengthDays),
					});

					expect(windowsRelationOf(draft, midNovember)).toEqual(
						expectedRelation(thenEnd, nowStart, today),
					);
					expect(windowsOf(draft, midNovember)).toEqual({
						// biome-ignore lint/suspicious/noThenProperty: the window contract names its first window "then", as the report reads it
						then: {
							start: dayOf(thenEnd - lengthDays + 1),
							end: dayOf(thenEnd),
							lengthDays,
						},
						now: { start: dayOf(nowStart), end: midNovember.today, lengthDays },
					});
				},
			),
			{ seed: 6625, numRuns: 200 },
		);
	});
});

describe("the report the server is asked to create", () => {
	it("carries no Now length for one owner", () => {
		expect(
			newReportOf(draftWith({ thenLength: "30" }), midNovember).thenAndNow,
		).toEqual({ thenEndDate: "2026-10-16", thenLengthDays: 30 });
	});

	it("carries no Now length for several owners", () => {
		expect(
			newReportForOwnersOf(draftWith({ thenLength: "30" }), midNovember)
				.thenAndNow,
		).toEqual({ thenEndDate: "2026-10-16", thenLengthDays: 30 });
	});
});

describe("reportChangeOf", () => {
	it("sends the frozen Then length as the Now length, whatever Now the report was read with", () => {
		const report = theEngagementStartReport();
		const draft = thenAndNowDraftOf({
			...report,
			thenAndNow: {
				...report.thenAndNow,
				nowWindow: { start: "2026-08-19", end: "2026-10-02", lengthDays: 45 },
			},
		});

		expect(reportChangeOf(draft, templates).thenAndNow).toEqual({
			nowLengthDays: 90,
		});
	});
});
