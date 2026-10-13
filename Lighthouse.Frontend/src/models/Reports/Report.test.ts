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

	it.each(["no-finished-items", "too-few-points", "no-process"])(
		"read a value absent because of %j",
		(absentReason) => {
			expect(
				ReportSchema.parse(aReportWhoseThenIsADash(absentReason)).thenAndNow
					.metrics["cycle-time"]?.atThen.p85?.absentReason,
			).toBe(absentReason);
		},
	);

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
