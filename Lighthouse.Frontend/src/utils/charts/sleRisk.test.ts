import { describe, expect, test } from "vitest";
import type { ISleRisk } from "../../models/Metrics/SleRisk";
import type { IWorkItem } from "../../models/WorkItem";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "./paceBands";
import {
	buildSleRiskColumnDescriptor,
	sleRiskAtRiskSummary,
	sleRiskColumnDescription,
	sleRiskColumnHeaderName,
	sleRiskSortValue,
} from "./sleRisk";

const zenithItem = (referenceId: string): IWorkItem => ({
	id: Number.parseInt(referenceId.replace("ZEN-", ""), 10),
	name: `Item ${referenceId}`,
	state: "In Progress",
	stateCategory: "Doing",
	type: "User Story",
	referenceId,
	url: `https://example.com/work/${referenceId}`,
	startedDate: new Date("2026-09-01"),
	closedDate: new Date("2026-09-01"),
	cycleTime: 0,
	workItemAge: 9,
	parentWorkItemReference: "",
	isBlocked: false,
});

const answer = (referenceId: string, risk: number | null): ISleRisk => ({
	referenceId,
	risk,
});

const descriptorFor = (answers: ISleRisk[]) =>
	buildSleRiskColumnDescriptor({
		answers,
		headerName: "SLE Risk",
		description: "how likely this one is to miss",
	});

describe("the risk column's wording", () => {
	test("names the target with whatever the team calls it", () => {
		expect(sleRiskColumnHeaderName("Service Level Expectation")).toBe(
			"Service Level Expectation Risk",
		);
	});

	test("says what the number is a share of, in the team's own word for one piece of work", () => {
		expect(sleRiskColumnDescription("Ticket")).toBe(
			"Of every Ticket still open at this age across the team's configured history, the share that went on to miss the target",
		);
	});

	test("names the evidence, so nobody reads the number as following the range picker", () => {
		// The wording is the whole of the disclosure. The evidence is the team's configured history
		// and the answer is about today, so a reader who moves the range and sees nothing change has
		// been told why before they file it as a bug.
		expect(sleRiskColumnDescription("Work Item")).toContain(
			"the team's configured history",
		);
	});
});

describe("building the risk column", () => {
	test("offers nothing at all when no item was answered for", () => {
		// A team with no published target is the only way this arrives empty, and there is then no
		// promise for anything to be at risk of breaking.
		expect(descriptorFor([])).toBeUndefined();
	});

	test("carries the percentage as the column's value, so the export reads as the column does", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-412"))).toBe("86%");
	});

	test("renders a risk of zero as a number, not as a blank", () => {
		// Zero is a real answer: the item is inside its target and the team has finished nothing that
		// ran this long. It is also falsy, so a label written as a truthiness check type-checks and
		// silently blanks every cell on a thin history - which is the whole population this slice
		// stopped hiding behind a sentinel.
		const descriptor = descriptorFor([answer("ZEN-412", 0)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-412"))).toBe("0%");
		expect(descriptor?.riskFor(zenithItem("ZEN-412"))).toBe(0);
	});

	test("says nothing at all about an item the answer never mentioned", () => {
		// An item the endpoint did not list - it entered the board after the answer was computed, or
		// it is no longer in flight. Inventing a risk for it would be worse than an empty cell.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-999"))).toBe("");
	});

	test("leaves an unmentioned item out of the ordering as well", () => {
		// An empty label parses to nothing and sorts to the bottom in both directions, which is where
		// a row making no claim belongs.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-999"))).toBeUndefined();
		expect(sleRiskSortValue("")).toBeUndefined();
	});

	test("orders a zero below every answered item rather than alongside the unmentioned ones", () => {
		// The distinction the empty string has to keep: no claim sorts nowhere, a claim of zero sorts
		// at the bottom of the claims.
		expect(sleRiskSortValue("0%")).toBe(0);
	});

	test("has no risk to order by for an item the answer never mentioned", () => {
		// The lookup misses, and a miss must read as "no answer" rather than throw on the way to
		// drawing the column.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-999"))).toBeUndefined();
	});

	test("keeps the risk itself available for ordering, not only its spelling", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-412"))).toBe(86);
	});
});

describe("painting the risk", () => {
	const paintedBy = (risk: number) =>
		descriptorFor([answer("ZEN-412", 86)])?.colorForRisk(risk);

	test.each([
		[0, 0],
		[24, 0],
		[25, 1],
		[49, 1],
		[50, 2],
		[74, 2],
		[75, 3],
		[99, 3],
		[100, 4],
	])(
		"paints %i in the same colour the chart paints its band %i",
		(risk, band) => {
			expect(paintedBy(risk)).toBe(PACE_BAND_COLORS_LOW_TO_HIGH[band]);
		},
	);

	test("leaves an item with no answer unpainted", () => {
		// Not the calmest colour. An absence of evidence must not be dressed as good news.
		expect(
			descriptorFor([answer("ZEN-412", 86)])?.colorForRisk(undefined),
		).toBeUndefined();
	});
});

describe("reading the number back out of a rendered label", () => {
	test("recovers the risk from the percentage the column carries", () => {
		expect(sleRiskSortValue("86%")).toBe(86);
	});

	test("has no number for the empty label an unmentioned row renders", () => {
		expect(sleRiskSortValue("")).toBeUndefined();
	});

	test("has no number for a label that was never a percentage", () => {
		expect(sleRiskSortValue("not a number")).toBeUndefined();
	});
});

describe("counting what is at risk", () => {
	test("counts the items more likely than not to miss the target", () => {
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 86),
			answer("ZEN-2", 50),
			answer("ZEN-3", 49),
			answer("ZEN-4", 12),
		]);

		// Fifty is the line and sits on the at-risk side of it: more likely than not.
		expect(summary.count).toBe(2);
	});

	test("counts an item that has already outlasted its target", () => {
		// Past the target every item that ever ran this long had already missed, so the number is
		// 100 by definition rather than by evidence - and it is the clearest trouble on the board.
		const summary = sleRiskAtRiskSummary([answer("ZEN-5", 100)]);

		expect(summary.count).toBe(1);
	});

	test("does not count an item whose history gives it a zero", () => {
		// A thin history now answers 0 rather than staying silent. Zero is below the threshold and
		// must be treated as the number it is, not as a missing one.
		const summary = sleRiskAtRiskSummary([answer("ZEN-6", 0)]);

		expect(summary.count).toBe(0);
	});

	test("takes its colour from the worst item it counted", () => {
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 60),
			answer("ZEN-2", 99),
		]);

		expect(summary.color).toBe(PACE_BAND_COLORS_LOW_TO_HIGH[3]);
	});

	test("reads an item past its target as the worst band there is", () => {
		// Certainty is the top of the ladder, and an item already past the target is there.
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 60),
			answer("ZEN-5", 100),
		]);

		expect(summary.color).toBe(
			PACE_BAND_COLORS_LOW_TO_HIGH[PACE_BAND_COLORS_LOW_TO_HIGH.length - 1],
		);
	});

	test("has no colour to give when it counted nothing", () => {
		const summary = sleRiskAtRiskSummary([answer("ZEN-3", 12)]);

		expect(summary.count).toBe(0);
		expect(summary.color).toBeUndefined();
	});

	test("counts nothing for a team that published no target", () => {
		// An empty answer is how that arrives, and it is the same shape as having no items.
		expect(sleRiskAtRiskSummary([]).count).toBe(0);
	});
});
