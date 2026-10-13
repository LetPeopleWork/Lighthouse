import { describe, expect, it } from "vitest";
import type { IReportTemplates } from "../../../../models/Reports/Report";
import { reportTypes, reportTypeTitleOf } from "./reportTypes";
import {
	newThenAndNowDraft,
	type ThenAndNowDraft,
} from "./thenAndNow/thenAndNowDraft";

const templates: IReportTemplates = {
	today: "2026-10-02",
	earliestStart: null,
	minimumLengthDays: 14,
	templates: [
		{ templateKey: "then-and-now", metrics: [{ key: "cycle-time" }] },
	],
};

const thenAndNow = reportTypes["then-and-now"];

const [thenStep, nowStep, metricsAndNameStep] = thenAndNow.steps;

const draftWith = (changes: Partial<ThenAndNowDraft>): ThenAndNowDraft => ({
	...newThenAndNowDraft(),
	...changes,
});

describe("the Then & Now report type", () => {
	it("adds the Then, Now and Metrics & name steps, in that order", () => {
		expect(thenAndNow.steps.map((step) => step.label)).toEqual([
			"Then",
			"Now",
			"Metrics & name",
		]);
	});

	it("completes Then once it names a day and an accepted number of days", () => {
		expect(thenStep.isComplete(draftWith({}), templates)).toBe(true);
	});

	it.each([
		["its end is no day", { thenEnd: "not a day" }],
		["its length is no number of days", { thenLength: "" }],
		["the server would refuse it", { thenLength: "13" }],
	])("leaves Then incomplete while %s", (_, changes) => {
		expect(thenStep.isComplete(draftWith(changes), templates)).toBe(false);
	});

	it.each([
		["30", true],
		["0", false],
	])("completes Now of %j days: %j", (nowLength, expected) => {
		expect(nowStep.isComplete(draftWith({ nowLength }), templates)).toBe(
			expected,
		);
	});

	it.each([
		["a name and a metric", {}, true],
		["a name of blanks", { name: "   " }, false],
		["no metric", { shownMetricKeys: [] }, false],
	])("completes Metrics & name with %s: %j", (_, changes, expected) => {
		expect(metricsAndNameStep.isComplete(draftWith(changes), templates)).toBe(
			expected,
		);
	});
});

describe("reportTypeTitleOf", () => {
	it.each([
		["then-and-now", "Then & Now"],
		["a-type-from-a-later-version", undefined],
	])("names %j as %j", (templateKey, expected) => {
		expect(reportTypeTitleOf(templateKey)).toBe(expected);
	});
});
