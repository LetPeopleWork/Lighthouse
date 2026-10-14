import { describe, expect, it } from "vitest";
import {
	ReportListSchema,
	ReportSchema,
	ReportTemplatesSchema,
} from "./Report";

const aListWithOwnerKind = (ownerKind: string) => ({
	reports: [
		{
			id: 42,
			name: "Then & Now — engagement start",
			templateKey: "then-and-now",
			ownerKind,
			ownerId: 3,
			createdOn: "2026-08-01",
		},
	],
	creationBlockedByCap: false,
	communityCap: 2,
});

const aReportWhoseThenIsADash = (absentReason: string) => ({
	id: 42,
	name: "Then & Now — engagement start",
	templateKey: "then-and-now",
	ownerKind: "Team",
	ownerId: 1,
	createdOn: "2026-08-01",
	concurrencyToken: "token-1",
	shownMetricKeys: ["cycle-time"],
	thenAndNow: {
		thenWindow: { start: "2026-05-03", end: "2026-07-31", lengthDays: 90 },
		nowWindow: { start: "2026-07-05", end: "2026-10-02", lengthDays: 90 },
		thenRebuildable: true,
		metrics: {
			"cycle-time": {
				atThen: { p85: { value: null, sampleSize: null, absentReason } },
				atNow: { p85: { value: 12, sampleSize: 41, absentReason: null } },
			},
		},
	},
});

describe("the report schemas", () => {
	it.each([
		["Team", "team"],
		["Portfolio", "portfolio"],
		["team", "team"],
		["portfolio", "portfolio"],
	])("read an owner kind of %j as %j", (sent, read) => {
		expect(
			ReportListSchema.parse(aListWithOwnerKind(sent)).reports[0].ownerKind,
		).toBe(read);
	});

	it("read a value absent because nothing finished", () => {
		expect(
			ReportSchema.parse(aReportWhoseThenIsADash("no-finished-items"))
				.thenAndNow.metrics["cycle-time"]?.atThen.p85?.absentReason,
		).toBe("no-finished-items");
	});

	// A report frozen by a later version may give a reason this one has no words for; the rest of it must still read.
	it.each(["too-few-points", "no-process", "a-reason-from-a-later-version"])(
		"read a value absent because of %j, a reason this version does not know, as absent for an unknown reason",
		(absentReason) => {
			expect(
				ReportSchema.parse(aReportWhoseThenIsADash(absentReason)).thenAndNow
					.metrics["cycle-time"]?.atThen.p85,
			).toEqual({ value: null, sampleSize: null, absentReason: "unknown" });
		},
	);

	// Owned by slice 04 (Story 6162): a report counts nothing beyond Then's limits, so the read keeps no such member.
	it("keep nothing a payload says about Then's limits", () => {
		const sent = aReportWhoseThenIsADash("no-finished-items");
		const withLimits = {
			...sent,
			thenAndNow: {
				...sent.thenAndNow,
				metrics: {
					"cycle-time": {
						...sent.thenAndNow.metrics["cycle-time"],
						beyondThenLimits: {
							above: { value: 2, sampleSize: 41, absentReason: null },
							below: { value: 0, sampleSize: 41, absentReason: null },
						},
					},
				},
			},
		};

		expect(
			ReportSchema.parse(withLimits).thenAndNow.metrics["cycle-time"],
		).not.toHaveProperty("beyondThenLimits");
	});

	it("read a side of daily values as its average, median, min and max", () => {
		const daily = (value: number) => ({
			value,
			sampleSize: 90,
			absentReason: null,
		});
		const sent = aReportWhoseThenIsADash("no-finished-items");
		const withWip = {
			...sent,
			thenAndNow: {
				...sent.thenAndNow,
				metrics: {
					wip: {
						atThen: {
							average: daily(8.62),
							median: daily(9),
							min: daily(6),
							max: daily(12),
						},
						atNow: {
							average: daily(7),
							median: daily(7),
							min: daily(5),
							max: daily(9),
						},
					},
				},
			},
		};

		expect(ReportSchema.parse(withWip).thenAndNow.metrics.wip?.atThen).toEqual({
			average: daily(8.62),
			median: daily(9),
			min: daily(6),
			max: daily(12),
		});
	});

	it("refuses report types whose metric is named by anything but a key", () => {
		expect(
			ReportTemplatesSchema.safeParse({
				today: "2026-10-02",
				earliestStart: null,
				minimumLengthDays: 14,
				templates: [{ templateKey: "then-and-now", metrics: [{ key: 5 }] }],
			}).success,
		).toBe(false);
	});
});
